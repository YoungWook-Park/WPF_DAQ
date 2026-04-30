// Phase B: OP220 공정 파서
//
// PLC 수신 배열 2개(공정 데이터 D2300, 설정 데이터 D1900)를
// Op220ProcessDto 로 변환한다.
// SP31~36 설정값은 OP210 과 동일하게 D1900 배열 오프셋 70~82 에 위치한다.
//
// 공정 배열 레이아웃 (D2300 기준 오프셋):
//   [0]       D2300 BackUp_Start
//   [1]       D2301 PC_Complete_Flag
//   [2]       D2302 Repair
//   [10..19]  D2310 Model (10워드 문자열)
//   [20..39]  D2320 Serial (20워드 문자열)
//   [60]      D2360 GuidingPressFitting_Judge
//   [61]      D2361 Guiding_ShortDist_Check   1워드/10000
//   [63]      D2363 Guiding_ShortDist_Judge
//
// 설정 배열 레이아웃: Op210Parser 와 동일 (D1900 오프셋 70~82)

using System;
using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    public sealed class Op220Parser
    {
        public Op220ProcessDto Parse(short[] proc, short[] settingOp200)
        {
            if (proc         == null) throw new ArgumentNullException(nameof(proc));
            if (settingOp200 == null) throw new ArgumentNullException(nameof(settingOp200));

            return new Op220ProcessDto
            {
                UpdateTime = DateTime.Now,
                Repair     = PlcParseHelper.Repair(proc[2]),
                Model      = PlcDataConverter.ShortToString(proc, 10, 10),
                Serial     = PlcParseHelper.Serial(proc, 20, 20),

                // ── APD31~33 : Guiding Press Fitting / Short Distance ─────
                Apd31 = PlcParseHelper.Judge(proc[60]),            // GuidingPressFitting_Judge
                Apd32 = ((double)proc[61] / 10000).ToString("0.0000"), // Guiding_ShortDist_Check 1워드
                Apd33 = PlcParseHelper.Judge(proc[63]),            // Guiding_ShortDist_Judge

                // ── SP31~36 : Guiding / RunOut 상하한 (D1900 배열 공유) ───
                Sp31 = Fmt00(settingOp200, 70),   // RunOutCheck_Input_Lower
                Sp32 = Fmt00(settingOp200, 72),   // RunOutCheck_Input_Upper
                Sp33 = PlcParseHelper.F4Int(settingOp200, 74),    // RunOutCheck_Space_Lower
                Sp34 = PlcParseHelper.F4Int(settingOp200, 76),    // RunOutCheck_Space_Upper
                Sp35 = PlcParseHelper.F4Int(settingOp200, 80),    // Guiding_ShortDist_Lower
                Sp36 = PlcParseHelper.F4Int(settingOp200, 82),    // Guiding_ShortDist_Upper
            };
        }

        private static string Fmt00(short[] d, int offset)
            => (PlcDataConverter.shortToInt(d, offset) / 100.0).ToString("00.0");
    }
}
