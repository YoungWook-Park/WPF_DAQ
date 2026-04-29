-- ============================================================
-- Step 7: 테스트 데이터 100만건 INSERT
-- EMPG 50만건 / EMPG_HIS 50만건
-- 기간: 2023-01-01 ~ 2025-12-31 균등 분포
-- ============================================================

USE [DB_eM]
GO

-- ------------------------------------------------------------
-- 1. STS_MODEL_TB 마스터 데이터 (FK 제약 선행 충족)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM STS_MODEL_TB WHERE MODEL = 'MODEL_A')
BEGIN
    INSERT INTO STS_MODEL_TB (MODEL, MODEL_DESC, PRODUCTION_QTY, FINISHED_QTY, DEFECTIVE_QTY, YIELD, CREATED_TIME)
    VALUES
        ('MODEL_A', '모델A', '0', '0', '0', '0', CONVERT(nvarchar(23), GETDATE(), 120)),
        ('MODEL_B', '모델B', '0', '0', '0', '0', CONVERT(nvarchar(23), GETDATE(), 120)),
        ('MODEL_C', '모델C', '0', '0', '0', '0', CONVERT(nvarchar(23), GETDATE(), 120)),
        ('MODEL_D', '모델D', '0', '0', '0', '0', CONVERT(nvarchar(23), GETDATE(), 120)),
        ('MODEL_E', '모델E', '0', '0', '0', '0', CONVERT(nvarchar(23), GETDATE(), 120));
END
GO

-- ------------------------------------------------------------
-- 2. EMPG 50만건 INSERT
-- RESULT_ID 형식: OP200-YYYYMMDD-NNNNNN (6자리 순번)
-- UPDATE_TIME: 2023-01-01 ~ 2025-12-31 균등 분포
-- TOTAL_JUDGE: 약 10% NG
-- ------------------------------------------------------------
SET NOCOUNT ON;
GO

DECLARE @i          INT = 1;
DECLARE @batchSize  INT = 1000;
DECLARE @totalRows  INT = 500000;

DECLARE @baseDate   DATETIME = '2023-01-01 00:00:00';
DECLARE @totalSecs  INT = DATEDIFF(SECOND, '2023-01-01', '2026-01-01'); -- 3년치 초

DECLARE @models     NVARCHAR(200) = 'MODEL_A|MODEL_B|MODEL_C|MODEL_D|MODEL_E';
DECLARE @model      NVARCHAR(50);
DECLARE @judge      NVARCHAR(10);
DECLARE @dt         DATETIME;
DECLARE @resultId   NVARCHAR(25);

WHILE @i <= @totalRows
BEGIN
    BEGIN TRANSACTION;

    DECLARE @batch INT = 0;
    WHILE @batch < @batchSize AND @i <= @totalRows
    BEGIN
        SET @dt = DATEADD(SECOND, ABS(CHECKSUM(NEWID())) % @totalSecs, @baseDate);

        -- 모델 선택 (순환)
        SET @model = CASE (@i % 5)
            WHEN 0 THEN 'MODEL_A'
            WHEN 1 THEN 'MODEL_B'
            WHEN 2 THEN 'MODEL_C'
            WHEN 3 THEN 'MODEL_D'
            ELSE         'MODEL_E'
        END;

        -- 판정 (10% NG)
        SET @judge = CASE WHEN (@i % 10 = 0) THEN 'NG' ELSE 'OK' END;

        SET @resultId = N'OP2-' + FORMAT(@dt, 'yyyyMMdd') + N'-' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6);

        INSERT INTO EMPG (
            RESULT_ID, UPDATE_TIME, REPAIR, MODEL,
            TOTAL_JUDGE, MAT_SERIAL01, MAT_SERIAL02,
            CREATE_DAYTIME,
            APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
            APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
            APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
            APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
            APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
            APD41, APD42, APD43, APD44,
            OP200_TOTAL_JUDGE
        )
        VALUES (
            @resultId,
            @dt,
            N'N',
            @model,
            @judge,
            N'SHAFT-' + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6),
            N'GEAR-'  + RIGHT('000000' + CAST(@i AS VARCHAR(6)), 6),
            @dt,
            -- APD01~08: 스페이서 (하중/변위/판정/스테이션)
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            @judge, N'ST01',
            -- APD09~16: 베어링
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            @judge, N'ST02',
            -- APD17~26: 스냅링/앤드플레이트
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            N'A',
            @judge, N'0.50',
            @judge, @judge,
            N'3.50', @judge,
            -- APD27~44: RUN OUT, 가이드링, LOTITE, ShaftOilCap
            N'0.03', @judge, N'0.04', @judge, @judge, N'1.20', @judge,
            @judge, @judge,
            N'12.5', N'0.50', N'12.5', N'0.50', N'12.5', N'0.50',
            @judge, N'5.20', @judge,
            @judge
        );

        SET @batch = @batch + 1;
        SET @i = @i + 1;
    END

    COMMIT TRANSACTION;

    -- 진행률 출력 (매 10만건)
    IF @i % 100000 = 1
        PRINT 'EMPG 진행: ' + CAST(@i - 1 AS VARCHAR) + ' / ' + CAST(@totalRows AS VARCHAR);
