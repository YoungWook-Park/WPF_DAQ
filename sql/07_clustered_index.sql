USE [DB_eM];
SET NOCOUNT ON;
GO

-- EMPG / EMPG_HIS 에 UPDATE_TIME 클러스터드 인덱스 추가
--
-- 배경:
--   현재 두 테이블은 RESULT_ID PK가 NONCLUSTERED → Heap 테이블.
--   UPDATE_TIME BETWEEN 범위 조회 시 Index Seek 후 행마다 Key Lookup(Heap RID)이 발생한다.
--   한달치 60,000건 조회 = Key Lookup 60,000회 → 심각한 성능 저하.
--   UPDATE_TIME을 클러스터드 키로 바꾸면 리프 레벨에 모든 컬럼이 포함되어 Key Lookup 불필요.
--
-- 주의: 실행 전 백업 권장. 데이터 양에 따라 수십 초 소요될 수 있음.

-- ── EMPG ──────────────────────────────────────────────────────────────

-- 1. 기존 NONCLUSTERED PK 제거
ALTER TABLE [dbo].[EMPG] DROP CONSTRAINT PK_EMPG;

-- 2. UPDATE_TIME 클러스터드 인덱스 생성 (범위 조회 최적화)
CREATE CLUSTERED INDEX CIX_EMPG_UPDATE_TIME
    ON [dbo].[EMPG] (UPDATE_TIME);

-- 3. RESULT_ID PK 재생성 (NONCLUSTERED 유지)
ALTER TABLE [dbo].[EMPG]
    ADD CONSTRAINT PK_EMPG PRIMARY KEY NONCLUSTERED (RESULT_ID);

-- ── EMPG_HIS ──────────────────────────────────────────────────────────

ALTER TABLE [dbo].[EMPG_HIS] DROP CONSTRAINT PK_EMPG_HIS;

CREATE CLUSTERED INDEX CIX_EMPG_HIS_UPDATE_TIME
    ON [dbo].[EMPG_HIS] (UPDATE_TIME);

ALTER TABLE [dbo].[EMPG_HIS]
    ADD CONSTRAINT PK_EMPG_HIS PRIMARY KEY NONCLUSTERED (RESULT_ID);

GO

-- 확인: 인덱스 목록
SELECT t.name AS 테이블, i.name AS 인덱스명, i.type_desc
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('EMPG', 'EMPG_HIS')
  AND i.type > 0
ORDER BY t.name, i.type DESC, i.name;
GO
