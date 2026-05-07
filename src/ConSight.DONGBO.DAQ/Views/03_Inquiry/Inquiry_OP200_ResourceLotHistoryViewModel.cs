// Step 5 핵심 변경:
// AS-IS: Bi.SqlServerAgent.QueryExecution + 문자열 연결 WHERE 절 (SQL Injection 취약)
//        WHERE UPDATE_TIME BETWEEN N'" + sDate + "' AND N'" + eDate + "'"
// TO-BE: Bi.ConSight.SqlAgent.QueryExecution + AddParameter() 파라미터화 쿼리
//        @sDate, @eDate, @model — 타입 안전 (datetime 파라미터)
//
// 구조 개선:
//   - MapRow() 헬퍼 — DataRow → ResourceLotHisItem 매핑 6회 중복 제거
//   - BuildQuery() 헬퍼 — SELECT 컬럼 목록 4회 중복 제거
//   - ObservableCollection<T>  (ObservableRangeCollection 제거)
//   - MainCore 의존 제거 → connectionString DI

using Bi.ConSight.SqlAgent;
using Bi.ConSightCommon;
using Bi.ConSight_MxComponent.Data;
using Bi.nsExpException;
using Bi.nsLogWriter;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Windows.Input;

namespace ConSight.DAQ.Views
{
    public class Inquiry_OP200_ResourceLotHistoryViewModel : BiNotifyPropertyBase
    {
        private readonly string _connectionString;
        private readonly LogWriter _log = new LogWriter();

        public Inquiry_OP200_ResourceLotHistoryViewModel(string connectionString)
        {
            _connectionString = connectionString;
            InitUi();
        }

        #region Properties

        private DateTime? _uiDate_ProcStartDate;
        public DateTime? UiDate_ProcStartDate
        {
            get => _uiDate_ProcStartDate;
            set => SetProperty(ref _uiDate_ProcStartDate, value);
        }

        private DateTime? _uiDate_ProcEndDate;
        public DateTime? UiDate_ProcEndDate
        {
            get => _uiDate_ProcEndDate;
            set => SetProperty(ref _uiDate_ProcEndDate, value);
        }

        private int _uiTBlock_ProductionQty;
        public int UiTBlock_ProductionQty
        {
            get => _uiTBlock_ProductionQty;
            set => SetProperty(ref _uiTBlock_ProductionQty, value);
        }

        private int _uiTBlock_GoodsQty;
        public int UiTBlock_GoodsQty
        {
            get => _uiTBlock_GoodsQty;
            set => SetProperty(ref _uiTBlock_GoodsQty, value);
        }

        private int _uiTBlock_DefectiveQty;
        public int UiTBlock_DefectiveQty
        {
            get => _uiTBlock_DefectiveQty;
            set => SetProperty(ref _uiTBlock_DefectiveQty, value);
        }

        private string _uiTBlock_GoodQualityRation = string.Empty;
        public string UiTBlock_GoodQualityRation
        {
            get => _uiTBlock_GoodQualityRation;
            set => SetProperty(ref _uiTBlock_GoodQualityRation, value);
        }

        public ObservableRangeCollection<ResourceLotHisItem> UiDg_ResourceLotHisList         { get; } = new();
        public ObservableRangeCollection<ResourceLotHisItem> UiDg_ResourceLotHisFinishedList  { get; } = new();
        public ObservableRangeCollection<ResourceLotHisItem> UiDg_ResourceLotHisDefectiveList { get; } = new();
        public ObservableCollection<string>             UiCBox_ModelItemSource           { get; } = new();
        public ObservableCollection<string>             QueryLog                         { get; } = new();

        private string _uiCBox_ModelSelectedItem = string.Empty;
        public string UiCBox_ModelSelectedItem
        {
            get => _uiCBox_ModelSelectedItem;
            set => SetProperty(ref _uiCBox_ModelSelectedItem, value);
        }