END

PRINT 'EMPG INSERT 완료: ' + CAST(@totalRows AS VARCHAR) + '건';
GO

-- ------------------------------------------------------------
-- 3. EMPG_HIS 50만건 INSERT (동일 구조, RESULT_ID 앞 구분자 변경)
-- ------------------------------------------------------------
DECLARE @j          INT = 1;
DECLARE @batchSize2 INT = 1000;
DECLARE @totalRows2 INT = 500000;

DECLARE @baseDate2  DATETIME = '2023-01-01 00:00:00';
DECLARE @totalSecs2 INT = DATEDIFF(SECOND, '2023-01-01', '2026-01-01');

DECLARE @model2     NVARCHAR(50);
DECLARE @judge2     NVARCHAR(10);
DECLARE @dt2        DATETIME;
DECLARE @resultId2  NVARCHAR(25);

WHILE @j <= @totalRows2
BEGIN
    BEGIN TRANSACTION;

    DECLARE @batch2 INT = 0;
    WHILE @batch2 < @batchSize2 AND @j <= @totalRows2
    BEGIN
        SET @dt2 = DATEADD(SECOND, ABS(CHECKSUM(NEWID())) % @totalSecs2, @baseDate2);

        SET @model2 = CASE (@j % 5)
            WHEN 0 THEN 'MODEL_A'
            WHEN 1 THEN 'MODEL_B'
            WHEN 2 THEN 'MODEL_C'
            WHEN 3 THEN 'MODEL_D'
            ELSE         'MODEL_E'
        END;

        SET @judge2 = CASE WHEN (@j % 10 = 0) THEN 'NG' ELSE 'OK' END;

        SET @resultId2 = N'HIS-' + FORMAT(@dt2, 'yyyyMMdd') + N'-' + RIGHT('000000' + CAST(@j AS VARCHAR(6)), 6);

        INSERT INTO EMPG_HIS (
            RESULT_ID, UPDATE_TIME, REPAIR, MODEL,
            TOTAL_JUDGE, MAT_SERIAL01, MAT_SERIAL02,
            CREATE_DAYTIME,
            APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
            APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
            APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
            APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
            APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
            APD41, APD42, APD43, APD44,
            OP200_TOTAL_JUDGE
        )
        VALUES (
            @resultId2, @dt2, N'N', @model2, @judge2,
            N'SHAFT-H' + RIGHT('000000' + CAST(@j AS VARCHAR(6)), 6),
            N'GEAR-H'  + RIGHT('000000' + CAST(@j AS VARCHAR(6)), 6),
            @dt2,
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(10.0 + (ABS(CHECKSUM(NEWID())) % 100) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 1.0 + (ABS(CHECKSUM(NEWID())) % 50)  * 0.01, 2) AS NVARCHAR(10)),
            @judge2, N'ST01',
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(15.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1, 2) AS NVARCHAR(10)),
            CAST(ROUND( 2.0 + (ABS(CHECKSUM(NEWID())) % 40) * 0.01, 2) AS NVARCHAR(10)),
            @judge2, N'ST02',
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            CAST(ROUND(5.0 + (ABS(CHECKSUM(NEWID())) % 30) * 0.01, 2) AS NVARCHAR(10)),
            N'A', @judge2, N'0.50', @judge2, @judge2, N'3.50', @judge2,
            N'0.03', @judge2, N'0.04', @judge2, @judge2, N'1.20', @judge2,
            @judge2, @judge2,
            N'12.5', N'0.50', N'12.5', N'0.50', N'12.5', N'0.50',
            @judge2, N'5.20', @judge2,
            @judge2
        );

        SET @batch2 = @batch2 + 1;
        SET @j = @j + 1;
    END

    COMMIT TRANSACTION;

    IF @j % 100000 = 1
        PRINT 'EMPG_HIS 진행: ' + CAST(@j - 1 AS VARCHAR) + ' / ' + CAST(@totalRows2 AS VARCHAR);
END

PRINT 'EMPG_HIS INSERT 완료: ' + CAST(@totalRows2 AS VARCHAR) + '건';
GO

-- ------------------------------------------------------------
-- 4. 데이터 확인
-- ------------------------------------------------------------
SELECT 'EMPG'     AS TB, COUNT(*) AS CNT, MIN(UPDATE_TIME) AS MIN_DT, MAX(UPDATE_TIME) AS MAX_DT FROM EMPG
UNION ALL
SELECT 'EMPG_HIS' AS TB, COUNT(*) AS CNT, MIN(UPDATE_TIME) AS MIN_DT, MAX(UPDATE_TIME) AS MAX_DT FROM EMPG_HIS;
GO
