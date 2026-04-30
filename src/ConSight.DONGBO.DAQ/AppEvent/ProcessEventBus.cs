// Phase G: ProcessEventBus — IProcessEventBus 스레드 안전 구현
//
// Publish()는 공정 백그라운드 스레드에서 호출된다.
// 핸들러가 UI를 갱신하는 경우 핸들러 내부에서 Dispatcher.InvokeAsync를 사용해야 한다.
// ProcessEventBus 자체는 Dispatcher를 알지 못한다 — 스레드 전환은 구독자 책임.

using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.AppEvent
{
    public sealed class ProcessEventBus : IProcessEventBus
    {
        private event Action<EmpgRow>? _handlers;
        private readonly object _lock = new();

        public void Publish(EmpgRow row)
        {
            Action<EmpgRow>? snapshot;
            lock (_lock) { snapshot = _handlers; }
            snapshot?.Invoke(row);
        }

        public void Subscribe(Action<EmpgRow> handler)
        {
            lock (_lock) { _handlers += handler; }
        }

        public void Unsubscribe(Action<EmpgRow> handler)
        {
            lock (_lock) { _handlers -= handler; }
        }
    }
}
