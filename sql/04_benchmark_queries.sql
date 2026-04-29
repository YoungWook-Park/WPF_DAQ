-- ============================================================
-- Step 8 / 9 / 10: 성능 측정 쿼리
-- Phase A (인덱스 없음) / Phase B (인덱스 후) 공통 사용
-- Phase C는 EF Core 생성 SQL을 별도 캡처
-- ============================================================

USE [DB_eM]
GO

-- 실행 전 버퍼 캐시 클리어 (매 측정 전 실행)
-- DBCC DROPCLEANBUFFERS;
-- DBCC FREEPROCCACHE;

SET STATISTICS TIME ON;
SET STATISTICS IO ON;
GO

-- ============================================================
-- Q1: 기간 1일 조회 (모델/판정 필터 없음)
-- ============================================================
PRINT '=== Q1: 기간 1일 조회 ===';
DECLARE @q1_start DATETIME = '2024-06-01 00:00:00';
DECLARE @q1_end   DATETIME = '2024-06-01 23:59:59';

SELECT
    UPDATE_TIME,
    REPAIR,
    MODEL,
    MAT_SERIAL01,
    MAT_SERIAL02,
    TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
    APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
    APD41, APD42, APD43, APD44
FROM EMPG
WHERE UPDATE_TIME BETWEEN @q1_start AND @q1_end
ORDER BY UPDATE_TIME DESC;
GO

-- ============================================================
-- Q2: 기간 1개월 + MODEL 필터
-- ============================================================
PRINT '=== Q2: 기간 1개월 + MODEL 필터 ===';
DECLARE @q2_start DATETIME = '2024-06-01 00:00:00';
DECLARE @q2_end   DATETIME = '2024-06-30 23:59:59';
DECLARE @q2_model NVARCHAR(50) = 'MODEL_A';

SELECT
    UPDATE_TIME,
    REPAIR,
    MODEL,
    MAT_SERIAL01,
    MAT_SERIAL02,
    TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
    APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
    APD41, APD42, APD43, APD44
FROM EMPG
WHERE UPDATE_TIME BETWEEN @q2_start AND @q2_end
  AND MODEL = @q2_model
ORDER BY UPDATE_TIME DESC;
GO

-- ============================================================
-- Q3: 기간 1개월 + TOTAL_JUDGE = 'NG' 필터
-- ============================================================
PRINT '=== Q3: 기간 1개월 + TOTAL_JUDGE NG 필터 ===';
DECLARE @q3_start DATETIME = '2024-06-01 00:00:00';
DECLARE @q3_end   DATETIME = '2024-06-30 23:59:59';

SELECT
    UPDATE_TIME,
    REPAIR,
    MODEL,
    MAT_SERIAL01,
    MAT_SERIAL02,
    TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
    APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
    APD41, APD42, APD43, APD44
FROM EMPG
WHERE UPDATE_TIME BETWEEN @q3_start AND @q3_end
  AND TOTAL_JUDGE = N'NG'
ORDER BY UPDATE_TIME DESC;
GO

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- 측정값 기록 양식 (결과를 아래에 채워넣을 것)
-- ============================================================
/*
Phase A (인덱스 없음):
  Q1 SQL Server 실행 시간:  ??? ms  (CPU time = ???ms, elapsed time = ???ms)
  Q2 SQL Server 실행 시간:  ??? ms
  Q3 SQL Server 실행 시간:  ??? ms

Phase B (IX_EMPG_UPDATE_TIME 인덱스 추가 후):
  Q1 SQL Server 실행 시간:  ??? ms
  Q2 SQL Server 실행 시간:  ??? ms
  Q3 SQL Server 실행 시간:  ??? ms
*/
