namespace ConSight.DAQ.Device.PLC
{
    /// <summary>
    /// PlcWriteBuffer 내 특정 워드 구간의 추상 접근자.
    ///
    /// SetWord / GetWord / SetBit / GetBit  →  PC 메모리만 변경
    /// Cmd_Write()                          →  버퍼 전체를 드라이버로 전송
    ///
    /// 파생 클래스는 워드/비트 접근을 도메인 언어로 래핑한다.
    /// </summary>
    public abstract class PlcWordRegionBase
    {
        private readonly PlcWriteBuffer _buffer;

        // 이 영역의 버퍼 절대 시작 인덱스 (여러 영역이 하나의 버퍼를 공유할 때 사용)
        private readonly int _baseOffset;

        protected PlcWordRegionBase(PlcWriteBuffer buffer, int baseOffset = 0)
        {
            _buffer     = buffer;
            _baseOffset = baseOffset;
        }

        // ── PC 메모리 워드 접근 ───────────────────────────────────────────

        protected void SetWord(int wordIndex, short value) =>
            _buffer.WriteWord(_baseOffset + wordIndex, value);

        protected short GetWord(int wordIndex) =>
            _buffer.ReadWord(_baseOffset + wordIndex);

        // ── PC 메모리 비트 접근 ───────────────────────────────────────────

        /// <param name="bitPos">0 = LSB, 15 = MSB</param>
        protected void SetBit(int wordIndex, int bitPos, bool on)
        {
            int   idx  = _baseOffset + wordIndex;
            short cur  = _buffer.ReadWord(idx);
            short next = on
                ? (short)((ushort)cur |  (1 << bitPos))   // ushort로 캐스팅해 sign-extension 방지
                : (short)((ushort)cur & ~(1 << bitPos));
            _buffer.WriteWord(idx, next);
        }

        protected bool GetBit(int wordIndex, int bitPos) =>
            (_buffer.ReadWord(_baseOffset + wordIndex) & (1 << bitPos)) != 0;

        // ── 드라이버 전송 ─────────────────────────────────────────────────

        /// <summary>PC 메모리(전역 배열 전체)를 드라이버로 전송</summary>
        public bool Cmd_Write() => _buffer.Cmd_Write();
    }
}
