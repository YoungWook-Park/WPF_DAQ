-- ============================================================
-- Step 1: DB 스키마 수정
-- 목적: UPDATE_TIME nvarchar(23) → datetime 타입 변경
-- 대상: EMPG, EMPG_HIS
-- 주의: 기존 데이터가 'yyyy-MM-dd HH:mm:ss.fff' 형식으로 저장된
--       경우 자동 변환됨. 변환 불가 데이터가 있으면 아래 검증
--       쿼리 먼저 실행할 것.
-- ============================================================

USE [DB_eM]
GO

-- ------------------------------------------------------------
-- 0. 변환 전 검증: 변환 불가 값 확인 (있으면 먼저 처리)
-- ------------------------------------------------------------
SELECT TOP 20 UPDATE_TIME
FROM EMPG
WHERE TRY_CAST(UPDATE_TIME AS DATETIME) IS NULL
  AND UPDATE_TIME IS NOT NULL;

SELECT TOP 20 UPDATE_TIME
FROM EMPG_HIS
WHERE TRY_CAST(UPDATE_TIME AS DATETIME) IS NULL
  AND UPDATE_TIME IS NOT NULL;

-- ------------------------------------------------------------
-- 1. EMPG 테이블 UPDATE_TIME 타입 변경
-- ------------------------------------------------------------
ALTER TABLE [dbo].[EMPG]
    ALTER COLUMN [UPDATE_TIME] DATETIME NOT NULL;
GO

-- ------------------------------------------------------------
-- 2. EMPG_HIS 테이블 UPDATE_TIME 타입 변경
-- ------------------------------------------------------------
ALTER TABLE [dbo].[EMPG_HIS]
    ALTER COLUMN [UPDATE_TIME] DATETIME NOT NULL;
GO

-- ------------------------------------------------------------
-- 3. 변경 확인
-- ------------------------------------------------------------
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('EMPG', 'EMPG_HIS')
  AND COLUMN_NAME = 'UPDATE_TIME'
ORDER BY TABLE_NAME;
GO
