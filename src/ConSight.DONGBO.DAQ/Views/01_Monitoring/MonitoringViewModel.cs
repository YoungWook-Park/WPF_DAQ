using System.Collections.ObjectModel;
using System.Windows;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.Views.Monitoring
{
    public sealed class MonitoringViewModel
    {
        public ObservableCollection<EmpgRow> Rows { get; } = [];
        private const int MaxRows = 200;

        public MonitoringViewModel(IProcessEventBus eventBus)
        {
            eventBus.Subscribe(OnRow);
        }

        private void OnRow(EmpgRow row)
        {
            // Publish는 백그라운드 스레드에서 호출 → UI 컬렉션 변경은 Dispatcher 경유
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int idx = FindRow(row);
                if (idx >= 0)
                    Rows[idx] = row;          // OP210/220/230: 기존 행 갱신
                else
                {
                    Rows.Insert(0, row);       // OP200: 신규 행 최상단 삽입
                    if (Rows.Count > MaxRows)
                        Rows.RemoveAt(Rows.Count - 1);
                }
            });
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
