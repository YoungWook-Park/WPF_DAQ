# CLAUDE.md

## 개발 환경

- .NET 10 · Windows 전용 (`net10.0-windows`, WPF)
- SQL Server Express: `Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True`
- PLC Simulator: 별도 프로세스, TCP `localhost:5000`

## 자주 사용하는 명령어

```powershell
dotnet build src/ConSight.DONGBO.slnx
dotnet run --project src/ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj
dotnet run --project src/ConSight.DONGBO.PlcSimulator/ConSight.DONGBO.PlcSimulator.csproj
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Integration
```

## 핵심 파일 & 유틸리티

| 파일 | 역할 |
|------|------|
| `Sequence/Controller/ControlUnit_DAQ.cs` | OP200~230 파이프라인 오케스트레이터 |
| `Device/PLC/Net/TcpPlcDriver.cs` | TCP `IPlcDriver` 구현체 |
| `Device/PLC/IPlcDriver.cs` | PLC 드라이버 계약 |
| `AppEvent/ProcessEventBus.cs` | `EmpgRow` 발행/구독 버스 |
| `Device/DB/EmpgRow.cs` | 제조 레코드 aggregate root (91 fields) |
| `Views/99_Test/ProcessPipelineTestView.xaml.cs:349` | Mock `short[]` 빌더 원본 — `MockArrayBuilder` 복제 시 동기화 유지 |

유틸리티 함수:
- `PlcParseHelper.F2/F2Int/F4Int/Judge/Serial/Repair()` — PLC short[] → 도메인 값 변환
- `PlcDataConverter.ShortToString(arr, offset, maxWords)` — PLC ASCII 디코딩 (Compat 어셈블리)

## 코드 스타일

**Namespace**: DAQ 프로젝트 신규 파일은 `ConSight.DAQ.*` 유지. Simulator 프로젝트는 `ConSight.DONGBO.PlcSimulator.*`  
**접근 제한**: 어셈블리 내부 전용은 `internal`. 테스트 접근 필요 시 `[assembly: InternalsVisibleTo("ConSight.DONGBO.DAQ.Tests")]` (AssemblyInfo.cs)  
**클래스**: 상속 없는 구현체는 `sealed class`  
**DTO**: `init`-only setter. `IEnumerable<string> Judges` 패턴으로 TotalJudge 재계산 대상 필드 선언  
**공유 상태**: `object _lock` + `lock` 블록으로 직렬화  
**주석**: 신규 파일은 Phase 블록 주석 없이. Why가 자명하지 않은 경우만 인라인 주석

## 테스트 지침

```csharp
[Trait("Category", "Unit")]        // DB·TCP 불필요, MockPlcDriver 또는 PlcMemory 직접 사용
[Trait("Category", "Integration")] // SQLEXPRESS 필요. 미가동 시 Assert.Skip() 으로 건너뜀
```

각 커밋 전 `dotnet test --filter Category=Unit` 통과 필수. Integration은 SQLEXPRESS 환경에서만 실행.

## 저장소 에티켓

**커밋 형식**: `feat(C2): 설명` (C2~C7 범위 명시). 버그 수정은 `fix: 설명`, 일지는 `devlog: YYYY-MM-DD`  
**커밋 전 체크**: `dotnet build src/ConSight.DONGBO.slnx` 성공 확인  
**브랜치**: `feature/featureA` — C2~C7 커밋을 순서대로 쌓음

## 핵심 불변조건

- DI 컨테이너 없음 — 모든 의존성은 `MainWindow.InitViews()` 에서 수동 wire-up
- `RunTimeTriggerLoopAsync()` 와 `PlcReadLoop.RunAsync()` 는 self-starting 아님 — `MainWindow` 에서 `_ = method(_cts.Token)` 으로 시작
- EventBus 구독자는 백그라운드 스레드에서 호출됨 → UI 갱신 시 `Dispatcher.InvokeAsync()` 필수
- Mock `short[]` 빌더(MockArrayBuilder)와 파서 오프셋 항상 동기화 유지

상세 아키텍처: `docs/architecture.md` | C2~C7 구현 계획: `docs/impl-plan.md`

## 작업 관리

| 커맨드 | 설명 |
|--------|------|
| `/devlog` | 오늘 작업일지 작성 + `devlog/YYYY-MM-DD.md` 생성 + git commit & push |
| `/today` | 작업 시작 전 목표 설정 — git log 확인 후 오늘 할 일 정리 |
| `/testgen` | 마지막 커밋 변경 파일 → xUnit 테스트 작성 → 실행 → 결과 보고 |
| `/review` | 마지막 커밋 구현 요약 + 코드 품질 검토 |
