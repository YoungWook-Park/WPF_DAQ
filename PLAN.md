# ConSight DONG_BO DAQ — 리팩토링 & 성능 비교 포트폴리오

## 목적
- 레거시 .NET Framework WPF 프로젝트를 .NET 8로 마이그레이션
- DevExpress 라이센스 의존성 제거 → 순수 WPF
- 블랙박스 DB DLL(`Bi.SqlServerAgent`) 교체 → 재사용 가능한 SqlConnection 라이브러리 자체 작성
- 인덱스 전/후, EF Core 도입 전/후 성능 수치 비교 → 포트폴리오 작성

## 원본 프로젝트
- 경로: `D:\project\ConSight0112_PLC_write`
- 대상: `PJT\DONG_BO\DAQ` 한정
- 원본은 수정하지 않음 (이 워크스페이스에서만 작업)

---

## AS-IS 문제점

| 분류 | 문제 | 영향 |
|---|---|---|
| 런타임 | .NET Framework 4.x | DevExpress 라이센스 만료 시 실행 불가 |
| UI | DevExpress WPF | 라이센스 비용, 제거 불가 구조 |
| DB 접근 | `Bi.SqlServerAgent.dll` 블랙박스 | 내부 코드 파악 불가, 유지보수 불가 |
| SQL | 문자열 연결 쿼리 | SQL 인젝션 취약점 |
| 날짜 필터 | `UPDATE_TIME`이 nvarchar(23)인데 날짜 범위 조회 | 문자열 비교 → 인덱스 효과 없음 |
| PK 설계 | `RESULT_ID` PK가 NONCLUSTERED | Heap 테이블 → 범위 스캔 Full Scan |
| 수치 데이터 타입 | APD/SP 컬럼이 nvarchar(10) | 정렬·비교 비효율 |
| OP100 의존 | OP200 처리 중 OP100 DB 원격 조회 (`SSMS_OP100.GetResultData`) | 불필요한 DB 왕복, 결합도 높음 |
| PLC 결합 | PLC 없이 앱 실행 불가 | 개발·테스트 환경에서 동작 불가 |

---

## TO-BE 목표

| 분류 | 개선 내용 |
|---|---|
| 런타임 | .NET 8 WPF |
| UI | 순수 WPF (`DataGrid`, `DatePicker`) |
| DB 접근 | 자체 작성 `Bi.ConSight.SqlAgent` 라이브러리 (SqlConnection 기반) |
| SQL | 파라미터화 쿼리 (`SqlParameter`) |
| 날짜 필터 | `UPDATE_TIME` → datetime 타입 변경 후 인덱스 활용 |
| 인덱스 | `UPDATE_TIME` 커버링 인덱스 추가 |
| OP100 의존 | `ProcessData_Op200_Data_Update` 내 OP100 DB 조회 블록 제거 |
| PLC | `IPlcDriver` 인터페이스 + Mock 구현 (UI 클릭 → ushort[] 전달) |
| EF Core | Phase 3에서 도입, ADO.NET 대비 성능·코드량 비교 |

---

## 프로젝트 구조

```
C:\project\ConSight_DONGBO_Refactor\
├── PLAN.md                          (이 파일)
├── PROGRESS.md                      (진행 현황)
├── sql\
│   ├── 01_schema_fix.sql            UPDATE_TIME 타입 변경
│   ├── 02_seed_data.sql             테스트 데이터 100만건 INSERT
│   ├── 03_index_add.sql             커버링 인덱스 생성
│   └── 04_benchmark_queries.sql     성능 측정 쿼리 (SET STATISTICS TIME ON)
├── src\
│   └── ConSight.DONGBO.sln
│       ├── Bi.ConSight.SqlAgent\    SqlConnection 재사용 라이브러리
│       ├── ConSight.DONGBO.DAQ\     WPF .NET 8 앱 (원본 구조 유지)
│       └── ConSight.DONGBO.MockPlc\ PLC Mock
└── benchmark\
    ├── phase1_no_index.md           인덱스 없음 측정 결과
    ├── phase2_with_index.md         인덱스 적용 후 측정 결과
    └── phase3_efcore.md             EF Core 도입 후 측정 결과
```

---

## 단계별 작업 계획

### Step 1 — DB 스키마 수정
- [ ] `sql/01_schema_fix.sql` 작성
  - `EMPG.UPDATE_TIME` nvarchar(23) → datetime
  - `EMPG_HIS.UPDATE_TIME` nvarchar(23) → datetime
- [ ] SSMS에서 실행 확인

### Step 2 — SqlConnection 재사용 라이브러리 (`Bi.ConSight.SqlAgent`)
- [ ] 솔루션 및 프로젝트 생성 (Class Library, .NET 8)
- [ ] `SqlConnectionFactory.cs` — 연결 문자열 관리
- [ ] `QueryExecution.cs` — SELECT → DataSet (기존 API 형태 유지, 파라미터 지원 추가)
- [ ] `NonQueryExecution.cs` — INSERT/UPDATE/DELETE
- [ ] 단위 테스트: STS_MODEL_TB 조회로 연결 확인

### Step 3 — WPF .NET 8 앱 프로젝트 구성
- [ ] `ConSight.DONGBO.DAQ` 프로젝트 생성 (.NET 8 WPF)
- [ ] 원본에서 비즈니스 로직 클래스 복사 (Parser_*, Write_*, ControlUnit_DAQ 등)
- [ ] `Bi.ConSightCommon`, `Bi.nsExpException`, `Bi.nsLogWriter` 참조 정리
- [ ] `using DevExpress.Xpf.Charts` 제거
- [ ] `Xamarin.CommunityToolkit` → `CommunityToolkit.Mvvm` 교체

