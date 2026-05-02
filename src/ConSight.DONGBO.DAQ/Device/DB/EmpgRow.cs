// Phase A: EMPG 행 도메인 DTO
//
// EMPG 테이블 한 행의 전체 상태를 보유하는 도메인 객체.
//
// 역할:
//   1. DB INSERT 단위 (OP200 공정 완료 시 From() 으로 생성)
//   2. DB UPDATE 단위 (OP210~230 서브공정 완료 시 ApplyXxx() 로 필드 갱신)
//   3. EventBus Publish 단위 (NormValueDictionary 를 대체하는 타입 안전 이벤트)
//   4. CSV 1행 단위 (EmpgCsvWriter 가 이 객체를 직접 씀)
//
// TotalJudge 재계산 규칙:
//   - 기존 TotalJudge 가 이미 NG 이면 → NG 유지
//   - 서브공정 판정(Judges) 중 하나라도 "OK" 가 아니면 → NG
//   - 그 외 → OK

using ConSight.DAQ.Device;

namespace ConSight.DAQ.Device.DB
{
    public class EmpgRow
    {
        private const string JudgeOk = "OK";
        private const string JudgeNg = "NG";

        // ── DB 식별자 ──────────────────────────────────────────────────────
        public string   ResultId    { get; set; } = string.Empty;
        public DateTime UpdateTime  { get; set; }
        public string   Repair      { get; set; } = string.Empty;
        public string   Model       { get; set; } = string.Empty;
        public string   MatSerial01 { get; set; } = string.Empty;
        public string   MatSerial02 { get; set; } = string.Empty;
        public string   TotalJudge  { get; set; } = string.Empty;

        // ── APD01~26 : OP200 공정 측정값 ──────────────────────────────────
        public string Apd01 { get; set; } = string.Empty;
        public string Apd02 { get; set; } = string.Empty;
        public string Apd03 { get; set; } = string.Empty;
        public string Apd04 { get; set; } = string.Empty;
        public string Apd05 { get; set; } = string.Empty;
        public string Apd06 { get; set; } = string.Empty;
        public string Apd07 { get; set; } = string.Empty;
        public string Apd08 { get; set; } = string.Empty;
        public string Apd09 { get; set; } = string.Empty;
        public string Apd10 { get; set; } = string.Empty;
        public string Apd11 { get; set; } = string.Empty;
        public string Apd12 { get; set; } = string.Empty;
        public string Apd13 { get; set; } = string.Empty;
        public string Apd14 { get; set; } = string.Empty;
        public string Apd15 { get; set; } = string.Empty;
        public string Apd16 { get; set; } = string.Empty;
        public string Apd17 { get; set; } = string.Empty;
        public string Apd18 { get; set; } = string.Empty;
        public string Apd19 { get; set; } = string.Empty;
        public string Apd20 { get; set; } = string.Empty;
        public string Apd21 { get; set; } = string.Empty;
        public string Apd22 { get; set; } = string.Empty;
        public string Apd23 { get; set; } = string.Empty;
        public string Apd24 { get; set; } = string.Empty;
        public string Apd25 { get; set; } = string.Empty;
        public string Apd26 { get; set; } = string.Empty;

        // ── APD27~30 : OP210 (RunOut Check) ───────────────────────────────
        public string Apd27 { get; set; } = string.Empty;
        public string Apd28 { get; set; } = string.Empty;
        public string Apd29 { get; set; } = string.Empty;
        public string Apd30 { get; set; } = string.Empty;

        // ── APD31~33 : OP220 (Guiding Press Fitting / Short Distance) ─────
        public string Apd31 { get; set; } = string.Empty;
        public string Apd32 { get; set; } = string.Empty;
        public string Apd33 { get; set; } = string.Empty;

