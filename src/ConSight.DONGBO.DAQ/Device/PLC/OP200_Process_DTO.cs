using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    public class OP200_Process_DTO
    {
        public string PLC_BackUp_Start     { get; set; } = string.Empty;
        public string PC_Complete_Flag     { get; set; } = string.Empty;
        public string PLC_Repair           { get; set; } = string.Empty;
        public string PLC_Model_Name       { get; set; } = string.Empty;
        public string PLC_Shaft_Serial_No  { get; set; } = string.Empty;
        public string PLC_Gear_Serial_No   { get; set; } = string.Empty;
        public string PLC_Total_Judgement  { get; set; } = string.Empty;
    }

    public class Parser_Op200
    {
        public OP200_Process_DTO ParseData(short[] data) => new OP200_Process_DTO
        {
            PLC_BackUp_Start    = data[0].ToString(),
            PC_Complete_Flag    = data[1].ToString(),
            PLC_Repair          = data[2].ToString(),
            PLC_Model_Name      = PlcDataConverter.ShortToString(data, 10, 10),
            PLC_Shaft_Serial_No = PlcDataConverter.ShortToString(data, 20, 20),
            PLC_Gear_Serial_No  = PlcDataConverter.ShortToString(data, 40, 20),
            PLC_Total_Judgement = data[60] == 1 ? MxComp_DB_JUDGE_CODE.OK : MxComp_DB_JUDGE_CODE.NG
        };
    }
}
