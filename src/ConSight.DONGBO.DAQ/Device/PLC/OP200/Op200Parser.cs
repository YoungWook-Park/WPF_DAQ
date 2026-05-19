// Phase B: OP200 공정 파서
//
// PLC 수신 배열 2개(공정 데이터 D2000, 설정 데이터 D1900)를
// Op200ProcessDto 로 변환한다.
//
// 호출 예:
//   var dto = new Op200Parser().Parse(procData, settingData);
//
// 공정 배열 레이아웃 (D2000 기준 오프셋):
//   [0]       D2000 BackUp_Start
//   [1]       D2001 PC_Complete_Flag
//   [2]       D2002 Repair
//   [10..19]  D2010 Model (10워드 문자열)
//   [20..39]  D2020 ShaftSerial (20워드 문자열)
//   [40..59]  D2040 GearSerial (20워드 문자열)
//   [60]      D2060 TotalJudge
//   [61]      D2061 GR_R1_Load        1워드/100
//   [62..63]  D2062 GR_R1_Stroke      2워드Int/100
//   [64]      D2064 GR_R2_Load        1워드/100
//   [65..66]  D2065 GR_R2_Stroke      2워드Int/100
//   [67]      D2067 GR_P_Load         1워드/100
//   [68..69]  D2068 GR_P_Stroke       2워드Int/100
//   [70]      D2070 GR_Judge
//   [71]      D2071 GR_IndexNo
//   [72]      D2072 BR_R1_Load        1워드/100
//   [73..74]  D2073 BR_R1_Stroke      2워드Int/100
//   [75]      D2075 BR_R2_Load        1워드/100
//   [76..77]  D2076 BR_R2_Stroke      2워드Int/100
//   [78]      D2078 BR_P_Load         1워드/100
//   [79..80]  D2079 BR_P_Stroke       2워드Int/100
//   [81]      D2081 BR_Judge
//   [82]      D2082 BR_IndexNo
//   [83..84]  D2083 SR_Groove_000Deg  2워드Int/10000
//   [85..86]  D2085 SR_Groove_180Deg  2워드Int/10000
//   [87..88]  D2087 SR_Groove_GradeData  2워드Int/100
//   [89]      D2089 SR_Groove_Grade
//   [90]      D2090 SR_Groove_Judge
//   [91..92]  D2091 SR_Heigh_Thick    2워드Int/100
//   [93]      D2093 SR_Heigh_Judge
//   [94]      D2094 SR_Judge
//   [95..96]  D2095 EndPlate_Data     2워드Int/100
//   [97]      D2097 EndPlate_Judge
//
// 설정 배열 레이아웃 (D1900 기준 오프셋):
//   SP01~02  [0..1]   GR_R1_Load      Lower/Upper  1워드/100
//   SP03~04  [2..5]   GR_R1_Stroke    Lower/Upper  2워드Int/100
//   SP05~06  [6..7]   GR_R2_Load      Lower/Upper  1워드/100
//   SP07~08  [8..11]  GR_R2_Stroke    Lower/Upper  2워드Int/100
//   SP09~10  [12..13] GR_P_Load       Lower/Upper  1워드/100
//   SP11~12  [14..17] GR_P_Stroke     Lower/Upper  2워드Int/100
//   SP13~14  [20..21] BR_R1_Load      Lower/Upper  1워드/100
//   SP15~16  [22..25] BR_R1_Stroke    Lower/Upper  2워드Int/100
//   SP17~18  [26..27] BR_R2_Load      Lower/Upper  1워드/100
//   SP19~20  [28..31] BR_R2_Stroke    Lower/Upper  2워드Int/100
//   SP21~22  [32..33] BR_P_Load       Lower/Upper  1워드/100
//   SP23~24  [34..37] BR_P_Stroke     Lower/Upper  2워드Int/100
//   SP25~26  [48..51] SR_Groove_Grade Lower/Upper  2워드Int/10000
//   SP27~28  [52..55] SR_Heigh_Thick  Lower/Upper  2워드Int/10000
//   SP29~30  [60..63] EndPlate_Data   Lower/Upper  2워드Int/10000

