using Bi.nsLogWriter;

namespace ConSight.DAQ.Device.PLC
{
    /// <summary>
    /// 테스트용 in-memory PLC 드라이버.
    /// 실제 하드웨어 없이 Write/Read 동작을 검증한다.
    /// </summary>
    public sealed class MockPlcDriver : IPlcDriver
    {
        private readonly Dictionary<string, short[]> _memory = new();
        private readonly object _lock = new();
        private readonly LogWriter _log = new();

        public bool IsConnected => true;

        public bool WriteWords(string deviceAddress, short[] data)
        {
            lock (_lock)
                _memory[deviceAddress] = (short[])data.Clone();

            _log.WriteInformation($"[MockPLC] WriteWords addr={deviceAddress} data=[{string.Join(",", data)}]");
            return true;
        }

        public bool ReadWords(string deviceAddress, int wordCount, out short[] data)
        {
            lock (_lock)
                data = _memory.TryGetValue(deviceAddress, out var stored)
                    ? (short[])stored.Clone()
                    : new short[wordCount];
            return true;
        }

        /// <summary>테스트 검증용: 지정 주소의 현재 메모리 스냅샷 반환</summary>
        public short[] PeekMemory(string deviceAddress)
        {
            lock (_lock)
                return _memory.TryGetValue(deviceAddress, out var d)
                    ? (short[])d.Clone()
                    : Array.Empty<short>();
        }
    }
}
