# featureA 작업 설계서: UI 모니터링 + PLC 시뮬레이터 + 신호 핸드셰이크 통합

## Context

현재 `ControlUnit_DAQ.ProcessData_OpXXX(dto)` 파이프라인은 완성되어 있으나,
**PLC → PC 방향의 polling 루프가 없다.**
`ProcessPipelineTestView` 가 mock 배열을 직접 만들어 `ProcessData_Op200(dto)` 를 호출할 뿐,
실제 설비처럼 BackUp_Start 펄스 신호로 트리거되지 않는다.

목적은 두 가지:

1. **비즈니스 로직 검증** — 실제 설비처럼 펄스 신호 핸드셰이크를 거쳐 `ControlUnit_DAQ` 가 동작하는지를 시뮬레이터로 재현하고 xUnit 으로 검증한다.
2. **운영 모니터링 UI** — `IProcessEventBus.Publish(EmpgRow)` 를 구독해 실시간 DataGrid 로 누적 표시한다 (현재는 단순 텍스트 한 줄만 갱신).

---

## 아키텍처 개요

```
┌─────────────────────────────────────────┐      TCP loopback       ┌────────────────────────────┐
│ ConSight.DONGBO.PlcSimulator (신규 WPF) │  ───────────────────►   │ ConSight.DONGBO.DAQ (기존) │
│                                         │   port 5000             │                            │
│  • 가상 PLC 메모리 (Dictionary)         │  ◄───────────────────   │  • TcpPlcDriver (신규)     │
│  • TCP 서버 (PlcSimulatorServer)        │                         │  • PlcReadLoop (신규)      │
│  • UI: OP200/210/220/230 트리거 버튼    │                         │  • ControlUnit_DAQ (기존)  │
│    → 메모리에 proc/setting 채우고       │                         │  • ProcessEventBus (기존)  │
│      BackUp_Start 비트 = 1              │                         │  • MonitoringView (신규)   │
│  • PC_Complete_Flag = 1 감지 시         │                         │                            │
│    BackUp_Start / PC_Complete_Flag 리셋 │                         │                            │
└─────────────────────────────────────────┘                         └────────────────────────────┘
```

### 신호 핸드셰이크 (1 사이클)

```
[Sim] 사용자가 OP200 버튼 클릭
  └─ 가상 메모리[D2000][0..99] = proc 배열, [D1900][0..99] = setting
  └─ 가상 메모리[D2000][0] = 1  (BackUp_Start)

[DAQ] PlcReadLoop (100ms 주기)
  └─ ReadWords("D2000", 100)
  └─ proc[0] == 1 이고 직전 사이클엔 0 이었나? (edge detection)
       └─ Yes → ReadWords("D1900", 100)
                Op200Parser.Parse(proc, setting) → DTO
                ControlUnit_DAQ.ProcessData_Op200(dto)
                  └─ DB INSERT/UPDATE
                  └─ EventBus.Publish(row)  ──► [DAQ Monitoring DataGrid]
                  └─ Op200WriteRegion.Set_PC_Complete_Flag(OK)
                  └─ Cmd_Write → WriteWords("D2001", [.., 1, ..])
                  └─ TimeTriggerQueue Enqueue (1초 후 0 리셋)

[Sim] WriteWords("D2001", ...) 수신
  └─ 메모리 갱신
  └─ PC_Complete_Flag(D2002) == 1 감지
       → 가상 메모리[D2000][0] = 0  (BackUp_Start 리셋)
       → 가상 메모리[D2002] = 0     (PC_Complete_Flag 리셋)

[DAQ] 1초 후 RunTimeTriggerLoopAsync
  └─ Op200WriteRegion.ReSet_PC_Complete_Flag → Cmd_Write
       (이미 시뮬레이터가 리셋했지만 약속된 1초 펄스를 끝까지 보낸다)
```

### PLC 주소 매핑

모든 OP 공통: `proc[0] = BackUp_Start`, `proc[1] = PC_Complete_Flag mirror`

| OP    | proc 영역 | proc 워드 수 | setting 영역 | setting 워드 수 | write 영역 | write 워드 수 |
|-------|-----------|-------------|--------------|-----------------|-----------|---------------|
| OP200 | D2000     | 100         | D1900        | 100             | D2001     | 3             |
| OP210 | D2200     | 70          | D1900 공유   | 100             | D2201     | 1             |
| OP220 | D2300     | 70          | D1900 공유   | 100             | D2301     | 1             |
| OP230 | D2400     | 80          | D1800        | 24              | D2401     | 1             |

> **주의**: OP200 의 `Op200WriteRegion` 은 D2001 시작이고 인덱스 1번(D2002)이 PC_Complete_Flag.
> OP210/220/230 은 write 영역 1워드(D2201/D2301/D2401)가 곧 PC_Complete_Flag.

