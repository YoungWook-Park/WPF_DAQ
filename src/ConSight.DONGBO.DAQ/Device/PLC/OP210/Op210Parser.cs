// Phase B: OP210 공정 파서
//
// PLC 수신 배열 2개(공정 데이터 D2200, 설정 데이터 D1900)를
// Op210ProcessDto 로 변환한다.
// SP31~36 설정값은 OP200 설정 배열(D1900) 의 오프셋 70~82 에 위치한다.
//
// 공정 배열 레이아웃 (D2200 기준 오프셋):
//   [0]       D2200 BackUp_Start
//   [1]       D2201 PC_Complete_Flag
//   [2]       D2202 Repair
//   [10..19]  D2210 Model (10워드 문자열)
//   [20..39]  D2220 Serial (20워드 문자열)
//   [60]      D2260 RunOutCheck_Input      1워드/10000
//   [62]      D2262 RunOutCheck_Input_Judge
//   [63..64]  D2263 RunOutCheck_Space      2워드Int/10000
//   [65]      D2265 RunOutCheck_Space_Judge
//
// 설정 배열 레이아웃 (D1900 기준 오프셋, OP200 설정 배열과 동일):
//   SP31  [70..71]  D1970 RunOutCheck_Input_Lower   2워드Int/100  "00.0"
//   SP32  [72..73]  D1972 RunOutCheck_Input_Upper   2워드Int/100  "00.0"
//   SP33  [74..75]  D1974 RunOutCheck_Space_Lower   2워드Int/10000
//   SP34  [76..77]  D1976 RunOutCheck_Space_Upper   2워드Int/10000
//   SP35  [80..81]  D1980 Guiding_ShortDist_Lower   2워드Int/10000
//   SP36  [82..83]  D1982 Guiding_ShortDist_Upper   2워드Int/10000

using System;
using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    public sealed class Op210Parser
    {
        public Op210ProcessDto Parse(short[] proc, short[] settingOp200)
        {
            if (proc           == null) throw new ArgumentNullException(nameof(proc));
            if (settingOp200   == null) throw new ArgumentNullException(nameof(settingOp200));

            return new Op210ProcessDto
            {
                UpdateTime = DateTime.Now,
                Repair     = PlcParseHelper.Repair(proc[2]),
                Model      = PlcDataConverter.ShortToString(proc, 10, 10),
                Serial     = PlcParseHelper.Serial(proc, 20, 20),

                // ── APD27~30 : RunOut Check ────────────────────────────────
                Apd27 = PlcParseHelper.F4Int(proc, 60),           // RunOutCheck_Input (단일 측정 → 2워드)
                Apd28 = PlcParseHelper.Judge(proc[62]),            // RunOutCheck_Input_Judge
                Apd29 = PlcParseHelper.F4Int(proc, 63),           // RunOutCheck_Space
                Apd30 = PlcParseHelper.Judge(proc[65]),            // RunOutCheck_Space_Judge

                // ── SP31~36 : RunOut / Guiding 상하한 (D1900 배열 공유) ───
                Sp31 = Fmt00(settingOp200, 70),   // RunOutCheck_Input_Lower
                Sp32 = Fmt00(settingOp200, 72),   // RunOutCheck_Input_Upper
                Sp33 = PlcParseHelper.F4Int(settingOp200, 74),    // RunOutCheck_Space_Lower
                Sp34 = PlcParseHelper.F4Int(settingOp200, 76),    // RunOutCheck_Space_Upper
                Sp35 = PlcParseHelper.F4Int(settingOp200, 80),    // Guiding_ShortDist_Lower
                Sp36 = PlcParseHelper.F4Int(settingOp200, 82),    // Guiding_ShortDist_Upper
            };
        }

        // RunOutCheck 상하한은 "00.0" 포맷 (÷100)
        private static string Fmt00(short[] d, int offset)
            => (PlcDataConverter.shortToInt(d, offset) / 100.0).ToString("00.0");
    }
}
