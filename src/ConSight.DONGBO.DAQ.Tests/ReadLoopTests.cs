using System.IO;
using ConSight.DAQ.AppEvent;
using Xunit;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Sequence;

namespace ConSight.DONGBO.DAQ.Tests;

[Trait("Category", "Unit")]
public class ReadLoopTests
{
    // 존재하지 않는 포트 → TCP connection refused → SqlException 즉시 발생 (no timeout wait)
    private const string FakeDb =
        "Server=127.0.0.1,19999;Database=FAKE;User ID=sa;Password=sa;" +
        "TrustServerCertificate=True;Connect Timeout=2";

    // ── 테스트용 IPlcDriver ──────────────────────────────────────────────────

    private sealed class RecordingDriver : IPlcDriver
    {
        private readonly Dictionary<string, short[]> _readData = new();
        private readonly object _lock = new();
        private readonly TaskCompletionSource _anyWriteTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<(string Addr, short[] Data)> WriteLog { get; } = new();
        public bool IsConnected => true;

        public void SetRead(string addr, short[] data)
        {
            lock (_lock) _readData[addr] = data;
        }

        public bool ReadWords(string addr, int wordCount, out short[] data)
        {
            lock (_lock)
                data = _readData.TryGetValue(addr, out var d)
                    ? (short[])d.Clone()
                    : new short[wordCount];
            return true;
        }

        public bool WriteWords(string addr, short[] data)
        {
            lock (_lock)
            {
                WriteLog.Add((addr, (short[])data.Clone()));
                _readData[addr] = (short[])data.Clone();
            }
            _anyWriteTcs.TrySetResult();
            return true;
        }

        public Task WaitForFirstWrite(TimeSpan timeout) =>
            Task.WhenAny(_anyWriteTcs.Task, Task.Delay(timeout))
                .ContinueWith(_ => { });
    }

    private sealed class FailingDriver : IPlcDriver
    {
        public bool IsConnected => false;
        public bool WriteWords(string addr, short[] data) => false;
        public bool ReadWords(string addr, int wordCount, out short[] data)
        {
            data = Array.Empty<short>();
            return false;
        }
    }

    // ── 헬퍼: ControlUnit_DAQ + PlcReadLoop 생성 ────────────────────────────

    private static (PlcReadLoop loop, ControlUnit_DAQ unit) CreateStack(IPlcDriver driver)
    {
        var buf200 = new PlcWriteBuffer(driver, "D2001", 3);
        var buf210 = new PlcWriteBuffer(driver, "D2201", 1);
        var buf220 = new PlcWriteBuffer(driver, "D2301", 1);
        var buf230 = new PlcWriteBuffer(driver, "D2401", 1);

        var op200Write = new Op200WriteRegion(buf200);
        var op210Write = new Op210WriteRegion(buf210);
        var op220Write = new Op220WriteRegion(buf220);
        var op230Write = new Op230WriteRegion(buf230);

        var csvWriter  = new EmpgCsvWriter(Path.Combine(Path.GetTempPath(), "daq_test_csv"));
        var eventBus   = new ProcessEventBus();
        var unit       = new ControlUnit_DAQ(
            FakeDb, op200Write, op210Write, op220Write, op230Write, csvWriter, eventBus);

        return (new PlcReadLoop(driver, unit), unit);
    }

    // ── 테스트 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlcReadLoop_TriggersOnce_OnRisingEdge()
    {
        var driver = new RecordingDriver();
        // OP200 proc[0]=1 (BackUp_Start) — 첫 이터레이션에서 rising edge 검출
        var proc = new short[100];
        proc[0] = 1;
        driver.SetRead("D2000", proc);
        driver.SetRead("D1900", new short[100]);

        var (loop, _) = CreateStack(driver);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var loopTask = loop.RunAsync(cts.Token);

        // D2001 에 최초 쓰기가 발생할 때까지 대기
        await driver.WaitForFirstWrite(TimeSpan.FromSeconds(7));

        cts.Cancel();
        try { await loopTask; } catch (OperationCanceledException) { }

        Assert.Contains(driver.WriteLog, x => x.Addr == "D2001");
    }

    [Fact]
    public async Task PlcReadLoop_NoRetrigger_WhenBackupStartStaysHigh()
    {
        var driver = new RecordingDriver();
        var proc = new short[100];
        proc[0] = 1; // proc[0] 계속 1 유지 (시뮬레이터 리셋 없음)
        driver.SetRead("D2000", proc);
        driver.SetRead("D1900", new short[100]);

        var (loop, _) = CreateStack(driver);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var loopTask = loop.RunAsync(cts.Token);

        // 첫 번째 트리거 대기
        await driver.WaitForFirstWrite(TimeSpan.FromSeconds(7));
        // 추가 이터레이션이 돌아도 재트리거 없음을 확인하기 위해 잠시 대기
        await Task.Delay(350);

        cts.Cancel();
        try { await loopTask; } catch (OperationCanceledException) { }

        int d2001Writes = driver.WriteLog.Count(x => x.Addr == "D2001");
        Assert.Equal(1, d2001Writes);
    }

    [Fact]
    public async Task PlcReadLoop_SkipsCycle_OnDriverReadFailure()
    {
        var driver = new FailingDriver();
        var (loop, _) = CreateStack(driver);

        // 300ms 동안 실행 → ReadWords가 false를 반환해도 예외 없이 continue
        using var cts = new CancellationTokenSource(300);
        var loopTask = loop.RunAsync(cts.Token);

        var ex = await Record.ExceptionAsync(async () => await loopTask);

        Assert.True(ex is null or OperationCanceledException);
    }
}
