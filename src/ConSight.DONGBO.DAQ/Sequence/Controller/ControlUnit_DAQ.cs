// Step 4 핵심 변경:
//   OP100 원격 DB 조회 블록 제거 → DataBackUp_ResultSet() 직접 호출
//
// Step 6 핵심 변경:
//   AS-IS: static Write_ProcessData_OP200 + PLC 드라이버 미추상화
//   TO-BE: IPlcDriver 주입, Op200WriteRegion DI
//          DataBackUp_ResultSet → op200Write.Set_PC_Complete_Flag + Cmd_Write (즉시)
//                               + EnqueueTimeTrigger (1초 뒤 Reset 펄스)
//          RunTimeTriggerLoopAsync → TimeTrigger 큐 소비 루프 (10ms 주기)

using Bi.nsExpException;
using Bi.nsLogWriter;
using ConSight.DAQ.Data;
using ConSight.DAQ.Device;
using ConSight.DAQ.Device.PLC.OP200;
using System.Text;

namespace ConSight.DAQ.Sequence
{
    public class ControlUnit_DAQ
    {
        private readonly string _connectionString;
        private readonly Op200WriteRegion _op200Write;
        private readonly LogWriter _log = new();

        public ControlUnit_DAQ(string connectionString, Op200WriteRegion op200Write)
        {
            _connectionString = connectionString;
            _op200Write       = op200Write;
        }

        // ── Step 4: OP100 원격 DB 조회 블록 제거 ─────────────────────────
        //
        // [AS-IS 제거 대상 — 원본 ControlUnit_DAQ.cs L488~526]
        //   SSMS_OP100 ssms_Op100 = new SSMS_OP100();
        //   bool hasSerial_Op100 = ...  (원격 DB 조회 분기)
        //   if (hasSerial_Op100) DataBackUp_ResultSet(OK);
        //   else                 DataBackUp_ResultSet(NG);
        //
        // [TO-BE] OP100 DB 왕복 제거, 항상 정상 Complete
        internal void ProcessData_Op200_Data_Update(OP200_Process_DTO resultData)
        {
            try
            {
                string tran_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                var ssms_Model = new SSMS_Model(_connectionString);
                var model = ssms_Model.GetByModel(resultData.PLC_Model_Name);

                if (model == null)
                {
                    model = new ModelProduction(resultData.PLC_Model_Name);
                    model.ApplyResult(resultData.PLC_Total_Judgement);
                    ssms_Model.Insert(model);
                }
                else
                {
                    model.ApplyResult(resultData.PLC_Total_Judgement);
                    ssms_Model.Update(model);
                }

                var ssms_Op200 = new SSMS_Op200(_connectionString);
                ssms_Op200.Insert(resultData);

                _log.WriteInformation(
                    $"[{tran_time}] 공정 완료, Model={resultData.PLC_Model_Name}, 판정={resultData.PLC_Total_Judgement}");

                DataBackUp_ResultSet();
            }
            catch (ExpException expEx)
            {
                DataBackUp_ResultSet(eDataBackup_ProcessResult.NG);
                _log.WriteExpException(expEx);
            }
            catch (Exception ex)
            {
                DataBackUp_ResultSet(eDataBackup_ProcessResult.NG);
                _log.WriteException(ex);
            }
        }

        // ── Step 6: TimeTrigger 루프 ──────────────────────────────────────
        //
        // 10ms 주기로 TimeTrigger 큐를 소비한다.
        // 타임아웃(기본 1초)이 된 항목만 Dequeue되므로, 큐 자체가 딜레이를 담당한다.
        //
        // 펄스 흐름:
        //   DataBackUp_ResultSet  →  Set(OK/NG) + Cmd_Write (즉시 신호 ON)
        //                         →  Enqueue(ReSet, 1000ms)
        //       ↓ 1초 후
        //   RunTimeTriggerLoopAsync  →  Dequeue() 성공
        //                            →  ReSet + Cmd_Write (신호 OFF = 펄스 완료)
        public async Task RunTimeTriggerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var trig = _op200Write.DequeueTimeTrigger();
                if (trig != null)
                {
                    switch (trig.TriggerJob)
                    {
                        case ePLCWriteWord_TimeTrigger.ReSet_PC_Complete_Flag:
                            _op200Write.ReSet_PC_Complete_Flag();
                            break;
                    }
                    _op200Write.Cmd_Write();
                }

                await Task.Delay(10, token).ConfigureAwait(false);
            }
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────

        private void DataBackUp_ResultSet(eDataBackup_ProcessResult result = eDataBackup_ProcessResult.OK)
        {
            // 1. PC 메모리 설정 후 즉시 드라이버로 전송 (신호 ON)
            _op200Write.Set_PC_Complete_Flag(result);
            _op200Write.Cmd_Write();

            // 2. 1초 뒤 리셋 펄스 예약 (신호 OFF)
            _op200Write.EnqueueTimeTrigger(
                new Write_TimeTriggerDataArgs(ePLCWriteWord_TimeTrigger.ReSet_PC_Complete_Flag));
        }
    }
}
