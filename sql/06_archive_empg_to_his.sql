-- ============================================================
-- EMPG → EMPG_HIS 이관 (아카이빙)
-- 조건: UPDATE_TIME < 현재 기준 6개월 이전
-- 방식: 10,000건씩 배치 INSERT/DELETE (트랜잭션 로그 부하 분산)
-- ============================================================
SET NOCOUNT ON;

DECLARE @cutoff    DATETIME = DATEADD(MONTH, -6, GETDATE());
DECLARE @batchSize INT      = 10000;
DECLARE @totalIns  INT      = 0;
DECLARE @totalDel  INT      = 0;
DECLARE @batchNo   INT      = 0;
DECLARE @thisIns   INT;
DECLARE @thisDel   INT;

PRINT N'============================================================';
PRINT N'EMPG → EMPG_HIS 아카이빙 시작';
PRINT N'기준일시 (6개월 이전) : ' + CONVERT(NVARCHAR, @cutoff, 121);
PRINT N'배치 크기              : ' + CAST(@batchSize AS NVARCHAR);
PRINT N'============================================================';

-- 사전 건수 확인
SELECT
    COUNT(*)                                                   AS 전체_이관대상,
    SUM(CASE WHEN h.RESULT_ID IS NULL     THEN 1 ELSE 0 END)  AS 신규_이관,
    SUM(CASE WHEN h.RESULT_ID IS NOT NULL THEN 1 ELSE 0 END)  AS 이미존재_SKIP
FROM EMPG e
LEFT JOIN EMPG_HIS h ON e.RESULT_ID = h.RESULT_ID
WHERE e.UPDATE_TIME < @cutoff;

CREATE TABLE #batch (RESULT_ID NVARCHAR(50) PRIMARY KEY);

-- 배치 루프
BEGIN TRY
    WHILE 1 = 1
    BEGIN
        TRUNCATE TABLE #batch;

        -- 이번 배치: 아직 EMPG_HIS에 없는 행 @batchSize건
        INSERT INTO #batch (RESULT_ID)
        SELECT TOP (@batchSize) e.RESULT_ID
        FROM EMPG e
        WHERE e.UPDATE_TIME < @cutoff
          AND NOT EXISTS (SELECT 1 FROM EMPG_HIS h WHERE h.RESULT_ID = e.RESULT_ID);

        -- @@ROWCOUNT를 변수에 먼저 저장 (IF 문이 @@ROWCOUNT를 0으로 초기화하기 때문)
        DECLARE @cnt INT = @@ROWCOUNT;
        IF @cnt = 0 BREAK;  -- 이관할 행 없으면 종료
        SET @batchNo += 1;

        BEGIN TRANSACTION;

            INSERT INTO EMPG_HIS
            SELECT e.*
            FROM EMPG e
            WHERE EXISTS (SELECT 1 FROM #batch b WHERE b.RESULT_ID = e.RESULT_ID);

            SET @thisIns = @@ROWCOUNT;

            DELETE e FROM EMPG e
            WHERE EXISTS (SELECT 1 FROM #batch b WHERE b.RESULT_ID = e.RESULT_ID);

            SET @thisDel = @@ROWCOUNT;

        COMMIT;

        SET @totalIns += @thisIns;
        SET @totalDel += @thisDel;

        PRINT N'배치 ' + CAST(@batchNo AS NVARCHAR)
            + N' | INSERT=' + CAST(@thisIns AS NVARCHAR)
            + N' DELETE=' + CAST(@thisDel AS NVARCHAR)
            + N' | 누계=' + CAST(@totalIns AS NVARCHAR) + N'건';
    END

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    PRINT N'[ERROR ' + CAST(ERROR_NUMBER() AS NVARCHAR) + N'] ' + ERROR_MESSAGE();
END CATCH;

DROP TABLE IF EXISTS #batch;

PRINT N'============================================================';
PRINT N'이관 완료 | 총 INSERT=' + CAST(@totalIns AS NVARCHAR) + N'건  DELETE=' + CAST(@totalDel AS NVARCHAR) + N'건';
PRINT N'============================================================';

SELECT
    (SELECT COUNT(*) FROM EMPG)     AS EMPG_잔여,
    (SELECT COUNT(*) FROM EMPG_HIS) AS EMPG_HIS_총건수,
    (SELECT MIN(UPDATE_TIME) FROM EMPG) AS EMPG_최소일시,
    (SELECT MAX(UPDATE_TIME) FROM EMPG) AS EMPG_최대일시;
