namespace ConSight.DAQ.Device.PLC
{
    /// <summary>
    /// 상위 어플리케이션의 전역 write 배열 캡슐화.
    ///
    /// PlcWordRegionBase.SetWord / SetBit  →  PC 메모리만 변경 (드라이버 미개입)
    /// Cmd_Write()                         →  현재 배열 전체를 드라이버로 전송
    /// </summary>
    public sealed class PlcWriteBuffer
    {
        private readonly short[] _buffer;
        private readonly object _lock = new();
        private readonly IPlcDriver _driver;
        private readonly string _deviceAddress;

        public int WordCount => _buffer.Length;

        public PlcWriteBuffer(IPlcDriver driver, string deviceAddress, int wordCount)
        {
            _driver        = driver;
            _deviceAddress = deviceAddress;
            _buffer        = new short[wordCount];
        }

        // ── PC 메모리 접근 (PlcWordRegionBase 전용) ───────────────────────

        internal short ReadWord(int absoluteIndex)
        {
            lock (_lock) return _buffer[absoluteIndex];
        }

        internal void WriteWord(int absoluteIndex, short value)
        {
            lock (_lock) _buffer[absoluteIndex] = value;
        }

        // ── 드라이버 전송 ─────────────────────────────────────────────────

        /// <summary>현재 전역 배열 전체를 드라이버로 전송 (PC 메모리 → PLC)</summary>
        public bool Cmd_Write()
        {
            short[] snapshot;
            lock (_lock) snapshot = (short[])_buffer.Clone();
            return _driver.WriteWords(_deviceAddress, snapshot);
        }

        public short[] GetSnapshot()
        {
            lock (_lock) return (short[])_buffer.Clone();
        }
    }
}
