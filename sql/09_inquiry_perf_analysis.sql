-- ============================================================
-- 09: 이력 조회 성능 분석 — Key Lookup 원인 규명 + 인덱스 전략 비교
--
-- 분석 대상 쿼리 (C# Inquiry_OP200_ResourceLotHistoryViewModel.QueryEmpg):
--   SELECT [APD01~44, SP01~50 등 ~100개 컬럼]
--   FROM EMPG
--   WHERE UPDATE_TIME BETWEEN @sDate AND @eDate [AND MODEL = @model]
--   ORDER BY UPDATE_TIME
--
-- 문제 진단:
--   IX_EMPG_UPDATE_TIME INCLUDE = 7개 컬럼
--   SELECT 요구 컬럼   = ~100개
--   → 매 행마다 Key Lookup 발생 → Logical Read 폭발
--
-- ser1/ser2 인덱스는 FindBySerial(단건) 전용 — 이 조회와 무관
-- ============================================================
USE [DB_eM]
GO
SET NOCOUNT ON;
GO

-- ============================================================
-- Step 0: 현재 인덱스 현황 확인
-- ============================================================
SELECT
    t.name                               AS [테이블],
    i.name                               AS [인덱스명],
    i.type_desc                          AS [유형],
    STRING_AGG(CASE WHEN ic.is_included_column = 0 THEN c.name ELSE NULL END, ', ')
        WITHIN GROUP (ORDER BY ic.key_ordinal)   AS [Seek Key 컬럼],
    STRING_AGG(CASE WHEN ic.is_included_column = 1 THEN c.name ELSE NULL END, ', ')
        WITHIN GROUP (ORDER BY ic.index_column_id) AS [INCLUDE 컬럼],
    i.fill_factor
FROM sys.indexes i
JOIN sys.tables       t  ON i.object_id = t.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns      c  ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
GROUP BY t.name, i.name, i.type_desc, i.fill_factor
ORDER BY t.name, i.name;
GO

-- ============================================================
-- Step 1: 실행계획 + 통계 측정용 변수 설정 (SSMS 에서 실행)
--
-- 실행 전 체크:
--   쿼리 메뉴 → "실제 실행 계획 포함" ON  (Ctrl+M)
--   출력 탭에서 Key Lookup 노드와 Logical reads 확인
-- ============================================================
SET STATISTICS TIME ON;
SET STATISTICS IO ON;
GO

-- ============================================================
-- Phase A-1: 현재 쿼리 패턴 (MODEL 없음, 전체 컬럼 SELECT)
--            → Key Lookup 과부하 재현
-- ============================================================
PRINT N'=== Phase A-1: UPDATE_TIME 필터, 전체 컬럼, MODEL 없음 ===';
DECLARE @sDate1 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate1 DATETIME = GETDATE();

SELECT
    UPDATE_TIME, REPAIR, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE,
    ISNULL(APD01,'') APD01, ISNULL(APD02,'') APD02, ISNULL(APD03,'') APD03,
    ISNULL(APD04,'') APD04, ISNULL(APD05,'') APD05, ISNULL(APD06,'') APD06,
    ISNULL(APD07,'') APD07, ISNULL(APD08,'') APD08, ISNULL(APD09,'') APD09,
    ISNULL(APD10,'') APD10, ISNULL(APD11,'') APD11, ISNULL(APD12,'') APD12,
    ISNULL(APD13,'') APD13, ISNULL(APD14,'') APD14, ISNULL(APD15,'') APD15,
    ISNULL(APD16,'') APD16, ISNULL(APD17,'') APD17, ISNULL(APD18,'') APD18,
    ISNULL(APD19,'') APD19, ISNULL(APD20,'') APD20, ISNULL(APD21,'') APD21,
    ISNULL(APD22,'') APD22, ISNULL(APD23,'') APD23, ISNULL(APD24,'') APD24,
    ISNULL(APD25,'') APD25, ISNULL(APD26,'') APD26, ISNULL(APD27,'') APD27,
    ISNULL(APD28,'') APD28, ISNULL(APD29,'') APD29, ISNULL(APD30,'') APD30,
    ISNULL(APD31,'') APD31, ISNULL(APD32,'') APD32, ISNULL(APD33,'') APD33,
    ISNULL(APD34,'') APD34, ISNULL(APD35,'') APD35, ISNULL(APD36,'') APD36,
    ISNULL(APD37,'') APD37, ISNULL(APD38,'') APD38, ISNULL(APD39,'') APD39,
    ISNULL(APD40,'') APD40, ISNULL(APD41,'') APD41, ISNULL(APD42,'') APD42,
    ISNULL(APD43,'') APD43, ISNULL(APD44,'') APD44,
    ISNULL(SP01,'') SP01, ISNULL(SP02,'') SP02, ISNULL(SP03,'') SP03,
    ISNULL(SP04,'') SP04, ISNULL(SP05,'') SP05, ISNULL(SP06,'') SP06,
    ISNULL(SP07,'') SP07, ISNULL(SP08,'') SP08, ISNULL(SP09,'') SP09,
    ISNULL(SP10,'') SP10, ISNULL(SP11,'') SP11, ISNULL(SP12,'') SP12
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate1 AND @eDate1
ORDER BY UPDATE_TIME;
GO

-- ============================================================
-- Phase A-2: MODEL 필터 추가 시 (현재 인덱스로는 비효율)
-- ============================================================
PRINT N'=== Phase A-2: UPDATE_TIME + MODEL 필터, 전체 컬럼 ===';
DECLARE @sDate2 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate2 DATETIME = GETDATE();
DECLARE @model2 NVARCHAR(50) = N'MODEL_A';

SELECT
    UPDATE_TIME, REPAIR, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE,
    ISNULL(APD01,'') APD01, ISNULL(APD02,'') APD02, ISNULL(APD03,'') APD03,
    ISNULL(APD04,'') APD04, ISNULL(APD05,'') APD05, ISNULL(APD06,'') APD06,
    ISNULL(APD07,'') APD07, ISNULL(APD08,'') APD08
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate2 AND @eDate2
  AND MODEL = @model2
ORDER BY UPDATE_TIME;
GO

-- ============================================================
-- Phase A-3: 그리드 요약 컬럼만 SELECT (비교 기준 — Key Lookup 없음)
--            인덱스 INCLUDE 범위 내 컬럼만 요청 → Key Lookup 0
-- ============================================================
PRINT N'=== Phase A-3: 요약 5개 컬럼만 (Key Lookup 없음 기대) ===';
DECLARE @sDate3 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate3 DATETIME = GETDATE();

SELECT UPDATE_TIME, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate3 AND @eDate3
ORDER BY UPDATE_TIME;
GO

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Step 2: 인덱스 추가 — MODEL 복합 인덱스
--
-- 근거:
--   현재 IX_EMPG_UPDATE_TIME : (UPDATE_TIME) Seek → MODEL 필터는 Residual Predicate
--   신규 IX_EMPG_MODEL_UPTIME: (MODEL, UPDATE_TIME) → MODEL 조건이 있으면 Seek 효율적
--
--   두 인덱스를 함께 두면 옵티마이저가 쿼리 조건에 따라 선택:
--     MODEL 없음  → IX_EMPG_UPDATE_TIME
--     MODEL 있음  → IX_EMPG_MODEL_UPTIME
-- ============================================================
PRINT N'=== 인덱스 추가: IX_EMPG_MODEL_UPTIME ===';

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG') AND name = 'IX_EMPG_MODEL_UPTIME'
)
    DROP INDEX IX_EMPG_MODEL_UPTIME ON EMPG;

