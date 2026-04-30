// Bi.ConSight_MxComponent.Data — PLC 드라이버 상수 및 PlcDataConverter 스텁
// Parser_ProcessData_Op* 가 사용하는 코드 문자열 상수 + 레지스터 변환 유틸리티
using System.Text;

namespace Bi.ConSight_MxComponent.Data
{
    public static class MxComp_DB_JUDGE_CODE
    {
        public const string OK   = "OK";
        public const string NG   = "NG";
        public const string PASS = "PASS";
    }

    public static class MxComp_DB_REPAIR_CODE
    {
        public const string AUTO   = "AUTO";
        public const string REPAIR = "REPAIR";
        public const string MASTER = "MASTER";
    }

    /// <summary>
    /// PLC short[] ↔ string / int 변환 유틸리티.
    /// 원본 Bi.ConSight_MxComponent.PlcDataConverter 로직 그대로 이식.
    /// </summary>
    public static class PlcDataConverter
    {
        /// <summary>short 배열(2바이트/레지스터)을 ASCII 문자열로 변환 — null 바이트 만나면 종료</summary>
        public static string ShortToString(short[] data, int startIndex, int length)
        {
            var result = new StringBuilder();
            for (int i = startIndex; i < startIndex + length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(data[i]);
                foreach (byte b in bytes)
                {
                    if (b == 0x00) return result.ToString().Trim();
                    result.Append((char)b);
                }
            }
            return result.ToString().Trim();
        }

        /// <summary>두 개의 연속 short를 32-bit int로 합성 (low word first)</summary>
        public static int shortToInt(short[] data, int startIndex)
        {
            int hi = (int)data[startIndex + 1] << 16;
            return hi | ((int)data[startIndex] & 0xFFFF);
        }
    }
}

