// Phase F: ControlUnit_DAQ — OP200~230 파이프라인 통합
//
// 변경 이력:
//   Phase A/B/C/D/E 에서 정의한 타입들을 조립해 완전한 공정 파이프라인을 구성한다.
//
// 주요 변경점:
//   1. ProcessData_Op200  : OP200_Process_DTO → Op200ProcessDto + EmpgRow 파이프라인
//   2. ProcessData_Op210/220/230 : FindBySerial → Apply → Update / InsertFallback 패턴
//   3. RunTimeTriggerLoopAsync  : IPlcWriteRegion 목록 순회로 일반화 (4개 공정 동일 루프)
//   4. DataBackUp_ResultSet     : IPlcWriteRegion 파라미터로 일반화
//   5. EmpgCsvWriter            : 모든 공정 완료 후 동일 Write 호출

using System.Diagnostics;
using Bi.nsExpException;
using Bi.nsLogWriter;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Data;
using ConSight.DAQ.Device;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;

namespace ConSight.DAQ.Sequence
{
    public class ControlUnit_DAQ
    {
        private readonly string           _connectionString;
        private readonly Op200WriteRegion _op200Write;
        private readonly Op210WriteRegion _op210Write;
        private readonly Op220WriteRegion _op220Write;
        private readonly Op230WriteRegion _op230Write;
        private readonly EmpgCsvWriter    _csvWriter;
        private readonly IProcessEventBus _eventBus;
        private readonly LogWriter        _log = new();

        // Phase F TimeTrigger 루프가 순회하는 Write Region 목록
        private readonly IReadOnlyList<IPlcWriteRegion> _allRegions;

        public ControlUnit_DAQ(
            string            connectionString,
            Op200WriteRegion  op200Write,
            Op210WriteRegion  op210Write,
            Op220WriteRegion  op220Write,
            Op230WriteRegion  op230Write,
            EmpgCsvWriter     csvWriter,
            IProcessEventBus  eventBus)
        {
            _connectionString = connectionString;
            _op200Write       = op200Write;
            _op210Write       = op210Write;
            _op220Write       = op220Write;
            _op230Write       = op230Write;
            _csvWriter        = csvWriter;
            _eventBus         = eventBus;
            _allRegions       = [op200Write, op210Write, op220Write, op230Write];
        }

        // ── OP200 : 메인 공정 ─────────────────────────────────────────────
        //
        // 파이프라인:
        //   FindBySerial(ShaftSerial) → 없으면 FindBySerial(GearSerial)
        //     found  : row.ApplyOp200(dto) → UpdateOp200Cols(row)
        //     missing: EmpgRow.From(dto)   → Insert(row)
        //   → STS_MODEL_TB 갱신 → CsvWriter.Append → DataBackUp_ResultSet
        //
        // Stopwatch 로 FindBySerial I/O 포함 전체 경과시간을 측정한다.
        // 인덱스 도입 전후 로그에서 경과시간을 비교하면 된다.

