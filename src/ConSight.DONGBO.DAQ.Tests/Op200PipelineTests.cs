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
    private PlcMemory            _plcMemory          = null!;
    private PlcSimulatorServer   _simulatorServer    = null!;
    private TcpPlcDriver         _plcDriver          = null!;
    private CancellationTokenSource _cancellationSource = null!;
    private Task _loopTask = Task.CompletedTask;

    public async Task InitializeAsync()
    {
        int port    = GetFreePort();
        _plcMemory  = new PlcMemory();
        _ = new SimulatorSignalHandler(_plcMemory);
        _simulatorServer = new PlcSimulatorServer(_plcMemory, port);
        _simulatorServer.Start();

        _plcDriver = new TcpPlcDriver("localhost", port);

        var op200Buffer      = new PlcWriteBuffer(_plcDriver, "D2001", 3);
        var op210Buffer      = new PlcWriteBuffer(_plcDriver, "D2201", 1);
        var op220Buffer      = new PlcWriteBuffer(_plcDriver, "D2301", 1);
        var op230Buffer      = new PlcWriteBuffer(_plcDriver, "D2401", 1);

        var op200WriteRegion = new Op200WriteRegion(op200Buffer);
        var op210WriteRegion = new Op210WriteRegion(op210Buffer);
        var op220WriteRegion = new Op220WriteRegion(op220Buffer);
        var op230WriteRegion = new Op230WriteRegion(op230Buffer);

        var csvWriter   = new EmpgCsvWriter(Path.Combine(Path.GetTempPath(), "daq_inttest_csv"));
        var eventBus    = new ProcessEventBus();
        var controlUnit = new ControlUnit_DAQ(
            SqlExpressSkip.ConnectionString,
            op200WriteRegion, op210WriteRegion, op220WriteRegion, op230WriteRegion,
            csvWriter, eventBus);

        _cancellationSource = new CancellationTokenSource();
        _loopTask = new PlcReadLoop(_plcDriver, controlUnit).RunAsync(_cancellationSource.Token);

        await Task.Delay(200); // TCP accept 루프 시작 대기
    }

    public async Task DisposeAsync()
    {
        _cancellationSource.Cancel();
        _plcDriver?.CloseConnection();
        _simulatorServer?.Stop();
        try { await _loopTask; } catch { }

        if (SqlExpressSkip.GetSkipReason() == null)
        {
            using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(SqlExpressSkip.ConnectionString);
            sqlConnection.Open();
            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText =
                "DELETE FROM EMPG WHERE MAT_SERIAL01='SN-00001';" +
                "DELETE FROM STS_MODEL_TB WHERE MODEL='MODEL-A';";
            try { sqlCommand.ExecuteNonQuery(); } catch { }
        }
    }

    // SQLEXPRESS 미가동 또는 EMPG 테이블 없으면 vacuous pass (early-return)

    [Fact]
    public async Task TriggerOp200_WritesPcCompleteFlag_AfterProcessing()
    {
        if (SqlExpressSkip.GetSkipReason() != null) return;

        // d[1] > 0: OK(1) 또는 NG(2) 모두 처리 완료 신호
        var completeSignalWritten = WaitForWrittenAsync("D2001", data => data.Length > 1 && data[1] > 0, 10_000);

        _plcMemory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
        _plcMemory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());

        Assert.True(await completeSignalWritten, "10초 내에 PC_Complete_Flag(D2001[1]>0)가 쓰이지 않음");
    }

    [Fact]
    public async Task TriggerOp200_InsertsOrUpdatesRow_InDb()
    {
        if (SqlExpressSkip.GetSkipReason() != null) return;

        var completeSignalWritten = WaitForWrittenAsync("D2001", data => data.Length > 1 && data[1] > 0, 10_000);

        _plcMemory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
        _plcMemory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());

        Assert.True(await completeSignalWritten, "10초 내에 PC_Complete_Flag가 쓰이지 않음");

        var row = new SSMS_Op200(SqlExpressSkip.ConnectionString).FindBySerial("SN-00001");
        Assert.NotNull(row);
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────────

    private Task<bool> WaitForWrittenAsync(string address, Func<short[], bool> predicate, int timeoutMs)
    {
        var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(string writtenAddress, short[] writtenData)
        {
            if (writtenAddress == address && predicate(writtenData))
                completionSource.TrySetResult(true);
        }

        _plcMemory.Written += Handler;
        return Task.WhenAny(completionSource.Task, Task.Delay(timeoutMs))
            .ContinueWith(_ =>
            {
                _plcMemory.Written -= Handler;
                return completionSource.Task.IsCompletedSuccessfully;
            });
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
