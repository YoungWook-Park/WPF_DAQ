// Phase B: OP230 공정 파서
//
// PLC 수신 배열 2개(공정 데이터 D2400, 설정 데이터 D1800)를
// Op230ProcessDto 로 변환한다.
//
// 공정 배열 레이아웃 (D2400 기준 오프셋):
//   [0]       D2400 BackUp_Start
//   [1]       D2401 PC_Complete_Flag
//   [2]       D2402 Repair
//   [10..19]  D2410 Model (10워드 문자열)
//   [20..39]  D2420 Serial01 (20워드 문자열)
//   [40..59]  D2440 Serial02 (20워드 문자열)
//   [60]      D2460 Lotite_Dispensing_Judge
//   [61]      D2461 Lotite_Vision_Judge
//   [62]      D2462 SOCP_R1_Load         1워드/100
//   [63..64]  D2463 SOCP_R1_Stroke       2워드Int/100
//   [65]      D2465 SOCP_R2_Load         1워드/100
//   [66..67]  D2466 SOCP_R2_Stroke       2워드Int/100
//   [68]      D2468 SOCP_P_Load          1워드/100
//   [69..70]  D2469 SOCP_P_Stroke        2워드Int/100
//   [71]      D2471 SOCP_Judge
//   [72..73]  D2472 SOC_Check            2워드Int/10000
//   [74]      D2474 SOC_Check_Judge
//
// 설정 배열 레이아웃 (D1800 기준 오프셋):
//   SP37~38  [0..1]   SOCP_R1_Load   Lower/Upper  1워드/100
//   SP39~40  [2..5]   SOCP_R1_Stroke Lower/Upper  2워드Int/100
//   SP41~42  [6..7]   SOCP_R2_Load   Lower/Upper  1워드/100
//   SP43~44  [8..11]  SOCP_R2_Stroke Lower/Upper  2워드Int/100
//   SP45~46  [12..13] SOCP_P_Load    Lower/Upper  1워드/100
//   SP47~48  [14..17] SOCP_P_Stroke  Lower/Upper  2워드Int/100
//   SP49~50  [18..21] SOC_Check      Lower/Upper  2워드Int/10000

using System;
using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    public sealed class Op230Parser
    {
        public Op230ProcessDto Parse(short[] proc, short[] settingOp230)
        {
            if (proc          == null) throw new ArgumentNullException(nameof(proc));
            if (settingOp230  == null) throw new ArgumentNullException(nameof(settingOp230));

            return new Op230ProcessDto
            {
                UpdateTime = DateTime.Now,
                Repair     = PlcParseHelper.Repair(proc[2]),
                Model      = PlcDataConverter.ShortToString(proc, 10, 10),
                Serial01   = PlcParseHelper.Serial(proc, 20, 20),
                Serial02   = PlcParseHelper.Serial(proc, 40, 20),

                // ── APD34~44 : Lotite / Shaft Oil Cap ────────────────────
                Apd34 = PlcParseHelper.Judge(proc[60]),            // Lotite_Dispensing_Judge
                Apd35 = PlcParseHelper.Judge(proc[61]),            // Lotite_Vision_Judge
                Apd36 = PlcParseHelper.F2(proc, 62),               // SOCP_R1_Load
                Apd37 = PlcParseHelper.F2Int(proc, 63),            // SOCP_R1_Stroke
                Apd38 = PlcParseHelper.F2(proc, 65),               // SOCP_R2_Load
                Apd39 = PlcParseHelper.F2Int(proc, 66),            // SOCP_R2_Stroke
                Apd40 = PlcParseHelper.F2(proc, 68),               // SOCP_P_Load
                Apd41 = PlcParseHelper.F2Int(proc, 69),            // SOCP_P_Stroke
                Apd42 = PlcParseHelper.Judge(proc[71]),            // SOCP_Judge
                Apd43 = PlcParseHelper.F4Int(proc, 72),            // SOC_Check
                Apd44 = PlcParseHelper.Judge(proc[74]),            // SOC_Check_Judge

                // ── SP37~50 : OP230 설정 상하한 ──────────────────────────
                Sp37 = PlcParseHelper.F2(settingOp230, 0),
                Sp38 = PlcParseHelper.F2(settingOp230, 1),
                Sp39 = PlcParseHelper.F2Int(settingOp230, 2),
                Sp40 = PlcParseHelper.F2Int(settingOp230, 4),
                Sp41 = PlcParseHelper.F2(settingOp230, 6),
                Sp42 = PlcParseHelper.F2(settingOp230, 7),
                Sp43 = PlcParseHelper.F2Int(settingOp230, 8),
                Sp44 = PlcParseHelper.F2Int(settingOp230, 10),
                Sp45 = PlcParseHelper.F2(settingOp230, 12),
                Sp46 = PlcParseHelper.F2(settingOp230, 13),
                Sp47 = PlcParseHelper.F2Int(settingOp230, 14),
                Sp48 = PlcParseHelper.F2Int(settingOp230, 16),
                Sp49 = PlcParseHelper.F4Int(settingOp230, 18),
                Sp50 = PlcParseHelper.F4Int(settingOp230, 20),
            };
        }
    }
}
