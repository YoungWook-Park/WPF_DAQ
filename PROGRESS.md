# 진행 현황

## 현재 단계: Step 7 대기 중

| Step | 내용 | 상태 |
|---|---|---|
| Step 1 | DB 스키마 수정 (UPDATE_TIME 타입 변경) | ✅ 완료 (SSMS 적용) |
| Step 2 | SqlConnection 재사용 라이브러리 작성 | ✅ 완료 |
| Step 3 | WPF .NET 10 앱 프로젝트 구성 | ✅ 완료 |
| Step 4 | ControlUnit_DAQ OP100 DB 조회 제거 | ✅ 완료 |
| Step 5 | DevExpress 제거 (View + ViewModel) | ✅ 완료 |
| Step 6 | PLC Mock 구현 | ✅ 완료 |
| Step 7 | 테스트 데이터 100만건 INSERT | ⬜ 대기 |
| Step 8 | 성능 측정 A: 인덱스 없음 | ⬜ 대기 |
| Step 9 | 성능 측정 B: 인덱스 적용 후 | ⬜ 대기 |
| Step 10 | 성능 측정 C: EF Core 도입 | ⬜ 대기 |

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
  - DevExpress 제거: dxe:DateEdit→DatePicker, dxg:GridControl→DataGrid (VirtualizingPanel.Recycling)
  - SQL Injection 수정: WHERE 절 문자열 연결 → @sDate/@eDate/@model 파라미터화
  - UPDATE_TIME Step1 타입 변경 반영: (string)→(DateTime)cast
  - ObservableRangeCollection 제거, MainCore 의존 제거 (connectionString DI)
  - DateTime? 프로퍼티로 DatePicker null 처리 안전화