CREATE NONCLUSTERED INDEX IX_EMPG_MODEL_UPTIME
ON [dbo].[EMPG] (MODEL, UPDATE_TIME)
INCLUDE (
    TOTAL_JUDGE,
    MAT_SERIAL01,
    MAT_SERIAL02,
    REPAIR,
    RESULT_ID,
    OP200_TOTAL_JUDGE
);
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG_HIS') AND name = 'IX_EMPG_HIS_MODEL_UPTIME'
)
    DROP INDEX IX_EMPG_HIS_MODEL_UPTIME ON EMPG_HIS;

CREATE NONCLUSTERED INDEX IX_EMPG_HIS_MODEL_UPTIME
ON [dbo].[EMPG_HIS] (MODEL, UPDATE_TIME)
INCLUDE (
    TOTAL_JUDGE,
    MAT_SERIAL01,
    MAT_SERIAL02,
    REPAIR,
    RESULT_ID,
    OP200_TOTAL_JUDGE
);
GO

UPDATE STATISTICS EMPG     IX_EMPG_MODEL_UPTIME     WITH FULLSCAN;
UPDATE STATISTICS EMPG_HIS IX_EMPG_HIS_MODEL_UPTIME WITH FULLSCAN;
GO

PRINT N'인덱스 생성 완료';
GO

-- ============================================================
-- Step 3: Phase B — 인덱스 추가 후 동일 쿼리 재측정
-- ============================================================
SET STATISTICS TIME ON;
SET STATISTICS IO ON;
GO

PRINT N'=== Phase B-1: 전체 컬럼, MODEL 없음 (인덱스 추가 후) ===';
DECLARE @sDate4 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate4 DATETIME = GETDATE();

