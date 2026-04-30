// Phase D: EmpgCsvWriter — CsvHelper 기반 CSV 기록기
//
// EmpgRow 한 행을 CSV 파일에 안전하게 추가한다.
// 원본의 StringBuilder.Append() 체인을 대체하며
// 쉼표·따옴표 등 특수문자를 CsvHelper 가 자동으로 처리한다.
//
// 파일 구조:
//   {baseFolderPath}\{yyyy-MM-dd}\{model}_{yyyy-MM-dd}.csv
//   — 하루 단위 폴더, 모델별 파일
//   — 파일이 없으면 헤더 + 데이터 행 생성
//   — 파일이 있으면 데이터 행만 추가 (append)
//
// 컬럼 순서 / 헤더 이름:
//   원본 LineDaqMainWindowVM.cs 의 csvColumsNames (OP200 모드) 와 동일하게 유지

using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace ConSight.DAQ.Device.DB
{
    public sealed class EmpgCsvWriter
    {
        private readonly string _baseFolderPath;

        public EmpgCsvWriter(string baseFolderPath)
        {
            _baseFolderPath = baseFolderPath;
        }

        /// <summary>EmpgRow 한 행을 오늘 날짜 CSV 파일에 추가한다.</summary>
        public void Append(EmpgRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            string today   = DateTime.Today.ToString("yyyy-MM-dd");
            string model   = string.IsNullOrWhiteSpace(row.Model) ? "UNKNOWN" : row.Model;
            string dir     = Path.Combine(_baseFolderPath, today);
            string csvPath = Path.Combine(dir, $"{model}_{today}.csv");

            Directory.CreateDirectory(dir);

            bool isNewFile = !File.Exists(csvPath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,   // 헤더 여부는 수동으로 제어
            };

            using var stream = new StreamWriter(csvPath, append: true, encoding: System.Text.Encoding.UTF8);
            using var csv    = new CsvWriter(stream, config);

            csv.Context.RegisterClassMap<EmpgRowMap>();

            if (isNewFile)
            {
                csv.WriteHeader<EmpgRow>();
                csv.NextRecord();
            }

            csv.WriteRecord(row);
            csv.NextRecord();
        }
    }

    // ── ClassMap: EmpgRow 프로퍼티 → CSV 컬럼 이름 + 순서 ─────────────────
    // 원본 csvColumsNames(OP200 모드) 와 동일한 순서로 정의한다.
    internal sealed class EmpgRowMap : ClassMap<EmpgRow>
    {
        public EmpgRowMap()
        {
            int i = 0;

            // ── 식별자 ─────────────────────────────────────────────────────
            Map(m => m.UpdateTime ).Index(i++).Name("Date/Time");
            Map(m => m.Repair     ).Index(i++).Name("Repair");
            Map(m => m.Model      ).Index(i++).Name("Model Name");
            Map(m => m.MatSerial02).Index(i++).Name("기어 Serial No");
            Map(m => m.MatSerial01).Index(i++).Name("샤프트 Serial No");
            Map(m => m.TotalJudge ).Index(i++).Name("종합 판정");

            // ── APD01~08 : Guide Ring Spacer ──────────────────────────────
            Map(m => m.Apd01).Index(i++).Name("G_S R1.Load");
            Map(m => m.Apd02).Index(i++).Name("G_S R1.Stroke");
            Map(m => m.Apd03).Index(i++).Name("G_S R2.Load");
            Map(m => m.Apd04).Index(i++).Name("G_S R2.Stroke");
            Map(m => m.Apd05).Index(i++).Name("G_S P.Load");
            Map(m => m.Apd06).Index(i++).Name("G_S P.Stroke");
            Map(m => m.Apd07).Index(i++).Name("G_S Judge");
            Map(m => m.Apd08).Index(i++).Name("G_S Index No.");

            // ── APD09~16 : Bearing ────────────────────────────────────────
            Map(m => m.Apd09).Index(i++).Name("BR R1.Load");
            Map(m => m.Apd10).Index(i++).Name("BR R1.Stroke");
            Map(m => m.Apd11).Index(i++).Name("BR R2.Load");
            Map(m => m.Apd12).Index(i++).Name("BR R2.Stroke");
            Map(m => m.Apd13).Index(i++).Name("BR P.Load");
            Map(m => m.Apd14).Index(i++).Name("BR P.Stroke");
            Map(m => m.Apd15).Index(i++).Name("BR Judge");
            Map(m => m.Apd16).Index(i++).Name("BR Index No.");

            // ── APD17~24 : Snap Ring ──────────────────────────────────────
            Map(m => m.Apd17).Index(i++).Name("SR 홈폭 0도");
            Map(m => m.Apd18).Index(i++).Name("SR 홈폭 180도");
            Map(m => m.Apd19).Index(i++).Name("SR 홈폭 Grade Data");
            Map(m => m.Apd20).Index(i++).Name("SR 홈폭 Grade");
            Map(m => m.Apd21).Index(i++).Name("SR 홈폭 Judge");
            Map(m => m.Apd22).Index(i++).Name("SR 높이 두께");
            Map(m => m.Apd23).Index(i++).Name("SR 높이 Judge");
            Map(m => m.Apd24).Index(i++).Name("SR Judge");

            // ── APD25~26 : End Plate ──────────────────────────────────────
            Map(m => m.Apd25).Index(i++).Name("End Plate Data");
            Map(m => m.Apd26).Index(i++).Name("End Plate Judge");

            // ── APD27~30 : OP210 RunOut Check ─────────────────────────────
            Map(m => m.Apd27).Index(i++).Name("Run Out Check Input");
            Map(m => m.Apd28).Index(i++).Name("Run Out Check Input Judge");
            Map(m => m.Apd29).Index(i++).Name("Run Out Check Space");
            Map(m => m.Apd30).Index(i++).Name("Run Out Check Space Judge");

            // ── APD31~33 : OP220 Guiding ─────────────────────────────────
            Map(m => m.Apd31).Index(i++).Name("GR 압입 Judge");
            Map(m => m.Apd32).Index(i++).Name("GR 단거리 Check");
            Map(m => m.Apd33).Index(i++).Name("GR 단거리 Judge");

            // ── APD34~44 : OP230 Lotite / Shaft Oil Cap ───────────────────
            Map(m => m.Apd34).Index(i++).Name("Lotite 도포 Judge");
            Map(m => m.Apd35).Index(i++).Name("Lotite 비전 Judge");
            Map(m => m.Apd36).Index(i++).Name("S_O_C_P R1_Load");
            Map(m => m.Apd37).Index(i++).Name("S_O_C_P R1_Stroke");
            Map(m => m.Apd38).Index(i++).Name("S_O_C_P R2_Load");
            Map(m => m.Apd39).Index(i++).Name("S_O_C_P R2_Stroke");
            Map(m => m.Apd40).Index(i++).Name("S_O_C_P P_Load");
            Map(m => m.Apd41).Index(i++).Name("S_O_C_P P_Stroke");
            Map(m => m.Apd42).Index(i++).Name("S_O_C_P Judge");
            Map(m => m.Apd43).Index(i++).Name("S_O_C Check");
            Map(m => m.Apd44).Index(i++).Name("S_O_C Check Judge");

            // ── SP01~12 : G_S 상하한 ──────────────────────────────────────
            Map(m => m.Sp01).Index(i++).Name("G_S R1.Load Lower");
            Map(m => m.Sp02).Index(i++).Name("G_S R1.Load Upper");
            Map(m => m.Sp03).Index(i++).Name("G_S R1.Stroke Lower");
            Map(m => m.Sp04).Index(i++).Name("G_S R1.Stroke Upper");
            Map(m => m.Sp05).Index(i++).Name("G_S R2.Load Lower");
            Map(m => m.Sp06).Index(i++).Name("G_S R2.Load Upper");
            Map(m => m.Sp07).Index(i++).Name("G_S R2.Stroke Lower");
            Map(m => m.Sp08).Index(i++).Name("G_S R2.Stroke Upper");
            Map(m => m.Sp09).Index(i++).Name("G_S P.Load Lower");
            Map(m => m.Sp10).Index(i++).Name("G_S P.Load Upper");
            Map(m => m.Sp11).Index(i++).Name("G_S P.Stroke Lower");
            Map(m => m.Sp12).Index(i++).Name("G_S P.Stroke Upper");

            // ── SP13~24 : BR 상하한 ───────────────────────────────────────
            Map(m => m.Sp13).Index(i++).Name("BR R1.Load Lower");
            Map(m => m.Sp14).Index(i++).Name("BR R1.Load Upper");
            Map(m => m.Sp15).Index(i++).Name("BR R1.Stroke Lower");
            Map(m => m.Sp16).Index(i++).Name("BR R1.Stroke Upper");
            Map(m => m.Sp17).Index(i++).Name("BR R2.Load Lower");
            Map(m => m.Sp18).Index(i++).Name("BR R2.Load Upper");
            Map(m => m.Sp19).Index(i++).Name("BR R2.Stroke Lower");
            Map(m => m.Sp20).Index(i++).Name("BR R2.Stroke Upper");
            Map(m => m.Sp21).Index(i++).Name("BR P.Load Lower");
            Map(m => m.Sp22).Index(i++).Name("BR P.Load Upper");
            Map(m => m.Sp23).Index(i++).Name("BR P.Stroke Lower");
            Map(m => m.Sp24).Index(i++).Name("BR P.Stroke Upper");

            // ── SP25~30 : SR / End Plate 상하한 ──────────────────────────
            Map(m => m.Sp25).Index(i++).Name("SR 홈폭 Grade Lower");
            Map(m => m.Sp26).Index(i++).Name("SR 홈폭 Grade Upper");
            Map(m => m.Sp27).Index(i++).Name("SR 높이 두께 Lower");
            Map(m => m.Sp28).Index(i++).Name("SR 높이 두께 Upper");
            Map(m => m.Sp29).Index(i++).Name("End Plate Data Lower");
            Map(m => m.Sp30).Index(i++).Name("End Plate Data Upper");

            // ── SP31~36 : RunOut / Guiding 상하한 ────────────────────────
            Map(m => m.Sp31).Index(i++).Name("Run Out Check Input Lower");
            Map(m => m.Sp32).Index(i++).Name("Run Out Check Input Upper");
            Map(m => m.Sp33).Index(i++).Name("Run Out Check Space Lower");
            Map(m => m.Sp34).Index(i++).Name("Run Out Check Space Upper");
            Map(m => m.Sp35).Index(i++).Name("GR 단거리 Lower");
            Map(m => m.Sp36).Index(i++).Name("GR 단거리 Upper");

            // ── SP37~48 : S_O_C_P 상하한 ─────────────────────────────────
            Map(m => m.Sp37).Index(i++).Name("S_O_C_P R1_load Lower");
            Map(m => m.Sp38).Index(i++).Name("S_O_C_P R1_load Upper");
            Map(m => m.Sp39).Index(i++).Name("S_O_C_P R1_Stroke Lower");
            Map(m => m.Sp40).Index(i++).Name("S_O_C_P R1_Stroke Upper");
            Map(m => m.Sp41).Index(i++).Name("S_O_C_P R2_load Lower");
            Map(m => m.Sp42).Index(i++).Name("S_O_C_P R2_load Upper");
            Map(m => m.Sp43).Index(i++).Name("S_O_C_P R2_Stroke Lower");
            Map(m => m.Sp44).Index(i++).Name("S_O_C_P R2_Stroke Upper");
            Map(m => m.Sp45).Index(i++).Name("S_O_C_P P_load Lower");
            Map(m => m.Sp46).Index(i++).Name("S_O_C_P P_load Upper");
            Map(m => m.Sp47).Index(i++).Name("S_O_C_P P_Stroke Lower");
            Map(m => m.Sp48).Index(i++).Name("S_O_C_P P_Stroke Upper");

            // ── SP49~50 : S_O_C Check 상하한 ─────────────────────────────
            Map(m => m.Sp49).Index(i++).Name("S_O_C Check Lower");
            Map(m => m.Sp50).Index(i++).Name("S_O_C Check Upper");
        }
    }
}
