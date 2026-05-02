-- ============================================================
-- 08: MAT_SERIAL01/02 인덱스 도입 전/후 성능 비교
--
-- 실행 순서:
--   1. Phase A — 인덱스 없이 FindBySerial 쿼리 실행 → 시간 기록
--   2. 인덱스 생성
--   3. Phase B — 동일 쿼리 재실행 → 시간 기록 후 비교
--
-- ※ 각 Phase 실행 전 캐시 클리어 권장:
--      DBCC DROPCLEANBUFFERS;
--      DBCC FREEPROCCACHE;
-- ============================================================
USE [DB_eM]
GO
SET NOCOUNT ON;
GO

-- ============================================================
-- 0. 기존 인덱스 정리 (재실행 시 충돌 방지)
-- ============================================================
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG')
      AND name = 'IX_EMPG_MAT_SERIAL'
)
    DROP INDEX IX_EMPG_MAT_SERIAL ON EMPG;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG_HIS')
      AND name = 'IX_EMPG_HIS_MAT_SERIAL'
)
    DROP INDEX IX_EMPG_HIS_MAT_SERIAL ON EMPG_HIS;
GO

-- ============================================================
-- Phase A: 인덱스 없음 — FindBySerial 쿼리 성능 측정
--
-- C# ProcessData_Op200 의 FindBySerial 이 실행하는 쿼리와 동일.
-- 중간(n=250000)과 끝(n=500000) 시리얼로 Full Scan 강도를 확인한다.
-- ============================================================
PRINT N'======================================================';
PRINT N'Phase A: 인덱스 없음 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
PRINT N'======================================================';
GO

-- 캐시 클리어 (선택적 — 순수 IO 측정 시 주석 해제)
-- DBCC DROPCLEANBUFFERS;
-- DBCC FREEPROCCACHE;

SET STATISTICS TIME ON;
SET STATISTICS IO ON;
GO

-- A-1: 중간 시리얼 (ShaftSerial 기준 → MAT_SERIAL01 히트)
PRINT N'--- A-1: MAT_SERIAL01 = SFT-250000 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'SFT-250000' OR MAT_SERIAL02 = N'SFT-250000'
ORDER BY CREATE_DAYTIME DESC;
GO

-- A-2: 끝 시리얼 (GearSerial 기준 → MAT_SERIAL02 히트)
PRINT N'--- A-2: MAT_SERIAL02 = GEA-499999 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'GEA-499999' OR MAT_SERIAL02 = N'GEA-499999'
ORDER BY CREATE_DAYTIME DESC;
GO

-- A-3: 존재하지 않는 시리얼 (Full Scan 최악의 경우)
PRINT N'--- A-3: 존재하지 않는 시리얼 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'SFT-ZZZZZZ' OR MAT_SERIAL02 = N'SFT-ZZZZZZ'
ORDER BY CREATE_DAYTIME DESC;
GO

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Phase B: 인덱스 생성
--
-- MAT_SERIAL01, MAT_SERIAL02 각각 별도 인덱스를 생성한다.
-- OR 조건에서 옵티마이저가 두 Index Seek 를 병렬로 실행 후 Merge 한다.
-- ============================================================
PRINT N'======================================================';
PRINT N'인덱스 생성 시작 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
PRINT N'======================================================';

CREATE NONCLUSTERED INDEX IX_EMPG_MAT_SERIAL
ON [dbo].[EMPG] (MAT_SERIAL01, MAT_SERIAL02)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, CREATE_DAYTIME);
GO

CREATE NONCLUSTERED INDEX IX_EMPG_HIS_MAT_SERIAL
ON [dbo].[EMPG_HIS] (MAT_SERIAL01, MAT_SERIAL02)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, CREATE_DAYTIME);
GO

PRINT N'인덱스 생성 완료 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
GO

-- 인덱스 통계 강제 갱신 (첫 Seek 정확도 확보)
UPDATE STATISTICS EMPG IX_EMPG_MAT_SERIAL WITH FULLSCAN;
UPDATE STATISTICS EMPG_HIS IX_EMPG_HIS_MAT_SERIAL WITH FULLSCAN;
GO

-- ============================================================
-- Phase C: 인덱스 있음 — 동일 쿼리 재측정
-- ============================================================
PRINT N'======================================================';
PRINT N'Phase C: 인덱스 있음 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
PRINT N'======================================================';
GO

-- DBCC DROPCLEANBUFFERS;
-- DBCC FREEPROCCACHE;

SET STATISTICS TIME ON;
SET STATISTICS IO ON;
GO

PRINT N'--- C-1: MAT_SERIAL01 = SFT-250000 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'SFT-250000' OR MAT_SERIAL02 = N'SFT-250000'
ORDER BY CREATE_DAYTIME DESC;
GO

PRINT N'--- C-2: MAT_SERIAL02 = GEA-499999 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'GEA-499999' OR MAT_SERIAL02 = N'GEA-499999'
ORDER BY CREATE_DAYTIME DESC;
GO

PRINT N'--- C-3: 존재하지 않는 시리얼 ---';
SELECT TOP 1 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, UPDATE_TIME
FROM EMPG
WHERE MAT_SERIAL01 = N'SFT-ZZZZZZ' OR MAT_SERIAL02 = N'SFT-ZZZZZZ'
ORDER BY CREATE_DAYTIME DESC;
GO

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- 인덱스 목록 확인
-- ============================================================
SELECT
    t.name      AS [테이블],
    i.name      AS [인덱스명],
    i.type_desc AS [유형],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [키 컬럼]
FROM sys.indexes i
JOIN sys.tables t  ON i.object_id = t.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
GROUP BY t.name, i.name, i.type_desc
ORDER BY t.name, i.name;
GO

-- ============================================================
-- 측정 결과 기록 양식
-- ============================================================
/*
Phase A (IX_EMPG_MAT_SERIAL 없음):
  A-1 elapsed time: ??? ms  |  logical reads: ???
  A-2 elapsed time: ??? ms  |  logical reads: ???
  A-3 elapsed time: ??? ms  |  logical reads: ???

Phase C (IX_EMPG_MAT_SERIAL 있음):
  C-1 elapsed time: ??? ms  |  logical reads: ???
  C-2 elapsed time: ??? ms  |  logical reads: ???
  C-3 elapsed time: ??? ms  |  logical reads: ???

C# ProcessData_Op200 Stopwatch (로그에서 확인):
  인덱스 없음 → 경과 ??? ms
  인덱스 있음 → 경과 ??? ms
*/
