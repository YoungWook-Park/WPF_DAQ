using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Views;
using ConSight.DAQ.Views.Monitoring;
using ConSight.DONGBO.DAQ.Views;

namespace ConSight.DONGBO.DAQ;

public partial class MainWindow : Window
{
    private Inquiry_OP200_ResourceLotHistoryViewModel_EfCore _efVm = null!;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += Window_Loaded;
        Closed += Window_Closed;
    }

    // ── 윈도우 이벤트 ─────────────────────────────────────────────────────

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        InitViews();
        MainCore.Instance.Initialize();
        MainCore.Instance.Start();
        MainCore.Instance.EventBus.Subscribe(cFunc_EventBus_ProcessCompleted);
        TxStatus.Text = "초기화 완료 — PLC 대기중";
    }

    private void Window_Closed(object? sender, EventArgs e) =>
        MainCore.Instance.Shutdown();

    // ── 뷰 초기화 (UI 배선만 담당) ────────────────────────────────────────

    private void InitViews()
    {
        AdoViewHost.Content = new Inquiry_OP200_ResourceLotHistoryView(MainCore.ConnectionString);

        _efVm = new Inquiry_OP200_ResourceLotHistoryViewModel_EfCore(MainCore.ConnectionString);
        _efVm.PropertyChanged += OnEfVmPropertyChanged;
        EfViewHost.Content = new Inquiry_OP200_ResourceLotHistoryView(_efVm);

        TestViewHost.Content = new ProcessPipelineTestView(MainCore.ConnectionString);

        MonitoringViewHost.Content = new MonitoringView(
            new MonitoringViewModel(MainCore.Instance.EventBus));
    }

    // ── 콜백 ─────────────────────────────────────────────────────────────

    private void OnEfVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Inquiry_OP200_ResourceLotHistoryViewModel_EfCore.LastQueryElapsedMs))
            TxEfElapsed.Text = _efVm.LastQueryElapsedMs.ToString("N0");
    }

    private void cFunc_EventBus_ProcessCompleted(EmpgRow row)
    {
        Dispatcher.InvokeAsync(() =>
        {
            TxStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {row.Model}  {row.MatSerial01}  {row.TotalJudge}";
        }, DispatcherPriority.Normal);
    }
}
