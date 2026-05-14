# 아키텍처 개요

## 솔루션 구조

```
src/
  ConSight.DONGBO.slnx
  Bi.ConSight.SqlAgent/         — ADO.NET 래퍼 (SqlConnectionFactory, QueryExecution, NonQueryExecution)
  ConSight.DONGBO.DAQ/          — 메인 WPF 앱 (net10.0-windows)
  ConSight.DONGBO.PlcSimulator/ — PLC 시뮬레이터 (featureA, TCP port 5000)
  ConSight.DONGBO.DAQ.Tests/    — xUnit 테스트 (featureA)
docs/
  architecture.md               — 이 파일
  impl-plan.md                  — C2~C7 단계별 구현 사양
```

---

## 4단계 제조 파이프라인

핵심 오케스트레이터: `Sequence/Controller/ControlUnit_DAQ.cs`

```
PLC Signal (short[])
  → Op200Parser → Op200ProcessDto → EmpgRow.From()        [DB INSERT]
  → Op210Parser → Op210ProcessDto → EmpgRow.ApplyOp210()  [DB UPDATE by MatSerial]
  → Op220Parser → Op220ProcessDto → EmpgRow.ApplyOp220()  [DB UPDATE by MatSerial]
  → Op230Parser → Op230ProcessDto → EmpgRow.ApplyOp230()  [DB UPDATE by MatSerial]
  → CSV append + IProcessEventBus.Publish(row)
```

OP200이 메인 공정(INSERT). OP210/220/230은 서브 공정 — 선행된 OP200의 MatSerial01 또는 MatSerial02 키로 조회 후 UPDATE.

---

## featureA 아키텍처 — PLC Simulator 연동

```
┌───────────────────────────────────┐   TCP loopback   ┌──────────────────────────────┐
│ ConSight.DONGBO.PlcSimulator      │ ───────────────► │ ConSight.DONGBO.DAQ          │
│                                   │   port 5000      │                              │
│  PlcMemory (Dictionary + lock)    │ ◄─────────────── │  TcpPlcDriver                │
│  PlcSimulatorServer (TcpListener) │                  │  PlcReadLoop (100ms 폴링)    │
│  SimulatorSignalHandler           │                  │  ControlUnit_DAQ             │
│  MockArrayBuilder                 │                  │  MonitoringView (DataGrid)   │
│  MainWindow (트리거 버튼 4개)      │                  │                              │
└───────────────────────────────────┘                  └──────────────────────────────┘
```

### 신호 핸드셰이크 (1 사이클, OP200 기준)

```
[Sim] OP200 버튼 클릭
  → PlcMemory["D1900"] = setting 배열 (100 words)
  → PlcMemory["D2000"] = proc 배열, proc[0] = 1 (BackUp_Start)

[DAQ] PlcReadLoop 100ms 주기
  → ReadWords("D2000", 100)
  → proc[0] == 1 && 직전 사이클 == 0 (rising edge 감지)
       → ReadWords("D1900", 100)
       → Op200Parser.Parse(proc, setting) → Op200ProcessDto
       → ControlUnit_DAQ.ProcessData_Op200(dto)
            → DB INSERT/UPDATE
            → EventBus.Publish(row)  ──► MonitoringView DataGrid
            → Op200WriteRegion.Set_PC_Complete_Flag(OK)
            → WriteWords("D2001", [0, 1, 0])   ← PC_Complete_Flag = OK
            → TimeTriggerQueue.Enqueue(ReSet, 1000ms)

[Sim] PlcSimulatorServer.ClientLoop 수신
  → PlcMemory.Write("D2001", [0, 1, 0])
  → SimulatorSignalHandler.OnWritten("D2001", data)
       → data[1] == 1 감지
       → PlcMemory["D2000"][0] = 0  (BackUp_Start 리셋)
       → PlcMemory["D2001"][1] = 0  (PC_Complete_Flag 리셋)

[DAQ] 1초 후 RunTimeTriggerLoopAsync
  → Op200WriteRegion.ReSet_PC_Complete_Flag → WriteWords("D2001", [0, 0, 0])
```