        // ── APD34~44 : OP230 (Lotite / Shaft Oil Cap) ─────────────────────
        public string Apd34 { get; set; } = string.Empty;
        public string Apd35 { get; set; } = string.Empty;
        public string Apd36 { get; set; } = string.Empty;
        public string Apd37 { get; set; } = string.Empty;
        public string Apd38 { get; set; } = string.Empty;
        public string Apd39 { get; set; } = string.Empty;
        public string Apd40 { get; set; } = string.Empty;
        public string Apd41 { get; set; } = string.Empty;
        public string Apd42 { get; set; } = string.Empty;
        public string Apd43 { get; set; } = string.Empty;
        public string Apd44 { get; set; } = string.Empty;

        // ── SP01~30 : OP200 설정 스냅샷 ───────────────────────────────────
        public string Sp01 { get; set; } = string.Empty;
        public string Sp02 { get; set; } = string.Empty;
        public string Sp03 { get; set; } = string.Empty;
        public string Sp04 { get; set; } = string.Empty;
        public string Sp05 { get; set; } = string.Empty;
        public string Sp06 { get; set; } = string.Empty;
        public string Sp07 { get; set; } = string.Empty;
        public string Sp08 { get; set; } = string.Empty;
        public string Sp09 { get; set; } = string.Empty;
        public string Sp10 { get; set; } = string.Empty;
        public string Sp11 { get; set; } = string.Empty;
        public string Sp12 { get; set; } = string.Empty;
        public string Sp13 { get; set; } = string.Empty;
        public string Sp14 { get; set; } = string.Empty;
        public string Sp15 { get; set; } = string.Empty;
        public string Sp16 { get; set; } = string.Empty;
        public string Sp17 { get; set; } = string.Empty;
        public string Sp18 { get; set; } = string.Empty;
        public string Sp19 { get; set; } = string.Empty;
        public string Sp20 { get; set; } = string.Empty;
        public string Sp21 { get; set; } = string.Empty;
        public string Sp22 { get; set; } = string.Empty;
        public string Sp23 { get; set; } = string.Empty;
        public string Sp24 { get; set; } = string.Empty;
        public string Sp25 { get; set; } = string.Empty;
        public string Sp26 { get; set; } = string.Empty;
        public string Sp27 { get; set; } = string.Empty;
        public string Sp28 { get; set; } = string.Empty;
        public string Sp29 { get; set; } = string.Empty;
        public string Sp30 { get; set; } = string.Empty;

        // ── SP31~36 : OP210/220 설정 스냅샷 ───────────────────────────────
        public string Sp31 { get; set; } = string.Empty;
        public string Sp32 { get; set; } = string.Empty;
        public string Sp33 { get; set; } = string.Empty;
        public string Sp34 { get; set; } = string.Empty;
        public string Sp35 { get; set; } = string.Empty;
        public string Sp36 { get; set; } = string.Empty;

        // ── SP37~50 : OP230 설정 스냅샷 ───────────────────────────────────
        public string Sp37 { get; set; } = string.Empty;
        public string Sp38 { get; set; } = string.Empty;
        public string Sp39 { get; set; } = string.Empty;
        public string Sp40 { get; set; } = string.Empty;
        public string Sp41 { get; set; } = string.Empty;
        public string Sp42 { get; set; } = string.Empty;
        public string Sp43 { get; set; } = string.Empty;
        public string Sp44 { get; set; } = string.Empty;
        public string Sp45 { get; set; } = string.Empty;
        public string Sp46 { get; set; } = string.Empty;
        public string Sp47 { get; set; } = string.Empty;
        public string Sp48 { get; set; } = string.Empty;
        public string Sp49 { get; set; } = string.Empty;
        public string Sp50 { get; set; } = string.Empty;

