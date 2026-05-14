namespace ConSight.DONGBO.PlcSimulator.Memory
{
    internal sealed class PlcMemory
    {
        private readonly Dictionary<string, short[]> _store = new();
        private readonly object _lock = new();

        internal PlcMemory()
        {
            // 각 OP 영역 사전 초기화 (0으로 채워진 배열)
            foreach (var (addr, count) in new (string, int)[]
            {
                ("D2000", 100), ("D2200", 70), ("D2300", 70), ("D2400", 80),
                ("D1900", 100), ("D1800", 24),
                ("D2001", 3),   ("D2201", 1),  ("D2301", 1),  ("D2401", 1),
            })
            {
                _store[addr] = new short[count];
            }
        }

        // Written은 lock 밖에서 발화 — handler 내부에서 Write 재진입 허용
        internal event Action<string, short[]>? Written;

        internal short[] Read(string addr, int count)
        {
            lock (_lock)
                return _store.TryGetValue(addr, out var d)
                    ? (short[])d.Clone()
                    : new short[count];
        }

        // 저장된 배열 전체를 반환 (없으면 빈 배열) — 크기를 모를 때 사용
        internal short[] ReadAll(string addr)
        {
            lock (_lock)
                return _store.TryGetValue(addr, out var d)
                    ? (short[])d.Clone()
                    : Array.Empty<short>();
        }

        internal void Write(string addr, short[] data)
        {
            lock (_lock)
                _store[addr] = (short[])data.Clone();

            Written?.Invoke(addr, (short[])data.Clone());
        }
    }
}
