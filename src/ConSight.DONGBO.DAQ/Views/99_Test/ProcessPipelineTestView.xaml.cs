// ProcessPipelineTestView — ControlUnit_DAQ 파이프라인 테스트 화면
//
// 테스트 방식:
//   ① 파서 테스트: 하드코딩된 mock short[] → Parser → DTO 필드 출력 (DB 불필요)
//   ② 전체 파이프라인: Parser → ControlUnit_DAQ → DB INSERT/UPDATE + MockPLC 쓰기
//
// mock 배열 레이아웃은 각 OpXxxParser 주석의 PLC 주소 오프셋을 그대로 따른다.

using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ConSight.DAQ.AppEvent;
using ConSight.DAQ.Data;
using ConSight.DAQ.Device;
using ConSight.DAQ.Device.DB;
using ConSight.DAQ.Device.PLC;
using ConSight.DAQ.Device.PLC.OP200;
using ConSight.DAQ.Device.PLC.OP210;
using ConSight.DAQ.Device.PLC.OP220;
using ConSight.DAQ.Device.PLC.OP230;
using ConSight.DAQ.Sequence;

namespace ConSight.DONGBO.DAQ.Views
{
    public partial class ProcessPipelineTestView : UserControl
    {
        private readonly string _connectionString;

        // ── 파서 인스턴스 ─────────────────────────────────────────────────
        private readonly Op200Parser _p200 = new();
        private readonly Op210Parser _p210 = new();
        private readonly Op220Parser _p220 = new();
        private readonly Op230Parser _p230 = new();

        // ── 파이프라인 인프라 (MockPLC 기반) ─────────────────────────────
        private readonly MockPlcDriver     _mockPlc   = new();
        private readonly ControlUnit_DAQ   _unit;
        private IDisposable? _eventBusSubscription;

        // ── 마지막으로 파싱한 DTO (파이프라인 버튼에서 재사용) ─────────────
        private Op200ProcessDto? _lastDto200;
        private Op210ProcessDto? _lastDto210;
        private Op220ProcessDto? _lastDto220;
        private Op230ProcessDto? _lastDto230;

        public ProcessPipelineTestView(string connectionString, IProcessEventBus sharedEventBus)
        {
            _connectionString = connectionString;
            InitializeComponent();

            // MockPLC 기반 write region
            var op200Write = new Op200WriteRegion(new PlcWriteBuffer(_mockPlc, "D2001", 3));
            var op210Write = new Op210WriteRegion(new PlcWriteBuffer(_mockPlc, "D2201", 1));
            var op220Write = new Op220WriteRegion(new PlcWriteBuffer(_mockPlc, "D2301", 1));
            var op230Write = new Op230WriteRegion(new PlcWriteBuffer(_mockPlc, "D2401", 1));

            var csvWriter = new EmpgCsvWriter(@"D:\[LDAQ_REFACTOR]\TEST_CSV");

            _unit = new ControlUnit_DAQ(
                connectionString,
                op200Write, op210Write, op220Write, op230Write,
                csvWriter, sharedEventBus);

            // EventBus 구독 — 파이프라인 결과 수신 (MonitoringView 와 동일 버스 공유)
            _eventBusSubscription = sharedEventBus.AsObservable()
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(OnEventBusRow);
        }

        // ────────────────────────────────────────────────────────────────
        // ① 파서 테스트 버튼
        // ────────────────────────────────────────────────────────────────

        private void BtnParseOp200_Click(object sender, RoutedEventArgs e)
        {
            short[] proc    = BuildOp200ProcArray();
            short[] setting = BuildOp200SettingArray();

            AppendLine("═══ OP200 파서 테스트 ═══════════════════════════════════");
            AppendLine($"proc 배열  크기 : {proc.Length} words");
            AppendLine($"setting 배열 크기: {setting.Length} words");
            AppendLine("");

            _lastDto200 = _p200.Parse(proc, setting);
            AppendDto200(_lastDto200);
        }

