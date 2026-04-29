namespace ConSight.DAQ.Device.PLC
{
    /// <summary>
    /// PLC 드라이버 계약.
    /// 실제 구현: MxComponentPlcDriver (미쓰비시 FX5)
    /// 테스트 구현: MockPlcDriver (in-memory)
    /// </summary>
    public interface IPlcDriver
    {
        bool IsConnected { get; }

        /// <summary>배열 단위 워드 쓰기 (PC → PLC)</summary>
        bool WriteWords(string deviceAddress, short[] data);

        /// <summary>배열 단위 워드 읽기 (PLC → PC)</summary>
        bool ReadWords(string deviceAddress, int wordCount, out short[] data);
    }
}