### PLC 주소 매핑

| OP | proc 영역 | proc 워드 수 | setting 영역 | setting 워드 수 | write 영역 | write 워드 수 | PC_Complete_Flag 위치 |
|----|-----------|-------------|--------------|-----------------|-----------|---------------|----------------------|
| OP200 | D2000 | 100 | D1900 | 100 | D2001 | 3 | D2001[1] (index 1) |
| OP210 | D2200 | 70  | D1900 | 100 | D2201 | 1 | D2201[0] (index 0) |
| OP220 | D2300 | 70  | D1900 | 100 | D2301 | 1 | D2301[0] (index 0) |
| OP230 | D2400 | 80  | D1800 | 24  | D2401 | 1 | D2401[0] (index 0) |

proc[0] = BackUp_Start (공통). D1900은 OP200/210/220 공유, D1800은 OP230 전용.

### TCP 프로토콜 (자체 바이너리, 빅엔디언)

```
Request  : [op:1B 'R'/'W'] [addrLen:1B] [addr:N B ASCII] [wordCount:2B BE]
           + (op='W') [payload: wordCount×2B BE shorts]

Response : [op:1B] [status:1B  0=OK/1=ERR] [wordCount:2B BE]
           + (op='R' && status=0) [payload: wordCount×2B BE shorts]
```

구현: `Device/PLC/Net/PlcWireProtocol.cs` (DAQ) / `Net/PlcWireProtocol.cs` (Simulator — 복사본, namespace만 변경)

---

## 핵심 패턴

**Immutable DTOs** — `Op200ProcessDto` 등은 `init`-only setter. APD(측정값) + SP(설정 스냅샷) 필드 보유.

**DTO 구조 규칙** — 공정 배열 앞단: `BackUp_Start[0], PC_Complete_Flag[1], Repair[2], Model[10..], Serial[20..]`. 그 뒤에 OP별 APD/SP 필드. `OP200_Process_DTO.cs` 가 원형.

**Parser Strategy** — OP별 파서(`Op200Parser` 등)가 raw `short[]` 2개(proc, setting)를 strongly-typed DTO로 변환. `namespace ConSight.DAQ.Device`. 공통 변환: `PlcParseHelper`, `PlcDataConverter`.

**EmpgRow (Aggregate Root)** — 1개 제조 레코드의 91개 필드 보유. `From(dto)` → OP200 완료 시 생성. `ApplyOp2X0()` → 서브공정 완료 시 갱신. TotalJudge는 각 Apply마다 재계산 — NG 확정 후 OK 복귀 불가.

**OP210–230 Fallback** — 선행 OP200 없이 서브공정 신호 수신 시 `BuildFallback()` 이 NG 팬텀 행 생성 → `InsertFallback()`.

**Write Region Abstraction** — `IPlcWriteRegion` OP별 구현체. `PlcWriteBuffer`(드라이버 + 주소 + 배열)를 주입받아 `Cmd_Write()`로 전송. TimeTrigger 패턴: `Set → Cmd_Write (즉시 ON)` → `Enqueue(ReSet, 1000ms)` → `Dequeue → Cmd_Write (OFF)`.

**Type-Safe EventBus** — `IProcessEventBus`: `event Action<EmpgRow>?` + lock/snapshot. 구독자는 백그라운드 스레드에서 호출 → UI는 `Dispatcher.InvokeAsync()` 필수.

**MonitoringViewModel 행 관리** — 수신 row의 MatSerial01이 기존 Rows에 없으면 `Insert(0)` (OP200 신규), 있으면 해당 행 교체 (OP210/220/230 갱신). 최대 200건 유지.

---

## Directory Map (ConSight.DONGBO.DAQ)

