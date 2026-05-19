// Phase A: OP210 서브공정 DTO
//
// OP200 행을 시리얼로 조회한 뒤 UPDATE 할 필드만 보유한다.
//
// APD27~30: RunOut Check
//   APD27 = RunOutCheck_Input       (측정값,  D2260)
//   APD28 = RunOutCheck_Input_Judge (판정,    D2262) ← TotalJudge 재계산 대상
//   APD29 = RunOutCheck_Space       (측정값,  D2263)
//   APD30 = RunOutCheck_Space_Judge (판정,    D2265) ← TotalJudge 재계산 대상
//
// SP31~36: RunOut / Guiding 상하한 (OP200 설정 파서에서 읽음)

namespace ConSight.DAQ.Device
{
    public sealed class Op210ProcessDto
    {
        // ── 식별/상태 ───────────────────────────────────────────────────────
        public DateTime UpdateTime { get; init; }
        public string   Repair     { get; init; } = string.Empty;
        public string   Model      { get; init; } = string.Empty;
        // OP210 은 시리얼 1개 (MAT_SERIAL01 또는 MAT_SERIAL02 로 EMPG 행 조회)
        public string   Serial     { get; init; } = string.Empty;

        // ── APD27~30 ───────────────────────────────────────────────────────
        public string Apd27 { get; init; } = string.Empty; // RunOutCheck_Input
        public string Apd28 { get; init; } = string.Empty; // RunOutCheck_Input_Judge
        public string Apd29 { get; init; } = string.Empty; // RunOutCheck_Space
        public string Apd30 { get; init; } = string.Empty; // RunOutCheck_Space_Judge

        // ── SP31~36 ────────────────────────────────────────────────────────
        public string Sp31 { get; init; } = string.Empty; // RunOutCheck_Input_Lower
        public string Sp32 { get; init; } = string.Empty; // RunOutCheck_Input_Upper
        public string Sp33 { get; init; } = string.Empty; // RunOutCheck_Space_Lower
        public string Sp34 { get; init; } = string.Empty; // RunOutCheck_Space_Upper
        public string Sp35 { get; init; } = string.Empty; // Guiding_ShortDistance_Lower
        public string Sp36 { get; init; } = string.Empty; // Guiding_ShortDistance_Upper

        // TotalJudge 재계산 시 검사할 판정 필드 목록
        // EmpgRow.ApplyOp210() 에서 사용한다.
        internal IEnumerable<string> Judges => [Apd28, Apd30];
    }
}
