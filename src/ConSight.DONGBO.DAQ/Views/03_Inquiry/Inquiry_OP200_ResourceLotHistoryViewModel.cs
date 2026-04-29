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

        public ObservableCollection<ResourceLotHisItem> UiDg_ResourceLotHisList         { get; } = new();
        public ObservableCollection<ResourceLotHisItem> UiDg_ResourceLotHisFinishedList  { get; } = new();
        public ObservableCollection<ResourceLotHisItem> UiDg_ResourceLotHisDefectiveList { get; } = new();
        public ObservableCollection<string>             UiCBox_ModelItemSource           { get; } = new();

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
            _cmdFind ??= new BiRelayCommand(PerformFind);

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

        private void PerformFind(object? _)
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

                bool hasModel = !string.IsNullOrEmpty(UiCBox_ModelSelectedItem);

                // ── 최근 데이터 (EMPG) ──────────────────────────────────────────────
                DataSet dsRecent = QueryEmpg("EMPG", hasModel);

                // ── 6개월 이전 데이터 (EMPG_HIS) ────────────────────────────────────
                DataSet? dsOld = null;
                if (UiDate_ProcStartDate < DateTime.Now.AddMonths(-6))
                    dsOld = QueryEmpg("EMPG_HIS", hasModel);   // nullable < DateTime is false when null — already guarded above

                // ── 결과 없음 처리 ───────────────────────────────────────────────────
                bool recentEmpty = dsRecent.Tables[0].Rows.Count == 0;
                bool oldEmpty    = dsOld == null || dsOld.Tables[0].Rows.Count == 0;
                if (recentEmpty && oldEmpty)
                {
                    ClearCollections();
                    ExpException.RaiseException(new Exception("조회된 데이터가 없습니다."));
                }

                // ── 매핑 ────────────────────────────────────────────────────────────
                var all       = new List<ResourceLotHisItem>();
                var finished  = new List<ResourceLotHisItem>();
                var defective = new List<ResourceLotHisItem>();
                int idx = 1;

                foreach (DataSet? ds in new[] { dsOld, dsRecent })
                {
                    if (ds == null) continue;
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        var item = MapRow(dr, idx++);
                        all.Add(item);
                        if (item.TotalJudge == MxComp_DB_JUDGE_CODE.OK) finished.Add(item);
                        else                                              defective.Add(item);
                    }
                }

                // ── ObservableCollection 교체 ────────────────────────────────────
                Repopulate(UiDg_ResourceLotHisList,         all);
                Repopulate(UiDg_ResourceLotHisFinishedList,  finished);
                Repopulate(UiDg_ResourceLotHisDefectiveList, defective);

                UiTBlock_ProductionQty  = all.Count;
                UiTBlock_GoodsQty       = finished.Count;
                UiTBlock_DefectiveQty   = defective.Count;
                UiTBlock_GoodQualityRation = all.Count == 0
                    ? "0.00"
                    : (finished.Count * 100.0 / all.Count).ToString("0.00");
            }
            catch (ExpException expEx) { _log.WriteExpException(expEx); }
            catch (Exception ex)       { _log.WriteException(ex); }
        }

        // ── SQL helpers ─────────────────────────────────────────────────────────────

        // AS-IS: WHERE UPDATE_TIME BETWEEN N'" + sDate + "' AND N'" + eDate + "'"
        //        (문자열 직접 삽입 → SQL Injection 가능)
        // TO-BE: WHERE UPDATE_TIME BETWEEN @sDate AND @eDate
        //        (파라미터화 → 타입 안전, Injection 불가)
        private DataSet QueryEmpg(string table, bool hasModel)
        {
            var qExe = new QueryExecution(_connectionString);
            string sql = BuildSelectCols() +
                $" FROM {table} A " +
                "WHERE UPDATE_TIME BETWEEN @sDate AND @eDate " +
                (hasModel ? "AND MODEL = @model " : "") +
                "ORDER BY UPDATE_TIME";
            qExe.AppendQuery(sql);
            qExe.AddParameter("@sDate", UiDate_ProcStartDate!.Value);   // null already guarded in PerformFind
            qExe.AddParameter("@eDate", UiDate_ProcEndDate!.Value);
            if (hasModel)
                qExe.AddParameter("@model", UiCBox_ModelSelectedItem);
            return qExe.Execute();
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
        // AS-IS: (string)dr["UPDATE_TIME"]  → InvalidCastException
        // TO-BE: ((DateTime)dr["UPDATE_TIME"]).ToString(...)
        private static ResourceLotHisItem MapRow(DataRow dr, int index) => new ResourceLotHisItem
        {
            No                = index,
            Date_Time         = ((DateTime)dr["UPDATE_TIME"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            Repair            = dr["REPAIR"].ToString()!,
            Model             = dr["MODEL"].ToString()!,
            Material01_Serial = dr["MAT_SERIAL01"].ToString()!,
            Material02_Serial = dr["MAT_SERIAL02"].ToString()!,
            TotalJudge        = dr["TOTAL_JUDGE"].ToString()!,

            APD01 = dr["GR_R1_Load"].ToString()!,       APD02 = dr["GR_R1_Stroke"].ToString()!,
            APD03 = dr["GR_R2_Load"].ToString()!,       APD04 = dr["GR_R2_Stroke"].ToString()!,
            APD05 = dr["GR_P_Load"].ToString()!,        APD06 = dr["GR_P_Stroke"].ToString()!,
            APD07 = dr["GR_Judge"].ToString()!,         APD08 = dr["GR_IndexNo"].ToString()!,
            APD09 = dr["BR_R1_Load"].ToString()!,       APD10 = dr["BR_R1_Stroke"].ToString()!,
            APD11 = dr["BR_R2_Load"].ToString()!,       APD12 = dr["BR_R2_Stroke"].ToString()!,
            APD13 = dr["BR_P_Load"].ToString()!,        APD14 = dr["BR_P_Stroke"].ToString()!,
            APD15 = dr["BR_Judge"].ToString()!,         APD16 = dr["BR_IndexNo"].ToString()!,
            APD17 = dr["SR_GrooveWith_0Deg"].ToString()!,
            APD18 = dr["SR_GrooveWith_180Deg"].ToString()!,
            APD19 = dr["SR_GrooveWith_Grade_Data"].ToString()!,
            APD20 = dr["SR_GrooveWith_Grade"].ToString()!,
            APD21 = dr["SR_GrooveWith_Judge"].ToString()!,
            APD22 = dr["SR_Heigh_Thick"].ToString()!,  APD23 = dr["SR_Heigh_Judge"].ToString()!,
            APD24 = dr["SR_Judge"].ToString()!,
            APD25 = dr["EndPlate_Data"].ToString()!,   APD26 = dr["EndPlate_Judge"].ToString()!,
            APD27 = dr["RunOutCheck_Input"].ToString()!,
            APD28 = dr["RunOutCheck_Input_Judgement"].ToString()!,
            APD29 = dr["RunOutCheck_Space"].ToString()!,
            APD30 = dr["RunOutCheck_Space_Judgement"].ToString()!,
            APD31 = dr["GuidingPressFitting_Judgement"].ToString()!,
            APD32 = dr["Guiding_ShortDistance_Check"].ToString()!,
            APD33 = dr["Guiding_ShortDistance_Judgement"].ToString()!,
            APD34 = dr["Lotite_Disp_Judge"].ToString()!,
            APD35 = dr["Lotite_Vision_Judge"].ToString()!,
            APD36 = dr["SOCP_R1_Load"].ToString()!,    APD37 = dr["SOCP_R1_Stroke"].ToString()!,
            APD38 = dr["SOCP_R2_Load"].ToString()!,    APD39 = dr["SOCP_R2_Stroke"].ToString()!,
            APD40 = dr["SOCP_P_Load"].ToString()!,     APD41 = dr["SOCP_P_Stroke"].ToString()!,
            APD42 = dr["SOCP_Judge"].ToString()!,
            APD43 = dr["SOC_Check"].ToString()!,       APD44 = dr["SOC_Check_Judge"].ToString()!,

            SP01 = dr["SP01"].ToString()!, SP02 = dr["SP02"].ToString()!, SP03 = dr["SP03"].ToString()!,
            SP04 = dr["SP04"].ToString()!, SP05 = dr["SP05"].ToString()!, SP06 = dr["SP06"].ToString()!,
            SP07 = dr["SP07"].ToString()!, SP08 = dr["SP08"].ToString()!, SP09 = dr["SP09"].ToString()!,
            SP10 = dr["SP10"].ToString()!, SP11 = dr["SP11"].ToString()!, SP12 = dr["SP12"].ToString()!,
            SP13 = dr["SP13"].ToString()!, SP14 = dr["SP14"].ToString()!, SP15 = dr["SP15"].ToString()!,
            SP16 = dr["SP16"].ToString()!, SP17 = dr["SP17"].ToString()!, SP18 = dr["SP18"].ToString()!,
            SP19 = dr["SP19"].ToString()!, SP20 = dr["SP20"].ToString()!, SP21 = dr["SP21"].ToString()!,
            SP22 = dr["SP22"].ToString()!, SP23 = dr["SP23"].ToString()!, SP24 = dr["SP24"].ToString()!,
            SP25 = dr["SP25"].ToString()!, SP26 = dr["SP26"].ToString()!, SP27 = dr["SP27"].ToString()!,
            SP28 = dr["SP28"].ToString()!, SP29 = dr["SP29"].ToString()!, SP30 = dr["SP30"].ToString()!,
            SP31 = dr["SP31"].ToString()!, SP32 = dr["SP32"].ToString()!, SP33 = dr["SP33"].ToString()!,
            SP34 = dr["SP34"].ToString()!, SP35 = dr["SP35"].ToString()!, SP36 = dr["SP36"].ToString()!,
            SP37 = dr["SP37"].ToString()!, SP38 = dr["SP38"].ToString()!, SP39 = dr["SP39"].ToString()!,
            SP40 = dr["SP40"].ToString()!, SP41 = dr["SP41"].ToString()!, SP42 = dr["SP42"].ToString()!,
            SP43 = dr["SP43"].ToString()!, SP44 = dr["SP44"].ToString()!, SP45 = dr["SP45"].ToString()!,
            SP46 = dr["SP46"].ToString()!, SP47 = dr["SP47"].ToString()!, SP48 = dr["SP48"].ToString()!,
            SP49 = dr["SP49"].ToString()!, SP50 = dr["SP50"].ToString()!,
        };

        private static void Repopulate<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items) collection.Add(item);
        }

        private void ClearCollections()
        {
            UiDg_ResourceLotHisList.Clear();
            UiDg_ResourceLotHisFinishedList.Clear();
            UiDg_ResourceLotHisDefectiveList.Clear();
        }

        #endregion
    }
}
