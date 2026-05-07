-- ============================================================
-- 07: EMPG / EMPG_HIS 각 50만 건 벌크 INSERT (성능 측정용)
--
-- 실행 전 전제:
--   STS_MODEL_TB 에 MODEL_A, MODEL_B, MODEL_C 가 존재해야 한다.
--   아래 Step 0 에서 없으면 자동 INSERT 한다.
--
-- 시리얼 규칙:
--   EMPG     MAT_SERIAL01 = 'SFT-NNNNNN'  MAT_SERIAL02 = 'GEA-NNNNNN'
--   EMPG_HIS MAT_SERIAL01 = 'SFH-NNNNNN'  MAT_SERIAL02 = 'GEH-NNNNNN'
--   N = 1 ~ 500000 (6자리 0-패딩)
-- ============================================================
USE [DB_eM]
GO
SET NOCOUNT ON;
GO

-- ============================================================
-- Step 0: 모델 사전 보장 (FK 위반 방지)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM STS_MODEL_TB WHERE MODEL = 'MODEL_A')
    INSERT INTO STS_MODEL_TB (MODEL) VALUES ('MODEL_A');
IF NOT EXISTS (SELECT 1 FROM STS_MODEL_TB WHERE MODEL = 'MODEL_B')
    INSERT INTO STS_MODEL_TB (MODEL) VALUES ('MODEL_B');
IF NOT EXISTS (SELECT 1 FROM STS_MODEL_TB WHERE MODEL = 'MODEL_C')
    INSERT INTO STS_MODEL_TB (MODEL) VALUES ('MODEL_C');
GO

-- ============================================================
-- Step 1: EMPG 50만 건 INSERT
--   UPDATE_TIME  : 2024-01-01 ~ 2025-05-01 (약 16개월) 균등 분산
--   TOTAL_JUDGE  : n % 10 = 0 → 'NG', 나머지 → 'OK'  (10% NG)
--   MODEL        : n % 3 으로 3개 모델 순환
-- ============================================================
PRINT N'[EMPG] 50만 건 INSERT 시작 - ' + CONVERT(NVARCHAR, GETDATE(), 121);

