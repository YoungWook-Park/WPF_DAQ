-- ============================================================
-- 10: 최종 인덱스 정의 (확정 적용본)
--
-- 기존 문제:
--   IX_EMPG_MAT_SERIAL   : (MAT_SERIAL01, MAT_SERIAL02) 복합 키
--   → OR 조건에서 MAT_SERIAL02 단독 Seek 불가 → Index Scan 강제
--
-- 이 스크립트에서 하는 일:
--   1. 잘못된 복합 Serial 인덱스 → 독립 2개로 교체
--      (MAT_SERIAL01, CREATE_DAYTIME DESC) / (MAT_SERIAL02, CREATE_DAYTIME DESC)
--   2. RESULT_ID 비클러스터 고유 인덱스 추가 (UPDATE WHERE 절)
--      ※ RESULT_ID 가 이미 PK(클러스터)이면 아래 DROP/CREATE 불필요 — 현황 확인 후 실행
--
-- 이미 올바른 인덱스 (03/09에서 생성, 유지):
--   IX_EMPG_UPDATE_TIME      : (UPDATE_TIME)          — 기간 조회 Range Scan
--   IX_EMPG_MODEL_UPTIME     : (MODEL, UPDATE_TIME)   — MODEL+기간 조회
--   (EMPG_HIS 동일)
-- ============================================================
USE [DB_eM]
GO
SET NOCOUNT ON;
GO

-- ============================================================
-- 현황 먼저 확인 (실행 후 인덱스 목록 검토)
-- ============================================================
SELECT
    t.name                                                                 AS [테이블],
    i.name                                                                 AS [인덱스명],
    i.type_desc                                                            AS [유형],
    STRING_AGG(CASE WHEN ic.is_included_column = 0 THEN c.name END, ', ')
        WITHIN GROUP (ORDER BY ic.key_ordinal)                             AS [Seek 키],
    STRING_AGG(CASE WHEN ic.is_included_column = 1 THEN c.name END, ', ')
        WITHIN GROUP (ORDER BY ic.index_column_id)                         AS [INCLUDE],
    i.is_unique
FROM sys.indexes i
JOIN sys.tables        t  ON i.object_id = t.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns       c  ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
GROUP BY t.name, i.name, i.type_desc, i.is_unique
ORDER BY t.name, i.name;
GO

-- ============================================================
-- 1. 잘못된 복합 Serial 인덱스 제거
--
-- 이유:
--   FindBySerial 쿼리:
--     WHERE MAT_SERIAL01 = @s OR MAT_SERIAL02 = @s
--     ORDER BY CREATE_DAYTIME DESC
--
--   (MAT_SERIAL01, MAT_SERIAL02) 복합 키는
--   → MAT_SERIAL01 = @s 조건에서는 Seek 가능
--   → MAT_SERIAL02 = @s 단독 조건에서는 Seek 불가 (Leading Column 아님)
--   → OR 로 묶이면 옵티마이저가 Index Scan 또는 Table Scan 선택
-- ============================================================
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('EMPG')     AND name = 'IX_EMPG_MAT_SERIAL')
    DROP INDEX IX_EMPG_MAT_SERIAL     ON [dbo].[EMPG];
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('EMPG_HIS') AND name = 'IX_EMPG_HIS_MAT_SERIAL')
    DROP INDEX IX_EMPG_HIS_MAT_SERIAL ON [dbo].[EMPG_HIS];
GO

-- ============================================================
-- 2. 독립 Serial 인덱스 2개 생성
--
-- 이유:
--   OR 조건에서 옵티마이저는 두 Index Seek 를 병렬 수행 후 Union Merge 한다.
--   (SQL Server 실행계획: "Index Seek + Index Seek → Concatenation → Top")
--
--   CREATE_DAYTIME DESC 를 Seek 키에 포함하는 이유:
--     TOP 1 ... ORDER BY CREATE_DAYTIME DESC 에서 Sort 연산 제거
--     → 각 Index Seek 가 이미 최신순으로 정렬되어 있으므로
--       Concatenation 단계에서 즉시 Top 1 을 선택 가능
--
--   RESULT_ID, TOTAL_JUDGE, MODEL INCLUDE:
--     UpdateSubCols / UpdateOp200Cols 의 WHERE RESULT_ID = ... 와
--     이후 EmpgRow 매핑에서 key lookup 없이 식별자 취득 가능
-- ============================================================
CREATE NONCLUSTERED INDEX IX_EMPG_MAT_SERIAL01
ON [dbo].[EMPG] (MAT_SERIAL01, CREATE_DAYTIME DESC)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, MAT_SERIAL02);
GO

CREATE NONCLUSTERED INDEX IX_EMPG_MAT_SERIAL02
ON [dbo].[EMPG] (MAT_SERIAL02, CREATE_DAYTIME DESC)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, MAT_SERIAL01);
GO

CREATE NONCLUSTERED INDEX IX_EMPG_HIS_MAT_SERIAL01
ON [dbo].[EMPG_HIS] (MAT_SERIAL01, CREATE_DAYTIME DESC)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, MAT_SERIAL02);
GO

