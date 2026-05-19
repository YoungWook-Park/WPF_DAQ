using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.Views.Monitoring
{
    public sealed class MonitoringViewModel : ObservableObject, IDisposable
    {
        public ObservableCollection<EmpgRow> Rows { get; } = [];
        private const int MaxRows = 100;

        private readonly IDisposable _subscription;

        public IRelayCommand Cmd_UcLoadedCommand { get; }
        public IRelayCommand<bool> CMD_VisibleChanged { get; }

        public MonitoringViewModel(IProcessEventBus eventBus)
        {
            _subscription = eventBus.AsObservable()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(OnRow);

            Cmd_UcLoadedCommand = new RelayCommand(PerformCmd_UcLoaded);
            CMD_VisibleChanged = new RelayCommand<bool>(PerformCMD_VisibleChanged);
        }

        public void Dispose() => _subscription.Dispose();

        private void PerformCmd_UcLoaded()
        {
        }

        private void PerformCMD_VisibleChanged(bool visible)
        {
        }

        // ObserveOnDispatcher() 로 UI 스레드 전환 — Application.Current.Dispatcher 불필요
        private void OnRow(EmpgRow row)
        {
            //int idx = FindRow(row);
            //if (idx >= 0)
            //    Rows[idx] = row;
            //else
            //{
            Rows.Insert(0, row);
            if (Rows.Count > MaxRows)
                Rows.RemoveAt(Rows.Count - 1);
            //}
        }

        // MatSerial01 기준으로 기존 행 탐색. 없으면 -1 반환.
        // OP230은 MatSerial02(GearSerial)도 포함하므로 양쪽 비교.
        private int FindRow(EmpgRow row)
        {
            if (string.IsNullOrEmpty(row.MatSerial01)) return -1;
            for (int i = 0; i < Rows.Count; i++)
                if (Rows[i].MatSerial01 == row.MatSerial01 ||
                    Rows[i].MatSerial02 == row.MatSerial01)
                    return i;
            return -1;
        }
    }
}