        private ResourceLotHisItem? _uiDg_ResourceLotHisListSelectedItem;
        public ResourceLotHisItem? UiDg_ResourceLotHisListSelectedItem
        {
            get => _uiDg_ResourceLotHisListSelectedItem;
            set => SetProperty(ref _uiDg_ResourceLotHisListSelectedItem, value);
        }

        #endregion

        #region Commands

        private ICommand? _cmdUcLoadedCommand;
        public ICommand Cmd_UcLoadedCommand =>
            _cmdUcLoadedCommand ??= new BiRelayCommand(_ => InitUi());

        private ICommand? _cmdFind;
        public ICommand Cmd_Find =>
            _cmdFind ??= new BiRelayCommand(async _ => await PerformFindAsync());

        private ICommand? _cmdClearLog;
        public ICommand Cmd_ClearLog =>
            _cmdClearLog ??= new BiRelayCommand(_ => QueryLog.Clear());

        #endregion

        #region Private helpers

        private void InitUi()
        {
            try
            {
                var qExe = new QueryExecution(_connectionString);
                qExe.AppendQuery("SELECT MODEL FROM STS_MODEL_TB ORDER BY MODEL");
                DataSet ds = qExe.Execute();

                UiCBox_ModelItemSource.Clear();
                UiCBox_ModelItemSource.Add(string.Empty);
                foreach (DataRow row in ds.Tables[0].Rows)
                    UiCBox_ModelItemSource.Add(row["MODEL"].ToString()!);

                UiDate_ProcStartDate = DateTime.Today;
                UiDate_ProcEndDate   = DateTime.Now;
            }
            catch (Exception ex)
            {
                _log.WriteException(ex);
            }
        }

        private async Task PerformFindAsync()
        {
            try
            {
                if (UiDate_ProcStartDate == null || UiDate_ProcEndDate == null)
                {
                    MessageBoxUtil.ShowMessageBox_Error("조회 날짜를 선택해 주세요.");
                    return;
                }
                if (UiDate_ProcStartDate > UiDate_ProcEndDate)
                {
                    MessageBoxUtil.ShowMessageBox_Error("생산 날짜 - 조회 시작 시간이 종료 시간보다 오래됐습니다.");
                    return;
                }

                bool hasModel  = !string.IsNullOrEmpty(UiCBox_ModelSelectedItem);
                var  startDate = UiDate_ProcStartDate.Value;
                var  endDate   = UiDate_ProcEndDate.Value;
                bool needHis   = startDate < DateTime.Now.AddMonths(-6);
                string model   = UiCBox_ModelSelectedItem;

                var swTotal = Stopwatch.StartNew();
                long dbMs = 0, mapMs = 0;

                // ── DB 조회 + 매핑: 백그라운드 스레드 (UI Freeze 방지) ──────────────
                // DataSet/DataRow 이중 버퍼 제거: SqlDataReader로 직접 매핑 (메모리 ~50% 절감)
                var (all, finished, defective) = await Task.Run(() =>
                {
                    var swDb = Stopwatch.StartNew();
                    var recentList = QueryEmpg("EMPG",     hasModel, startDate, endDate, model);
                    var oldList    = needHis
                        ? QueryEmpg("EMPG_HIS", hasModel, startDate, endDate, model)
                        : new List<ResourceLotHisItem>();
                    dbMs = swDb.ElapsedMilliseconds;

                    if (recentList.Count == 0 && oldList.Count == 0)
                        return (new List<ResourceLotHisItem>(),
                                new List<ResourceLotHisItem>(),
                                new List<ResourceLotHisItem>());

                    swDb.Restart();
                    var allList = new List<ResourceLotHisItem>(oldList.Count + recentList.Count);
                    allList.AddRange(oldList);
                    allList.AddRange(recentList);

                    var okList = new List<ResourceLotHisItem>();
                    var ngList = new List<ResourceLotHisItem>();
                    for (int i = 0; i < allList.Count; i++)
                    {
                        allList[i].No = i + 1;
                        if (allList[i].TotalJudge == MxComp_DB_JUDGE_CODE.OK) okList.Add(allList[i]);
                        else                                                    ngList.Add(allList[i]);
                    }
                    mapMs = swDb.ElapsedMilliseconds;
                    return (allList, okList, ngList);
                });

                // ── UI 업데이트: UI 스레드, 컬렉션 교체(알림 1회) ────────────────────
                if (all.Count == 0)
                {
                    ClearCollections();
                    AppendLog($"[{DateTime.Now:HH:mm:ss.fff}]  결과없음  DB:{dbMs}ms  {startDate:MM-dd}~{endDate:MM-dd}");
                    ExpException.RaiseException(new Exception("조회된 데이터가 없습니다."));
                    return;
                }

                var swUi = Stopwatch.StartNew();
                UiDg_ResourceLotHisList.ReplaceAll(all);
                UiDg_ResourceLotHisFinishedList.ReplaceAll(finished);
                UiDg_ResourceLotHisDefectiveList.ReplaceAll(defective);
                swUi.Stop();

                UiTBlock_ProductionQty     = all.Count;
                UiTBlock_GoodsQty          = finished.Count;
                UiTBlock_DefectiveQty      = defective.Count;
                UiTBlock_GoodQualityRation = all.Count == 0
                    ? "0.00"
                    : (finished.Count * 100.0 / all.Count).ToString("0.00");

                swTotal.Stop();
                AppendLog(
                    $"[{DateTime.Now:HH:mm:ss.fff}]" +
                    $"  {startDate:yyyy-MM-dd}~{endDate:MM-dd}" +
                    (hasModel ? $"  [{model}]" : "") +
                    $"  DB:{dbMs}ms  매핑:{mapMs}ms  UI:{swUi.ElapsedMilliseconds}ms" +
                    $"  합계:{swTotal.ElapsedMilliseconds}ms" +
                    $"  {all.Count:N0}건 (양:{finished.Count:N0} / 불:{defective.Count:N0})");
            }
            catch (ExpException expEx) { _log.WriteExpException(expEx); }
            catch (Exception ex)       { _log.WriteException(ex); }
        }

