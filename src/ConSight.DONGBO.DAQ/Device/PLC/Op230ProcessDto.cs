// Phase A: OP230 서브공정 DTO
//
// OP200 행을 시리얼로 조회한 뒤 UPDATE 할 필드만 보유한다.
// OP230 은 시리얼 2개(Serial01/Serial02)를 보유하며 OP200 과 동일한 방식으로 조회한다.
//
// APD34~44: Lotite / Shaft Oil Cap Press·Check
//   APD34 = Lotite_Dispensing_Judge    (판정,   D2460) ← TotalJudge 재계산 대상
//   APD35 = Lotite_Vision_Judge        (판정,   D2461) ← TotalJudge 재계산 대상
//   APD36 = SOCP_R1_Load              (측정값, D2462)
//   APD37 = SOCP_R1_Stroke            (측정값, D2463)
//   APD38 = SOCP_R2_Load              (측정값, D2465)
//   APD39 = SOCP_R2_Stroke            (측정값, D2466)
//   APD40 = SOCP_P_Load               (측정값, D2468)
//   APD41 = SOCP_P_Stroke             (측정값, D2469)
//   APD42 = SOCP_Judge                (판정,   D2471) ← TotalJudge 재계산 대상
//   APD43 = SOC_Check                 (측정값, D2472)
//   APD44 = SOC_Check_Judge           (판정,   D2474) ← TotalJudge 재계산 대상
//
// SP37~50: OP230 설정 파라미터

namespace ConSight.DAQ.Device
{
    public sealed class Op230ProcessDto
    {
        // ── 식별/상태 ───────────────────────────────────────────────────────
        public DateTime UpdateTime  { get; init; }
        public string   Repair      { get; init; } = string.Empty;
        public string   Model       { get; init; } = string.Empty;
        // OP230 은 시리얼 2개 (MAT_SERIAL01 / MAT_SERIAL02 중 하나로 EMPG 행 조회)
        public string   Serial01    { get; init; } = string.Empty;
        public string   Serial02    { get; init; } = string.Empty;

        // ── APD34~44 ───────────────────────────────────────────────────────
        public string Apd34 { get; init; } = string.Empty; // Lotite_Dispensing_Judge
        public string Apd35 { get; init; } = string.Empty; // Lotite_Vision_Judge
        public string Apd36 { get; init; } = string.Empty; // SOCP_R1_Load
        public string Apd37 { get; init; } = string.Empty; // SOCP_R1_Stroke
        public string Apd38 { get; init; } = string.Empty; // SOCP_R2_Load
        public string Apd39 { get; init; } = string.Empty; // SOCP_R2_Stroke
        public string Apd40 { get; init; } = string.Empty; // SOCP_P_Load
        public string Apd41 { get; init; } = string.Empty; // SOCP_P_Stroke
        public string Apd42 { get; init; } = string.Empty; // SOCP_Judge
        public string Apd43 { get; init; } = string.Empty; // SOC_Check
        public string Apd44 { get; init; } = string.Empty; // SOC_Check_Judge

        // ── SP37~50 ────────────────────────────────────────────────────────
        public string Sp37 { get; init; } = string.Empty;
        public string Sp38 { get; init; } = string.Empty;
        public string Sp39 { get; init; } = string.Empty;
        public string Sp40 { get; init; } = string.Empty;
        public string Sp41 { get; init; } = string.Empty;
        public string Sp42 { get; init; } = string.Empty;
        public string Sp43 { get; init; } = string.Empty;
        public string Sp44 { get; init; } = string.Empty;
        public string Sp45 { get; init; } = string.Empty;
        public string Sp46 { get; init; } = string.Empty;
        public string Sp47 { get; init; } = string.Empty;
        public string Sp48 { get; init; } = string.Empty;
        public string Sp49 { get; init; } = string.Empty;
        public string Sp50 { get; init; } = string.Empty;

        internal IEnumerable<string> Judges => [Apd34, Apd35, Apd42, Apd44];
    }
}