        private void BtnParseOp210_Click(object sender, RoutedEventArgs e)
        {
            short[] proc    = BuildOp210ProcArray();
            short[] setting = BuildOp200SettingArray();   // OP210 설정은 D1900 공유

            AppendLine("═══ OP210 파서 테스트 ═══════════════════════════════════");
            _lastDto210 = _p210.Parse(proc, setting);
            AppendDto210(_lastDto210);
        }

        private void BtnParseOp220_Click(object sender, RoutedEventArgs e)
        {
            short[] proc    = BuildOp220ProcArray();
            short[] setting = BuildOp200SettingArray();

            AppendLine("═══ OP220 파서 테스트 ═══════════════════════════════════");
            _lastDto220 = _p220.Parse(proc, setting);
            AppendDto220(_lastDto220);
        }

        private void BtnParseOp230_Click(object sender, RoutedEventArgs e)
        {
            short[] proc    = BuildOp230ProcArray();
            short[] setting = BuildOp230SettingArray();

            AppendLine("═══ OP230 파서 테스트 ═══════════════════════════════════");
            _lastDto230 = _p230.Parse(proc, setting);
            AppendDto230(_lastDto230);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxResult.Clear();
            TxPlcLog.Clear();
        }

        // ────────────────────────────────────────────────────────────────
        // ② 전체 파이프라인 버튼
        // ────────────────────────────────────────────────────────────────

        private void BtnPipeOp200_Click(object sender, RoutedEventArgs e)
        {
            // 파서가 먼저 실행되지 않았으면 자동 실행
            if (_lastDto200 == null)
                _lastDto200 = _p200.Parse(BuildOp200ProcArray(), BuildOp200SettingArray());

            AppendLine("═══ OP200 전체 파이프라인 ═══════════════════════════════");
            AppendLine($"  → ControlUnit_DAQ.ProcessData_Op200() 호출");
            AppendLine($"  → Model={_lastDto200.Model}  Serial={_lastDto200.ShaftSerial}  Judge={_lastDto200.TotalJudge}");

            try
            {
                _unit.ProcessData_Op200(_lastDto200);
                AppendLine("  [DB] EMPG INSERT 완료");
            }
            catch (Exception ex)
            {
                AppendLine($"  [오류] {ex.GetType().Name}: {ex.Message}");
            }

            FlushPlcLog();
        }

        private void BtnPipeOp210_Click(object sender, RoutedEventArgs e)
        {
            if (_lastDto210 == null)
                _lastDto210 = _p210.Parse(BuildOp210ProcArray(), BuildOp200SettingArray());

            AppendLine("═══ OP210 전체 파이프라인 ═══════════════════════════════");
            AppendLine($"  → ControlUnit_DAQ.ProcessData_Op210() 호출");
            AppendLine($"  → Serial={_lastDto210.Serial}");

            try
            {
                _unit.ProcessData_Op210(_lastDto210);
                AppendLine("  [DB] EMPG UPDATE (RunOut Check) 완료");
            }
            catch (Exception ex)
            {
                AppendLine($"  [오류] {ex.GetType().Name}: {ex.Message}");
            }

            FlushPlcLog();
        }

        private void BtnPipeOp220_Click(object sender, RoutedEventArgs e)
        {
            if (_lastDto220 == null)
                _lastDto220 = _p220.Parse(BuildOp220ProcArray(), BuildOp200SettingArray());

            AppendLine("═══ OP220 전체 파이프라인 ═══════════════════════════════");
            AppendLine($"  → ControlUnit_DAQ.ProcessData_Op220() 호출");
            AppendLine($"  → Serial={_lastDto220.Serial}");

            try
            {
                _unit.ProcessData_Op220(_lastDto220);
                AppendLine("  [DB] EMPG UPDATE (Guiding Press Fitting) 완료");
            }
            catch (Exception ex)
            {
                AppendLine($"  [오류] {ex.GetType().Name}: {ex.Message}");
            }

            FlushPlcLog();
        }

