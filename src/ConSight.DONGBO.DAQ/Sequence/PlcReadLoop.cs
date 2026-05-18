using ConSight.DAQ.Device;
using ConSight.DAQ.Device.PLC;

namespace ConSight.DAQ.Sequence
{
    public sealed class PlcReadLoop
    {
        private sealed record OpMeta(
            string ProcAddr,    int ProcCount,
            string SettingAddr, int SettingCount,
            Func<short[], short[], object> Parse,
            Action<object> Process);

        private readonly IPlcDriver _driver;
        private readonly OpMeta[] _ops;
        private readonly short[] _prevProc0 = new short[4]; // rising-edge 검출용

        public PlcReadLoop(IPlcDriver driver, ControlUnit_DAQ controlUnit)
        {
            _driver = driver;
            _ops =
            [
                new("D2000", 100, "D1900", 100,
                    (p, s) => new Op200Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op200((Op200ProcessDto)dto)),
                new("D2200",  70, "D1900", 100,
                    (p, s) => new Op210Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op210((Op210ProcessDto)dto)),
                new("D2300",  70, "D1900", 100,
                    (p, s) => new Op220Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op220((Op220ProcessDto)dto)),
                new("D2400",  80, "D1800",  24,
                    (p, s) => new Op230Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op230((Op230ProcessDto)dto)),
            ];
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < _ops.Length; i++)
                {
                    var op = _ops[i];
                    if (!_driver.ReadWords(op.ProcAddr, op.ProcCount, out var proc))
                        continue;

                    if (proc[0] == 1 && _prevProc0[i] == 0) // rising edge
                    {
                        if (_driver.ReadWords(op.SettingAddr, op.SettingCount, out var setting))
                        {
                            var dto = op.Parse(proc, setting);
                            op.Process(dto);
                        }
                    }
                    _prevProc0[i] = proc[0];
                }
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }
    }
}
