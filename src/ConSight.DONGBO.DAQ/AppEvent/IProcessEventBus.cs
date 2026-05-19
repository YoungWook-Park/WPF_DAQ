using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.AppEvent
{
    public interface IProcessEventBus
    {
        /// <summary>공정 완료 EmpgRow 스트림. 구독자는 스레드 전환을 스스로 처리한다.</summary>
        IObservable<EmpgRow> AsObservable();

        /// <summary>공정 완료 시 EmpgRow를 모든 구독자에게 발행한다.</summary>
        void Publish(EmpgRow row);
    }
}