        // ── OP200 → EmpgRow 생성 ──────────────────────────────────────────
        // APD27~44, SP31~50 은 서브공정 완료 시 ApplyXxx() 로 채워진다.
        public static EmpgRow From(Op200ProcessDto dto)
        {
            return new EmpgRow
            {
                ResultId    = Guid.NewGuid().ToString("N"),
                UpdateTime  = dto.UpdateTime,
                Repair      = dto.Repair,
                Model       = dto.Model,
                MatSerial01 = dto.ShaftSerial,
                MatSerial02 = dto.GearSerial,
                TotalJudge  = dto.TotalJudge,

                Apd01 = dto.Apd01, Apd02 = dto.Apd02, Apd03 = dto.Apd03, Apd04 = dto.Apd04,
                Apd05 = dto.Apd05, Apd06 = dto.Apd06, Apd07 = dto.Apd07, Apd08 = dto.Apd08,
                Apd09 = dto.Apd09, Apd10 = dto.Apd10, Apd11 = dto.Apd11, Apd12 = dto.Apd12,
                Apd13 = dto.Apd13, Apd14 = dto.Apd14, Apd15 = dto.Apd15, Apd16 = dto.Apd16,
                Apd17 = dto.Apd17, Apd18 = dto.Apd18, Apd19 = dto.Apd19, Apd20 = dto.Apd20,
                Apd21 = dto.Apd21, Apd22 = dto.Apd22, Apd23 = dto.Apd23, Apd24 = dto.Apd24,
                Apd25 = dto.Apd25, Apd26 = dto.Apd26,

                Sp01 = dto.Sp01, Sp02 = dto.Sp02, Sp03 = dto.Sp03, Sp04 = dto.Sp04,
                Sp05 = dto.Sp05, Sp06 = dto.Sp06, Sp07 = dto.Sp07, Sp08 = dto.Sp08,
                Sp09 = dto.Sp09, Sp10 = dto.Sp10, Sp11 = dto.Sp11, Sp12 = dto.Sp12,
                Sp13 = dto.Sp13, Sp14 = dto.Sp14, Sp15 = dto.Sp15, Sp16 = dto.Sp16,
                Sp17 = dto.Sp17, Sp18 = dto.Sp18, Sp19 = dto.Sp19, Sp20 = dto.Sp20,
                Sp21 = dto.Sp21, Sp22 = dto.Sp22, Sp23 = dto.Sp23, Sp24 = dto.Sp24,
                Sp25 = dto.Sp25, Sp26 = dto.Sp26, Sp27 = dto.Sp27, Sp28 = dto.Sp28,
                Sp29 = dto.Sp29, Sp30 = dto.Sp30,
            };
        }

        // ── OP200 필드 병합 ───────────────────────────────────────────────

        /// <summary>
        /// OP200 측정값을 현재 행에 덮어쓴다.
        /// 기존 서브공정 컬럼(APD27~44, SP31~50)은 그대로 유지된다.
        /// </summary>
        public void ApplyOp200(Op200ProcessDto dto)
        {
            UpdateTime  = dto.UpdateTime;
            Repair      = dto.Repair;
            Model       = dto.Model;
            MatSerial01 = dto.ShaftSerial;
            MatSerial02 = dto.GearSerial;
            TotalJudge  = dto.TotalJudge;

            Apd01 = dto.Apd01; Apd02 = dto.Apd02; Apd03 = dto.Apd03; Apd04 = dto.Apd04;
            Apd05 = dto.Apd05; Apd06 = dto.Apd06; Apd07 = dto.Apd07; Apd08 = dto.Apd08;
            Apd09 = dto.Apd09; Apd10 = dto.Apd10; Apd11 = dto.Apd11; Apd12 = dto.Apd12;
            Apd13 = dto.Apd13; Apd14 = dto.Apd14; Apd15 = dto.Apd15; Apd16 = dto.Apd16;
            Apd17 = dto.Apd17; Apd18 = dto.Apd18; Apd19 = dto.Apd19; Apd20 = dto.Apd20;
            Apd21 = dto.Apd21; Apd22 = dto.Apd22; Apd23 = dto.Apd23; Apd24 = dto.Apd24;
            Apd25 = dto.Apd25; Apd26 = dto.Apd26;

            Sp01 = dto.Sp01; Sp02 = dto.Sp02; Sp03 = dto.Sp03; Sp04 = dto.Sp04;
            Sp05 = dto.Sp05; Sp06 = dto.Sp06; Sp07 = dto.Sp07; Sp08 = dto.Sp08;
            Sp09 = dto.Sp09; Sp10 = dto.Sp10; Sp11 = dto.Sp11; Sp12 = dto.Sp12;
            Sp13 = dto.Sp13; Sp14 = dto.Sp14; Sp15 = dto.Sp15; Sp16 = dto.Sp16;
            Sp17 = dto.Sp17; Sp18 = dto.Sp18; Sp19 = dto.Sp19; Sp20 = dto.Sp20;
            Sp21 = dto.Sp21; Sp22 = dto.Sp22; Sp23 = dto.Sp23; Sp24 = dto.Sp24;
            Sp25 = dto.Sp25; Sp26 = dto.Sp26; Sp27 = dto.Sp27; Sp28 = dto.Sp28;
            Sp29 = dto.Sp29; Sp30 = dto.Sp30;
        }

