// Step 6: Write_ProcessData_OP200 (정적 클래스) → Op200WriteRegion (인스턴스 DI) 로 대체
//
// AS-IS: static Int16[] WriteDataList + static Set_PC_Complete_Flag()
//         → 전역 상태, 테스트 불가, IPlcDriver 미추상화
//
// TO-BE: PlcWordRegionBase 상속, IPlcDriver 주입
//   SetWord / SetBit  →  PC 메모리만 변경
//   Cmd_Write()       →  현재 배열 전체를 드라이버로 전송 (배열 단위)
//   TimeTrigger       →  1초 딜레이 후 리셋 펄스 신호 생성

using ConSight.DAQ.Data;
using ConSight.DAQ.Device.PLC;

namespace ConSight.DAQ.Device.PLC.OP200
{
    // ── OP200 Write Buffer 레이아웃 (3 words) ─────────────────────────────
    //
    //  Index │ 이름               │ 타입      │ PLC 주소
    //  ──────┼────────────────────┼───────────┼─────────
    //    0   │ PC_Response        │ 비트 필드  │ D2001
    //    1   │ PC_Complete_Flag   │ Word 값   │ D2002  (NONE=0 / OK=1 / NG=2)
    //    2   │ PC_Power_On        │ Word 값   │ D2003
    //
    // ─────────────────────────────────────────────────────────────────────

    public sealed class Op200WriteRegion : PlcWordRegionBase, IPlcWriteRegion
    {
        // ── 워드 위치 ────────────────────────────────────────────────────

        public enum WordPos
        {
            PC_Response       = 0,   // 비트 필드 (BitPos_PC_Response 참조)
            PC_Complete_Flag  = 1,   // 백업 결과 값 (eDataBackup_ProcessResult)
            PC_Power_On       = 2,   // 전원 ON 플래그
        }

        // ── Word 0 비트 정의 (PC_Response) ──────────────────────────────
        // BRM/Inspection 프로젝트의 eBitPos_PLCRequest_PCResponse_Op300 패턴 참조

        public enum BitPos_PC_Response
        {
            NONE = -1,
            BackupStart_Received    = 0,   // PLC 백업 시작 요청 수신 확인
            Power_On_Status         = 1,   // PC 전원 ON 상태
            Backup_Complete_OK      = 2,   // 백업 완료 (정상)
            Backup_Complete_NG      = 3,   // 백업 완료 (불량)
            Sequence_Reset          = 9,   // 시퀀스 리셋
        }

        // ── TimeTrigger 큐 (펄스 신호 생성) ─────────────────────────────
        // Set → Cmd_Write (즉시) → Enqueue → 1초 뒤 Reset → Cmd_Write (펄스 완료)

        internal readonly TimeTriggerQueue TimeTrigger = new();

        public Op200WriteRegion(PlcWriteBuffer buffer) : base(buffer, baseOffset: 0) { }

        // ── PC_Response (Word 0) 비트 조작 ──────────────────────────────

        public void Set_BackupStart_Received()   =>
            SetBit((int)WordPos.PC_Response, (int)BitPos_PC_Response.BackupStart_Received, true);

        public void ReSet_BackupStart_Received() =>
            SetBit((int)WordPos.PC_Response, (int)BitPos_PC_Response.BackupStart_Received, false);

        public void Set_PowerOn_Status(bool on) =>
            SetBit((int)WordPos.PC_Response, (int)BitPos_PC_Response.Power_On_Status, on);

        public void Set_Sequence_Reset() =>
            SetBit((int)WordPos.PC_Response, (int)BitPos_PC_Response.Sequence_Reset, true);

        public void ReSet_Sequence_Reset() =>
            SetBit((int)WordPos.PC_Response, (int)BitPos_PC_Response.Sequence_Reset, false);

        // ── PC_Complete_Flag (Word 1) ────────────────────────────────────

        public void Set_PC_Complete_Flag(eDataBackup_ProcessResult result = eDataBackup_ProcessResult.OK) =>
            SetWord((int)WordPos.PC_Complete_Flag, (short)result);

        public void ReSet_PC_Complete_Flag() =>
            SetWord((int)WordPos.PC_Complete_Flag, (short)eDataBackup_ProcessResult.NONE);

        // ── PC_Power_On (Word 2) ─────────────────────────────────────────

        public void Set_PC_PowerOn_Flag(bool on = true) =>
            SetWord((int)WordPos.PC_Power_On, on ? (short)1 : (short)0);

        // ── TimeTrigger 큐 인터페이스 ────────────────────────────────────

        public void EnqueueTimeTrigger(Write_TimeTriggerDataArgs args) =>
            TimeTrigger.Enqueue(args);

        public Write_TimeTriggerDataArgs? DequeueTimeTrigger() =>
            TimeTrigger.Dequeue();
    }
}
