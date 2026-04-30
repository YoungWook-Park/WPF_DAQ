// Phase E: IPlcWriteRegion — 서브공정 Write Region 공통 인터페이스
//
// ControlUnit_DAQ 의 TimeTrigger 루프가 OP210/220/230 를
// 동일한 코드로 처리할 수 있도록 추상화한다.
//
// 구현체: Op200WriteRegion, Op210WriteRegion, Op220WriteRegion, Op230WriteRegion

using ConSight.DAQ.Data;

namespace ConSight.DAQ.Device.PLC
{
    public interface IPlcWriteRegion
    {
        /// <summary>PC_Complete_Flag 워드에 결과값을 기록한다.</summary>
        void Set_PC_Complete_Flag(eDataBackup_ProcessResult result = eDataBackup_ProcessResult.OK);

        /// <summary>PC_Complete_Flag 워드를 NONE(0) 으로 초기화한다.</summary>
        void ReSet_PC_Complete_Flag();

        /// <summary>1초 펄스 리셋 신호를 TimeTrigger 큐에 예약한다.</summary>
        void EnqueueTimeTrigger(Write_TimeTriggerDataArgs args);

        /// <summary>
        /// 지연 시간이 경과한 TimeTrigger 항목을 꺼낸다.
        /// 아직 대기 중이거나 큐가 비어 있으면 null 을 반환한다.
        /// </summary>
        Write_TimeTriggerDataArgs? DequeueTimeTrigger();

        /// <summary>PC 메모리(버퍼 전체)를 드라이버로 전송한다.</summary>
        bool Cmd_Write();
    }
}