### TCP 프로토콜 (자체 바이너리, 빅엔디언)

```
Request  : [op:1B 'R'/'W'] [addrLen:1B] [addr:N B ASCII] [wordCount:2B BE]
           + (op == 'W') [payload: wordCount * 2B BE shorts]

Response : [op:1B] [status:1B  0=OK / 1=ERR] [wordCount:2B BE]
           + (op == 'R' && status == 0) [payload: wordCount * 2B BE shorts]
```

---

## 구현 현황

| 커밋 | 범위 | 상태 |
|------|------|------|
| C1 | PlcWireProtocol + TcpPlcDriver | ✅ 완료 |
| C2 | PlcSimulator 인프라 (csproj + PlcMemory + PlcSimulatorServer) | 예정 |
| C3 | PlcSimulator 로직+UI (SignalHandler + MockArrayBuilder + MainWindow) | 예정 |
| C4 | PlcReadLoop + DAQ MainWindow.xaml.cs 수정 | 예정 |
| C5 | MonitoringView + ViewModel + MainWindow.xaml 수정 | 예정 |
| C6 | xUnit 프로젝트 + Unit 테스트 | 예정 |
| C7 | Integration 테스트 + 빌드 정리 | 예정 |

---

## 결정 사항

| 항목 | 결정 |
|------|------|
| 시뮬레이터 형태 | 별도 WPF 프로젝트 `src/ConSight.DONGBO.PlcSimulator/` |
| TCP 프로토콜 | 자체 단순 바이너리 (위 정의) |
| 모니터링 위치 | MainWindow TabControl 의 첫 번째 탭으로 신규 추가 (기존 3개 탭 유지) |
| 테스트 프레임워크 | xUnit, `src/ConSight.DONGBO.DAQ.Tests/` |
| Pipeline Test 탭 | 유지 (mock 직접 호출 단위 테스트 가치) |
| DAQ 폴링 주기 | 100ms |
| DataGrid 행 관리 | OP200 신호 → 새 행 `Insert(0)`. OP210/220/230 → MatSerial01(없으면 MatSerial02) 기준 기존 행 교체. 최대 200건 유지 (초과 시 끝 제거). |
| PlcReadLoop 호출 방식 | `ProcessData_OpXXX` 동기 blocking 호출. edge detection이 중복 트리거를 막고, 순서 보장이 중요하므로 단순 직렬 실행 채택. |
| TCP 연결 실패 처리 | 연속 실패 5회 이상 시 5초 backoff 대기 후 재연결 시도. 실패마다 로그 1회만 출력(중복 억제). |

---

## 알려진 설계 이슈

C1 코드 리뷰에서 발견된 항목. 수용 가능 수준이면 현행 유지, 개선 필요 시 해당 커밋에서 처리.

| # | 대상 | 이슈 | 조치 |
|---|------|------|------|
| 1 | `TcpPlcDriver` | `IDisposable` 미구현 — 소켓이 앱 종료 시 명시적으로 닫히지 않음 | T3.3에서 `MainWindow.Window_Closed`에 `CloseConnection()` 호출 추가로 해결 |
| 2 | `TryReadResponse` | `op='W'` 응답의 `wordCount ≠ 0` 여도 `true` 반환 (방어 체크 없음) | 로컬 시뮬레이터 전제이므로 현행 유지. 실 PLC 연결 시 추가 검증 필요 |
| 3 | `TcpPlcDriver` | backoff 진입 후 `_consecutiveFailures` 계속 증가 (리셋 안 됨) | 성공 시 `ResetFailures()`가 0으로 리셋하므로 동작은 정확. 현행 유지 |
| 4 | `EnsureConnected` | `TcpClient.Connect()` 동기 블로킹 — 타임아웃(기본 수십 초) 동안 폴링 루프 멈춤 | 로컬 loopback 전제이므로 현행 유지. 실 PLC 환경 전환 시 `ConnectAsync` + timeout 적용 필요 |

---

## 작업 분해

### T1. TCP 통신 인프라

| 파일 | 내용 |
|------|------|
| `src/ConSight.DONGBO.DAQ/Device/PLC/Net/PlcWireProtocol.cs` | Encode/Decode 정적 메서드. Simulator 측에는 **파일 복사** (`src/ConSight.DONGBO.PlcSimulator/Net/PlcWireProtocol.cs`). 소스 링크 대신 복사 채택 — namespace 충돌 없고 유지보수 단순. 프로토콜 변경 시 두 파일 동기화 필요. |
| `src/ConSight.DONGBO.DAQ/Device/PLC/Net/TcpPlcDriver.cs` | `IPlcDriver` 구현. `TcpClient` 1개 lazy 연결, `lock` 으로 ReadWords/WriteWords 직렬화. 연속 실패 5회 시 `_backoffUntil = DateTime.UtcNow + 5s` 설정 → 이후 호출에서 backoff 미만이면 즉시 `false` 반환 (폴링 루프에 CPU 낭비 없음). backoff 해제 후 재연결 시도. |

