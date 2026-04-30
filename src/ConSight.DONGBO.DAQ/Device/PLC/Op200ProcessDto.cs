// Phase A: OP200 공정 DTO
//
// PLC 수신 즉시 파싱되는 불변 데이터 객체.
// DB Insert / CSV Write / EventBus Publish 공통 파라미터로 사용한다.
//
// APD01~26: OP200 공정 측정값
//   APD01~08  : Guide Ring Spacer (R1/R2/P Load·Stroke, Judge, IndexNo)
//   APD09~16  : Bearing           (R1/R2/P Load·Stroke, Judge, IndexNo)
//   APD17~24  : Snap Ring Groove / Heigh
//   APD25~26  : End Plate
//
// SP01~30: OP200 설정 스냅샷 (공정 완료 시점의 설정값)
//   SP01~12   : Guide Ring Spacer 상하한
//   SP13~24   : Bearing 상하한
//   SP25~28   : Snap Ring Grade / Heigh 상하한
//   SP29~30   : End Plate 상하한
//
// APD27~44, SP31~50 는 OP210~230 이 EMPG 행에 직접 UPDATE 한다.

namespace ConSight.DAQ.Device
{
    public sealed class Op200ProcessDto
    {
        // ── 식별/상태 ───────────────────────────────────────────────────────
        public DateTime UpdateTime  { get; init; }
        public string   Repair      { get; init; } = string.Empty;
        public string   Model       { get; init; } = string.Empty;
        public string   ShaftSerial { get; init; } = string.Empty;
        public string   GearSerial  { get; init; } = string.Empty;
        public string   TotalJudge  { get; init; } = string.Empty;

        // ── APD01~08 : Guide Ring Spacer ───────────────────────────────────
        public string Apd01 { get; init; } = string.Empty; // GR_R1_Load
        public string Apd02 { get; init; } = string.Empty; // GR_R1_Stroke
        public string Apd03 { get; init; } = string.Empty; // GR_R2_Load
        public string Apd04 { get; init; } = string.Empty; // GR_R2_Stroke
        public string Apd05 { get; init; } = string.Empty; // GR_P_Load
        public string Apd06 { get; init; } = string.Empty; // GR_P_Stroke
        public string Apd07 { get; init; } = string.Empty; // GR_Judge
        public string Apd08 { get; init; } = string.Empty; // GR_IndexNo

        // ── APD09~16 : Bearing ─────────────────────────────────────────────
        public string Apd09 { get; init; } = string.Empty; // BR_R1_Load
        public string Apd10 { get; init; } = string.Empty; // BR_R1_Stroke
        public string Apd11 { get; init; } = string.Empty; // BR_R2_Load
        public string Apd12 { get; init; } = string.Empty; // BR_R2_Stroke
        public string Apd13 { get; init; } = string.Empty; // BR_P_Load
        public string Apd14 { get; init; } = string.Empty; // BR_P_Stroke
        public string Apd15 { get; init; } = string.Empty; // BR_Judge
        public string Apd16 { get; init; } = string.Empty; // BR_IndexNo

        // ── APD17~24 : Snap Ring ───────────────────────────────────────────
        public string Apd17 { get; init; } = string.Empty; // SR_Groove_000Deg
        public string Apd18 { get; init; } = string.Empty; // SR_Groove_180Deg
        public string Apd19 { get; init; } = string.Empty; // SR_Groove_Grade_Data
        public string Apd20 { get; init; } = string.Empty; // SR_Groove_Grade
        public string Apd21 { get; init; } = string.Empty; // SR_Groove_Judge
        public string Apd22 { get; init; } = string.Empty; // SR_Heigh_Thick
        public string Apd23 { get; init; } = string.Empty; // SR_Heigh_Judge
        public string Apd24 { get; init; } = string.Empty; // SR_Judge

        // ── APD25~26 : End Plate ───────────────────────────────────────────
        public string Apd25 { get; init; } = string.Empty; // EndPlate_Data
        public string Apd26 { get; init; } = string.Empty; // EndPlate_Judge

        // ── SP01~12 : Guide Ring Spacer 상하한 ────────────────────────────
        public string Sp01 { get; init; } = string.Empty; // GR_R1_Load_Lower
        public string Sp02 { get; init; } = string.Empty; // GR_R1_Load_Upper
        public string Sp03 { get; init; } = string.Empty; // GR_R1_Stroke_Lower
        public string Sp04 { get; init; } = string.Empty; // GR_R1_Stroke_Upper
        public string Sp05 { get; init; } = string.Empty; // GR_R2_Load_Lower
        public string Sp06 { get; init; } = string.Empty; // GR_R2_Load_Upper
        public string Sp07 { get; init; } = string.Empty; // GR_R2_Stroke_Lower
        public string Sp08 { get; init; } = string.Empty; // GR_R2_Stroke_Upper
        public string Sp09 { get; init; } = string.Empty; // GR_P_Load_Lower
        public string Sp10 { get; init; } = string.Empty; // GR_P_Load_Upper
        public string Sp11 { get; init; } = string.Empty; // GR_P_Stroke_Lower
        public string Sp12 { get; init; } = string.Empty; // GR_P_Stroke_Upper

        // ── SP13~24 : Bearing 상하한 ───────────────────────────────────────
        public string Sp13 { get; init; } = string.Empty; // BR_R1_Load_Lower
        public string Sp14 { get; init; } = string.Empty; // BR_R1_Load_Upper
        public string Sp15 { get; init; } = string.Empty; // BR_R1_Stroke_Lower
        public string Sp16 { get; init; } = string.Empty; // BR_R1_Stroke_Upper
        public string Sp17 { get; init; } = string.Empty; // BR_R2_Load_Lower
        public string Sp18 { get; init; } = string.Empty; // BR_R2_Load_Upper
        public string Sp19 { get; init; } = string.Empty; // BR_R2_Stroke_Lower
        public string Sp20 { get; init; } = string.Empty; // BR_R2_Stroke_Upper
        public string Sp21 { get; init; } = string.Empty; // BR_P_Load_Lower
        public string Sp22 { get; init; } = string.Empty; // BR_P_Load_Upper
        public string Sp23 { get; init; } = string.Empty; // BR_P_Stroke_Lower
        public string Sp24 { get; init; } = string.Empty; // BR_P_Stroke_Upper

        // ── SP25~28 : Snap Ring Grade / Heigh 상하한 ──────────────────────
        public string Sp25 { get; init; } = string.Empty; // SR_Groove_Grade_Lower
        public string Sp26 { get; init; } = string.Empty; // SR_Groove_Grade_Upper
        public string Sp27 { get; init; } = string.Empty; // SR_Heigh_Thick_Lower
        public string Sp28 { get; init; } = string.Empty; // SR_Heigh_Thick_Upper

        // ── SP29~30 : End Plate 상하한 ────────────────────────────────────
        public string Sp29 { get; init; } = string.Empty; // EndPlate_Data_Lower
        public string Sp30 { get; init; } = string.Empty; // EndPlate_Data_Upper
    }
}