| 경로 | 내용 |
|------|------|
| `AppEvent/` | `IProcessEventBus`, `ProcessEventBus` |
| `Common/` | `Constants_App`, `ObservableRangeCollection` |
| `Compat/` | 레거시 라이브러리 스텁 (MxComponent, LogWriter, ExpException) |
| `Data/DriverDataWrite/` | `TimeTriggerQueue`, `Write_TimeTriggerDataArgs` |
| `Define/` | `eDataBackup_ProcessResult`, `ePLCWriteWord_TimeTrigger` |
| `Device/DB/` | `EmpgRow`, `SSMS_Op200`, `SSMS_SubProcess`, `SSMS_Model`, `EmpgCsvWriter` |
| `Device/DB/EfCore/` | `DongBoDbContext`, `EmpgEntity` |
| `Device/PLC/` | 파서(`Op200Parser`~`Op230Parser`), DTO, `IPlcDriver`, `MockPlcDriver`, `PlcWriteBuffer` |
| `Device/PLC/Net/` | `PlcWireProtocol`, `TcpPlcDriver` (featureA) |
| `Device/PLC/OP200~230/` | `Op200WriteRegion`~`Op230WriteRegion` |
| `Sequence/Controller/` | `ControlUnit_DAQ` |
| `Sequence/` | `PlcReadLoop` (featureA) |
| `Views/01_Monitoring/` | `MonitoringView`, `MonitoringViewModel` (featureA) |
| `Views/03_Inquiry/` | 이력 조회 뷰 (ADO.NET + EF Core) |
| `Views/99_Test/` | `ProcessPipelineTestView` — 수동 파이프라인 테스트 UI |

## Directory Map (ConSight.DONGBO.PlcSimulator)

| 경로 | 내용 |
|------|------|
| `Memory/` | `PlcMemory` — `Dictionary<string, short[]>` + lock + `Written` 이벤트 |
| `Net/` | `PlcWireProtocol` (DAQ 복사본), `PlcSimulatorServer` — TcpListener + 단일 클라이언트 |
| `Logic/` | `SimulatorSignalHandler` — PC_Complete 감지·리셋, `MockArrayBuilder` — proc/setting 배열 생성 |
| `MainWindow.xaml(.cs)` | OP200~230 트리거 버튼, 메모리 스냅샷, 통신 로그 |
| `App.xaml(.cs)` | PlcMemory → PlcSimulatorServer → SimulatorSignalHandler 초기화 |

---

## Composition

DI 컨테이너 없음. 의존성은 `MainWindow.InitViews()` 에서 수동 wire-up.

```
MainWindow.InitViews()
  TcpPlcDriver("localhost", 5000)
  PlcWriteBuffer × 4  →  Op200~230WriteRegion × 4
  ControlUnit_DAQ(connStr, op200~230Write, csvWriter, eventBus)
  PlcReadLoop(driver, controlUnit)
  MonitoringViewModel(eventBus)  →  MonitoringView
  _ = controlUnit.RunTimeTriggerLoopAsync(_cts.Token)
  _ = plcLoop.RunAsync(_cts.Token)
```

`IPlcDriver` 구현체:
- `MockPlcDriver` — 인메모리, Pipeline Test 탭과 Unit 테스트
- `TcpPlcDriver` — PlcSimulator `localhost:5000` 연결 (featureA)

---

## Bi.ConSight.SqlAgent

- `SqlConnectionFactory` — 연결 문자열로 `SqlConnection` 생성
- `QueryExecution` — SELECT → `DataTable`
- `NonQueryExecution` — INSERT/UPDATE/DELETE

---

## DB 인덱스 (필수)

```sql
CREATE CLUSTERED INDEX IX_EMPG_UPDATE_TIME ON EMPG_HIS (UPDATE_TIME);
CREATE CLUSTERED INDEX IX_EMPGHIS_UPDATE_TIME ON EMPG (UPDATE_TIME);
```

클러스터형 인덱스 적용 후 대량 범위 조회 약 25% 성능 개선 (2000ms → 1509ms, 10.5만 건 기준).
