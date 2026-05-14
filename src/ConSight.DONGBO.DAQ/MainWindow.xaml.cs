using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
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
using ConSight.DAQ.Views;
using ConSight.DONGBO.DAQ.Views;

namespace ConSight.DONGBO.DAQ;

public partial class MainWindow : Window
{
    private const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True";

    private readonly ProcessEventBus _eventBus = new();
    private Inquiry_OP200_ResourceLotHistoryViewModel_EfCore _efVm = null!;

    private TcpPlcDriver _tcpDriver = null!;
    private PlcReadLoop  _plcLoop   = null!;
    private readonly CancellationTokenSource _cts = new();

    public MainWindow()
    {
        InitializeComponent();
        InitViews();

        _eventBus.Subscribe(OnProcessCompleted);
    }

    private void InitViews()
    {
        // ADO.NET View
        var adoView = new Inquiry_OP200_ResourceLotHistoryView(ConnectionString);
        AdoViewHost.Content = adoView;

        // EF Core View
        _efVm = new Inquiry_OP200_ResourceLotHistoryViewModel_EfCore(ConnectionString);
        _efVm.PropertyChanged += OnEfVmPropertyChanged;
        var efView = new Inquiry_OP200_ResourceLotHistoryView(_efVm);
        EfViewHost.Content = efView;

        TxStatus.Text = "연결 완료";

        // Pipeline Test 탭
        var testView = new ProcessPipelineTestView(ConnectionString);
        TestViewHost.Content = testView;

        // PLC 인프라 wire-up
        _tcpDriver = new TcpPlcDriver("localhost", 5000);

        var buf200 = new PlcWriteBuffer(_tcpDriver, "D2001", 3);
        var buf210 = new PlcWriteBuffer(_tcpDriver, "D2201", 1);
        var buf220 = new PlcWriteBuffer(_tcpDriver, "D2301", 1);
        var buf230 = new PlcWriteBuffer(_tcpDriver, "D2401", 1);

        var op200Write = new Op200WriteRegion(buf200);
        var op210Write = new Op210WriteRegion(buf210);
        var op220Write = new Op220WriteRegion(buf220);
        var op230Write = new Op230WriteRegion(buf230);

        var csvWriter   = new EmpgCsvWriter(Path.Combine(AppContext.BaseDirectory, "DAQ_CSV"));
        var controlUnit = new ControlUnit_DAQ(
            ConnectionString, op200Write, op210Write, op220Write, op230Write, csvWriter, _eventBus);

        _plcLoop = new PlcReadLoop(_tcpDriver, controlUnit);

        // MonitoringViewHost.Content は C5 で設定
        _ = controlUnit.RunTimeTriggerLoopAsync(_cts.Token);
        _ = _plcLoop.RunAsync(_cts.Token);
    }

    private void OnEfVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Inquiry_OP200_ResourceLotHistoryViewModel_EfCore.LastQueryElapsedMs))
            TxEfElapsed.Text = _efVm.LastQueryElapsedMs.ToString("N0");
    }

    // 공정 완료 이벤트 — 백그라운드 스레드에서 호출되므로 Dispatcher 경유
    private void OnProcessCompleted(EmpgRow row)
    {
        Dispatcher.InvokeAsync(() =>
        {
            TxStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {row.Model}  {row.MatSerial01}  {row.TotalJudge}";
        }, DispatcherPriority.Normal);
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _cts.Cancel();
        _tcpDriver?.CloseConnection();
    }
}