using System;
using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    public sealed class Op200Parser
    {
        public Op200ProcessDto Parse(short[] proc, short[] setting)
        {
            if (proc    == null) throw new ArgumentNullException(nameof(proc));
            if (setting == null) throw new ArgumentNullException(nameof(setting));

            return new Op200ProcessDto
            {
                UpdateTime  = DateTime.Now,
                Repair      = PlcParseHelper.Repair(proc[2]),
                Model       = PlcDataConverter.ShortToString(proc, 10, 10),
                ShaftSerial = PlcParseHelper.Serial(proc, 20, 20),
                GearSerial  = PlcParseHelper.Serial(proc, 40, 20),
                TotalJudge  = PlcParseHelper.Judge(proc[60]),

                // ── APD01~08 : Guide Ring Spacer ──────────────────────────
                Apd01 = PlcParseHelper.F2(proc, 61),
                Apd02 = PlcParseHelper.F2Int(proc, 62),
                Apd03 = PlcParseHelper.F2(proc, 64),
                Apd04 = PlcParseHelper.F2Int(proc, 65),
                Apd05 = PlcParseHelper.F2(proc, 67),
                Apd06 = PlcParseHelper.F2Int(proc, 68),
                Apd07 = PlcParseHelper.Judge(proc[70]),
                Apd08 = proc[71].ToString(),

                // ── APD09~16 : Bearing ────────────────────────────────────
                Apd09 = PlcParseHelper.F2(proc, 72),
                Apd10 = PlcParseHelper.F2Int(proc, 73),
                Apd11 = PlcParseHelper.F2(proc, 75),
                Apd12 = PlcParseHelper.F2Int(proc, 76),
                Apd13 = PlcParseHelper.F2(proc, 78),
                Apd14 = PlcParseHelper.F2Int(proc, 79),
                Apd15 = PlcParseHelper.Judge(proc[81]),
                Apd16 = proc[82].ToString(),

                // ── APD17~24 : Snap Ring ──────────────────────────────────
                Apd17 = PlcParseHelper.F4Int(proc, 83),
                Apd18 = PlcParseHelper.F4Int(proc, 85),
                Apd19 = PlcParseHelper.F2Int(proc, 87),
                Apd20 = proc[89].ToString(),
                Apd21 = PlcParseHelper.Judge(proc[90]),
                Apd22 = PlcParseHelper.F2Int(proc, 91),
                Apd23 = PlcParseHelper.Judge(proc[93]),
                Apd24 = PlcParseHelper.Judge(proc[94]),

                // ── APD25~26 : End Plate ──────────────────────────────────
                Apd25 = PlcParseHelper.F2Int(proc, 95),
                Apd26 = PlcParseHelper.Judge(proc[97]),

                // ── SP01~12 : Guide Ring Spacer 상하한 ────────────────────
                Sp01 = PlcParseHelper.F2(setting, 0),
                Sp02 = PlcParseHelper.F2(setting, 1),
                Sp03 = PlcParseHelper.F2Int(setting, 2),
                Sp04 = PlcParseHelper.F2Int(setting, 4),
                Sp05 = PlcParseHelper.F2(setting, 6),
                Sp06 = PlcParseHelper.F2(setting, 7),
                Sp07 = PlcParseHelper.F2Int(setting, 8),
                Sp08 = PlcParseHelper.F2Int(setting, 10),
                Sp09 = PlcParseHelper.F2(setting, 12),
                Sp10 = PlcParseHelper.F2(setting, 13),
                Sp11 = PlcParseHelper.F2Int(setting, 14),
                Sp12 = PlcParseHelper.F2Int(setting, 16),

                // ── SP13~24 : Bearing 상하한 ──────────────────────────────
                Sp13 = PlcParseHelper.F2(setting, 20),
                Sp14 = PlcParseHelper.F2(setting, 21),
                Sp15 = PlcParseHelper.F2Int(setting, 22),
                Sp16 = PlcParseHelper.F2Int(setting, 24),
                Sp17 = PlcParseHelper.F2(setting, 26),
                Sp18 = PlcParseHelper.F2(setting, 27),
                Sp19 = PlcParseHelper.F2Int(setting, 28),
                Sp20 = PlcParseHelper.F2Int(setting, 30),
                Sp21 = PlcParseHelper.F2(setting, 32),
                Sp22 = PlcParseHelper.F2(setting, 33),
                Sp23 = PlcParseHelper.F2Int(setting, 34),
                Sp24 = PlcParseHelper.F2Int(setting, 36),

                // ── SP25~28 : Snap Ring Grade / Heigh 상하한 ─────────────
                Sp25 = PlcParseHelper.F4Int(setting, 48),
                Sp26 = PlcParseHelper.F4Int(setting, 50),
                Sp27 = PlcParseHelper.F4Int(setting, 52),
                Sp28 = PlcParseHelper.F4Int(setting, 54),

                // ── SP29~30 : End Plate 상하한 ────────────────────────────
                Sp29 = PlcParseHelper.F4Int(setting, 60),
                Sp30 = PlcParseHelper.F4Int(setting, 62),
            };
        }
    }
}