        internal void ProcessData_Op200(Op200ProcessDto dto)
        {
            //FeatureB Edit
            var sw = Stopwatch.StartNew();
            try
            {
                var ssms200 = new SSMS_Op200(_connectionString);
                //FeatureB Edit

                // ShaftSerial → GearSerial 순으로 기존 행 조회
                var existing = ssms200.FindBySerial(dto.ShaftSerial);
                if (existing == null && !string.IsNullOrEmpty(dto.GearSerial))
                    existing = ssms200.FindBySerial(dto.GearSerial);

                EmpgRow row;
                if (existing != null)
                {
                    existing.ApplyOp200(dto);
                    ssms200.UpdateOp200Cols(existing);
                    row = existing;
                    _log.WriteInformation(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP200 UPDATE  Serial={dto.ShaftSerial}  판정={dto.TotalJudge}");
                }
                else
                {
                    row = EmpgRow.From(dto);
                    ssms200.Insert(row);
                    _log.WriteInformation(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP200 INSERT  Serial={dto.ShaftSerial}  판정={dto.TotalJudge}");
                }

                // 모델 통계 갱신
                var ssmsModel = new SSMS_Model(_connectionString);
                var model     = ssmsModel.GetByModel(dto.Model);
                if (model == null)
                {
                    model = new ModelProduction(dto.Model);
                    model.ApplyResult(dto.TotalJudge);
                    ssmsModel.Insert(model);
                }
                else
                {
                    model.ApplyResult(dto.TotalJudge);
                    ssmsModel.Update(model);
                }

                _csvWriter.Append(row);
                _eventBus.Publish(row);

                sw.Stop();
                _log.WriteInformation(
                    $"[ProcessData_Op200] 경과={sw.ElapsedMilliseconds}ms  Model={dto.Model}  판정={dto.TotalJudge}");

                DataBackUp_ResultSet(_op200Write);
            }
            catch (ExpException expEx)
            {
                sw.Stop();
                DataBackUp_ResultSet(_op200Write, eDataBackup_ProcessResult.NG);
                _log.WriteExpException(expEx);
            }
            catch (Exception ex)
            {
                sw.Stop();
                DataBackUp_ResultSet(_op200Write, eDataBackup_ProcessResult.NG);
                _log.WriteException(ex);
            }
        }

        // ── OP210 : RunOut Check (단일 시리얼) ────────────────────────────
        //
        // 파이프라인:
        //   FindBySerial(dto.Serial) →
        //     found  : row.ApplyOp210(dto) → UpdateSubCols(row)
        //     missing: BuildFallback()     → ApplyOp210(dto) → InsertFallback(row)
        //   → CsvWriter.Append(row) → DataBackUp_ResultSet(_op210Write)

        internal void ProcessData_Op210(Op210ProcessDto dto)
        {
            try
            {
                var ssms = new SSMS_SubProcess(_connectionString);
                var row  = ssms.FindBySerial(dto.Serial);

                if (row != null)
                {
                    row.ApplyOp210(dto);
                    ssms.UpdateSubCols(row);
                    _log.WriteInformation(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP210 UPDATE  Serial={dto.Serial}  판정={row.TotalJudge}");
                }
                else
                {
                    _log.Write(LogLevel.Low,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP210 [경고] OP200 행 없음 → InsertFallback  Serial={dto.Serial}");

                    row = BuildFallback(dto.Repair, dto.Model, dto.Serial, string.Empty);
                    row.ApplyOp210(dto);
                    ssms.InsertFallback(row);
                }

                _csvWriter.Append(row);
                _eventBus.Publish(row);
                DataBackUp_ResultSet(_op210Write);
            }
            catch (ExpException expEx)
            {
                DataBackUp_ResultSet(_op210Write, eDataBackup_ProcessResult.NG);
                _log.WriteExpException(expEx);
            }
            catch (Exception ex)
            {
                DataBackUp_ResultSet(_op210Write, eDataBackup_ProcessResult.NG);
                _log.WriteException(ex);
            }
        }

        // ── OP220 : Guiding Press Fitting (단일 시리얼) ───────────────────

        internal void ProcessData_Op220(Op220ProcessDto dto)
        {
            try
            {
                var ssms = new SSMS_SubProcess(_connectionString);
                var row  = ssms.FindBySerial(dto.Serial);

                if (row != null)
                {
                    row.ApplyOp220(dto);
                    ssms.UpdateSubCols(row);
                    _log.WriteInformation(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP220 UPDATE  Serial={dto.Serial}  판정={row.TotalJudge}");
                }
                else
                {
                    _log.Write(LogLevel.Low,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP220 [경고] OP200 행 없음 → InsertFallback  Serial={dto.Serial}");

                    row = BuildFallback(dto.Repair, dto.Model, dto.Serial, string.Empty);
                    row.ApplyOp220(dto);
                    ssms.InsertFallback(row);
                }

                _csvWriter.Append(row);
                _eventBus.Publish(row);
                DataBackUp_ResultSet(_op220Write);
            }
            catch (ExpException expEx)
            {
                DataBackUp_ResultSet(_op220Write, eDataBackup_ProcessResult.NG);
                _log.WriteExpException(expEx);
            }
            catch (Exception ex)
            {
                DataBackUp_ResultSet(_op220Write, eDataBackup_ProcessResult.NG);
                _log.WriteException(ex);
            }
        }

        // ── OP230 : Lotite / Shaft Oil Cap (시리얼 2개) ──────────────────
        //
        // Serial01 으로 먼저 조회 → 없으면 Serial02 로 재조회 → 둘 다 없으면 InsertFallback

        internal void ProcessData_Op230(Op230ProcessDto dto)
        {
            try
            {
                var ssms = new SSMS_SubProcess(_connectionString);
                var row  = ssms.FindBySerial(dto.Serial01);

                if (row == null && !string.IsNullOrEmpty(dto.Serial02))
                    row = ssms.FindBySerial(dto.Serial02);

                if (row != null)
                {
                    row.ApplyOp230(dto);
                    ssms.UpdateSubCols(row);
                    _log.WriteInformation(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP230 UPDATE  Serial01={dto.Serial01}  판정={row.TotalJudge}");
                }
                else
                {
                    _log.Write(LogLevel.Low,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] OP230 [경고] OP200 행 없음 → InsertFallback  Serial01={dto.Serial01}");

                    row = BuildFallback(dto.Repair, dto.Model, dto.Serial01, dto.Serial02);
                    row.ApplyOp230(dto);
                    ssms.InsertFallback(row);
                }

                _csvWriter.Append(row);
                _eventBus.Publish(row);
                DataBackUp_ResultSet(_op230Write);
            }
            catch (ExpException expEx)
            {
                DataBackUp_ResultSet(_op230Write, eDataBackup_ProcessResult.NG);
                _log.WriteExpException(expEx);
            }
            catch (Exception ex)
            {
                DataBackUp_ResultSet(_op230Write, eDataBackup_ProcessResult.NG);
                _log.WriteException(ex);
            }
        }

        // ── TimeTrigger 루프 (일반화) ─────────────────────────────────────
        //
        // 10ms 주기로 _allRegions 의 모든 Write Region 을 순회하며
        // 타임아웃된 TimeTrigger 항목을 소비한다.
        //
        // 펄스 흐름:
        //   DataBackUp_ResultSet → Set(OK/NG) + Cmd_Write (즉시 ON)
        //                        → EnqueueTimeTrigger(ReSet, 1000ms)
        //       ↓ 1초 후
        //   RunTimeTriggerLoopAsync → Dequeue 성공
        //                          → ReSet + Cmd_Write (OFF = 펄스 완료)

        public async Task RunTimeTriggerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var region in _allRegions)
                {
                    var trig = region.DequeueTimeTrigger();
                    if (trig == null) continue;

                    if (trig.TriggerJob == ePLCWriteWord_TimeTrigger.ReSet_PC_Complete_Flag)
                        region.ReSet_PC_Complete_Flag();

                    region.Cmd_Write();
                }

                await Task.Delay(10, token).ConfigureAwait(false);
            }
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────

        private void DataBackUp_ResultSet(
            IPlcWriteRegion region,
            eDataBackup_ProcessResult result = eDataBackup_ProcessResult.OK)
        {
            region.Set_PC_Complete_Flag(result);
            region.Cmd_Write();
            region.EnqueueTimeTrigger(
                new Write_TimeTriggerDataArgs(ePLCWriteWord_TimeTrigger.ReSet_PC_Complete_Flag));
        }

        /// <summary>
        /// OP200 행이 DB에 없을 때 서브공정 데이터만으로 EMPG 행 뼈대를 구성한다.
        /// TotalJudge 는 NG 로 초기화 (OP200 판정 부재).
        /// </summary>
        private static EmpgRow BuildFallback(
            string repair, string model, string serial01, string serial02)
        {
            return new EmpgRow
            {
                ResultId    = Guid.NewGuid().ToString("N"),
                UpdateTime  = DateTime.Now,
                Repair      = repair,
                Model       = model,
                MatSerial01 = serial01,
                MatSerial02 = string.IsNullOrEmpty(serial02) ? "DB SERIAL NULL" : serial02,
                TotalJudge  = "NG",
            };
        }
    }
}