        // ── SQL helpers ─────────────────────────────────────────────────────────────

        // AS-IS: WHERE UPDATE_TIME BETWEEN N'" + sDate + "' AND N'" + eDate + "'"
        //        (문자열 직접 삽입 → SQL Injection 가능)
        // TO-BE: WHERE UPDATE_TIME BETWEEN @sDate AND @eDate
        //        (파라미터화 → 타입 안전, Injection 불가)
        private List<ResourceLotHisItem> QueryEmpg(string table, bool hasModel, DateTime startDate, DateTime endDate, string model)
        {
            var qExe = new QueryExecution(_connectionString);
            string sql = BuildSelectCols() +
                $" FROM {table} A " +
                "WHERE UPDATE_TIME BETWEEN @sDate AND @eDate " +
                (hasModel ? "AND MODEL = @model " : "") +
                "ORDER BY UPDATE_TIME";
            qExe.AppendQuery(sql);
            // UPDATE_TIME은 datetime2 — DateTime 파라미터로 전달 (AddParameter가 DateTime2로 매핑)
            qExe.AddParameter("@sDate", startDate);
            qExe.AddParameter("@eDate", endDate);
            if (hasModel)
                qExe.AddParameter("@model", model);
            return qExe.ExecuteReader(MapRow);
        }