;WITH
L0 AS (SELECT 1 c UNION ALL SELECT 1),
L1 AS (SELECT 1 c FROM L0 a CROSS JOIN L0 b),
L2 AS (SELECT 1 c FROM L1 a CROSS JOIN L1 b),
L3 AS (SELECT 1 c FROM L2 a CROSS JOIN L2 b),
L4 AS (SELECT 1 c FROM L3 a CROSS JOIN L3 b),
L5 AS (SELECT 1 c FROM L4 a CROSS JOIN L4 b),
Nums AS (
    SELECT TOP 500000
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM L5
)
INSERT INTO EMPG (
    RESULT_ID, UPDATE_TIME, REPAIR, MODEL, CYCLETIME, CREATE_DAYTIME,
    MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, OP200_TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26,
    APD27, APD28, APD29, APD30,
    APD31, APD32, APD33,
    APD34, APD35, APD36, APD37, APD38, APD39, APD40, APD41, APD42, APD43, APD44,
    SP01,  SP02,  SP03,  SP04,  SP05,  SP06,  SP07,  SP08,  SP09,  SP10,
    SP11,  SP12,  SP13,  SP14,  SP15,  SP16,  SP17,  SP18,  SP19,  SP20,
    SP21,  SP22,  SP23,  SP24,  SP25,  SP26,  SP27,  SP28,  SP29,  SP30,
    SP31,  SP32,  SP33,  SP34,  SP35,  SP36,
    SP37,  SP38,  SP39,  SP40,  SP41,  SP42,  SP43,  SP44,  SP45,  SP46,
    SP47,  SP48,  SP49,  SP50
)
SELECT
    -- 식별자
    N'R-BULK-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)        AS RESULT_ID,
    DATEADD(SECOND, -(500001 - n) * 2, '2025-05-01T00:00:00')       AS UPDATE_TIME,
    N'0'                                                             AS REPAIR,
    CASE n % 3
        WHEN 0 THEN N'MODEL_A'
        WHEN 1 THEN N'MODEL_B'
        ELSE        N'MODEL_C'
    END                                                              AS MODEL,
    0                                                                AS CYCLETIME,
    CONVERT(NVARCHAR(30),
        DATEADD(SECOND, -(500001 - n) * 2, '2025-05-01T00:00:00'),
        121)                                                         AS CREATE_DAYTIME,
    -- 시리얼 (ShaftSerial → MAT_SERIAL01, GearSerial → MAT_SERIAL02)
    N'SFT-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)           AS MAT_SERIAL01,
    N'GEA-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)           AS MAT_SERIAL02,
    -- 판정
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END                  AS TOTAL_JUDGE,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END                  AS OP200_TOTAL_JUDGE,
    -- APD01~26 (OP200 측정값 mock)
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD01,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD02,
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD03,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD04,
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD05,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD06,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD07,
    CAST(n % 5 + 1 AS NVARCHAR(2))               AS APD08,
    CAST(11.9 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD09,
    CAST( 5.6 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD10,
    CAST(12.2 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD11,
    CAST( 5.7 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD12,
    CAST(12.3 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD13,
    CAST( 5.65 + (n % 5) * 0.1 AS NVARCHAR(10)) AS APD14,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD15,
    CAST(n % 5 + 1 AS NVARCHAR(2))               AS APD16,
    CAST( 3.1 + (n %  5) * 0.01 AS NVARCHAR(10)) AS APD17,
    CAST( 3.08 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD18,
    CAST( 3.09 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD19,
    CHAR(65 + (n % 4))                            AS APD20,   -- A~D
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD21,
    CAST( 4.95 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD22,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD23,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD24,
    CAST( 0.05 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD25,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD26,
    -- APD27~44 (서브공정 — 빈값)
    N'' AS APD27, N'' AS APD28, N'' AS APD29, N'' AS APD30,
    N'' AS APD31, N'' AS APD32, N'' AS APD33,
    N'' AS APD34, N'' AS APD35, N'' AS APD36, N'' AS APD37,
    N'' AS APD38, N'' AS APD39, N'' AS APD40, N'' AS APD41,
    N'' AS APD42, N'' AS APD43, N'' AS APD44,
    -- SP01~50 (설정 스냅샷 — 빈값)
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N''
FROM Nums;

PRINT N'[EMPG] 50만 건 INSERT 완료 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
PRINT N'[EMPG] 현재 행 수: ' + CAST((SELECT COUNT(*) FROM EMPG) AS NVARCHAR);
GO

-- ============================================================
-- Step 2: EMPG_HIS 50만 건 INSERT
--   UPDATE_TIME  : 2022-01-01 ~ 2023-12-31 (오래된 이력 데이터)
--   시리얼       : SFH-NNNNNN / GEH-NNNNNN (EMPG 와 겹치지 않음)
-- ============================================================
PRINT N'[EMPG_HIS] 50만 건 INSERT 시작 - ' + CONVERT(NVARCHAR, GETDATE(), 121);

;WITH
L0 AS (SELECT 1 c UNION ALL SELECT 1),
L1 AS (SELECT 1 c FROM L0 a CROSS JOIN L0 b),
L2 AS (SELECT 1 c FROM L1 a CROSS JOIN L1 b),
L3 AS (SELECT 1 c FROM L2 a CROSS JOIN L2 b),
L4 AS (SELECT 1 c FROM L3 a CROSS JOIN L3 b),
L5 AS (SELECT 1 c FROM L4 a CROSS JOIN L4 b),
Nums AS (
    SELECT TOP 500000
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM L5
)
INSERT INTO EMPG_HIS (
    RESULT_ID, UPDATE_TIME, REPAIR, MODEL, CYCLETIME, CREATE_DAYTIME,
    MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, OP200_TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26,
    APD27, APD28, APD29, APD30,
    APD31, APD32, APD33,
    APD34, APD35, APD36, APD37, APD38, APD39, APD40, APD41, APD42, APD43, APD44,
    SP01,  SP02,  SP03,  SP04,  SP05,  SP06,  SP07,  SP08,  SP09,  SP10,
    SP11,  SP12,  SP13,  SP14,  SP15,  SP16,  SP17,  SP18,  SP19,  SP20,
    SP21,  SP22,  SP23,  SP24,  SP25,  SP26,  SP27,  SP28,  SP29,  SP30,
    SP31,  SP32,  SP33,  SP34,  SP35,  SP36,
    SP37,  SP38,  SP39,  SP40,  SP41,  SP42,  SP43,  SP44,  SP45,  SP46,
    SP47,  SP48,  SP49,  SP50
)
SELECT
    N'R-HIS-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)         AS RESULT_ID,
    DATEADD(SECOND, -(500001 - n) * 2, '2023-12-31T00:00:00')       AS UPDATE_TIME,
    N'0'                                                             AS REPAIR,
    CASE n % 3
        WHEN 0 THEN N'MODEL_A'
        WHEN 1 THEN N'MODEL_B'
        ELSE        N'MODEL_C'
    END                                                              AS MODEL,
    0                                                                AS CYCLETIME,
    CONVERT(NVARCHAR(30),
        DATEADD(SECOND, -(500001 - n) * 2, '2023-12-31T00:00:00'),
        121)                                                         AS CREATE_DAYTIME,
    N'SFH-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)           AS MAT_SERIAL01,
    N'GEH-' + RIGHT('000000' + CAST(n AS NVARCHAR(6)), 6)           AS MAT_SERIAL02,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END                  AS TOTAL_JUDGE,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END                  AS OP200_TOTAL_JUDGE,
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD01,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD02,
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD03,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD04,
    CAST(12.0 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD05,
    CAST( 5.5 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD06,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD07,
    CAST(n % 5 + 1 AS NVARCHAR(2))               AS APD08,
    CAST(11.9 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD09,
    CAST( 5.6 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD10,
    CAST(12.2 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD11,
    CAST( 5.7 + (n %  5) * 0.1 AS NVARCHAR(10)) AS APD12,
    CAST(12.3 + (n % 10) * 0.1 AS NVARCHAR(10)) AS APD13,
    CAST( 5.65 + (n % 5) * 0.1 AS NVARCHAR(10)) AS APD14,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD15,
    CAST(n % 5 + 1 AS NVARCHAR(2))               AS APD16,
    CAST( 3.1 + (n %  5) * 0.01 AS NVARCHAR(10)) AS APD17,
    CAST( 3.08 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD18,
    CAST( 3.09 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD19,
    CHAR(65 + (n % 4))                            AS APD20,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD21,
    CAST( 4.95 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD22,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD23,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD24,
    CAST( 0.05 + (n % 5) * 0.01 AS NVARCHAR(10)) AS APD25,
    CASE WHEN n % 10 = 0 THEN N'NG' ELSE N'OK' END AS APD26,
    N'' AS APD27, N'' AS APD28, N'' AS APD29, N'' AS APD30,
    N'' AS APD31, N'' AS APD32, N'' AS APD33,
    N'' AS APD34, N'' AS APD35, N'' AS APD36, N'' AS APD37,
    N'' AS APD38, N'' AS APD39, N'' AS APD40, N'' AS APD41,
    N'' AS APD42, N'' AS APD43, N'' AS APD44,
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N'',N'',N'',N'',N'',N'',N'',
    N'',N'',N'',N''
FROM Nums;

PRINT N'[EMPG_HIS] 50만 건 INSERT 완료 - ' + CONVERT(NVARCHAR, GETDATE(), 121);
PRINT N'[EMPG_HIS] 현재 행 수: ' + CAST((SELECT COUNT(*) FROM EMPG_HIS) AS NVARCHAR);
GO

-- ============================================================
-- 최종 확인
-- ============================================================
SELECT
    'EMPG'     AS [테이블],
    COUNT(*)   AS [행 수],
    MIN(UPDATE_TIME) AS [최소 UPDATE_TIME],
    MAX(UPDATE_TIME) AS [최대 UPDATE_TIME]
FROM EMPG
UNION ALL
SELECT
    'EMPG_HIS',
    COUNT(*),
    MIN(UPDATE_TIME),
    MAX(UPDATE_TIME)
FROM EMPG_HIS;
GO
