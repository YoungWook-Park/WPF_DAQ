-- ============================================================
-- 현재 시간 기준 EMPG 테스트 데이터 1건 INSERT
-- 윈도우 스케줄러 테스트용 (RESULT_ID 접두사: SCHED-)
-- ============================================================
DECLARE @now       DATETIME    = GETDATE();
DECLARE @resultId  NVARCHAR(50)= N'SCHED-' + FORMAT(@now, 'yyyyMMddHHmmss') + N'-' + RIGHT('000' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS NVARCHAR(3)), 3);
DECLARE @judge     NVARCHAR(10)= CASE WHEN ABS(CHECKSUM(NEWID())) % 10 < 9 THEN N'OK' ELSE N'NG' END;
-- STS_MODEL_TB FK 제약으로 반드시 존재하는 MODEL 값 사용
DECLARE @model     NVARCHAR(50)= N'MODEL_A';

INSERT INTO EMPG (
    RESULT_ID, UPDATE_TIME, REPAIR, MODEL, TOTAL_JUDGE,
    MAT_SERIAL01, MAT_SERIAL02, CREATE_DAYTIME, OP200_TOTAL_JUDGE,
    APD01, APD02, APD03, APD04, APD05, APD06, APD07, APD08,
    APD09, APD10, APD11, APD12, APD13, APD14, APD15, APD16,
    APD17, APD18, APD19, APD20, APD21, APD22, APD23, APD24,
    APD25, APD26, APD27, APD28, APD29, APD30, APD31, APD32,
    APD33, APD34, APD35, APD36, APD37, APD38, APD39, APD40,
    APD41, APD42, APD43, APD44,
    SP01,  SP02,  SP03,  SP04,  SP05,  SP06,  SP07,  SP08,  SP09,  SP10,
    SP11,  SP12,  SP13,  SP14,  SP15,  SP16,  SP17,  SP18,  SP19,  SP20,
    SP21,  SP22,  SP23,  SP24,  SP25,  SP26,  SP27,  SP28,  SP29,  SP30,
    SP31,  SP32,  SP33,  SP34,  SP35,  SP36,  SP37,  SP38,  SP39,  SP40,
    SP41,  SP42,  SP43,  SP44,  SP45,  SP46,  SP47,  SP48,  SP49,  SP50
)
VALUES (
    @resultId, @now, N'0', @model, @judge,
    N'SFT-' + FORMAT(@now, 'yyyyMMddHHmmss'),
    N'GEA-' + FORMAT(@now, 'yyyyMMddHHmmss'),
    @now, @judge,
    -- APD01~APD44: 스페이서/베어링/스냅링/엔드플레이트/가이드링/SOCP 계측값 mock
    N'12.34', N'5.67',   N'12.10', N'5.55',   N'12.50', N'5.80',   @judge, N'1',
    N'11.90', N'5.60',   N'12.20', N'5.70',   N'12.30', N'5.65',   @judge, N'2',
    N'3.10',  N'3.08',   N'3.09', N'A',       @judge,   N'4.95',   @judge, @judge,
    N'0.05',  @judge,    N'0.02', @judge,     N'0.03',  @judge,    @judge, N'45.2',
    @judge,   @judge,    @judge,  N'10.5',    N'4.2',   N'10.3',   N'4.1', N'10.6',
    N'4.3',   @judge,    N'0.8',  @judge,
    -- SP01~SP50: 모두 빈 값 (예비 파라미터)
    N'', N'', N'', N'', N'', N'', N'', N'', N'', N'',
    N'', N'', N'', N'', N'', N'', N'', N'', N'', N'',
    N'', N'', N'', N'', N'', N'', N'', N'', N'', N'',
    N'', N'', N'', N'', N'', N'', N'', N'', N'', N'',
    N'', N'', N'', N'', N'', N'', N'', N'', N'', N''
);

PRINT N'------------------------------------------------------------';
PRINT N'INSERT OK: ' + @resultId;
PRINT N'------------------------------------------------------------';

-- INSERT된 행 전체 컬럼 확인 (로그에서 실제 저장값 검증용)
SELECT
    -- 식별/기본 정보
    RESULT_ID, UPDATE_TIME, REPAIR, MODEL, TOTAL_JUDGE,
    MAT_SERIAL01, MAT_SERIAL02, CREATE_DAYTIME, OP200_TOTAL_JUDGE,
    -- 스페이서 (APD01~08)
    APD01 AS GR_R1_Load,      APD02 AS GR_R1_Stroke,
    APD03 AS GR_R2_Load,      APD04 AS GR_R2_Stroke,
    APD05 AS GR_P_Load,       APD06 AS GR_P_Stroke,
    APD07 AS GR_Judge,        APD08 AS GR_IndexNo,
    -- 베어링 (APD09~16)
    APD09 AS BR_R1_Load,      APD10 AS BR_R1_Stroke,
    APD11 AS BR_R2_Load,      APD12 AS BR_R2_Stroke,
    APD13 AS BR_P_Load,       APD14 AS BR_P_Stroke,
    APD15 AS BR_Judge,        APD16 AS BR_IndexNo,
    -- 스냅링 (APD17~24)
    APD17 AS SR_Groove_0Deg,  APD18 AS SR_Groove_180Deg,
    APD19 AS SR_GradeData,    APD20 AS SR_Grade,
    APD21 AS SR_GrooveJudge,  APD22 AS SR_HeighThick,
    APD23 AS SR_HeighJudge,   APD24 AS SR_Judge,
    -- 엔드플레이트 (APD25~26)
    APD25 AS EndPlate_Data,   APD26 AS EndPlate_Judge,
    -- RunOut (APD27~30)
    APD27 AS RunOut_Input,    APD28 AS RunOut_InputJudge,
    APD29 AS RunOut_Space,    APD30 AS RunOut_SpaceJudge,
    -- 가이드링 (APD31~33)
    APD31 AS Guiding_PressJudge,
    APD32 AS Guiding_ShortDist,
    APD33 AS Guiding_ShortJudge,
    -- LOTITE (APD34~35)
    APD34 AS Lotite_DispJudge, APD35 AS Lotite_VisionJudge,
    -- SOCP (APD36~44)
    APD36 AS SOCP_R1_Load,    APD37 AS SOCP_R1_Stroke,
    APD38 AS SOCP_R2_Load,    APD39 AS SOCP_R2_Stroke,
    APD40 AS SOCP_P_Load,     APD41 AS SOCP_P_Stroke,
    APD42 AS SOCP_Judge,
    APD43 AS SOC_Check,       APD44 AS SOC_CheckJudge
FROM EMPG
WHERE RESULT_ID = @resultId;
