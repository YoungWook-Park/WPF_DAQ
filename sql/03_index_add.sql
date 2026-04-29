-- ============================================================
-- Step 9: 인덱스 생성 (Phase B 측정 전 실행)
-- ============================================================

USE [DB_eM]
GO

-- ------------------------------------------------------------
-- 1. EMPG 커버링 인덱스
--    UPDATE_TIME 기간 조회 + 자주 사용하는 필터/출력 컬럼 INCLUDE
-- ------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_EMPG_UPDATE_TIME
ON [dbo].[EMPG] (UPDATE_TIME)
INCLUDE (
    TOTAL_JUDGE,
    MODEL,
    MAT_SERIAL01,
    MAT_SERIAL02,
    RESULT_ID,
    REPAIR,
    OP200_TOTAL_JUDGE
);
GO

-- ------------------------------------------------------------
-- 2. EMPG_HIS 동일 인덱스
-- ------------------------------------------------------------
CREATE NONCLUSTERED INDEX IX_EMPG_HIS_UPDATE_TIME
ON [dbo].[EMPG_HIS] (UPDATE_TIME)
INCLUDE (
    TOTAL_JUDGE,
    MODEL,
    MAT_SERIAL01,
    MAT_SERIAL02,
    RESULT_ID,
    REPAIR,
    OP200_TOTAL_JUDGE
);
GO

-- ------------------------------------------------------------
-- 3. 인덱스 생성 확인
-- ------------------------------------------------------------
SELECT
    t.name        AS TableName,
    i.name        AS IndexName,
    i.type_desc   AS IndexType,
    i.is_unique
FROM sys.indexes i
JOIN sys.tables  t ON i.object_id = t.object_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
ORDER BY t.name, i.name;
GO