        private void BtnPipeOp230_Click(object sender, RoutedEventArgs e)
        {
            if (_lastDto230 == null)
                _lastDto230 = _p230.Parse(BuildOp230ProcArray(), BuildOp230SettingArray());

            AppendLine("═══ OP230 전체 파이프라인 ═══════════════════════════════");
            AppendLine($"  → ControlUnit_DAQ.ProcessData_Op230() 호출");
            AppendLine($"  → Serial01={_lastDto230.Serial01}  Serial02={_lastDto230.Serial02}");

            try
            {
                _unit.ProcessData_Op230(_lastDto230);
                AppendLine("  [DB] EMPG UPDATE (Lotite / SOC) 완료");
            }
            catch (Exception ex)
            {
                AppendLine($"  [오류] {ex.GetType().Name}: {ex.Message}");
            }

            FlushPlcLog();
        }

        // ────────────────────────────────────────────────────────────────
        // EventBus 콜백 (ProcessData_Opxxx 내부에서 호출됨)
        // ────────────────────────────────────────────────────────────────

        // ObserveOnDispatcher() 로 UI 스레드 전환 — Dispatcher.InvokeAsync 불필요
        private void OnEventBusRow(EmpgRow row)
        {
            AppendLine($"  [EventBus] Publish 수신 → Model={row.Model}  Serial={row.MatSerial01}  TotalJudge={row.TotalJudge}");
        }

        // ────────────────────────────────────────────────────────────────
        // MockPLC 쓰기 로그 출력
        // ────────────────────────────────────────────────────────────────

