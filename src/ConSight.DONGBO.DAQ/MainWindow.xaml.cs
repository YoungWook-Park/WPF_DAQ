using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Views;
using ConSight.DONGBO.DAQ.Views;

namespace ConSight.DONGBO.DAQ;

public partial class MainWindow : Window
{
    private const string ConnectionString =
        @"Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True";

    private readonly ProcessEventBus _eventBus = new();
    private Inquiry_OP200_ResourceLotHistoryViewModel_EfCore _efVm = null!;

    public MainWindow()
    {
        InitializeComponent();
        InitViews();

        // Phase G: 공정 완료 이벤트 구독 → 하단 상태바 실시간 갱신
        _eventBus.Subscribe(OnProcessCompleted);
    }

    private void InitViews()
    {
        // ADO.NET View — 기존 코드 그대로
        var adoView = new Inquiry_OP200_ResourceLotHistoryView(ConnectionString);
        AdoViewHost.Content = adoView;

        // EF Core View — 독립 ViewModel
        _efVm = new Inquiry_OP200_ResourceLotHistoryViewModel_EfCore(ConnectionString);
        _efVm.PropertyChanged += OnEfVmPropertyChanged;
        var efView = new Inquiry_OP200_ResourceLotHistoryView(_efVm);
        EfViewHost.Content = efView;

        TxStatus.Text = "연결 완료";

        // Pipeline Test 탭
        var testView = new ProcessPipelineTestView(ConnectionString);
        TestViewHost.Content = testView;
    }

    // EF Core 탭: 조회 완료 시 하단 상태바에 경과시간 표시
    private void OnEfVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Inquiry_OP200_ResourceLotHistoryViewModel_EfCore.LastQueryElapsedMs))
            TxEfElapsed.Text = _efVm.LastQueryElapsedMs.ToString("N0");
    }

    // Phase G: 공정 완료 이벤트 핸들러 — 백그라운드 스레드에서 호출되므로 Dispatcher 경유
    private void OnProcessCompleted(EmpgRow row)
    {
        Dispatcher.InvokeAsync(() =>
        {
            TxStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {row.Model}  {row.MatSerial01}  {row.TotalJudge}";
        }, DispatcherPriority.Normal);
    }
}