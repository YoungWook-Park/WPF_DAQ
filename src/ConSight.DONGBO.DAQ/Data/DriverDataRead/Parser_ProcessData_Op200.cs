using Bi.ConSight_MxComponent.Data;
using Bi.nsExpException;
using System.Reflection;

namespace ConSight.DAQ
{
    /// <summary>
    /// OP200 PLC 레지스터 배열을 파싱해 정적 프로퍼티에 저장.
    /// Step 6(PLC Mock) 완성 후 IPlcDriver를 통해 데이터를 수신한다.
    /// </summary>
    public class Parser_ProcessData_Op200
    {
        public static string D2000_PLC_BackUp_Start                  { get; set; } = string.Empty;
        public static string D2001_PC_Complete_Flag                  { get; set; } = string.Empty;
        public static string D2002_PLC_Repair                        { get; set; } = string.Empty;
        public static string D2010_PLC_Model_Name                    { get; set; } = string.Empty;
        public static string D2020_PLC_Shaft_Serial_No               { get; set; } = string.Empty;
        public static string D2040_PLC_Gear_Serial_No                { get; set; } = string.Empty;
        public static string D2060_PLC_Total_Judgment                { get; set; } = string.Empty;
        public static string D2061_PLC_Guide_Ring_Spacer_R1_Load     { get; set; } = string.Empty;
        public static string D2062_PLC_Guide_Ring_Spacer_R1_Stroke   { get; set; } = string.Empty;
        public static string D2064_PLC_Guide_Ring_Spacer_R2_Load     { get; set; } = string.Empty;
        public static string D2065_PLC_Guide_Ring_Spacer_R2_Stroke   { get; set; } = string.Empty;
        public static string D2067_PLC_Guide_Ring_Spacer_P_Load      { get; set; } = string.Empty;
        public static string D2068_PLC_Guide_Ring_Spacer_P_Stroke    { get; set; } = string.Empty;
        public static string D2070_PLC_Guide_Ring_Spacer_Judge       { get; set; } = string.Empty;
        public static string D2071_PLC_Guide_Ring_Spacer_Index_No    { get; set; } = string.Empty;
        public static string D2072_PLC_Bearing_R1_Load               { get; set; } = string.Empty;
        public static string D2073_PLC_Bearing_R1_Stroke             { get; set; } = string.Empty;
        public static string D2075_PLC_Bearing_R2_Load               { get; set; } = string.Empty;
        public static string D2076_PLC_Bearing_R2_Stroke             { get; set; } = string.Empty;
        public static string D2078_PLC_Bearing_P_Load                { get; set; } = string.Empty;
        public static string D2079_PLC_Bearing_P_Stroke              { get; set; } = string.Empty;
        public static string D2081_PLC_Bearing_Judge                 { get; set; } = string.Empty;
        public static string D2082_PLC_Bearing_Index_No              { get; set; } = string.Empty;
        public static string D2083_PLC_Snap_Ring_Groove_000Deg       { get; set; } = string.Empty;
        public static string D2085_PLC_Snap_Ring_Groove_180Deg       { get; set; } = string.Empty;
        public static string D2087_PLC_Snap_Ring_Groove_Grade_Data   { get; set; } = string.Empty;
        public static string D2089_PLC_Snap_Ring_Groove_Grade        { get; set; } = string.Empty;
        public static string D2090_PLC_Snap_Ring_Groove_Judge        { get; set; } = string.Empty;
        public static string D2091_PLC_Snap_Ring_Heigh_Thick         { get; set; } = string.Empty;
        public static string D2093_PLC_Snap_Ring_Heigh_Judge         { get; set; } = string.Empty;
        public static string D2094_PLC_Snap_Ring_Judge               { get; set; } = string.Empty;
        public static string D2095_PLC_End_Plate_Data                { get; set; } = string.Empty;
        public static string D2097_PLC_End_Plate_Judge               { get; set; } = string.Empty;

