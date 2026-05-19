# DAQ 아키텍처 — PLC 구현·시퀀스 구조 파악 가이드

## 1. 전체 레이어 지도

```
┌──────────────────────────────────────────────────────────────────┐
│  MainCore.cs  (싱글톤 진입점 — 수명 관리 & Wire-up)              │
└──────────────────────────────────────────────────────────────────┘
         │ Initialize()에서 수동 생성·주입
         ▼
┌─────────────────────┐    ┌──────────────────────────────────────┐
│   PlcReadLoop.cs    │    │       ControlUnit_DAQ.cs             │
│  (100ms 폴링 루프)  │───▶│  (파이프라인 오케스트레이터)         │
└─────────────────────┘    └──────────────────────────────────────┘
         │ IPlcDriver                        │ DB / EventBus
         ▼                                   ▼
┌─────────────────────┐    ┌──────────┐  ┌───────────────────────┐
│   TcpPlcDriver.cs   │    │ SSMS_*.cs│  │   ProcessEventBus.cs  │
│  (TCP localhost:    │    │  (ADO.NET│  │  (Subject<EmpgRow>)   │
│   5000)             │    │   접근)  │  └───────────────────────┘
└─────────────────────┘    └──────────┘
```

---

## 2. 코드 읽기 순서 (처음 파악할 때)

| 순서  | 파일                                       | 파악할 것                                     |
| --- | ---------------------------------------- | ----------------------------------------- |
| 1   | `Device/PLC/IPlcDriver.cs`               | PLC 계약 — `ReadWords` / `WriteWords`       |
| 2   | `Device/PLC/PlcWriteBuffer.cs`           | short[] 래퍼, `Cmd_Write()`                 |
| 3   | `Device/PLC/PlcWordRegionBase.cs`        | `SetWord` / `SetBit` 추상화                  |
| 4   | `Device/PLC/OP230/Op230WriteRegion.cs`   | WriteRegion 구현 패턴 (가장 단순)                 |
| 5   | `Device/PLC/Op230ProcessDto.cs`          | DTO 필드 구조 패턴                              |
| 6   | `Device/PLC/Op230Parser.cs`              | `short[]` → DTO 변환 패턴                     |
| 7   | `Device/DB/EmpgRow.cs`                   | 도메인 집합체 — `From()` / `ApplyXxx()`         |
| 8   | `Sequence/PlcReadLoop.cs`                | `OpMeta` 테이블 — 공정 등록 방법                   |
| 9   | `Sequence/Controller/ControlUnit_DAQ.cs` | `ProcessData_Op230()` 패턴 + TimeTrigger 흐름 |
| 10  | `MainCore.cs`                            | 전체 wire-up                                |

---

## 3. 데이터 흐름 (OP230 예시)

```
① PlcReadLoop  100ms마다 D2400 읽기
               proc[0] == 1 && prev == 0  → rising edge 감지
               D1800 (설정 24워드) 읽기

② Op230Parser  short[] proc, short[] setting
               → Op230ProcessDto { Serial01, Serial02, Apd34~44, Sp37~50 }

③ ControlUnit  FindBySerial(Serial01)
               → found  : row.ApplyOp230(dto) → SSMS.UpdateSubCols(row)
               → missing: BuildFallback()     → ApplyOp230() → InsertFallback()
               _csvWriter.Append(row)
               _eventBus.Publish(row)          ← MonitoringViewModel 등 구독자에 전달

④ DataBackUp   Op230WriteRegion.Set_PC_Complete_Flag(OK/NG)
               Cmd_Write()  → D2401 즉시 ON
               EnqueueTimeTrigger(ReSet, 1000ms)

⑤ TimeTrigger  10ms 루프에서 1초 후 Dequeue
               ReSet_PC_Complete_Flag() → Cmd_Write() → D2401 OFF (펄스 완료)
```

---

