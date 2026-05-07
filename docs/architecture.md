# 아키텍처 개요

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

## 핵심 패턴

**Immutable DTOs** — `Op200ProcessDto` 등은 `init`-only setter. 44개 측정 필드(APD01–44) + 50개 설정 스냅샷 필드(SP01–50).

**Parser Strategy** — OP별 파서 클래스(`Op200Parser` 등)가 raw `short[]`를 strongly-typed DTO로 변환. 오프셋은 주석에 기재(D2000, D2010 등). 공통 변환: `PlcParseHelper` (F2, F2Int, F4Int, Judge, Serial).

PLC setting 영역:
- **D1900** — OP200/210/220 공유 (SP01–36)
- **D1800** — OP230 전용 (SP37–50)

**EmpgRow (Aggregate Root)** — 1개 제조 레코드의 91개 필드 보유. 생명주기: `From(dto)` → OP200 완료 시 생성, `ApplyOp2X0()` → 서브 공정 완료 시 갱신. `TotalJudge`는 각 Apply마다 재계산 — NG 확정 후 OK로 복귀 불가.

**OP210–230 Fallback** — 선행 OP200 없이 서브 공정 신호 수신 시 `BuildFallback()`이 NG 팬텀 행을 생성하고 `InsertFallback()` 호출 → 제조 이상 상황에서의 데이터 손실 방지.

**Write Region Abstraction** — `IPlcWriteRegion` OP별 구현체. 4개를 `_allRegions[]`에 수집해 `RunTimeTriggerLoopAsync()` 내 단일 `foreach`로 처리. TimeTrigger 패턴: 펄스 Enqueue → 1000ms 대기 → 리셋 Dequeue (PLC 핸드셰이크 프로토콜).

**Type-Safe EventBus** — `IProcessEventBus`는 `event Action<EmpgRow>?` + lock/snapshot 패턴으로 스레드 안전. 구독자는 백그라운드 스레드에서 호출 → UI 갱신 시 `Dispatcher.InvokeAsync()` 필수.

---

## Directory Map (ConSight.DONGBO.DAQ)

| 경로 | 내용 |
|------|------|
| `AppEvent/` | `IProcessEventBus`, `ProcessEventBus` |
| `Common/` | `Constants_App`, `ObservableRangeCollection` |
| `Compat/` | 레거시 라이브러리 스텁 (MxComponent, LogWriter, ExpException) |
| `Data/DriverDataRead/` | 레거시 파서 스텁 (`Device/PLC/` 파서로 대체됨) |
| `Data/DriverDataWrite/` | `TimeTriggerQueue`, `Write_TimeTriggerDataArgs` |
| `Define/` | 결과 타입 및 PLC write 워드 enum |
| `Device/DB/` | `EmpgRow`, `SSMS_Op200`, `SSMS_SubProcess`, `SSMS_Model`, `EmpgCsvWriter` |
| `Device/DB/EfCore/` | `DongBoDbContext`, `EmpgEntity`, `EmpgHisEntity`; `HasBaseType(null)`으로 EF Core TPH 비활성화 |
| `Device/PLC/` | 파서, DTO, `IPlcDriver`, `IPlcWriteRegion`, `PlcWriteBuffer` |
| `Device/PLC/Net/` | `PlcWireProtocol` (encode/decode), `TcpPlcDriver` — TCP 기반 `IPlcDriver` (featureA) |
| `Device/PLC/OP200–230/` | OP별 write region 구현체 |
| `Sequence/Controller/` | `ControlUnit_DAQ` — 메인 파이프라인 오케스트레이터 |
| `Sequence/` | `PlcReadLoop` — 100ms 폴링 루프, BackUp_Start edge detection (featureA) |
| `Views/01_Monitoring/` | `MonitoringView` + `MonitoringViewModel` — EventBus 구독 실시간 DataGrid (featureA) |
| `Views/03_Inquiry/` | 이력 조회 뷰 — ADO.NET(legacy)과 EF Core(`_EfCore` suffix) 이중 구현 |
| `Views/99_Test/` | `ProcessPipelineTestView` — 수동 파이프라인 테스트 UI |

---

## Composition

DI 컨테이너 없음. 모든 의존성은 `MainWindow.InitViews()` 와 `ProcessPipelineTestView` 생성자에서 수동 wire-up.

`IPlcDriver` 구현체:
- `MockPlcDriver` — 인메모리, Pipeline Test 탭과 단위 테스트에서 사용
- `TcpPlcDriver` — `PlcSimulator` `localhost:5000` 에 연결 (featureA)

`ControlUnit_DAQ.RunTimeTriggerLoopAsync()` 과 `PlcReadLoop.RunAsync()` 는 self-starting 아님 — `MainWindow` 에서 백그라운드 Task로 직접 시작해야 함.

---

## Bi.ConSight.SqlAgent

sibling 클래스 라이브러리 (`src/Bi.ConSight.SqlAgent/`):
- `SqlConnectionFactory` — 연결 문자열로 `SqlConnection` 생성
- `QueryExecution` — SELECT 쿼리 실행, `DataTable` 반환
- `NonQueryExecution` — INSERT/UPDATE/DELETE 실행

Inquiry 뷰는 ADO.NET 경로에 이 라이브러리 사용. EF Core 뷰는 `DongBoDbContext` 직접 사용.

---

## DB 인덱스 (필수)

EMPG 테이블에 커버링 인덱스 필요 (없으면 500K+ 행 기준 ~1ms → 295–330ms로 성능 저하):

```sql
CREATE NONCLUSTERED INDEX IX_EMPG_UPDATE_TIME
ON EMPG (UPDATE_TIME)
INCLUDE (TOTAL_JUDGE, MODEL, MAT_SERIAL01, MAT_SERIAL02, RESULT_ID);
```
