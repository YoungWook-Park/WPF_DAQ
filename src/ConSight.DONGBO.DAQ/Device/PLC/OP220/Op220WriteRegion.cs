// Phase E: Op220WriteRegion — OP220 (Guiding Press Fitting) PC Write 버퍼 래퍼
//
// AS-IS: Write_ProcessData_OP220 (static Int16[] WriteDataList, static TimeTriggerQueue)
// TO-BE: PlcWordRegionBase 상속 + IPlcWriteRegion 구현, IPlcDriver DI
//
// Write Buffer 레이아웃 (1 word):
//
//  Index │ 이름             │ 타입    │ PLC 주소
//  ──────┼──────────────────┼─────────┼─────────
//    0   │ PC_Complete_Flag │ Word 값 │ D2301  (NONE=0 / OK=1 / NG=2)
//
// PlcWriteBuffer 생성 시 device address = "D2301", wordCount = 1 로 주입한다.

using ConSight.DAQ.Data;
using ConSight.DAQ.Device.PLC;

namespace ConSight.DAQ.Device.PLC.OP220
{
    public sealed class Op220WriteRegion : PlcWordRegionBase, IPlcWriteRegion
    {
        public enum WordPos
        {
            PC_Complete_Flag = 0,
        }

        internal readonly TimeTriggerQueue TimeTrigger = new();

        public Op220WriteRegion(PlcWriteBuffer buffer) : base(buffer, baseOffset: 0) { }

        // ── PC_Complete_Flag (Word 0) ────────────────────────────────────

        public void Set_PC_Complete_Flag(eDataBackup_ProcessResult result = eDataBackup_ProcessResult.OK) =>
            SetWord((int)WordPos.PC_Complete_Flag, (short)result);

        public void ReSet_PC_Complete_Flag() =>
            SetWord((int)WordPos.PC_Complete_Flag, (short)eDataBackup_ProcessResult.NONE);

        // ── TimeTrigger 큐 인터페이스 ────────────────────────────────────

        public void EnqueueTimeTrigger(Write_TimeTriggerDataArgs args) =>
            TimeTrigger.Enqueue(args);

        public Write_TimeTriggerDataArgs? DequeueTimeTrigger() =>
            TimeTrigger.Dequeue();
    }
}