### T2. PLC 시뮬레이터 프로젝트

| 파일 | 내용 |
|------|------|
| `src/ConSight.DONGBO.PlcSimulator/PlcSimulator.csproj` | net10.0-windows, UseWPF, CommunityToolkit.Mvvm. |
| `Memory/PlcMemory.cs` | `Dictionary<string, short[]>` + lock. 쓰기 시 `event Action<string, short[]> Written` 발생. |
| `Net/PlcSimulatorServer.cs` | `TcpListener` 5000 포트. accept loop + 클라이언트 read loop. 신규 클라이언트 accept 시 기존 클라이언트 명시적 close (단일 클라이언트 유지). |
| `Logic/SimulatorSignalHandler.cs` | `PlcMemory.Written` 구독. PC_Complete_Flag 감지 규칙: OP200 → D2001에 WriteWords 수신, **payload[1] == 1** (D2002=PC_Complete_Flag); OP210 → D2201 payload[0]==1; OP220 → D2301 payload[0]==1; OP230 → D2401 payload[0]==1. 감지 시 해당 OP의 BackUp_Start(proc[0]) 및 PC_Complete_Flag 0으로 리셋. |
| `Logic/MockArrayBuilder.cs` | 기존 `ProcessPipelineTestView.xaml.cs:349-537` 빌더 로직 복제 (6개 정적 메서드). |
| `MainWindow.xaml / .cs` | OP200/210/220/230 트리거 버튼 + 메모리 스냅샷 + 통신 로그. |
| `App.xaml / .cs` | PlcMemory·PlcSimulatorServer·SimulatorSignalHandler 초기화 후 MainWindow 표시. |

### T3. DAQ 측 Read 폴링 루프

| 파일 | 내용 |
|------|------|
| `src/ConSight.DONGBO.DAQ/Sequence/PlcReadLoop.cs` (신규) | 4개 OP 메타 배열 보유. 100ms 주기로 각 OP proc[0] read. 0→1 edge 감지 시 setting read → 파서 → `ControlUnit_DAQ.ProcessData_OpXXX(dto)` **동기 blocking 호출** (DB 작업 포함). 호출 중 루프는 멈추지만 BackUp_Start=1이 유지되므로 edge detection으로 중복 트리거 없음. TcpPlcDriver.ReadWords 실패 시 이번 사이클 skip (backoff 중이면 즉시 pass). |
| `src/ConSight.DONGBO.DAQ/MainWindow.xaml.cs` (수정) | `TcpPlcDriver` + 4개 WriteRegion + `ControlUnit_DAQ` + `PlcReadLoop` 구성. `RunTimeTriggerLoopAsync` 와 `PlcReadLoop.RunAsync` 두 백그라운드 Task 시작. `Window_Closed` 에서 CancellationToken 취소. |

> `ControlUnit_DAQ.cs` 는 변경 없음. `ProcessData_OpXXX` 는 `internal` 유지.
> 테스트 프로젝트에서 `internal` 접근을 위해 DAQ 프로젝트에 `[assembly: InternalsVisibleTo("ConSight.DONGBO.DAQ.Tests")]` 추가.

### T4. 모니터링 DataGrid 화면

| 파일 | 내용 |
|------|------|
| `src/ConSight.DONGBO.DAQ/Views/01_Monitoring/MonitoringView.xaml` | DataGrid. 컬럼: UpdateTime, Model, MatSerial01/02, TotalJudge, Apd07/15/24/26(OP200 판정), Apd28/30(RunOut), Apd31/33(Guiding), Apd42/44(SOC). NG 행 빨간 RowStyle. |
| `Views/01_Monitoring/MonitoringViewModel.cs` | `ObservableCollection<EmpgRow>` 보유 (최대 200건). `IProcessEventBus.Subscribe`. 수신 row를 `Dispatcher.InvokeAsync` 내에서 처리: OP200(MatSerial01이 Rows에 없음) → `Insert(0, row)`, 200건 초과 시 `RemoveAt(Count-1)`. OP210/220/230 → `Rows.FirstOrDefault(r => r.MatSerial01 == row.MatSerial01 || r.MatSerial02 == row.MatSerial01)` 로 기존 행 탐색 후 교체(`Rows[idx] = row`). 공정 로직 대응: OP200=INSERT, OP210/220/230=UPDATE 관계를 DataGrid도 동일하게 반영. |
| `MainWindow.xaml` (수정) | TabControl 첫 자식에 Monitoring 탭 추가. |

### T5. xUnit 통합 테스트

