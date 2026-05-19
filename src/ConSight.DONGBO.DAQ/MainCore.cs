using System.IO;
using Bi.nsLogWriter;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.Net;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;
using ConSight.DAQ.Sequence;

namespace ConSight.DONGBO.DAQ;

public sealed partial class MainCore
{
    public static readonly MainCore Instance = new();

    #region Properties

    public LogWriter       Log      { get; } = new();
    public ProcessEventBus EventBus { get; } = new();

    internal TcpPlcDriver    PlcDriver   { get; private set; } = null!;
    internal ControlUnit_DAQ ControlUnit { get; private set; } = null!;
    internal PlcReadLoop     PlcLoop     { get; private set; } = null!;

    public const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True";

    private readonly CancellationTokenSource _cts = new();

    #endregion

    private MainCore() { }

    // ── 초기화 ────────────────────────────────────────────────────────────

    public void Initialize()
    {
        if (PlcLoop is not null) return;

        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(Initialize)}, Action=디바이스 초기화 시작");

        PlcDriver = new TcpPlcDriver("localhost", 5000);

        var buf200 = new PlcWriteBuffer(PlcDriver, "D2001", 3);
        var buf210 = new PlcWriteBuffer(PlcDriver, "D2201", 1);
        var buf220 = new PlcWriteBuffer(PlcDriver, "D2301", 1);
        var buf230 = new PlcWriteBuffer(PlcDriver, "D2401", 1);

        var csvWriter = new EmpgCsvWriter(
            Path.Combine(AppContext.BaseDirectory, "DAQ_CSV"));

        ControlUnit = new ControlUnit_DAQ(
            ConnectionString,
            new Op200WriteRegion(buf200),
            new Op210WriteRegion(buf210),
            new Op220WriteRegion(buf220),
            new Op230WriteRegion(buf230),
            csvWriter,
            EventBus);

        PlcLoop = new PlcReadLoop(PlcDriver, ControlUnit);

        SubscribeDriverEvents();

        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(Initialize)}, Action=초기화 완료");
    }

    // ── 루프 시작 / 종료 ─────────────────────────────────────────────────

    public void Start()
    {
        _ = ControlUnit.RunTimeTriggerLoopAsync(_cts.Token);
        _ = PlcLoop.RunAsync(_cts.Token);

        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(Start)}, Action=PLC 폴링 루프 시작");
    }

    public void Shutdown()
    {
        _cts.Cancel();
        _eventBusSubscription?.Dispose();
        PlcDriver?.CloseConnection();
        EventBus.Dispose();

        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(Shutdown)}, Action=DAQ 종료");
    }
}