        public static bool ParseProcess(short[] in_ReceiveData)
        {
            try
            {
                if (in_ReceiveData == null)
                    throw new ArgumentNullException(nameof(in_ReceiveData));

                D2000_PLC_BackUp_Start  = in_ReceiveData[0].ToString();
                D2001_PC_Complete_Flag  = in_ReceiveData[1].ToString();

                // 수리 상태
                D2002_PLC_Repair = in_ReceiveData[2] switch
                {
                    0 => MxComp_DB_REPAIR_CODE.AUTO,
                    1 => MxComp_DB_REPAIR_CODE.REPAIR,
                    2 => MxComp_DB_REPAIR_CODE.MASTER,
                    _ => in_ReceiveData[2].ToString()
                };

                D2010_PLC_Model_Name = PlcDataConverter.ShortToString(in_ReceiveData, 10, 10);

                string serialNo = PlcDataConverter.ShortToString(in_ReceiveData, 20, 20);
                D2020_PLC_Shaft_Serial_No = string.IsNullOrWhiteSpace(serialNo) ? string.Empty : serialNo;

                serialNo = PlcDataConverter.ShortToString(in_ReceiveData, 40, 20);
                D2040_PLC_Gear_Serial_No  = string.IsNullOrWhiteSpace(serialNo) ? string.Empty : serialNo;

                D2060_PLC_Total_Judgment = in_ReceiveData[60] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[60].ToString()
                };

                D2061_PLC_Guide_Ring_Spacer_R1_Load   = ((double)in_ReceiveData[61] / 100).ToString("0.00");
                D2062_PLC_Guide_Ring_Spacer_R1_Stroke = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 62) / 100).ToString("0.00");
                D2064_PLC_Guide_Ring_Spacer_R2_Load   = ((double)in_ReceiveData[64] / 100).ToString("0.00");
                D2065_PLC_Guide_Ring_Spacer_R2_Stroke = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 65) / 100).ToString("0.00");
                D2067_PLC_Guide_Ring_Spacer_P_Load    = ((double)in_ReceiveData[67] / 100).ToString("0.00");
                D2068_PLC_Guide_Ring_Spacer_P_Stroke  = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 68) / 100).ToString("0.00");

                D2070_PLC_Guide_Ring_Spacer_Judge = in_ReceiveData[70] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[70].ToString()
                };
                D2071_PLC_Guide_Ring_Spacer_Index_No = in_ReceiveData[71].ToString();

                D2072_PLC_Bearing_R1_Load   = ((double)in_ReceiveData[72] / 100).ToString("0.00");
                D2073_PLC_Bearing_R1_Stroke = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 73) / 100).ToString("0.00");
                D2075_PLC_Bearing_R2_Load   = ((double)in_ReceiveData[75] / 100).ToString("0.00");
                D2076_PLC_Bearing_R2_Stroke = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 76) / 100).ToString("0.00");
                D2078_PLC_Bearing_P_Load    = ((double)in_ReceiveData[78] / 100).ToString("0.00");
                D2079_PLC_Bearing_P_Stroke  = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 79) / 100).ToString("0.00");

                D2081_PLC_Bearing_Judge = in_ReceiveData[81] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[81].ToString()
                };
                D2082_PLC_Bearing_Index_No = in_ReceiveData[82].ToString();

                D2083_PLC_Snap_Ring_Groove_000Deg    = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 83) / 10000).ToString("0.0000");
                D2085_PLC_Snap_Ring_Groove_180Deg    = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 85) / 10000).ToString("0.0000");
                D2087_PLC_Snap_Ring_Groove_Grade_Data = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 87) / 100).ToString("0.00");
                D2089_PLC_Snap_Ring_Groove_Grade      = in_ReceiveData[89].ToString();

                D2090_PLC_Snap_Ring_Groove_Judge = in_ReceiveData[90] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[90].ToString()
                };

                D2091_PLC_Snap_Ring_Heigh_Thick = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 91) / 100).ToString("0.00");

                D2093_PLC_Snap_Ring_Heigh_Judge = in_ReceiveData[93] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[93].ToString()
                };

                D2094_PLC_Snap_Ring_Judge = in_ReceiveData[94] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[94].ToString()
                };

                D2095_PLC_End_Plate_Data  = ((double)PlcDataConverter.shortToInt(in_ReceiveData, 95) / 100).ToString("0.00");

                D2097_PLC_End_Plate_Judge = in_ReceiveData[97] switch
                {
                    1 => MxComp_DB_JUDGE_CODE.OK,
                    2 => MxComp_DB_JUDGE_CODE.NG,
                    4 => MxComp_DB_JUDGE_CODE.PASS,
                    _ => in_ReceiveData[97].ToString()
                };

                return true;
            }
            catch (ExpException expEx) { ExpException.RaiseExpException(expEx); return false; }
            catch (Exception ex)       { ExpException.RaiseException(ex);       return false; }
        }
    }
}