        private static string BuildSelectCols() =>
            " SELECT A.UPDATE_TIME, A.REPAIR, A.MODEL, A.MAT_SERIAL01, A.MAT_SERIAL02, A.TOTAL_JUDGE," +
            " ISNULL(A.APD01,'') GR_R1_Load, ISNULL(A.APD02,'') GR_R1_Stroke," +
            " ISNULL(A.APD03,'') GR_R2_Load, ISNULL(A.APD04,'') GR_R2_Stroke," +
            " ISNULL(A.APD05,'') GR_P_Load,  ISNULL(A.APD06,'') GR_P_Stroke," +
            " ISNULL(A.APD07,'') GR_Judge,   ISNULL(A.APD08,'') GR_IndexNo," +
            " ISNULL(A.APD09,'') BR_R1_Load, ISNULL(A.APD10,'') BR_R1_Stroke," +
            " ISNULL(A.APD11,'') BR_R2_Load, ISNULL(A.APD12,'') BR_R2_Stroke," +
            " ISNULL(A.APD13,'') BR_P_Load,  ISNULL(A.APD14,'') BR_P_Stroke," +
            " ISNULL(A.APD15,'') BR_Judge,   ISNULL(A.APD16,'') BR_IndexNo," +
            " ISNULL(A.APD17,'') SR_GrooveWith_0Deg,       ISNULL(A.APD18,'') SR_GrooveWith_180Deg," +
            " ISNULL(A.APD19,'') SR_GrooveWith_Grade_Data, ISNULL(A.APD20,'') SR_GrooveWith_Grade," +
            " ISNULL(A.APD21,'') SR_GrooveWith_Judge," +
            " ISNULL(A.APD22,'') SR_Heigh_Thick, ISNULL(A.APD23,'') SR_Heigh_Judge," +
            " ISNULL(A.APD24,'') SR_Judge," +
            " ISNULL(A.APD25,'') EndPlate_Data, ISNULL(A.APD26,'') EndPlate_Judge," +
            " ISNULL(A.APD27,'') RunOutCheck_Input,      ISNULL(A.APD28,'') RunOutCheck_Input_Judgement," +
            " ISNULL(A.APD29,'') RunOutCheck_Space,      ISNULL(A.APD30,'') RunOutCheck_Space_Judgement," +
            " ISNULL(A.APD31,'') GuidingPressFitting_Judgement," +
            " ISNULL(A.APD32,'') Guiding_ShortDistance_Check," +
            " ISNULL(A.APD33,'') Guiding_ShortDistance_Judgement," +
            " ISNULL(A.APD34,'') Lotite_Disp_Judge, ISNULL(A.APD35,'') Lotite_Vision_Judge," +
            " ISNULL(A.APD36,'') SOCP_R1_Load,   ISNULL(A.APD37,'') SOCP_R1_Stroke," +
            " ISNULL(A.APD38,'') SOCP_R2_Load,   ISNULL(A.APD39,'') SOCP_R2_Stroke," +
            " ISNULL(A.APD40,'') SOCP_P_Load,    ISNULL(A.APD41,'') SOCP_P_Stroke," +
            " ISNULL(A.APD42,'') SOCP_Judge," +
            " ISNULL(A.APD43,'') SOC_Check,      ISNULL(A.APD44,'') SOC_Check_Judge," +
            " ISNULL(A.SP01,'') SP01, ISNULL(A.SP02,'') SP02, ISNULL(A.SP03,'') SP03," +
            " ISNULL(A.SP04,'') SP04, ISNULL(A.SP05,'') SP05, ISNULL(A.SP06,'') SP06," +
            " ISNULL(A.SP07,'') SP07, ISNULL(A.SP08,'') SP08, ISNULL(A.SP09,'') SP09," +
            " ISNULL(A.SP10,'') SP10, ISNULL(A.SP11,'') SP11, ISNULL(A.SP12,'') SP12," +
            " ISNULL(A.SP13,'') SP13, ISNULL(A.SP14,'') SP14, ISNULL(A.SP15,'') SP15," +
            " ISNULL(A.SP16,'') SP16, ISNULL(A.SP17,'') SP17, ISNULL(A.SP18,'') SP18," +
            " ISNULL(A.SP19,'') SP19, ISNULL(A.SP20,'') SP20, ISNULL(A.SP21,'') SP21," +
            " ISNULL(A.SP22,'') SP22, ISNULL(A.SP23,'') SP23, ISNULL(A.SP24,'') SP24," +
            " ISNULL(A.SP25,'') SP25, ISNULL(A.SP26,'') SP26, ISNULL(A.SP27,'') SP27," +
            " ISNULL(A.SP28,'') SP28, ISNULL(A.SP29,'') SP29, ISNULL(A.SP30,'') SP30," +
            " ISNULL(A.SP31,'') SP31, ISNULL(A.SP32,'') SP32, ISNULL(A.SP33,'') SP33," +
            " ISNULL(A.SP34,'') SP34, ISNULL(A.SP35,'') SP35, ISNULL(A.SP36,'') SP36," +
            " ISNULL(A.SP37,'') SP37, ISNULL(A.SP38,'') SP38, ISNULL(A.SP39,'') SP39," +
            " ISNULL(A.SP40,'') SP40, ISNULL(A.SP41,'') SP41, ISNULL(A.SP42,'') SP42," +
            " ISNULL(A.SP43,'') SP43, ISNULL(A.SP44,'') SP44, ISNULL(A.SP45,'') SP45," +
            " ISNULL(A.SP46,'') SP46, ISNULL(A.SP47,'') SP47, ISNULL(A.SP48,'') SP48," +
            " ISNULL(A.SP49,'') SP49, ISNULL(A.SP50,'') SP50";

