namespace ConSight.DAQ
{
    public class Constants_App
    {
        public const string ApplicationName = "ConSight.DAQ(Line Data Acquisition) GUI";

        public const string SettingFilePath = @"D:\[LDAQ_GUI]\SETTING";
        public const string SettingFileExt  = "json";

        public static string GetSettingFileFullPath(string fileName)
            => string.IsNullOrEmpty(fileName) ? string.Empty
               : $@"{SettingFilePath}\{fileName}.{SettingFileExt}";

        public const string DEF_SETTING_ITEM_SYSTEM    = "SYSTEM";
        public const string DEF_SETTING_ITEM_DATA_BASE = "DATA_BASE";

        public const string DeviceConfigFilePath = @"D:\[LDAQ_GUI]\DEVICE_CONFIG";
        public const string DeviceConfigFileExt  = "json";

        public static string GetDeviceConfigFileFullPath(string fileName)
            => string.IsNullOrEmpty(fileName) ? string.Empty
               : $@"{DeviceConfigFilePath}\{fileName}.{DeviceConfigFileExt}";

        public const string DEF_DEVICE_CONFIG_ITEM_MEL_PLC = "MEL_PLC";

        public const string LogFilePath = @"D:\[LDAQ_GUI]\LOG";

        public const string DIO_OFF = "OFF";
        public const string DIO_ON  = "ON";

        public const string DataBase_ERR_No_Data = "No data.";
    }

    public static class RESULT
    {
        public const int OK              =       0;
        public const int NO_RESPONSE     = -18000;
        public const int NOT_CONNECTED   = -17000;
        public const int TIMEOUT         =  -4000;
        public const int INTERNAL_ERROR  =  -9000;
    }
}