## 4. OpMeta 테이블 (`PlcReadLoop.cs:22`)

각 공정이 `_ops[]` 배열에 한 줄로 등록된다.

```csharp
new("D2000", 100, "D1900", 100,
    (p, s) => new Op200Parser().Parse(p, s),
    dto    => controlUnit.ProcessData_Op200((Op200ProcessDto)dto)),
```

| 인수 | 의미 |
|------|------|
| `"D2000"` | 공정 데이터 PLC 주소 |
| `100` | 읽을 word 수 |
| `"D1900"` | 설정 데이터 PLC 주소 |
| `100` | 설정 word 수 |
| `Parse` | `short[] → object(DTO)` 변환 람다 |
| `Process` | `object(DTO) → void` 처리 람다 |

rising edge는 `proc[0] == 1 && _prevProc0[i] == 0` 조건으로 감지한다.  
감지 후 `_prevProc0[i] = 0` 으로 강제 리셋 — 처리 중 새 신호가 와도 재진입하지 않는다.

---

## 5. WriteRegion 구조

```
PlcWordRegionBase  (SetWord / SetBit / Cmd_Write)
    └── Op2x0WriteRegion  (IPlcWriteRegion 구현)
            ├── Set_PC_Complete_Flag(result)   ← 결과 즉시 ON
            ├── ReSet_PC_Complete_Flag()        ← 1초 후 OFF
            ├── EnqueueTimeTrigger(args)
            └── DequeueTimeTrigger()
```

OP200만 `PC_Response` 비트 필드(3 word)를 가지고, OP210~230은 `PC_Complete_Flag` 1 word만 사용한다.

---

## 6. EmpgRow — 필드 소유 공정

| 필드 범위 | 소유 공정 | 채워지는 시점 |
|-----------|-----------|---------------|
| 식별자, Apd01~26, Sp01~30 | OP200 | `From()` 또는 `ApplyOp200()` |
| Apd27~30, Sp31~36 | OP210 | `ApplyOp210()` |
| Apd31~33, Sp31~36 | OP220 | `ApplyOp220()` (Sp31~36 공유) |
| Apd34~44, Sp37~50 | OP230 | `ApplyOp230()` |

`TotalJudge` 재계산 규칙:  
기존 NG → NG 유지 / 새 판정 중 비-OK → NG / 그 외 → OK

---

## 7. OP240 추가 체크리스트

공정 하나를 추가하면 건드려야 하는 파일은 **정확히 8곳**이다.  
OP230을 참조 패턴으로 사용하면 된다.

### 7-1. 새로 만드는 파일 (3개)

#### `Device/PLC/OP240/Op240WriteRegion.cs`
```
Op230WriteRegion.cs 복사 후:
- namespace → ConSight.DAQ.Device.PLC.OP240
- PlcWriteBuffer 주소: "D2501" (PLC 사양 확인)
- 필요시 word 수 조정
```

#### `Device/PLC/Op240ProcessDto.cs`
```
Op230ProcessDto.cs 복사 후:
- 클래스명 → Op240ProcessDto
- 공정 측정 필드 Apd45~ 추가
- 설정 필드 Sp51~ 추가
- IEnumerable<string> Judges → 판정 필드명 목록
```

#### `Device/PLC/Op240Parser.cs`
```
Op230Parser.cs 복사 후:
- 클래스명 → Op240Parser
- 반환 타입 → Op240ProcessDto
- PLC 주소 오프셋을 실제 D2500 레이아웃에 맞게 수정
```

### 7-2. 기존 파일 수정 (5곳)

#### `Device/DB/EmpgRow.cs`
```
1. Apd45~N 필드 추가  (// APD45~ : OP240 블록)
2. Sp51~M  필드 추가  (// SP51~  : OP240 설정 스냅샷)
3. ApplyOp240(Op240ProcessDto dto) 메서드 추가
   - UpdateTime, Apd, Sp 필드 병합
   - RecalcTotalJudge(dto.Judges) 호출
```

