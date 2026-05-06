# WPF DAQ — ConSight DAQ 리팩토링 포트폴리오

> **목적**: 3,800줄 단일 `ControlUnit_DAQ` 클래스를 타입 안전하고 테스트 가능한 아키텍처로 분해한 리팩토링 기록

---

## 프로젝트 구조

```
WPF_DAQ/
├── src/
│   ├── Bi.ConSight.SqlAgent/          # SqlConnection 재사용 라이브러리 (net10.0)
│   └── ConSight.DONGBO.DAQ/           # WPF 앱 (net10.0-windows)
│       ├── AppEvent/                  # Phase G: IProcessEventBus (타입 안전 이벤트 버스)
│       ├── Compat/                    # 외부 라이브러리 스텁 (MxComponent, LogWriter, ExpException)
│       ├── Data/                      # TimeTriggerQueue, Write_TimeTriggerDataArgs
│       ├── Define/                    # 열거형 (eDataBackup_ProcessResult, ePLCWriteWord_TimeTrigger)
│       ├── Device/
│       │   ├── DB/                    # EmpgRow, SSMS_Op200, SSMS_SubProcess, EmpgCsvWriter
│       │   │   └── EfCore/            # EmpgEntity, DongBoDbContext (EF Core)
│       │   └── PLC/                   # IPlcDriver, MockPlcDriver, PlcWriteBuffer, PlcWordRegionBase
│       │       ├── OP200/             # Op200WriteRegion (3-word), Op200Parser, Op200ProcessDto
│       │       ├── OP210/             # Op210WriteRegion (1-word), Op210Parser, Op210ProcessDto
│       │       ├── OP220/             # Op220WriteRegion (1-word), Op220Parser, Op220ProcessDto
│       │       └── OP230/             # Op230WriteRegion (1-word), Op230Parser, Op230ProcessDto
│       ├── Sequence/Controller/       # ControlUnit_DAQ (공정 파이프라인 오케스트레이터)
│       └── Views/
│           ├── 03_Inquiry/            # Inquiry_OP200_ResourceLotHistory (ADO.NET + EF Core)
│           └── 99_Test/               # ProcessPipelineTestView (파이프라인 수동 테스트)
├── sql/                               # 테스트 데이터 SQL, 아카이브 스크립트
├── benchmark/                         # 성능 측정 결과 (Phase 1~3)
├── PLAN.md                            # 작업 계획
└── PROGRESS.md                        # 작업 이력
```

---

## 리팩토링 단계 요약

### Phase A — 타입 DTO 도입

**AS-IS**: `OP200_Process_DTO` (object[] 박싱, 컴파일 타임 타입 검사 없음)
**TO-BE**: `Op200ProcessDto` / `Op210ProcessDto` / `Op220ProcessDto` / `Op230ProcessDto`

- `init` setter로 불변 보장
- APD01~44, SP01~50 전 필드 명시적 타입 정의

---

### Phase B — 파서 분리

**AS-IS**: `Parser_ProcessData_Op200` (static 메서드, 600줄 단일 클래스)
**TO-BE**: `Op200Parser` / `Op210Parser` / `Op220Parser` / `Op230Parser`

```
PLC short[] proc + short[] setting
    └─ OpXxxParser.Parse()
           └─ OpXxxProcessDto (불변)
```

- `PlcParseHelper`: F2/F2Int/F4Int/Judge/Repair/Serial 공용 변환 헬퍼
- `PlcDataConverter`: ShortToString, shortToInt (MxComponent 스텁)

---

### Phase C — EmpgRow 도메인 객체

**AS-IS**: `OP200_Process_DTO` 를 직접 DB 파라미터로 전달 (SQL 문자열 연결)
**TO-BE**: `EmpgRow` → 단일 도메인 객체, 파라미터화 쿼리

```csharp
// OP200 완료 시
var row = EmpgRow.From(dto);
new SSMS_Op200(connectionString).Insert(row);   // 파라미터화 INSERT

// OP210~230 완료 시
row.ApplyOp210(dto);   // 필드 병합 + RecalcTotalJudge()
ssms.UpdateSubCols(row);
```

---

### Phase D — 서브공정 DB 레이어