### Step 4 — ControlUnit_DAQ.cs: OP100 DB 조회 제거
- [ ] `ProcessData_Op200_Data_Update()` 내 `SSMS_OP100` 블록 제거
- [ ] `hasSerial_Op100` 분기 제거 → `DataBackUp_ResultSet()` 단순화

### Step 5 — DevExpress 제거: Inquiry_OP200_ResourceLotHistoryView 한 쌍
- [ ] `dxe:DateEdit` → `DatePicker`
- [ ] `dxg:GridControl` + `dxg:TableView` → `DataGrid` + `VirtualizingStackPanel`
- [ ] `dxmvvm:EventToCommand` → `i:InvokeCommandAction`
- [ ] `dx:DXImage` 버튼 → 텍스트/유니코드 아이콘
- [ ] NG 셀 색상: `dxgt:GridRowThemeKey` → `DataGrid.CellStyle` + `DataTrigger`
- [ ] ViewModel: `ObservableRangeCollection` → `ObservableCollection` (CommunityToolkit.Mvvm)
- [ ] ViewModel: `QueryExecution` → 새 `Bi.ConSight.SqlAgent` 라이브러리로 교체
- [ ] ViewModel: 파라미터화 쿼리 적용 (SQL injection 수정)

### Step 6 — PLC Mock
- [ ] `IPlcDriver` 인터페이스 정의
- [ ] `MockPlcDriver` 구현 (ushort[] 반환)
- [ ] UI에 "PLC 시뮬레이션" 버튼 추가
- [ ] 클릭 → `PlcMockDataFactory.CreateOP200Sample()` → 기존 `Write_ProcessData_OP200` 로직 통과

### Step 7 — 테스트 데이터 100만건
- [ ] `sql/02_seed_data.sql` 작성
  - STS_MODEL_TB 마스터 (5~10개 모델)
  - EMPG 50만건 (2023-01-01 ~ 2025-12-31 균등)
  - EMPG_HIS 50만건 (동일 기간)
- [ ] SSMS에서 실행

### Step 8 — 성능 측정 A: 인덱스 없음
- [ ] `sql/04_benchmark_queries.sql` 작성
  - Q1: 기간 1일 조회
  - Q2: 기간 1개월 + MODEL 필터
  - Q3: 기간 + TOTAL_JUDGE 필터
- [ ] Stopwatch 측정 (앱 레벨)
- [ ] SET STATISTICS TIME ON 측정 (SQL 레벨)
- [ ] 실행계획 캡처 (Table Scan 확인)
- [ ] `benchmark/phase1_no_index.md` 결과 기록

### Step 9 — 성능 측정 B: 인덱스 적용
- [ ] `sql/03_index_add.sql` 작성 및 실행
  ```sql
  CREATE NONCLUSTERED INDEX IX_EMPG_UPDATE_TIME
  ON EMPG (UPDATE_TIME)
  INCLUDE (TOTAL_JUDGE, MODEL, MAT_SERIAL01, MAT_SERIAL02, RESULT_ID);
  ```
- [ ] 동일 Q1/Q2/Q3 재측정
- [ ] 실행계획 캡처 (Index Seek 확인)
- [ ] `benchmark/phase2_with_index.md` 결과 기록

### Step 10 — 성능 측정 C: EF Core 도입
- [ ] `Microsoft.EntityFrameworkCore.SqlServer` 9.x 추가
- [ ] `DongBoDbContext` 작성
- [ ] `EmpgEntity` 매핑 (projection으로 필요한 컬럼만 SELECT)
- [ ] `AsNoTracking()` 적용
- [ ] 동일 쿼리 LINQ로 작성, 생성 SQL 확인
- [ ] 동일 Q1/Q2/Q3 재측정
- [ ] `benchmark/phase3_efcore.md` 결과 기록

---

## 성능 비교 기록 양식

| 쿼리 | Phase A (인덱스 없음) | Phase B (인덱스) | Phase C (EF Core) |
|---|---|---|---|
| Q1: 기간 1일 | ?ms | ?ms | ?ms |
| Q2: 기간 1개월 + MODEL | ?ms | ?ms | ?ms |
| Q3: 기간 + TOTAL_JUDGE | ?ms | ?ms | ?ms |

| 항목 | AS-IS | TO-BE |
|---|---|---|
| 런타임 | .NET Framework 4.x | .NET 8 |
| UI | DevExpress WPF | 순수 WPF |
| DB 접근 | 블랙박스 DLL | 자체 SqlAgent → EF Core |
| SQL 보안 | 문자열 연결 (SQL injection) | 파라미터화 쿼리 |
| PLC 의존 | 하드코딩 직접 호출 | IPlcDriver Mock 가능 |
| 조회 코드량 | ~80줄 (문자열 연결) | ~15줄 (LINQ) |
| Q2 쿼리 성능 | ?ms | ?ms (목표: 90%+ 감소) |

---

## 참고: 원본 핵심 파일 위치

| 파일 | 원본 경로 |
|---|---|
| 이력조회 View | `PJT\DONG_BO\DAQ\Views\03_Inquiry\Inquiry_OP200_ResourceLotHistoryView.xaml` |
| 이력조회 ViewModel | `PJT\DONG_BO\DAQ\Views\03_Inquiry\Inquiry_OP200_ResourceLotHistoryViewModel.cs` |
| ControlUnit (OP100 제거 대상) | `PJT\DONG_BO\DAQ\Sequence\Controller\ControlUnit_DAQ.cs` |
| DB 설정 | `Setting_DataBase` → `LocalDB_ConnectionString` |
| Write_OP200 | `PJT\DONG_BO\DAQ\Data\DriverDataWrite\Write_ProcessData_OP200.cs` |
