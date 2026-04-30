// Phase G: IProcessEventBus — 공정 완료 이벤트를 EmpgRow 타입으로 발행/구독
//
// AS-IS: NormValueDictionary[key] = row (object 박싱) + ProcessMessageItem(MsgObject?)
//         → string 키 오타, 컴파일 타임 타입 검사 없음, 구독자 목록 관리 불가
//
// TO-BE: Publish(EmpgRow) / Subscribe(Action<EmpgRow>) → 타입 안전, 박싱 없음

using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.AppEvent
{
    public interface IProcessEventBus
    {
        /// <summary>공정 완료 시 EmpgRow를 모든 구독자에게 발행한다.</summary>
        void Publish(EmpgRow row);

        /// <summary>EmpgRow 수신 핸들러를 등록한다.</summary>
        void Subscribe(Action<EmpgRow> handler);

        /// <summary>등록된 핸들러를 해제한다.</summary>
        void Unsubscribe(Action<EmpgRow> handler);
    }
}