| 파일 | 분류 | 내용 |
|------|------|------|
| `src/ConSight.DONGBO.DAQ.Tests/ConSight.DONGBO.DAQ.Tests.csproj` | — | **net10.0-windows UseWPF** (DAQ 프로젝트가 WPF이므로 동일 TFM 필수). xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk. |
| `Helpers/InMemoryHandshakeFixture.cs` | — | PlcMemory + PlcSimulatorServer(랜덤 포트) + TcpPlcDriver + PlcReadLoop 인프라 픽스처. |
| `WireProtocolTests.cs` | Unit | Encode/Decode round-trip, 잘못된 opcode 처리. |
| `HandshakeTests.cs` | Unit | ReadWords 빈 메모리 → 0 배열. WriteWords D2001[PC_Complete_Flag=1] → BackUp_Start 자동 리셋. 0일 때는 유지. |
| `ReadLoopTests.cs` | Unit | BackUp_Start 0→1 edge 한 번만 트리거. 1 유지 시 재트리거 없음. |
| `Op200PipelineTests.cs` | Integration | Simulator 트리거 → DB INSERT. TotalJudge NG 전파. SQLEXPRESS 없으면 Skip. |

### T6. 정리

- 솔루션 빌드 확인: `dotnet build src/ConSight.DONGBO.slnx`
- `ProcessPipelineTestView` 유지, MockArrayBuilder 복제 사실 주석 명시.
- 수동 end-to-end 검증 (아래 참조).

---

## 커밋 전략

| 커밋 | 포함 범위 | 신규/수정 파일 수 |
|------|-----------|-----------------|
| C1 | T1 — PlcWireProtocol + TcpPlcDriver | 2 신규 |
| C2 | T2(인프라) — csproj + PlcMemory + PlcSimulatorServer + slnx 수정 | 3~4 신규 + 1 수정 |
| C3 | T2(로직+UI) — SimulatorSignalHandler + MockArrayBuilder + MainWindow + App | 4~5 신규 |
| C4 | T3 — PlcReadLoop + MainWindow.xaml.cs 수정 | 1 신규 + 1 수정 |
| C5 | T4 — MonitoringView + ViewModel + MainWindow.xaml 수정 | 3 신규 + 2 수정 |
| C6 | T5(Unit) — 테스트 프로젝트 + WireProtocol/Handshake/ReadLoop 테스트 | 4 신규 |
| C7 | T5(Integration) + T6 — Op200PipelineTests + 정리 | 1 신규 |

각 커밋 전 `dotnet build` 성공 확인 필수.

---

## 재사용 자산

| 파일 | 용도 |
|------|------|
| `Device/PLC/IPlcDriver.cs` | `TcpPlcDriver` 가 새 구현체로 추가 |
| `Device/PLC/PlcWriteBuffer.cs` | 드라이버만 교체, 코드 변경 없음 |
| `Device/PLC/OP*/Op*WriteRegion.cs` | 변경 없음 |
| `Sequence/Controller/ControlUnit_DAQ.cs` | 변경 없음 (PlcReadLoop 가 호출) |
| `AppEvent/ProcessEventBus.cs` | 변경 없음 |
| `Common/ObservableRangeCollection.cs` | MonitoringViewModel 에서 사용 |
| `Views/99_Test/ProcessPipelineTestView.xaml.cs:349-537` | MockArrayBuilder 로 복제 |

---

## 검증 방법

### 빌드

```powershell
dotnet build src/ConSight.DONGBO.slnx
```

### 단위 테스트 (DB 불필요)

```powershell
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit
```

### 통합 테스트 (SQLEXPRESS 필요)

```powershell
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Integration
```

### 수동 end-to-end

1. SQLEXPRESS 가동 + `DB_eM` + `IX_EMPG_UPDATE_TIME` 인덱스 존재 (`CLAUDE.md` 참조).
2. 시뮬레이터 실행: `dotnet run --project src/ConSight.DONGBO.PlcSimulator` (5000 포트 listen).
3. DAQ 실행: `dotnet run --project src/ConSight.DONGBO.DAQ` (loopback 연결).
4. DAQ Monitoring 탭 확인.
5. 시뮬레이터 OP200 버튼 → Monitoring DataGrid 1행 추가 (TotalJudge=OK). DB EMPG INSERT 확인.
6. 시뮬레이터 OP210 버튼 → **동일 Serial 행 갱신** (새 행 추가 아님). DB EMPG UPDATE 확인.
7. 시뮬레이터 OP220 버튼 → 동일 Serial 행 갱신.
8. 시뮬레이터 OP230 버튼 → 동일 Serial 행 갱신.
9. 시뮬레이터 종료 후 DAQ 단독 실행 → ReadWords 실패 5회 후 5초 backoff 로그 확인 (CPU 과부하 없음).