CREATE NONCLUSTERED INDEX IX_EMPG_HIS_MAT_SERIAL02
ON [dbo].[EMPG_HIS] (MAT_SERIAL02, CREATE_DAYTIME DESC)
INCLUDE (RESULT_ID, UPDATE_TIME, TOTAL_JUDGE, MODEL, MAT_SERIAL01);
GO

-- ============================================================
-- 3. RESULT_ID 비클러스터 고유 인덱스
--
-- 이유:
--   UpdateOp200Cols / UpdateSubCols 모두 WHERE RESULT_ID = @RESULT_ID 로 UPDATE.
--   RESULT_ID 가 클러스터 PK 가 아닌 경우 이 인덱스 없이는 Table Scan.
--
--   RESULT_ID 가 이미 PK(클러스터)라면 이 블록은 건너뛴다.
--   확인 방법:
--     SELECT CONSTRAINT_TYPE FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
--     WHERE TABLE_NAME='EMPG' AND CONSTRAINT_NAME LIKE '%RESULT_ID%';
--
-- GUID 전환 이후 단편화 주의사항:
--   RESULT_ID 가 클러스터 PK 이면 무작위 GUID INSERT 로 인해 페이지 분열 발생.
--   이 경우 IDENTITY BIGINT PK 를 클러스터로 두고 RESULT_ID 를 비클러스터 고유로 유지 권장.
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG') AND name = 'IX_EMPG_RESULT_ID'
)
CREATE UNIQUE NONCLUSTERED INDEX IX_EMPG_RESULT_ID
ON [dbo].[EMPG] (RESULT_ID);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('EMPG_HIS') AND name = 'IX_EMPG_HIS_RESULT_ID'
)
CREATE UNIQUE NONCLUSTERED INDEX IX_EMPG_HIS_RESULT_ID
ON [dbo].[EMPG_HIS] (RESULT_ID);
GO

-- ============================================================
-- 4. 통계 갱신 (새 인덱스 첫 Seek 정확도 확보)
-- ============================================================
UPDATE STATISTICS [dbo].[EMPG]     IX_EMPG_MAT_SERIAL01     WITH FULLSCAN;
UPDATE STATISTICS [dbo].[EMPG]     IX_EMPG_MAT_SERIAL02     WITH FULLSCAN;
UPDATE STATISTICS [dbo].[EMPG]     IX_EMPG_RESULT_ID        WITH FULLSCAN;
UPDATE STATISTICS [dbo].[EMPG_HIS] IX_EMPG_HIS_MAT_SERIAL01 WITH FULLSCAN;
UPDATE STATISTICS [dbo].[EMPG_HIS] IX_EMPG_HIS_MAT_SERIAL02 WITH FULLSCAN;
UPDATE STATISTICS [dbo].[EMPG_HIS] IX_EMPG_HIS_RESULT_ID    WITH FULLSCAN;
GO

-- ============================================================
-- 5. 최종 인덱스 현황 확인
-- ============================================================
SELECT
    t.name                                                                 AS [테이블],
    i.name                                                                 AS [인덱스명],
    i.type_desc                                                            AS [유형],
    i.is_unique                                                            AS [고유],
    STRING_AGG(CASE WHEN ic.is_included_column = 0 THEN c.name END, ', ')
        WITHIN GROUP (ORDER BY ic.key_ordinal)                             AS [Seek 키],
    STRING_AGG(CASE WHEN ic.is_included_column = 1 THEN c.name END, ', ')
        WITHIN GROUP (ORDER BY ic.index_column_id)                         AS [INCLUDE]
FROM sys.indexes i
JOIN sys.tables        t  ON i.object_id = t.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns       c  ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.name IS NOT NULL
GROUP BY t.name, i.name, i.type_desc, i.is_unique
ORDER BY t.name, i.name;
GO

-- ============================================================
-- 참고: Key Lookup 이 여전히 발생하는 이유 (조회 화면)
--
-- Inquiry_OP200_ResourceLotHistoryViewModel 은 APD01~44 + SP01~50 약 100개 컬럼을
-- SELECT 한다. 어떤 비클러스터 인덱스도 이 모든 컬럼을 INCLUDE 할 수 없으므로
-- Key Lookup 은 구조적으로 발생한다.
--
-- 실용적 개선 방향 (우선순위 순):
--   A. 페이지네이션 (TOP N OFFSET M) — 한 번에 조회하는 행 수를 제한
--      → Key Lookup 횟수 자체를 줄이는 가장 효과적인 방법
--   B. 요약 그리드 / 상세 패널 분리 — 그리드에는 요약 6개 컬럼, 선택 시 상세 로딩
--      → 그리드 단계에서 Key Lookup 0 달성 (IX_EMPG_UPDATE_TIME INCLUDE 컬럼 범위 내)
--   C. 현 구조 유지 + 인덱스만 최적화 — 인덱스로 Range Scan 및 Sort 비용을 줄이되
--      Key Lookup 비용은 수용 (운영 환경 조회 건수가 수천 건 이하이면 충분)
-- ============================================================
