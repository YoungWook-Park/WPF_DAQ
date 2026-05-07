// Step 10: EF Core 독립 ViewModel (ADO.NET 버전과 병행 존재, 수정 없음)
//
// [AS-IS] Inquiry_OP200_ResourceLotHistoryViewModel.cs (ADO.NET)
//   QueryExecution + 문자열 연결 SQL → DataSet → 수동 MapRow()
//   WHERE UPDATE_TIME BETWEEN @sDate AND @eDate  (파라미터는 Step 5에서 수정됨)
//   코드량: BuildSelectCols ~80줄 + MapRow ~50줄 = 약 130줄
//
// [TO-BE] 이 파일 (EF Core)
//   DongBoDbContext + AsNoTracking() + Select projection → List<T>
//   코드량: QueryEmpgAsync ~12줄
//   SQL Injection 방지: EF Core 파라미터화 자동 처리
//   성능 측정: 인덱스 Phase B(ADO.NET)와 동일 인덱스 환경에서 비교
//
// 성능 측정 포인트:
//   Stopwatch.StartNew() → QueryEmpgAsync() 완료 → ElapsedMs 기록
//   → benchmark/phase3_efcore.md 에 결과 기입
//FeatureA Edit

using Bi.ConSightCommon;
using Bi.nsExpException;
using Bi.nsLogWriter;
using ConSight.DAQ.Device.DB.EfCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ConSight.DAQ.Views
{
    public class Inquiry_OP200_ResourceLotHistoryViewModel_EfCore : BiNotifyPropertyBase
    {
        private readonly string _connectionString;
        private readonly LogWriter _log = new LogWriter();

        public Inquiry_OP200_ResourceLotHistoryViewModel_EfCore(string connectionString)
        {
            _connectionString = connectionString;
            InitUi();
        }

        #region Properties
        public ObservableCollection<string> QueryLog { get; } = new();


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

        // Phase C 측정용: 마지막 조회 경과시간 (ms)
        private long _lastQueryElapsedMs;
        public long LastQueryElapsedMs
        {
            get => _lastQueryElapsedMs;
            set => SetProperty(ref _lastQueryElapsedMs, value);
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
            _cmdFind ??= new BiRelayCommand(async _ => await PerformFindAsync());

        private ICommand? _cmdClearLog;
        public ICommand Cmd_ClearLog =>
            _cmdClearLog ??= new BiRelayCommand(_ => QueryLog.Clear());

        #endregion

        #region Private helpers

        private DongBoDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DongBoDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            return new DongBoDbContext(options);
        }

        private void InitUi()
        {
            try
            {
                using var ctx = CreateContext();
                var models = ctx.Empg
                    .AsNoTracking()
                    .Select(e => e.Model)
                    .Where(m => m != null)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                UiCBox_ModelItemSource.Clear();
                UiCBox_ModelItemSource.Add(string.Empty);
                foreach (var m in models)
                    UiCBox_ModelItemSource.Add(m!);

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

                bool   hasModel  = !string.IsNullOrEmpty(UiCBox_ModelSelectedItem);
                var    startDate = UiDate_ProcStartDate.Value;
                var    endDate   = UiDate_ProcEndDate.Value;
                string model     = UiCBox_ModelSelectedItem;

                var swTotal = Stopwatch.StartNew();

                // ── DB 조회 (EF Core projection → List<T> 직접 반환) ─────────────
                // EF Core 생성 SQL (예시):
                //   SELECT [e].[UPDATE_TIME], [e].[REPAIR], [e].[MODEL], ...
                //   FROM [EMPG] AS [e]
                //   WHERE [e].[UPDATE_TIME] >= @p0 AND [e].[UPDATE_TIME] <= @p1
                //   ORDER BY [e].[UPDATE_TIME] ASC
                var swDb = Stopwatch.StartNew();
                using var ctx = CreateContext();

                var recentItems = await QueryEmpgAsync<EmpgEntity>(
                    ctx.Empg, hasModel, startDate, endDate);

                List<ResourceLotHisItem> oldItems = new();
                if (startDate < DateTime.Now.AddMonths(-6))
                    oldItems = await QueryEmpgAsync<EmpgHisEntity>(
                        ctx.EmpgHis, hasModel, startDate, endDate);

                swDb.Stop();
                long dbMs = swDb.ElapsedMilliseconds;

                // ── 결과 없음 처리 ───────────────────────────────────────────────
                if (recentItems.Count == 0 && oldItems.Count == 0)
                {
                    ClearCollections();
                    AppendLog($"[{DateTime.Now:HH:mm:ss.fff}]  결과없음  DB:{dbMs}ms  {startDate:MM-dd}~{endDate:MM-dd}");
                    ExpException.RaiseException(new Exception("조회된 데이터가 없습니다."));
                    return;
                }

                // ── 분류 (old → recent 순서 유지, No 일괄 부여) ──────────────────
                // ADO.NET 버전과 달리 EF Core는 projection 단계에서 매핑이 완료되므로
                // 이 단계는 순수 분류(OK/NG) 작업만 수행함.
                var swMap = Stopwatch.StartNew();
                var all       = new List<ResourceLotHisItem>(oldItems.Count + recentItems.Count);
                var finished  = new List<ResourceLotHisItem>();
                var defective = new List<ResourceLotHisItem>();
                int idx = 1;

                foreach (var item in oldItems.Concat(recentItems))
                {
                    item.No = idx++;
                    all.Add(item);
                    if (item.TotalJudge == "OK") finished.Add(item);
                    else                         defective.Add(item);
                }
                swMap.Stop();
                long mapMs = swMap.ElapsedMilliseconds;

                // ── UI 업데이트 ──────────────────────────────────────────────────
                var swUi = Stopwatch.StartNew();
                Repopulate(UiDg_ResourceLotHisList,         all);
                Repopulate(UiDg_ResourceLotHisFinishedList,  finished);
                Repopulate(UiDg_ResourceLotHisDefectiveList, defective);
                swUi.Stop();

                UiTBlock_ProductionQty     = all.Count;
                UiTBlock_GoodsQty          = finished.Count;
                UiTBlock_DefectiveQty      = defective.Count;
                UiTBlock_GoodQualityRation = all.Count == 0
                    ? "0.00"
                    : (finished.Count * 100.0 / all.Count).ToString("0.00");

                swTotal.Stop();
                LastQueryElapsedMs = swTotal.ElapsedMilliseconds;

                AppendLog(
                    $"[{DateTime.Now:HH:mm:ss.fff}]" +
                    $"  {startDate:yyyy-MM-dd}~{endDate:MM-dd}" +
                    (hasModel ? $"  [{model}]" : "") +
                    $"  DB:{dbMs}ms  분류:{mapMs}ms  UI:{swUi.ElapsedMilliseconds}ms" +
                    $"  합계:{swTotal.ElapsedMilliseconds}ms" +
                    $"  {all.Count:N0}건 (양:{finished.Count:N0} / 불:{defective.Count:N0})");
            }
            catch (ExpException expEx) { _log.WriteExpException(expEx); }
            catch (Exception ex)       { _log.WriteException(ex); }
        }

        // ── EF Core 핵심 쿼리 ────────────────────────────────────────────────────
        // AS-IS (ADO.NET): BuildSelectCols() ~80줄 + MapRow() ~50줄 = 약 130줄
        // TO-BE (EF Core): Select projection + AsNoTracking = 약 20줄
        private async Task<List<ResourceLotHisItem>> QueryEmpgAsync<T>(
            IQueryable<T> dbSet,
            bool hasModel,
            DateTime from,
            DateTime to)
            where T : EmpgEntity
        {
            var query = dbSet
                .AsNoTracking()
                .Where(e => e.UpdateTime >= from && e.UpdateTime <= to);

            if (hasModel)
                query = query.Where(e => e.Model == UiCBox_ModelSelectedItem);

            return await query
                .OrderBy(e => e.UpdateTime)
                .Select(e => new ResourceLotHisItem
                {
                    Date_Time         = e.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Repair            = e.Repair            ?? string.Empty,
                    Model             = e.Model             ?? string.Empty,
                    Material01_Serial = e.MatSerial01       ?? string.Empty,
                    Material02_Serial = e.MatSerial02       ?? string.Empty,
                    TotalJudge        = e.TotalJudge        ?? string.Empty,
                    APD01 = e.Apd01 ?? string.Empty, APD02 = e.Apd02 ?? string.Empty,
                    APD03 = e.Apd03 ?? string.Empty, APD04 = e.Apd04 ?? string.Empty,
                    APD05 = e.Apd05 ?? string.Empty, APD06 = e.Apd06 ?? string.Empty,
                    APD07 = e.Apd07 ?? string.Empty, APD08 = e.Apd08 ?? string.Empty,
                    APD09 = e.Apd09 ?? string.Empty, APD10 = e.Apd10 ?? string.Empty,
                    APD11 = e.Apd11 ?? string.Empty, APD12 = e.Apd12 ?? string.Empty,
                    APD13 = e.Apd13 ?? string.Empty, APD14 = e.Apd14 ?? string.Empty,
                    APD15 = e.Apd15 ?? string.Empty, APD16 = e.Apd16 ?? string.Empty,
                    APD17 = e.Apd17 ?? string.Empty, APD18 = e.Apd18 ?? string.Empty,
                    APD19 = e.Apd19 ?? string.Empty, APD20 = e.Apd20 ?? string.Empty,
                    APD21 = e.Apd21 ?? string.Empty, APD22 = e.Apd22 ?? string.Empty,
                    APD23 = e.Apd23 ?? string.Empty, APD24 = e.Apd24 ?? string.Empty,
                    APD25 = e.Apd25 ?? string.Empty, APD26 = e.Apd26 ?? string.Empty,
                    APD27 = e.Apd27 ?? string.Empty, APD28 = e.Apd28 ?? string.Empty,
                    APD29 = e.Apd29 ?? string.Empty, APD30 = e.Apd30 ?? string.Empty,
                    APD31 = e.Apd31 ?? string.Empty, APD32 = e.Apd32 ?? string.Empty,
                    APD33 = e.Apd33 ?? string.Empty, APD34 = e.Apd34 ?? string.Empty,
                    APD35 = e.Apd35 ?? string.Empty, APD36 = e.Apd36 ?? string.Empty,
                    APD37 = e.Apd37 ?? string.Empty, APD38 = e.Apd38 ?? string.Empty,
                    APD39 = e.Apd39 ?? string.Empty, APD40 = e.Apd40 ?? string.Empty,
                    APD41 = e.Apd41 ?? string.Empty, APD42 = e.Apd42 ?? string.Empty,
                    APD43 = e.Apd43 ?? string.Empty, APD44 = e.Apd44 ?? string.Empty,
                    SP01  = e.Sp01  ?? string.Empty, SP02  = e.Sp02  ?? string.Empty,
                    SP03  = e.Sp03  ?? string.Empty, SP04  = e.Sp04  ?? string.Empty,
                    SP05  = e.Sp05  ?? string.Empty, SP06  = e.Sp06  ?? string.Empty,
                    SP07  = e.Sp07  ?? string.Empty, SP08  = e.Sp08  ?? string.Empty,
                    SP09  = e.Sp09  ?? string.Empty, SP10  = e.Sp10  ?? string.Empty,
                    SP11  = e.Sp11  ?? string.Empty, SP12  = e.Sp12  ?? string.Empty,
                    SP13  = e.Sp13  ?? string.Empty, SP14  = e.Sp14  ?? string.Empty,
                    SP15  = e.Sp15  ?? string.Empty, SP16  = e.Sp16  ?? string.Empty,
                    SP17  = e.Sp17  ?? string.Empty, SP18  = e.Sp18  ?? string.Empty,
                    SP19  = e.Sp19  ?? string.Empty, SP20  = e.Sp20  ?? string.Empty,
                    SP21  = e.Sp21  ?? string.Empty, SP22  = e.Sp22  ?? string.Empty,
                    SP23  = e.Sp23  ?? string.Empty, SP24  = e.Sp24  ?? string.Empty,
                    SP25  = e.Sp25  ?? string.Empty, SP26  = e.Sp26  ?? string.Empty,
                    SP27  = e.Sp27  ?? string.Empty, SP28  = e.Sp28  ?? string.Empty,
                    SP29  = e.Sp29  ?? string.Empty, SP30  = e.Sp30  ?? string.Empty,
                    SP31  = e.Sp31  ?? string.Empty, SP32  = e.Sp32  ?? string.Empty,
                    SP33  = e.Sp33  ?? string.Empty, SP34  = e.Sp34  ?? string.Empty,
                    SP35  = e.Sp35  ?? string.Empty, SP36  = e.Sp36  ?? string.Empty,
                    SP37  = e.Sp37  ?? string.Empty, SP38  = e.Sp38  ?? string.Empty,
                    SP39  = e.Sp39  ?? string.Empty, SP40  = e.Sp40  ?? string.Empty,
                    SP41  = e.Sp41  ?? string.Empty, SP42  = e.Sp42  ?? string.Empty,
                    SP43  = e.Sp43  ?? string.Empty, SP44  = e.Sp44  ?? string.Empty,
                    SP45  = e.Sp45  ?? string.Empty, SP46  = e.Sp46  ?? string.Empty,
                    SP47  = e.Sp47  ?? string.Empty, SP48  = e.Sp48  ?? string.Empty,
                    SP49  = e.Sp49  ?? string.Empty, SP50  = e.Sp50  ?? string.Empty,
                })
                .ToListAsync();
        }

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
        private void AppendLog(string entry)
        {
            if (QueryLog.Count >= 200) QueryLog.RemoveAt(QueryLog.Count - 1);
            QueryLog.Insert(0, entry);
        }
        #endregion
    }
}