        // ── 서브공정 필드 병합 ─────────────────────────────────────────────

        /// <summary>OP210 측정값을 현재 행에 덮어쓰고 TotalJudge 를 재계산한다.</summary>
        public void ApplyOp210(Op210ProcessDto dto)
        {
            UpdateTime = dto.UpdateTime;

            Apd27 = dto.Apd27; Apd28 = dto.Apd28;
            Apd29 = dto.Apd29; Apd30 = dto.Apd30;

            Sp31 = dto.Sp31; Sp32 = dto.Sp32; Sp33 = dto.Sp33;
            Sp34 = dto.Sp34; Sp35 = dto.Sp35; Sp36 = dto.Sp36;

            RecalcTotalJudge(dto.Judges);
        }

        /// <summary>OP220 측정값을 현재 행에 덮어쓰고 TotalJudge 를 재계산한다.</summary>
        public void ApplyOp220(Op220ProcessDto dto)
        {
            UpdateTime = dto.UpdateTime;

            Apd31 = dto.Apd31; Apd32 = dto.Apd32; Apd33 = dto.Apd33;

            // SP31~36 은 OP210 과 동일 설정 구간 — 최신값으로 갱신
            Sp31 = dto.Sp31; Sp32 = dto.Sp32; Sp33 = dto.Sp33;
            Sp34 = dto.Sp34; Sp35 = dto.Sp35; Sp36 = dto.Sp36;

            RecalcTotalJudge(dto.Judges);
        }

        /// <summary>OP230 측정값을 현재 행에 덮어쓰고 TotalJudge 를 재계산한다.</summary>
        public void ApplyOp230(Op230ProcessDto dto)
        {
            UpdateTime = dto.UpdateTime;

            Apd34 = dto.Apd34; Apd35 = dto.Apd35; Apd36 = dto.Apd36;
            Apd37 = dto.Apd37; Apd38 = dto.Apd38; Apd39 = dto.Apd39;
            Apd40 = dto.Apd40; Apd41 = dto.Apd41; Apd42 = dto.Apd42;
            Apd43 = dto.Apd43; Apd44 = dto.Apd44;

            Sp37 = dto.Sp37; Sp38 = dto.Sp38; Sp39 = dto.Sp39; Sp40 = dto.Sp40;
            Sp41 = dto.Sp41; Sp42 = dto.Sp42; Sp43 = dto.Sp43; Sp44 = dto.Sp44;
            Sp45 = dto.Sp45; Sp46 = dto.Sp46; Sp47 = dto.Sp47; Sp48 = dto.Sp48;
            Sp49 = dto.Sp49; Sp50 = dto.Sp50;

            RecalcTotalJudge(dto.Judges);
        }

        // ── TotalJudge 재계산 ─────────────────────────────────────────────
        // 기존 TotalJudge 가 이미 NG 이거나 새 판정 중 하나라도 OK 가 아니면 → NG
        private void RecalcTotalJudge(IEnumerable<string> newJudges)
        {
            if (TotalJudge != JudgeOk || newJudges.Any(j => j != JudgeOk))
                TotalJudge = JudgeNg;
        }
    }
}
