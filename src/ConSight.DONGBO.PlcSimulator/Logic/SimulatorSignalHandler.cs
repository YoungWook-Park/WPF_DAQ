using ConSight.DONGBO.PlcSimulator.Memory;

namespace ConSight.DONGBO.PlcSimulator.Logic
{
    internal sealed class SimulatorSignalHandler
    {
        private readonly PlcMemory _memory;

        internal SimulatorSignalHandler(PlcMemory memory)
        {
            _memory = memory;
            memory.Written += OnWritten;
        }

        private void OnWritten(string addr, short[] data)
        {
            // DAQ가 PC_Complete_Flag를 1로 쓰면 BackUp_Start + PC_Complete 리셋
            if (addr == "D2001" && data.Length > 1 && data[1] == 1)
            {
                ResetWord("D2000", 0);  // OP200 BackUp_Start
                ResetWord("D2001", 1);  // OP200 PC_Complete_Flag
            }
            else if (addr == "D2201" && data.Length > 0 && data[0] == 1)
            {
                ResetWord("D2200", 0);
                ResetWord("D2201", 0);
            }
            else if (addr == "D2301" && data.Length > 0 && data[0] == 1)
            {
                ResetWord("D2300", 0);
                ResetWord("D2301", 0);
            }
            else if (addr == "D2401" && data.Length > 0 && data[0] == 1)
            {
                ResetWord("D2400", 0);
                ResetWord("D2401", 0);
            }
        }

        // 기존 저장 배열에서 index 위치만 0으로 리셋하고 재저장
        private void ResetWord(string addr, int index)
        {
            var d = _memory.Read(addr, index + 1);
            if (d.Length <= index) return;
            d[index] = 0;
            _memory.Write(addr, d);
            // Write가 다시 Written을 발화하지만 값이 0이므로 조건에 걸리지 않아 재귀 없음
        }
    }
}
