using System.IO;
using System.Net;
using System.Net.Sockets;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.Net;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;
using ConSight.DAQ.Sequence;
using ConSight.DONGBO.DAQ.Tests.Helpers;
using ConSight.DONGBO.PlcSimulator.Logic;
using ConSight.DONGBO.PlcSimulator.Memory;
using ConSight.DONGBO.PlcSimulator.Net;
using Xunit;

namespace ConSight.DONGBO.DAQ.Tests;

[Trait("Category", "Integration")]
public sealed class Op200PipelineTests : IAsyncLifetime
{
    private PlcMemory _memory = null!;
    private PlcSimulatorServer _server = null!;
    private TcpPlcDriver _driver = null!;
    private CancellationTokenSource _cts = null!;
    private Task _loopTask = Task.CompletedTask;

    public async Task InitializeAsync()
    {
        int port = GetFreePort();
        _memory = new PlcMemory();
        _ = new SimulatorSignalHandler(_memory);
        _server = new PlcSimulatorServer(_memory, port);
        _server.Start();

        _driver = new TcpPlcDriver("localhost", port);

        var buf200 = new PlcWriteBuffer(_driver, "D2001", 3);
        var buf210 = new PlcWriteBuffer(_driver, "D2201", 1);
        var buf220 = new PlcWriteBuffer(_driver, "D2301", 1);
        var buf230 = new PlcWriteBuffer(_driver, "D2401", 1);

        var op200Write = new Op200WriteRegion(buf200);
        var op210Write = new Op210WriteRegion(buf210);
        var op220Write = new Op220WriteRegion(buf220);
        var op230Write = new Op230WriteRegion(buf230);

        var csvWriter = new EmpgCsvWriter(Path.Combine(Path.GetTempPath(), "daq_inttest_csv"));
        var eventBus  = new ProcessEventBus();
        var unit      = new ControlUnit_DAQ(
            SqlExpressSkip.ConnectionString,
            op200Write, op210Write, op220Write, op230Write,
            csvWriter, eventBus);

        _cts = new CancellationTokenSource();
        _loopTask = new PlcReadLoop(_driver, unit).RunAsync(_cts.Token);

        await Task.Delay(200); // TCP accept loop 시작 대기
    }

    public async Task DisposeAsync()
    {
        _cts.Cancel();
        _driver?.CloseConnection();
        _server?.Stop();
        try { await _loopTask; } catch { }
    }

    // SQLEXPRESS 미가동 또는 EMPG 테이블 없으면 vacuous pass (조건 없는 return)
    // xUnit 2.x 에서 SkipException을 InitializeAsync 밖 본문에서만 처리하므로 early-return 패턴 사용

    [Fact]
    public async Task TriggerOp200_WritesPcCompleteFlag_AfterProcessing()
    {
        if (SqlExpressSkip.GetSkipReason() != null) return;

        // d[1] > 0: OK(1) 또는 NG(2) 모두 처리 완료 신호
        var pcWritten = WaitForWrittenAsync("D2001", d => d.Length > 1 && d[1] > 0, 10_000);

        _memory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
        _memory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());

        Assert.True(await pcWritten, "10초 내에 PC_Complete_Flag(D2001[1]>0)가 쓰이지 않음");
    }

    [Fact]
    public async Task TriggerOp200_InsertsOrUpdatesRow_InDb()
    {
        if (SqlExpressSkip.GetSkipReason() != null) return;

        var pcWritten = WaitForWrittenAsync("D2001", d => d.Length > 1 && d[1] > 0, 10_000);

        _memory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
        _memory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());

        Assert.True(await pcWritten, "10초 내에 PC_Complete_Flag가 쓰이지 않음");

        var row = new SSMS_Op200(SqlExpressSkip.ConnectionString).FindBySerial("SN-00001");
        Assert.NotNull(row);
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────────

    private Task<bool> WaitForWrittenAsync(string addr, Func<short[], bool> predicate, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(string a, short[] data)
        {
            if (a == addr && predicate(data))
                tcs.TrySetResult(true);
        }

        _memory.Written += Handler;
        return Task.WhenAny(tcs.Task, Task.Delay(timeoutMs))
            .ContinueWith(_ =>
            {
                _memory.Written -= Handler;
                return tcs.Task.IsCompletedSuccessfully;
            });
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
