using System.IO;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;
using ConSight.DAQ.Sequence;
using Xunit;

namespace ConSight.DONGBO.DAQ.Tests;

[Trait("Category", "Unit")]
public class ReadLoopTests
{
    // 존재하지 않는 포트 → TCP connection refused → SqlException 즉시 발생
    private const string FakeConnectionString =
        "Server=127.0.0.1,19999;Database=FAKE;User ID=sa;Password=sa;" +
        "TrustServerCertificate=True;Connect Timeout=2";

    // ── 테스트용 IPlcDriver ──────────────────────────────────────────────────

    // ReadWords가 미리 세팅된 값을 반환하고, WriteWords는 WriteLog에 기록한다.
    private sealed class RecordingDriver : IPlcDriver
    {
        private readonly Dictionary<string, short[]> _registeredReadData = new();
        private readonly object _lock = new();
        private readonly TaskCompletionSource _firstWriteCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<(string Address, short[] Data)> WriteLog { get; } = new();
        public bool IsConnected => true;

        public void SetRead(string address, short[] data)
        {
            lock (_lock) _registeredReadData[address] = data;
        }

        public bool ReadWords(string address, int wordCount, out short[] data)
        {
            lock (_lock)
                data = _registeredReadData.TryGetValue(address, out var storedData)
                    ? (short[])storedData.Clone()
                    : new short[wordCount];
            return true;
        }

        public bool WriteWords(string address, short[] data)
        {
            lock (_lock)
            {
                WriteLog.Add((address, (short[])data.Clone()));
                _registeredReadData[address] = (short[])data.Clone();
            }
            _firstWriteCompletion.TrySetResult();
            return true;
        }

        public Task WaitForFirstWrite(TimeSpan timeout) =>
            Task.WhenAny(_firstWriteCompletion.Task, Task.Delay(timeout))
                .ContinueWith(_ => { });
    }

    // ReadWords가 항상 실패하는 드라이버 (TCP 연결 불가 시뮬레이션)
    private sealed class FailingDriver : IPlcDriver
    {
        public bool IsConnected => false;
        public bool WriteWords(string address, short[] data) => false;
        public bool ReadWords(string address, int wordCount, out short[] data)
        {
            data = Array.Empty<short>();
            return false;
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static (PlcReadLoop readLoop, ControlUnit_DAQ controlUnit) CreateStack(IPlcDriver driver)
    {
        var op200Buffer      = new PlcWriteBuffer(driver, "D2001", 3);
        var op210Buffer      = new PlcWriteBuffer(driver, "D2201", 1);
        var op220Buffer      = new PlcWriteBuffer(driver, "D2301", 1);
        var op230Buffer      = new PlcWriteBuffer(driver, "D2401", 1);

        var op200WriteRegion = new Op200WriteRegion(op200Buffer);
        var op210WriteRegion = new Op210WriteRegion(op210Buffer);
        var op220WriteRegion = new Op220WriteRegion(op220Buffer);
        var op230WriteRegion = new Op230WriteRegion(op230Buffer);

        var csvWriter   = new EmpgCsvWriter(Path.Combine(Path.GetTempPath(), "daq_test_csv"));
        var eventBus    = new ProcessEventBus();
        var controlUnit = new ControlUnit_DAQ(
            FakeConnectionString,
            op200WriteRegion, op210WriteRegion, op220WriteRegion, op230WriteRegion,
            csvWriter, eventBus);

        return (new PlcReadLoop(driver, controlUnit), controlUnit);
    }

    // ── 테스트 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlcReadLoop_TriggersOnce_OnRisingEdge()
    {
        var mockDriver  = new RecordingDriver();
        var processData = new short[100];
        processData[0]  = 1; // BackUp_Start
        mockDriver.SetRead("D2000", processData);
        mockDriver.SetRead("D1900", new short[100]);

        var (readLoop, _) = CreateStack(mockDriver);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var loopTask = readLoop.RunAsync(cancellationSource.Token);

        await mockDriver.WaitForFirstWrite(TimeSpan.FromSeconds(7));

        cancellationSource.Cancel();
        try { await loopTask; } catch (OperationCanceledException) { }

        Assert.Contains(mockDriver.WriteLog, entry => entry.Address == "D2001");
    }

    [Fact]
    public async Task PlcReadLoop_NoRetrigger_WhenBackupStartStaysHigh()
    {
        var mockDriver  = new RecordingDriver();
        var processData = new short[100];
        processData[0]  = 1; // proc[0] 계속 1 유지 (시뮬레이터 리셋 없음)
        mockDriver.SetRead("D2000", processData);
        mockDriver.SetRead("D1900", new short[100]);

        var (readLoop, _) = CreateStack(mockDriver);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var loopTask = readLoop.RunAsync(cancellationSource.Token);

        await mockDriver.WaitForFirstWrite(TimeSpan.FromSeconds(7));
        await Task.Delay(350); // 추가 이터레이션 3~4회 소비

        cancellationSource.Cancel();
        try { await loopTask; } catch (OperationCanceledException) { }

        int writeCountForResultAddress = mockDriver.WriteLog.Count(entry => entry.Address == "D2001");
        Assert.Equal(1, writeCountForResultAddress);
    }

    [Fact]
    public async Task PlcReadLoop_SkipsCycle_OnDriverReadFailure()
    {
        var (readLoop, _) = CreateStack(new FailingDriver());

        using var cancellationSource = new CancellationTokenSource(300);
        var loopTask = readLoop.RunAsync(cancellationSource.Token);

        var exception = await Record.ExceptionAsync(async () => await loopTask);

        Assert.True(exception is null or OperationCanceledException);
    }
}
