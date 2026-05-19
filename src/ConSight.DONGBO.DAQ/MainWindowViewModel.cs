using System.ComponentModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Views;

namespace ConSight.DONGBO.DAQ;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private string _txStatusText   = "준비";
    [ObservableProperty] private string _txEfElapsedText = "—";

    public Inquiry_OP200_ResourceLotHistoryViewModel_EfCore EfVm { get; }

    private IDisposable? _subscription;

    public MainWindowViewModel()
    {
        EfVm = new Inquiry_OP200_ResourceLotHistoryViewModel_EfCore(MainCore.ConnectionString);
        EfVm.PropertyChanged += OnEfVmPropertyChanged;
    }

    public void Initialize()
    {
        MainCore.Instance.Initialize();
        MainCore.Instance.Start();
        _subscription = MainCore.Instance.EventBus.AsObservable()
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(cFunc_EventBus_ProcessCompleted);
        TxStatusText = "초기화 완료 — PLC 대기중";
    }

    public void Shutdown()
    {
        _subscription?.Dispose();
        MainCore.Instance.Shutdown();
    }

    public void Dispose() => Shutdown();

    private void cFunc_EventBus_ProcessCompleted(EmpgRow row)
    {
        TxStatusText = $"[{DateTime.Now:HH:mm:ss}] {row.Model}  {row.MatSerial01}  {row.TotalJudge}";
    }

    private void OnEfVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EfVm.LastQueryElapsedMs))
            TxEfElapsedText = EfVm.LastQueryElapsedMs.ToString("N0");
    }
}