| 클래스 | 역할 |
|--------|------|
| `SSMS_SubProcess` | FindBySerial / UpdateSubCols / InsertFallback |
| `EmpgCsvWriter` | CsvHelper + `EmpgRowMap : ClassMap<EmpgRow>` |

---

### Phase E — IPlcWriteRegion 추상화

**AS-IS**: `Write_ProcessData_OP200` (static Int16[] WriteDataList, 전역 상태)
**TO-BE**: `PlcWordRegionBase` 상속 + `IPlcWriteRegion` 구현

```
IPlcWriteRegion
├── Op200WriteRegion  (3 words: PC_Response | PC_Complete_Flag | PC_Power_On)
├── Op210WriteRegion  (1 word:  PC_Complete_Flag)
├── Op220WriteRegion  (1 word:  PC_Complete_Flag)
└── Op230WriteRegion  (1 word:  PC_Complete_Flag)
```

TimeTrigger 펄스 흐름:
```
Set(OK/NG) + Cmd_Write (즉시 ON)
    → EnqueueTimeTrigger(ReSet, 1000ms)
        → [1초 후] RunTimeTriggerLoopAsync → ReSet + Cmd_Write (OFF)
```

---

### Phase F — ControlUnit_DAQ 재조립

전체 파이프라인 (OP200 예):

```
Op200Parser.Parse(proc, setting)
    └─ Op200ProcessDto
           └─ EmpgRow.From(dto)
                  ├─ SSMS_Model.GetByModel() → Insert/Update
                  ├─ SSMS_Op200.Insert(row)
                  ├─ EmpgCsvWriter.Append(row)
                  ├─ IProcessEventBus.Publish(row)
                  └─ DataBackUp_ResultSet(op200Write) → PLC 펄스
```

OP210~230: `SSMS_SubProcess.FindBySerial()` → found: `ApplyOpXxx + UpdateSubCols` / missing: `BuildFallback + InsertFallback`

---

### Phase G — IProcessEventBus (타입 안전 이벤트 버스)

**AS-IS**: `NormValueDictionary["lastRow"] = row` (object 박싱, string 키 오타 위험)
**TO-BE**: `IProcessEventBus.Publish(EmpgRow)` (타입 안전, 박싱 없음)

```csharp
// 구현체 — event + lock 스냅샷 패턴
public sealed class ProcessEventBus : IProcessEventBus
{
    private event Action<EmpgRow>? _handlers;
    private readonly object _lock = new();

    public void Publish(EmpgRow row)
    {
        Action<EmpgRow>? snapshot;
        lock (_lock) { snapshot = _handlers; }
        snapshot?.Invoke(row);   // 락 밖에서 호출 → 데드락 방지
    }
}

// MainWindow 구독 — UI 스레드 마샬링
_eventBus.Subscribe(row =>
    Dispatcher.InvokeAsync(() =>
        TxStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {row.Model}  {row.TotalJudge}"));
```

---

### 테스트 뷰 — ProcessPipelineTestView

`Views/99_Test/ProcessPipelineTestView.xaml`을 **Pipeline Test** 탭으로 접근 가능.

| 버튼 | 동작 |
|------|------|
| OP200~230 파서 | mock `short[]` → Parser → DTO 전 필드 출력 (DB 불필요) |
| OP200~230 파이프라인 | Parser → ControlUnit_DAQ → DB INSERT/UPDATE + MockPLC 쓰기 로그 |

---

## 성능 벤치마크 결과

| 단계 | 조건 | 응답시간 | Logical Reads |
|------|------|----------|---------------|
| Phase 1 | 인덱스 없음 (Full Scan) | 295~330ms | 50,004 |
| Phase 2 | Index Seek (MAT_SERIAL01) | 0~5ms | 13~261 |
| Phase 3 | EF Core (AsNoTracking + 투영) | 1~3ms | 13~261 |

> 인덱스 추가만으로 **최대 99.7% 응답시간 단축**

---

## 빌드 환경

- .NET 10.0 (net10.0-windows WPF)
- SQL Server Express (`.\\SQLEXPRESS`, `DB_eM`)
- 패키지: `CsvHelper 33`, `Microsoft.EntityFrameworkCore.SqlServer 9.0.4`, `CommunityToolkit.Mvvm 8.4.2`

```bash
cd src
dotnet build ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj
# → 경고 0개, 오류 0개
```
