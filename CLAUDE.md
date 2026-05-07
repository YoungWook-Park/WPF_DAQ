# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 따라야 할 지침을 제공합니다.

## 빌드 및 실행

```powershell
dotnet build src/ConSight.DONGBO.slnx
dotnet run --project src/ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj
dotnet run --project src/ConSight.DONGBO.PlcSimulator/ConSight.DONGBO.PlcSimulator.csproj  # featureA, 별도 터미널
```

런타임: SQL Server Express `Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True`

## 테스트

```powershell
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit         # DB 불필요
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Integration  # SQLEXPRESS 필요
```

인앱 테스트: **Pipeline Test** 탭 → Parser/Full Pipeline 버튼 (소스: `Views/99_Test/ProcessPipelineTestView.xaml.cs`)

## 솔루션 구조

```
src/
  ConSight.DONGBO.slnx
  Bi.ConSight.SqlAgent/         # ADO.NET 래퍼 (SqlConnectionFactory, QueryExecution, NonQueryExecution)
  ConSight.DONGBO.DAQ/          # 메인 WPF 앱 (.NET 10-windows)
  ConSight.DONGBO.PlcSimulator/ # PLC 시뮬레이터 (featureA, TCP port 5000)
  ConSight.DONGBO.DAQ.Tests/    # xUnit 테스트 (featureA)
docs/
  architecture.md               # 아키텍처 상세 (패턴, Directory Map, Composition)
  featureA-work-plan.md         # featureA 설계서 (PLC 주소, TCP 프로토콜, 작업 분해)
```

## 아키텍처

상세 내용: `docs/architecture.md`

핵심 불변조건:
- DI 컨테이너 없음 — `ControlUnit_DAQ` 등 모든 의존성은 `MainWindow.InitViews()` 에서 수동 wire-up
- `RunTimeTriggerLoopAsync()` 와 `PlcReadLoop.RunAsync()` 는 self-starting 아님 — `MainWindow` 에서 직접 Task 시작
- EventBus 구독자는 백그라운드 스레드에서 호출됨 → UI 갱신 시 `Dispatcher.InvokeAsync()` 필수
- mock `short[]` 빌더와 파서 오프셋은 항상 동기화 유지 (`ProcessPipelineTestView.xaml.cs` ↔ 각 `OpXXXParser`)

## 작업 관리

| 커맨드 | 설명 |
|--------|------|
| `/devlog` | 오늘 작업일지 작성 + `devlog/YYYY-MM-DD.md` 생성 + git commit & push |
| `/today` | 작업 시작 전 목표 설정 — git log 확인 후 오늘 할 일 정리 |
| `/testgen` | 마지막 커밋 변경 파일 → xUnit 테스트 작성 → 실행 → 결과 보고 |
| `/review` | 마지막 커밋 구현 요약 + 코드 품질 검토 |