SELECT
    UPDATE_TIME, REPAIR, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE,
    ISNULL(APD01,'') APD01, ISNULL(APD02,'') APD02,
    ISNULL(APD03,'') APD03, ISNULL(APD04,'') APD04,
    ISNULL(APD05,'') APD05, ISNULL(APD06,'') APD06,
    ISNULL(APD07,'') APD07, ISNULL(APD08,'') APD08
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate4 AND @eDate4
ORDER BY UPDATE_TIME;
GO

PRINT N'=== Phase B-2: 전체 컬럼, MODEL 있음 (신규 인덱스 활용 기대) ===';
DECLARE @sDate5 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate5 DATETIME = GETDATE();
DECLARE @model5 NVARCHAR(50) = N'MODEL_A';

SELECT
    UPDATE_TIME, REPAIR, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE,
    ISNULL(APD01,'') APD01, ISNULL(APD02,'') APD02,
    ISNULL(APD03,'') APD03, ISNULL(APD04,'') APD04,
    ISNULL(APD05,'') APD05, ISNULL(APD06,'') APD06,
    ISNULL(APD07,'') APD07, ISNULL(APD08,'') APD08
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate5 AND @eDate5
  AND MODEL = @model5
ORDER BY UPDATE_TIME;
GO

PRINT N'=== Phase B-3: 요약 5개 컬럼 + MODEL (Key Lookup 0 기대) ===';
DECLARE @sDate6 DATETIME = DATEADD(DAY, -7, GETDATE());
DECLARE @eDate6 DATETIME = GETDATE();
DECLARE @model6 NVARCHAR(50) = N'MODEL_A';

SELECT UPDATE_TIME, MODEL, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE
FROM EMPG
WHERE UPDATE_TIME BETWEEN @sDate6 AND @eDate6
  AND MODEL = @model6
ORDER BY UPDATE_TIME;
GO

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Step 4: Key Lookup 수 직접 확인 (dm_exec_query_stats 활용)
--
-- SSMS 실행계획 화면에서도 확인 가능:
--   Key Lookup (Clustered) 노드 → 툴팁의 "Actual Number of Rows" 확인
--   이 값 = 조회된 행 수 → Key Lookup이 많을수록 느림
-- ============================================================
PRINT N'=== Key Lookup 통계 (최근 캐시된 쿼리 기준) ===';
SELECT TOP 20
    qs.execution_count                                          AS [실행수],
    qs.total_logical_reads / qs.execution_count                AS [평균_LogicalRead],
    qs.total_elapsed_time  / qs.execution_count / 1000         AS [평균_경과ms],
    SUBSTRING(qt.text, 1, 150)                                 AS [SQL 일부]
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text LIKE '%EMPG%'
  AND qt.text NOT LIKE '%dm_exec%'
ORDER BY qs.total_logical_reads / qs.execution_count DESC;
GO

-- ============================================================
-- Step 5: 인덱스 단편화 확인 (단편화 > 30% 이면 REBUILD 권장)
-- ============================================================
SELECT
    OBJECT_NAME(s.object_id)    AS [테이블],
    i.name                      AS [인덱스명],
    s.avg_fragmentation_in_percent AS [단편화%],
    s.page_count                AS [페이지수],
    CASE
        WHEN s.avg_fragmentation_in_percent > 30 THEN 'REBUILD 권장'
        WHEN s.avg_fragmentation_in_percent > 10 THEN 'REORGANIZE 권장'
        ELSE 'OK'
    END                         AS [상태]
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
JOIN sys.indexes i ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE OBJECT_NAME(s.object_id) IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
ORDER BY s.avg_fragmentation_in_percent DESC;
GO

-- ============================================================
-- 측정 결과 기록 양식
-- ============================================================
/*
Phase A (IX_EMPG_MODEL_UPTIME 없음):
  A-1 전체컬럼 MODEL없음  elapsed: ??? ms  logical reads: ???  Key Lookup: ???회
  A-2 전체컬럼 MODEL있음  elapsed: ??? ms  logical reads: ???  Key Lookup: ???회
  A-3 요약5컬럼           elapsed: ??? ms  logical reads: ???  Key Lookup: 0회

Phase B (IX_EMPG_MODEL_UPTIME 추가 후):
  B-1 전체컬럼 MODEL없음  elapsed: ??? ms  logical reads: ???  Key Lookup: ???회
  B-2 전체컬럼 MODEL있음  elapsed: ??? ms  logical reads: ???  Key Lookup: ???회
  B-3 요약5컬럼 MODEL있음 elapsed: ??? ms  logical reads: ???  Key Lookup: 0회

결론:
  - A-3 vs A-1 : 요약컬럼만 조회 시 속도 차이 = Key Lookup 비용 측정
  - A-2 vs B-2 : MODEL 복합 인덱스 효과 측정
  - B-3 비교   : Key Lookup 0이 목표치 (인덱스 커버링 달성)
*/