#### `Sequence/PlcReadLoop.cs` (1줄 추가)
```csharp
// _ops[] 배열 끝에 추가
new("D2500",  80, "D????",  ??,        // ← PLC 사양 확인
    (p, s) => new Op240Parser().Parse(p, s),
    dto    => controlUnit.ProcessData_Op240((Op240ProcessDto)dto)),
```

`_prevProc0` 배열은 `_ops.Length` 기반으로 자동 확장된다 — 별도 수정 불필요.

#### `Sequence/Controller/ControlUnit_DAQ.cs`
```
1. 필드:  private readonly Op240WriteRegion _op240Write;
2. 생성자 파라미터: Op240WriteRegion op240Write 추가
3. 생성자 본문:
     _op240Write = op240Write;
     _allRegions = [op200Write, op210Write, op220Write, op230Write, op240Write];
                                                                    ↑ 추가
4. ProcessData_Op240() 메서드 추가
   - Op230 패턴 그대로: FindBySerial → ApplyOp240 → UpdateSubCols / InsertFallback
   - DataBackUp_ResultSet(_op240Write) 호출
```

#### `MainCore.cs` (`Initialize()` 내부)
```csharp
var buf240 = new PlcWriteBuffer(PlcDriver, "D2501", 1);   // ← 주소·크기 확인

ControlUnit = new ControlUnit_DAQ(
    ConnectionString,
    new Op200WriteRegion(buf200),
    new Op210WriteRegion(buf210),
    new Op220WriteRegion(buf220),
    new Op230WriteRegion(buf230),
    new Op240WriteRegion(buf240),   // ← 추가
    csvWriter,
    EventBus);
```

#### DB 스키마 (EMPG 테이블)
```sql
-- Apd45~, Sp51~ 컬럼 추가 마이그레이션
ALTER TABLE EMPG ADD Apd45 NVARCHAR(20), Apd46 NVARCHAR(20), ...
```
`SSMS_SubProcess.cs` / `SSMS_Op200.cs`의 INSERT·UPDATE SQL도 신규 컬럼을 포함해야 한다.

### 7-3. 선택적 수정

| 파일 | 이유 |
|------|------|
| `Views/99_Test/ProcessPipelineTestView.xaml.cs` | OP240 파서·파이프라인 수동 테스트 버튼 추가 |
| `Views/99_Test/ProcessPipelineTestView.xaml` | 테스트 UI 버튼 배치 |
| `Device/DB/EfCore/EmpgEntity.cs` | EF Core 엔티티에 신규 컬럼 프로퍼티 추가 |

---

## 8. 파일 위치 빠른 참조

```
src/ConSight.DONGBO.DAQ/
├── MainCore.cs                          ← wire-up 진입점
├── Sequence/
│   ├── PlcReadLoop.cs                   ← OpMeta 등록
│   └── Controller/ControlUnit_DAQ.cs    ← ProcessData_OpXxx + TimeTrigger
├── Device/
│   ├── PLC/
│   │   ├── IPlcDriver.cs
│   │   ├── PlcWriteBuffer.cs
│   │   ├── PlcWordRegionBase.cs
│   │   ├── PlcParseHelper.cs            ← F2 / Judge / Serial 변환
│   │   ├── Op{N}Parser.cs               ← short[] → DTO
│   │   ├── Op{N}ProcessDto.cs           ← 공정별 DTO
│   │   └── OP{N}/Op{N}WriteRegion.cs    ← PC Write 피드백
│   └── DB/
│       ├── EmpgRow.cs                   ← 도메인 집합체
│       ├── SSMS_Op200.cs                ← OP200 DB 접근
│       └── SSMS_SubProcess.cs           ← OP210~230 DB 접근
└── AppEvent/
    └── ProcessEventBus.cs               ← Subject<EmpgRow>
```
