namespace ConSight.DAQ.Data
{
    /// <summary>DB 연결 설정 — 원본 Setting_DataBase 호환 (ISettingItem 의존 제거)</summary>
    public class Setting_DataBase
    {
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 로컬 DB
        public string LocalDB_ConnectionString { get; set; }
            = @"Data Source=(local);Initial Catalog=DB_eM;Integrated Security=SSPI";
        public uint DB_DataKeepMonth { get; set; } = 0;

        // 원격 DB (OP100 시리얼 조회용 — Step 4에서 제거 예정)
        public string RemoteDB_IP_Address { get; set; } = "127.0.0.1";
        public long   RemoteDB_Port_Number { get; set; } = 1433;
        public string RemoteDB_Uid    { get; set; } = "OP_200";
        public string RemoteDB_Pwd    { get; set; } = "1111";
        public string RemoteDB_DbName { get; set; } = string.Empty;
    }
}