        private void FlushPlcLog()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}]");

            foreach (var (addr, label) in new[] {
                ("D2001", "OP200  D2001 [PC_Response | PC_Complete_Flag | PC_Power_On]"),
                ("D2201", "OP210  D2201 [PC_Complete_Flag]"),
                ("D2301", "OP220  D2301 [PC_Complete_Flag]"),
                ("D2401", "OP230  D2401 [PC_Complete_Flag]"),
            })
            {
                var mem = _mockPlc.PeekMemory(addr);
                if (mem.Length > 0)
                {
                    sb.AppendLine(label);
                    sb.AppendLine("  words: " + string.Join(", ", mem.Select((v, i) => $"[{i}]={v}")));
                }
            }

            sb.AppendLine("─────────────────────────────");
            TxPlcLog.AppendText(sb.ToString());
            TxPlcLog.ScrollToEnd();
        }

        // ────────────────────────────────────────────────────────────────
        // DTO 출력 헬퍼
        // ────────────────────────────────────────────────────────────────

        private void AppendDto200(Op200ProcessDto d)
        {
            AppendLine($"  UpdateTime : {d.UpdateTime:yyyy-MM-dd HH:mm:ss.fff}");
            AppendLine($"  Repair     : {d.Repair}");
            AppendLine($"  Model      : {d.Model}");
            AppendLine($"  ShaftSerial: {d.ShaftSerial}");
            AppendLine($"  GearSerial : {d.GearSerial}");
            AppendLine($"  TotalJudge : {d.TotalJudge}");
            AppendLine($"  ── APD01~08 (Guide Ring Spacer) ──");
            AppendLine($"  APD01 GR_R1_Load   : {d.Apd01}  APD02 GR_R1_Stroke: {d.Apd02}");
            AppendLine($"  APD03 GR_R2_Load   : {d.Apd03}  APD04 GR_R2_Stroke: {d.Apd04}");
            AppendLine($"  APD05 GR_P_Load    : {d.Apd05}  APD06 GR_P_Stroke : {d.Apd06}");
            AppendLine($"  APD07 GR_Judge     : {d.Apd07}  APD08 GR_IndexNo  : {d.Apd08}");
            AppendLine($"  ── APD09~16 (Bearing) ──");
            AppendLine($"  APD09 BR_R1_Load   : {d.Apd09}  APD10 BR_R1_Stroke: {d.Apd10}");
            AppendLine($"  APD11 BR_R2_Load   : {d.Apd11}  APD12 BR_R2_Stroke: {d.Apd12}");
            AppendLine($"  APD13 BR_P_Load    : {d.Apd13}  APD14 BR_P_Stroke : {d.Apd14}");
            AppendLine($"  APD15 BR_Judge     : {d.Apd15}  APD16 BR_IndexNo  : {d.Apd16}");
            AppendLine($"  ── APD17~24 (Snap Ring) ──");
            AppendLine($"  APD17 SR_Groove_000: {d.Apd17}  APD18 SR_Groove_180: {d.Apd18}");
            AppendLine($"  APD19 SR_GradeData : {d.Apd19}  APD20 SR_Grade    : {d.Apd20}");
            AppendLine($"  APD21 SR_Groove_Jud: {d.Apd21}  APD22 SR_HeighThick: {d.Apd22}");
            AppendLine($"  APD23 SR_Heigh_Jud : {d.Apd23}  APD24 SR_Judge    : {d.Apd24}");
            AppendLine($"  ── APD25~26 (End Plate) ──");
            AppendLine($"  APD25 EndPlate_Data: {d.Apd25}  APD26 EndPlate_Jud: {d.Apd26}");
            AppendLine($"  ── SP01~12 (GR 상하한) ──");
            AppendLine($"  SP01={d.Sp01}  SP02={d.Sp02}  SP03={d.Sp03}  SP04={d.Sp04}");
            AppendLine($"  SP05={d.Sp05}  SP06={d.Sp06}  SP07={d.Sp07}  SP08={d.Sp08}");
            AppendLine($"  SP09={d.Sp09}  SP10={d.Sp10}  SP11={d.Sp11}  SP12={d.Sp12}");
            AppendLine($"  ── SP13~24 (BR 상하한) ──");
            AppendLine($"  SP13={d.Sp13}  SP14={d.Sp14}  SP15={d.Sp15}  SP16={d.Sp16}");
            AppendLine($"  SP17={d.Sp17}  SP18={d.Sp18}  SP19={d.Sp19}  SP20={d.Sp20}");
            AppendLine($"  SP21={d.Sp21}  SP22={d.Sp22}  SP23={d.Sp23}  SP24={d.Sp24}");
            AppendLine($"  ── SP25~30 (SR/EndPlate 상하한) ──");
            AppendLine($"  SP25={d.Sp25}  SP26={d.Sp26}  SP27={d.Sp27}  SP28={d.Sp28}");
            AppendLine($"  SP29={d.Sp29}  SP30={d.Sp30}");
            AppendLine("");
        }

        private void AppendDto210(Op210ProcessDto d)
        {
            AppendLine($"  Repair={d.Repair}  Model={d.Model}  Serial={d.Serial}");
            AppendLine($"  ── APD27~30 (RunOut Check) ──");
            AppendLine($"  APD27 Input     : {d.Apd27}  APD28 Input_Judge : {d.Apd28}");
            AppendLine($"  APD29 Space     : {d.Apd29}  APD30 Space_Judge : {d.Apd30}");
            AppendLine($"  ── SP31~36 (RunOut/Guiding 상하한) ──");
            AppendLine($"  SP31={d.Sp31}  SP32={d.Sp32}  SP33={d.Sp33}");
            AppendLine($"  SP34={d.Sp34}  SP35={d.Sp35}  SP36={d.Sp36}");
            AppendLine("");
        }

        private void AppendDto220(Op220ProcessDto d)
        {
            AppendLine($"  Repair={d.Repair}  Model={d.Model}  Serial={d.Serial}");
            AppendLine($"  ── APD31~33 (Guiding) ──");
            AppendLine($"  APD31 PressFitting_Judge: {d.Apd31}");
            AppendLine($"  APD32 ShortDist_Check   : {d.Apd32}");
            AppendLine($"  APD33 ShortDist_Judge   : {d.Apd33}");
            AppendLine($"  ── SP31~36 ──");
            AppendLine($"  SP31={d.Sp31}  SP32={d.Sp32}  SP33={d.Sp33}");
            AppendLine($"  SP34={d.Sp34}  SP35={d.Sp35}  SP36={d.Sp36}");
            AppendLine("");
        }

        private void AppendDto230(Op230ProcessDto d)
        {
            AppendLine($"  Repair={d.Repair}  Model={d.Model}");
            AppendLine($"  Serial01={d.Serial01}  Serial02={d.Serial02}");
            AppendLine($"  ── APD34~44 (Lotite / SOC) ──");
            AppendLine($"  APD34 Lotite_Disp_Jud: {d.Apd34}  APD35 Lotite_Vision_Jud: {d.Apd35}");
            AppendLine($"  APD36 R1_Load        : {d.Apd36}  APD37 R1_Stroke        : {d.Apd37}");
            AppendLine($"  APD38 R2_Load        : {d.Apd38}  APD39 R2_Stroke        : {d.Apd39}");
            AppendLine($"  APD40 P_Load         : {d.Apd40}  APD41 P_Stroke         : {d.Apd41}");
            AppendLine($"  APD42 SOCP_Judge     : {d.Apd42}  APD43 SOC_Check        : {d.Apd43}");
            AppendLine($"  APD44 SOC_Check_Judge: {d.Apd44}");
            AppendLine($"  ── SP37~50 ──");
            AppendLine($"  SP37={d.Sp37}  SP38={d.Sp38}  SP39={d.Sp39}  SP40={d.Sp40}");
            AppendLine($"  SP41={d.Sp41}  SP42={d.Sp42}  SP43={d.Sp43}  SP44={d.Sp44}");
            AppendLine($"  SP45={d.Sp45}  SP46={d.Sp46}  SP47={d.Sp47}  SP48={d.Sp48}");
            AppendLine($"  SP49={d.Sp49}  SP50={d.Sp50}");
            AppendLine("");
        }

        // ────────────────────────────────────────────────────────────────
        // Mock short[] 배열 빌더 — PLC 오프셋 규칙 그대로 적용
        // 이 빌더 로직은 ConSight.DONGBO.PlcSimulator.Logic.MockArrayBuilder 에 복제됨.
        // 오프셋·값 수정 시 MockArrayBuilder 도 반드시 동기화할 것.
        // ────────────────────────────────────────────────────────────────

        // OP200 공정 배열 (proc, D2000 기준, 100 words)
        private static short[] BuildOp200ProcArray()
        {
            var a = new short[100];
            a[0]  = 1;      // BackUp_Start
            a[2]  = 0;      // Repair: AUTO

            EncodeAscii(a, 10, 10, "MODEL-A");      // Model
            EncodeAscii(a, 20, 20, "SN-00001");     // ShaftSerial
            EncodeAscii(a, 40, 20, "GR-00001");     // GearSerial

            a[60] = 1;      // TotalJudge: OK

            // APD01~08 : Guide Ring Spacer
            a[61] = 1234;                  // GR_R1_Load   → 12.34
            SetInt32(a, 62, 123456);       // GR_R1_Stroke → 1234.56
            a[64] = 987;                   // GR_R2_Load   → 9.87
            SetInt32(a, 65, 98765);        // GR_R2_Stroke → 987.65
            a[67] = 500;                   // GR_P_Load    → 5.00
            SetInt32(a, 68, 50000);        // GR_P_Stroke  → 500.00
            a[70] = 1;                     // GR_Judge     : OK
            a[71] = 3;                     // GR_IndexNo

            // APD09~16 : Bearing
            a[72] = 2345;                  // BR_R1_Load   → 23.45
            SetInt32(a, 73, 234567);       // BR_R1_Stroke → 2345.67
            a[75] = 1111;                  // BR_R2_Load   → 11.11
            SetInt32(a, 76, 111100);       // BR_R2_Stroke → 1111.00
            a[78] = 600;                   // BR_P_Load    → 6.00
            SetInt32(a, 79, 60000);        // BR_P_Stroke  → 600.00
            a[81] = 1;                     // BR_Judge     : OK
            a[82] = 2;                     // BR_IndexNo

            // APD17~24 : Snap Ring
            SetInt32(a, 83, 12345);        // SR_Groove_000 → 1.2345 (/10000)
            SetInt32(a, 85, 12356);        // SR_Groove_180 → 1.2356
            SetInt32(a, 87, 11000);        // SR_GradeData  → 110.00 (/100)
            a[89] = 2;                     // SR_Grade
            a[90] = 1;                     // SR_Groove_Judge: OK
            SetInt32(a, 91, 15000);        // SR_Heigh_Thick → 150.00
            a[93] = 1;                     // SR_Heigh_Judge : OK
            a[94] = 1;                     // SR_Judge       : OK

            // APD25~26 : End Plate
            SetInt32(a, 95, 10050);        // EndPlate_Data → 100.50
            a[97] = 1;                     // EndPlate_Judge: OK

            return a;
        }

        // OP200 설정 배열 (D1900 기준, 100 words — OP210/220 도 동일 배열 공유)
        private static short[] BuildOp200SettingArray()
        {
            var a = new short[100];

            // SP01~02 : GR_R1_Load Lower/Upper
            a[0] = 500;  a[1] = 2000;
            // SP03~04 : GR_R1_Stroke Lower/Upper (2-word)
            SetInt32(a, 2, 50000); SetInt32(a, 4, 200000);
            // SP05~06 : GR_R2_Load Lower/Upper
            a[6] = 400;  a[7] = 1800;
            // SP07~08 : GR_R2_Stroke Lower/Upper
            SetInt32(a, 8, 40000); SetInt32(a, 10, 180000);
            // SP09~10 : GR_P_Load Lower/Upper
            a[12] = 100; a[13] = 1000;
            // SP11~12 : GR_P_Stroke Lower/Upper
            SetInt32(a, 14, 10000); SetInt32(a, 16, 100000);

            // SP13~14 : BR_R1_Load Lower/Upper
            a[20] = 1000; a[21] = 4000;
            // SP15~16 : BR_R1_Stroke Lower/Upper
            SetInt32(a, 22, 100000); SetInt32(a, 24, 400000);
            // SP17~18 : BR_R2_Load Lower/Upper
            a[26] = 800; a[27] = 3000;
            // SP19~20 : BR_R2_Stroke Lower/Upper
            SetInt32(a, 28, 80000); SetInt32(a, 30, 300000);
            // SP21~22 : BR_P_Load Lower/Upper
            a[32] = 200; a[33] = 1500;
            // SP23~24 : BR_P_Stroke Lower/Upper
            SetInt32(a, 34, 20000); SetInt32(a, 36, 150000);

            // SP25~26 : SR_Groove_Grade Lower/Upper (/10000)
            SetInt32(a, 48, 10000); SetInt32(a, 50, 20000);
            // SP27~28 : SR_Heigh_Thick Lower/Upper (/10000)
            SetInt32(a, 52, 12000); SetInt32(a, 54, 18000);

            // SP29~30 : EndPlate_Data Lower/Upper (/10000)
            SetInt32(a, 60, 9500); SetInt32(a, 62, 11000);

            // SP31~32 : RunOutCheck_Input Lower/Upper (/100 → "00.0" 포맷)
            SetInt32(a, 70, 500);  SetInt32(a, 72, 1500);
            // SP33~34 : RunOutCheck_Space Lower/Upper (/10000)
            SetInt32(a, 74, 5000); SetInt32(a, 76, 15000);
            // SP35~36 : Guiding_ShortDist Lower/Upper (/10000)
            SetInt32(a, 80, 8000); SetInt32(a, 82, 12000);

            return a;
        }

        // OP210 공정 배열 (D2200 기준, 70 words)
        private static short[] BuildOp210ProcArray()
        {
            var a = new short[70];
            a[0] = 1;   // BackUp_Start
            a[2] = 0;   // Repair: AUTO
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");   // OP200 과 동일 시리얼

            SetInt32(a, 60, 9800);    // RunOutCheck_Input  → 0.9800 (/10000)
            a[62] = 1;                // RunOutCheck_Input_Judge: OK
            SetInt32(a, 63, 10200);   // RunOutCheck_Space  → 1.0200
            a[65] = 1;                // RunOutCheck_Space_Judge: OK
            return a;
        }

        // OP220 공정 배열 (D2300 기준, 70 words)
        private static short[] BuildOp220ProcArray()
        {
            var a = new short[70];
            a[0] = 1;
            a[2] = 0;
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");

            a[60] = 1;                          // GuidingPressFitting_Judge: OK
            a[61] = (short)9950;                // Guiding_ShortDist_Check 1워드 → 0.9950 (/10000)
            a[63] = 1;                          // Guiding_ShortDist_Judge: OK
            return a;
        }

        // OP230 공정 배열 (D2400 기준, 80 words)
        private static short[] BuildOp230ProcArray()
        {
            var a = new short[80];
            a[0] = 1;
            a[2] = 0;
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");  // Serial01
            EncodeAscii(a, 40, 20, "GR-00001");  // Serial02

            a[60] = 1;              // Lotite_Dispensing_Judge: OK
            a[61] = 1;              // Lotite_Vision_Judge    : OK
            a[62] = 3456;           // SOCP_R1_Load  → 34.56
            SetInt32(a, 63, 34567); // SOCP_R1_Stroke → 345.67
            a[65] = 3200;           // SOCP_R2_Load  → 32.00
            SetInt32(a, 66, 32000); // SOCP_R2_Stroke → 320.00
            a[68] = 1500;           // SOCP_P_Load   → 15.00
            SetInt32(a, 69, 15000); // SOCP_P_Stroke  → 150.00
            a[71] = 1;              // SOCP_Judge     : OK
            SetInt32(a, 72, 99990); // SOC_Check      → 9.9990 (/10000)
            a[74] = 1;              // SOC_Check_Judge: OK
            return a;
        }

        // OP230 설정 배열 (D1800 기준, 24 words)
        private static short[] BuildOp230SettingArray()
        {
            var a = new short[24];
            a[0] = 2000; a[1] = 5000;           // SOCP_R1_Load Lower/Upper
            SetInt32(a, 2,  20000); SetInt32(a, 4,  50000); // SOCP_R1_Stroke
            a[6] = 1500; a[7] = 4500;           // SOCP_R2_Load Lower/Upper
            SetInt32(a, 8,  15000); SetInt32(a, 10, 45000); // SOCP_R2_Stroke
            a[12] = 1000; a[13] = 3000;         // SOCP_P_Load Lower/Upper
            SetInt32(a, 14, 10000); SetInt32(a, 16, 30000); // SOCP_P_Stroke
            SetInt32(a, 18, 80000); SetInt32(a, 20, 120000); // SOC_Check Lower/Upper (/10000)
            return a;
        }

        // ────────────────────────────────────────────────────────────────
        // 공용 유틸
        // ────────────────────────────────────────────────────────────────

        // ASCII 문자열 → PLC short 인코딩 (첫 번째 문자 → 하위 바이트, 두 번째 → 상위 바이트)
        private static void EncodeAscii(short[] arr, int offset, int maxWords, string s)
        {
            int words = Math.Min(maxWords, (s.Length + 1) / 2 + 1);
            for (int i = 0; i < words && offset + i < arr.Length; i++)
            {
                byte lo = (i * 2)     < s.Length ? (byte)s[i * 2]     : (byte)0;
                byte hi = (i * 2 + 1) < s.Length ? (byte)s[i * 2 + 1] : (byte)0;
                arr[offset + i] = (short)(lo | (hi << 8));
            }
        }

        // 32-bit int → 2개의 연속 short (low word first)
        private static void SetInt32(short[] arr, int offset, int value)
        {
            arr[offset]     = (short)(value & 0xFFFF);
            arr[offset + 1] = (short)((value >> 16) & 0xFFFF);
        }

        private void AppendLine(string text)
        {
            TxResult.AppendText(text + "\n");
            TxResult.ScrollToEnd();
        }
    }
}
