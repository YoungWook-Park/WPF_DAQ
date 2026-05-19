using System.Reactive.Subjects;
using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.AppEvent
{
    public sealed class ProcessEventBus : IProcessEventBus, IDisposable
    {
        private readonly Subject<EmpgRow> _subject = new();

        public IObservable<EmpgRow> AsObservable() => _subject;

        public void Publish(EmpgRow row) => _subject.OnNext(row);

        public void Dispose() => _subject.OnCompleted();
    }
}