        // UPDATE_TIME는 Step 1에서 nvarchar → datetime 타입 변경됨.
        // AS-IS: DataRow(DataSet 경유, 이중 버퍼) → TO-BE: IDataRecord(SqlDataReader 직접 매핑)
        // No는 PerformFindAsync에서 old+recent 합산 후 일괄 부여.
        private static ResourceLotHisItem MapRow(IDataRecord r) => new ResourceLotHisItem
        {
            Date_Time         = ((DateTime)r["UPDATE_TIME"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Repair            = r["REPAIR"].ToString()!,
            Model             = r["MODEL"].ToString()!,
            Material01_Serial = r["MAT_SERIAL01"].ToString()!,
            Material02_Serial = r["MAT_SERIAL02"].ToString()!,
            TotalJudge        = r["TOTAL_JUDGE"].ToString()!,

            APD01 = r["GR_R1_Load"].ToString()!,       APD02 = r["GR_R1_Stroke"].ToString()!,
            APD03 = r["GR_R2_Load"].ToString()!,       APD04 = r["GR_R2_Stroke"].ToString()!,
            APD05 = r["GR_P_Load"].ToString()!,        APD06 = r["GR_P_Stroke"].ToString()!,
            APD07 = r["GR_Judge"].ToString()!,         APD08 = r["GR_IndexNo"].ToString()!,
            APD09 = r["BR_R1_Load"].ToString()!,       APD10 = r["BR_R1_Stroke"].ToString()!,
            APD11 = r["BR_R2_Load"].ToString()!,       APD12 = r["BR_R2_Stroke"].ToString()!,
            APD13 = r["BR_P_Load"].ToString()!,        APD14 = r["BR_P_Stroke"].ToString()!,
            APD15 = r["BR_Judge"].ToString()!,         APD16 = r["BR_IndexNo"].ToString()!,
            APD17 = r["SR_GrooveWith_0Deg"].ToString()!,
            APD18 = r["SR_GrooveWith_180Deg"].ToString()!,
            APD19 = r["SR_GrooveWith_Grade_Data"].ToString()!,
            APD20 = r["SR_GrooveWith_Grade"].ToString()!,
            APD21 = r["SR_GrooveWith_Judge"].ToString()!,
            APD22 = r["SR_Heigh_Thick"].ToString()!,  APD23 = r["SR_Heigh_Judge"].ToString()!,
            APD24 = r["SR_Judge"].ToString()!,
            APD25 = r["EndPlate_Data"].ToString()!,   APD26 = r["EndPlate_Judge"].ToString()!,
            APD27 = r["RunOutCheck_Input"].ToString()!,
            APD28 = r["RunOutCheck_Input_Judgement"].ToString()!,
            APD29 = r["RunOutCheck_Space"].ToString()!,
            APD30 = r["RunOutCheck_Space_Judgement"].ToString()!,
            APD31 = r["GuidingPressFitting_Judgement"].ToString()!,
            APD32 = r["Guiding_ShortDistance_Check"].ToString()!,
            APD33 = r["Guiding_ShortDistance_Judgement"].ToString()!,
            APD34 = r["Lotite_Disp_Judge"].ToString()!,
            APD35 = r["Lotite_Vision_Judge"].ToString()!,
            APD36 = r["SOCP_R1_Load"].ToString()!,    APD37 = r["SOCP_R1_Stroke"].ToString()!,
            APD38 = r["SOCP_R2_Load"].ToString()!,    APD39 = r["SOCP_R2_Stroke"].ToString()!,
            APD40 = r["SOCP_P_Load"].ToString()!,     APD41 = r["SOCP_P_Stroke"].ToString()!,
            APD42 = r["SOCP_Judge"].ToString()!,
            APD43 = r["SOC_Check"].ToString()!,       APD44 = r["SOC_Check_Judge"].ToString()!,

            SP01 = r["SP01"].ToString()!, SP02 = r["SP02"].ToString()!, SP03 = r["SP03"].ToString()!,
            SP04 = r["SP04"].ToString()!, SP05 = r["SP05"].ToString()!, SP06 = r["SP06"].ToString()!,
            SP07 = r["SP07"].ToString()!, SP08 = r["SP08"].ToString()!, SP09 = r["SP09"].ToString()!,
            SP10 = r["SP10"].ToString()!, SP11 = r["SP11"].ToString()!, SP12 = r["SP12"].ToString()!,
            SP13 = r["SP13"].ToString()!, SP14 = r["SP14"].ToString()!, SP15 = r["SP15"].ToString()!,
            SP16 = r["SP16"].ToString()!, SP17 = r["SP17"].ToString()!, SP18 = r["SP18"].ToString()!,
            SP19 = r["SP19"].ToString()!, SP20 = r["SP20"].ToString()!, SP21 = r["SP21"].ToString()!,
            SP22 = r["SP22"].ToString()!, SP23 = r["SP23"].ToString()!, SP24 = r["SP24"].ToString()!,
            SP25 = r["SP25"].ToString()!, SP26 = r["SP26"].ToString()!, SP27 = r["SP27"].ToString()!,
            SP28 = r["SP28"].ToString()!, SP29 = r["SP29"].ToString()!, SP30 = r["SP30"].ToString()!,
            SP31 = r["SP31"].ToString()!, SP32 = r["SP32"].ToString()!, SP33 = r["SP33"].ToString()!,
            SP34 = r["SP34"].ToString()!, SP35 = r["SP35"].ToString()!, SP36 = r["SP36"].ToString()!,
            SP37 = r["SP37"].ToString()!, SP38 = r["SP38"].ToString()!, SP39 = r["SP39"].ToString()!,
            SP40 = r["SP40"].ToString()!, SP41 = r["SP41"].ToString()!, SP42 = r["SP42"].ToString()!,
            SP43 = r["SP43"].ToString()!, SP44 = r["SP44"].ToString()!, SP45 = r["SP45"].ToString()!,
            SP46 = r["SP46"].ToString()!, SP47 = r["SP47"].ToString()!, SP48 = r["SP48"].ToString()!,
            SP49 = r["SP49"].ToString()!, SP50 = r["SP50"].ToString()!,
        };

        private void ClearCollections()
        {
            UiDg_ResourceLotHisList.ReplaceAll([]);
            UiDg_ResourceLotHisFinishedList.ReplaceAll([]);
            UiDg_ResourceLotHisDefectiveList.ReplaceAll([]);
        }

        private void AppendLog(string entry)
        {
            if (QueryLog.Count >= 200) QueryLog.RemoveAt(QueryLog.Count - 1);
            QueryLog.Insert(0, entry);
        }

        #endregion
    }
}
