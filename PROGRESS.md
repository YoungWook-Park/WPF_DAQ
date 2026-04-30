# 진행 현황

## 현재 단계: Phase G + 빌드픽스 + 테스트 뷰 완료 ✅

| Step | 내용 | 상태 |
|---|---|---|
| Step 1 | DB 스키마 수정 (UPDATE_TIME nvarchar→datetime) | ✅ 완료 (sqlcmd 적용) |
| Step 2 | SqlConnection 재사용 라이브러리 작성 | ✅ 완료 |
| Step 3 | WPF .NET 10 앱 프로젝트 구성 | ✅ 완료 |
| Step 4 | ControlUnit_DAQ OP100 DB 조회 제거 | ✅ 완료 |
| Step 5 | DevExpress 제거 (View + ViewModel) | ✅ 완료 |
| Step 6 | PLC Mock 구현 | ✅ 완료 |
| Step 7 | 테스트 데이터 INSERT (EMPG 500,022건 / EMPG_HIS 500,000건) | ✅ 완료 (sqlcmd set-based) |
| Step 8 | 성능 측정 A: 인덱스 없음 | ✅ 완료 (phase1_no_index.md) |
| Step 9 | 성능 측정 B: 인덱스 적용 후 | ✅ 완료 (phase2_with_index.md) |
| Step 10 | 성능 측정 C: EF Core 도입 + 측정 | ✅ 완료 (phase3_efcore.md) |
| Phase A~F | ControlUnit_DAQ 전면 리팩토링 (타입 DTO, 파서 분리, IPlcWriteRegion) | ✅ 완료 |
| Phase G | IProcessEventBus 도입 (NormValueDictionary 대체, 타입 안전 이벤트) | ✅ 완료 |
| 빌드픽스 | Op200WriteRegion internal→public, LogWriter.WriteWarning, MxComp 중복 | ✅ 완료 |
| 테스트 뷰 | ProcessPipelineTestView (파서/전체 파이프라인 테스트 탭) | ✅ 완료 |

## 변경 이력
- 2026-04-28: 워크스페이스 생성, PLAN.md 작성
- 2026-04-28: Step 1 완료 — SSMS에서 UPDATE_TIME nvarchar→datetime 적용
- 2026-04-28: Step 2 완료 — Bi.ConSight.SqlAgent (net10.0 classlib, Microsoft.Data.SqlClient 7.0.1)
  - SqlConnectionFactory / QueryExecution / NonQueryExecution 구현
  - 원본 API(AppendQuery/Execute/QueryCollection) 호환 + AddParameter() 추가
- 2026-04-28: Step 3 완료 — ConSight.DONGBO.DAQ 프로젝트 net10.0-windows 구성
- 2026-04-28: Step 4 완료 — ControlUnit_DAQ.ProcessData_Op200_Data_Update() OP100 원격 DB 조회 블록 제거
- 2026-04-28: Step 5 완료 — Inquiry_OP200_ResourceLotHistory View/ViewModel/CodeBehind 재작성
- 2026-04-28: Step 6 완료 — PLC Mock + 추상화 레이어 구현
- 2026-04-29: Step 7~9 완료 — sqlcmd set-based INSERT(50만건×2), Phase A/B 측정
  - EMPG_HIS 테이블 신규 생성 (SELECT TOP 0 * INTO + PK)
  - set-based INSERT (sys.all_objects CROSS JOIN, ~60초 소요)
  - Phase A: Full Scan 295~330ms / 50,004 logical reads
  - Phase B: Index Seek ~0~5ms / 13~261 logical reads (최대 99.7% 개선)
- 2026-04-29: Step 10 완료 — EF Core 독립 구현 + BenchmarkRunner 측정
  - EmpgEntity / EmpgHisEntity (동일 스키마, 테이블명만 상속으로 구분)
  - DongBoDbContext (EMPG + EMPG_HIS DbSet, Migration 미사용)
  - Inquiry_OP200_ResourceLotHistoryViewModel_EfCore.cs (async/await, AsNoTracking, Select projection)
  - LastQueryElapsedMs 프로퍼티로 앱 레벨 Stopwatch 측정 내장
  - Microsoft.EntityFrameworkCore.SqlServer 9.0.4 패키지 추가
  - 빌드 결과: 경고 0, 오류 0
  - DevExpress 제거: dxe:DateEdit→DatePicker, dxg:GridControl→DataGrid (VirtualizingPanel.Recycling)
  - SQL Injection 수정: WHERE 절 문자열 연결 → @sDate/@eDate/@model 파라미터화
  - UPDATE_TIME Step1 타입 변경 반영: (string)→(DateTime)cast
  - ObservableRangeCollection 제거, MainCore 의존 제거 (connectionString DI)
  - DateTime? 프로퍼티로 DatePicker null 처리 안전화
- 2026-04-30: Phase A~F 완료 — ControlUnit_DAQ 전면 리팩토링
  - Phase A: Op200/210/220/230ProcessDto (불변 init-setter DTO)
  - Phase B: Op200/210/220/230Parser (PLC short[] → 타입 DTO 파서)
  - Phase C: EmpgRow (From/ApplyOp210~230/RecalcTotalJudge), SSMS_Op200 파라미터화
  - Phase D: SSMS_SubProcess (FindBySerial/UpdateSubCols/InsertFallback), EmpgCsvWriter (CsvHelper ClassMap)
  - Phase E: IPlcWriteRegion + Op210/220/230WriteRegion (1-word 버퍼, TimeTrigger 큐)
  - Phase F: ControlUnit_DAQ 재작성 (4-OP 파이프라인, IPlcWriteRegion 일반화, BuildFallback)
- 2026-04-30: Phase G 완료 — IProcessEventBus (타입 안전 이벤트 버스)
  - AppEvent/IProcessEventBus.cs: Publish/Subscribe/Unsubscribe 인터페이스
  - AppEvent/ProcessEventBus.cs: event Action<EmpgRow>? + lock 스냅샷 패턴 (스레드 안전)
  - ControlUnit_DAQ: IProcessEventBus DI, ProcessData_Opxxx() 완료 후 Publish()
  - MainWindow: ProcessEventBus 구독 → Dispatcher.InvokeAsync로 TxStatus 실시간 갱신
- 2026-04-30: 빌드픽스 (0 오류 0 경고)
  - Op200WriteRegion.DequeueTimeTrigger(): internal → public (IPlcWriteRegion 구현 요건)
  - LogWriterCompat: WriteWarning() 메서드 추가
  - PlcParseHelper: using Bi.ConSight_MxComponent 제거 (MxComp_DB_JUDGE_CODE 중복 참조 해소)
- 2026-04-30: ProcessPipelineTestView 추가 (Views/99_Test/)
  - ① 파서 테스트: mock short[] → Parser → DTO 전 필드 출력 (DB 불필요)
  - ② 전체 파이프라인: MockPlcDriver + ControlUnit_DAQ → DB/CSV/EventBus + PLC 쓰기 로그
  - MainWindow에 "Pipeline Test" 탭으로 통합
