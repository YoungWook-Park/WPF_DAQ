// Phase A: OP220 서브공정 DTO
//
// OP200 행을 시리얼로 조회한 뒤 UPDATE 할 필드만 보유한다.
//
// APD31~33: Guiding Press Fitting / Short Distance
//   APD31 = GuidingPressFitting_Judge    (판정,   D2360) ← TotalJudge 재계산 대상
//   APD32 = Guiding_ShortDistance_Check  (측정값, D2361)
//   APD33 = Guiding_ShortDistance_Judge  (판정,   D2363) ← TotalJudge 재계산 대상
//
// SP31~36: OP210 과 동일 설정 파라미터 구간을 공유한다.

namespace ConSight.DAQ.Device
{
    public sealed class Op220ProcessDto
    {
        // ── 식별/상태 ───────────────────────────────────────────────────────
        public DateTime UpdateTime { get; init; }
        public string   Repair     { get; init; } = string.Empty;
        public string   Model      { get; init; } = string.Empty;
        public string   Serial     { get; init; } = string.Empty;

        // ── APD31~33 ───────────────────────────────────────────────────────
        public string Apd31 { get; init; } = string.Empty; // GuidingPressFitting_Judge
        public string Apd32 { get; init; } = string.Empty; // Guiding_ShortDistance_Check
        public string Apd33 { get; init; } = string.Empty; // Guiding_ShortDistance_Judge

        // ── SP31~36 (OP210 과 공유) ────────────────────────────────────────
        public string Sp31 { get; init; } = string.Empty;
        public string Sp32 { get; init; } = string.Empty;
        public string Sp33 { get; init; } = string.Empty;
        public string Sp34 { get; init; } = string.Empty;
        public string Sp35 { get; init; } = string.Empty;
        public string Sp36 { get; init; } = string.Empty;

        internal IEnumerable<string> Judges => [Apd31, Apd33];
    }
}
