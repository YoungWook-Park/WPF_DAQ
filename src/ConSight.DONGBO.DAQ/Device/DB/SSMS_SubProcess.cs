// Phase C: SSMS_SubProcess — OP210/220/230 서브공정 DB 연산
//
// 역할:
//   1. FindBySerial()   — 시리얼로 EMPG 행 조회 → EmpgRow?
//   2. UpdateSubCols()  — EmpgRow 의 서브공정 필드(APD27~44, SP31~50, TotalJudge)를 UPDATE
//   3. InsertFallback() — OP200 행이 없을 때 서브공정 데이터만으로 신규 INSERT
//
// 서브공정 파이프라인:
//   dto 파싱 → FindBySerial(serial) →
//     found  : row.ApplyOpXxx(dto) → UpdateSubCols(row)
//     missing: EmpgRow 직접 구성   → InsertFallback(row)

using System.Data;
using Bi.ConSight.SqlAgent;
using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.Device
{
    public sealed class SSMS_SubProcess
    {
        private readonly string _connectionString;

        public SSMS_SubProcess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ── 1. FindBySerial ───────────────────────────────────────────────
        /// <summary>
        /// MAT_SERIAL01 또는 MAT_SERIAL02 가 일치하는 가장 최근 EMPG 행을 반환한다.
        /// 해당 행이 없으면 null 을 반환한다.
        /// </summary>
        public EmpgRow? FindBySerial(string serial)
        {
            if (string.IsNullOrEmpty(serial)) return null;

            string s = SqlQuotedString(serial);
            var qExe = new QueryExecution(_connectionString);
            qExe.AppendQuery(
                "SELECT TOP 1 * FROM EMPG " +
                "WHERE MAT_SERIAL01 = " + s + " OR MAT_SERIAL02 = " + s + " " +
                "ORDER BY CREATE_DAYTIME DESC");

            var rows = qExe.ExecuteReader<EmpgRow>(MapRow);
            return rows.FirstOrDefault();
        }


        // ── 2. UpdateSubCols ──────────────────────────────────────────────
        /// <summary>
        /// EMPG 행의 서브공정 컬럼(APD27~44, SP31~50, TotalJudge, UpdateTime)을
        /// EmpgRow 기준으로 UPDATE 한다.
        /// </summary>
        public void UpdateSubCols(EmpgRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "UPDATE EMPG SET " +
                "UPDATE_TIME = " + SqlDateTime(row.UpdateTime) + "," +
                "TOTAL_JUDGE = " + SqlQuotedString(row.TotalJudge) + "," +
                "APD27 = " + SqlQuotedString(row.Apd27) + "," +
                "APD28 = " + SqlQuotedString(row.Apd28) + "," +
                "APD29 = " + SqlQuotedString(row.Apd29) + "," +
                "APD30 = " + SqlQuotedString(row.Apd30) + "," +
                "APD31 = " + SqlQuotedString(row.Apd31) + "," +
                "APD32 = " + SqlQuotedString(row.Apd32) + "," +
                "APD33 = " + SqlQuotedString(row.Apd33) + "," +
                "APD34 = " + SqlQuotedString(row.Apd34) + "," +
                "APD35 = " + SqlQuotedString(row.Apd35) + "," +
                "APD36 = " + SqlQuotedString(row.Apd36) + "," +
                "APD37 = " + SqlQuotedString(row.Apd37) + "," +
                "APD38 = " + SqlQuotedString(row.Apd38) + "," +
                "APD39 = " + SqlQuotedString(row.Apd39) + "," +
                "APD40 = " + SqlQuotedString(row.Apd40) + "," +
                "APD41 = " + SqlQuotedString(row.Apd41) + "," +
                "APD42 = " + SqlQuotedString(row.Apd42) + "," +
                "APD43 = " + SqlQuotedString(row.Apd43) + "," +
                "APD44 = " + SqlQuotedString(row.Apd44) + "," +
                "SP31 = " + SqlQuotedString(row.Sp31) + "," +
                "SP32 = " + SqlQuotedString(row.Sp32) + "," +
                "SP33 = " + SqlQuotedString(row.Sp33) + "," +
                "SP34 = " + SqlQuotedString(row.Sp34) + "," +
                "SP35 = " + SqlQuotedString(row.Sp35) + "," +
                "SP36 = " + SqlQuotedString(row.Sp36) + "," +
                "SP37 = " + SqlQuotedString(row.Sp37) + "," +
                "SP38 = " + SqlQuotedString(row.Sp38) + "," +
                "SP39 = " + SqlQuotedString(row.Sp39) + "," +
                "SP40 = " + SqlQuotedString(row.Sp40) + "," +
                "SP41 = " + SqlQuotedString(row.Sp41) + "," +
                "SP42 = " + SqlQuotedString(row.Sp42) + "," +
                "SP43 = " + SqlQuotedString(row.Sp43) + "," +
                "SP44 = " + SqlQuotedString(row.Sp44) + "," +
                "SP45 = " + SqlQuotedString(row.Sp45) + "," +
                "SP46 = " + SqlQuotedString(row.Sp46) + "," +
                "SP47 = " + SqlQuotedString(row.Sp47) + "," +
                "SP48 = " + SqlQuotedString(row.Sp48) + "," +
                "SP49 = " + SqlQuotedString(row.Sp49) + "," +
                "SP50 = " + SqlQuotedString(row.Sp50) + " " +
                "WHERE RESULT_ID = " + SqlQuotedString(row.ResultId));

            nqExe.Execute();
        }

        // ── 3. InsertFallback ─────────────────────────────────────────────
        /// <summary>
        /// OP200 행이 DB에 없을 때 서브공정 데이터만으로 EMPG 행을 신규 삽입한다.
        /// APD01~26 / SP01~30 은 빈 값으로 채워진다.
        /// </summary>
        public void InsertFallback(EmpgRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // ResultId 가 없으면 지금 생성
            if (string.IsNullOrEmpty(row.ResultId))
                row.ResultId = Guid.NewGuid().ToString("N");

            var nqExe = new NonQueryExecution(_connectionString);
            string sql1 = "";
            string sql2 = "";

            sql1 += "RESULT_ID,";        sql2 += SqlQuotedString(row.ResultId) + ",";
            sql1 += "UPDATE_TIME,";      sql2 += SqlDateTime(row.UpdateTime) + ",";
            sql1 += "REPAIR,";           sql2 += SqlQuotedString(row.Repair) + ",";
            sql1 += "MODEL,";            sql2 += SqlQuotedString(row.Model) + ",";
            sql1 += "CYCLETIME,";        sql2 += "0,";
            sql1 += "CREATE_DAYTIME,";   sql2 += SqlQuotedString(now) + ",";
            sql1 += "MAT_SERIAL01,";     sql2 += SqlQuotedString(row.MatSerial01) + ",";
            sql1 += "MAT_SERIAL02,";     sql2 += SqlQuotedString(row.MatSerial02) + ",";
            sql1 += "TOTAL_JUDGE,";      sql2 += SqlQuotedString(row.TotalJudge) + ",";

            sql1 += "APD27,";            sql2 += SqlQuotedString(row.Apd27) + ",";
            sql1 += "APD28,";            sql2 += SqlQuotedString(row.Apd28) + ",";
            sql1 += "APD29,";            sql2 += SqlQuotedString(row.Apd29) + ",";
            sql1 += "APD30,";            sql2 += SqlQuotedString(row.Apd30) + ",";
            sql1 += "APD31,";            sql2 += SqlQuotedString(row.Apd31) + ",";
            sql1 += "APD32,";            sql2 += SqlQuotedString(row.Apd32) + ",";
            sql1 += "APD33,";            sql2 += SqlQuotedString(row.Apd33) + ",";
            sql1 += "APD34,";            sql2 += SqlQuotedString(row.Apd34) + ",";
            sql1 += "APD35,";            sql2 += SqlQuotedString(row.Apd35) + ",";
            sql1 += "APD36,";            sql2 += SqlQuotedString(row.Apd36) + ",";
            sql1 += "APD37,";            sql2 += SqlQuotedString(row.Apd37) + ",";
            sql1 += "APD38,";            sql2 += SqlQuotedString(row.Apd38) + ",";
            sql1 += "APD39,";            sql2 += SqlQuotedString(row.Apd39) + ",";
            sql1 += "APD40,";            sql2 += SqlQuotedString(row.Apd40) + ",";
            sql1 += "APD41,";            sql2 += SqlQuotedString(row.Apd41) + ",";
            sql1 += "APD42,";            sql2 += SqlQuotedString(row.Apd42) + ",";
            sql1 += "APD43,";            sql2 += SqlQuotedString(row.Apd43) + ",";
            sql1 += "APD44,";            sql2 += SqlQuotedString(row.Apd44) + ",";

            sql1 += "SP31,";             sql2 += SqlQuotedString(row.Sp31) + ",";
            sql1 += "SP32,";             sql2 += SqlQuotedString(row.Sp32) + ",";
            sql1 += "SP33,";             sql2 += SqlQuotedString(row.Sp33) + ",";
            sql1 += "SP34,";             sql2 += SqlQuotedString(row.Sp34) + ",";
            sql1 += "SP35,";             sql2 += SqlQuotedString(row.Sp35) + ",";
            sql1 += "SP36,";             sql2 += SqlQuotedString(row.Sp36) + ",";
            sql1 += "SP37,";             sql2 += SqlQuotedString(row.Sp37) + ",";
            sql1 += "SP38,";             sql2 += SqlQuotedString(row.Sp38) + ",";
            sql1 += "SP39,";             sql2 += SqlQuotedString(row.Sp39) + ",";
            sql1 += "SP40,";             sql2 += SqlQuotedString(row.Sp40) + ",";
            sql1 += "SP41,";             sql2 += SqlQuotedString(row.Sp41) + ",";
            sql1 += "SP42,";             sql2 += SqlQuotedString(row.Sp42) + ",";
            sql1 += "SP43,";             sql2 += SqlQuotedString(row.Sp43) + ",";
            sql1 += "SP44,";             sql2 += SqlQuotedString(row.Sp44) + ",";
            sql1 += "SP45,";             sql2 += SqlQuotedString(row.Sp45) + ",";
            sql1 += "SP46,";             sql2 += SqlQuotedString(row.Sp46) + ",";
            sql1 += "SP47,";             sql2 += SqlQuotedString(row.Sp47) + ",";
            sql1 += "SP48,";             sql2 += SqlQuotedString(row.Sp48) + ",";
            sql1 += "SP49,";             sql2 += SqlQuotedString(row.Sp49) + ",";
            sql1 += "SP50,";             sql2 += SqlQuotedString(row.Sp50) + ",";

            sql1 = sql1.TrimEnd(',');
            sql2 = sql2.TrimEnd(',');
            nqExe.AppendQuery("INSERT INTO EMPG (" + sql1 + ") VALUES (" + sql2 + ")");

            nqExe.Execute();
        }

        // ── DataRecord → EmpgRow 매핑 ─────────────────────────────────────

        private static string SqlQuotedString(string? value) =>
            "'" + (value ?? string.Empty).Replace("'", "''") + "'";

        private static string SqlDateTime(DateTime dt) =>
            "'" + dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";

        private static EmpgRow MapRow(IDataRecord r)
        {
            static string S(IDataRecord rec, string col)
                => rec[col] == DBNull.Value ? string.Empty : (rec[col] as string ?? string.Empty);

            return new EmpgRow
            {
                ResultId    = S(r, "RESULT_ID"),
                UpdateTime  = r["UPDATE_TIME"] == DBNull.Value ? DateTime.MinValue : (DateTime)r["UPDATE_TIME"],
                Repair      = S(r, "REPAIR"),
                Model       = S(r, "MODEL"),
                MatSerial01 = S(r, "MAT_SERIAL01"),
                MatSerial02 = S(r, "MAT_SERIAL02"),
                TotalJudge  = S(r, "TOTAL_JUDGE"),

                Apd01 = S(r,"APD01"), Apd02 = S(r,"APD02"), Apd03 = S(r,"APD03"), Apd04 = S(r,"APD04"),
                Apd05 = S(r,"APD05"), Apd06 = S(r,"APD06"), Apd07 = S(r,"APD07"), Apd08 = S(r,"APD08"),
                Apd09 = S(r,"APD09"), Apd10 = S(r,"APD10"), Apd11 = S(r,"APD11"), Apd12 = S(r,"APD12"),
                Apd13 = S(r,"APD13"), Apd14 = S(r,"APD14"), Apd15 = S(r,"APD15"), Apd16 = S(r,"APD16"),
                Apd17 = S(r,"APD17"), Apd18 = S(r,"APD18"), Apd19 = S(r,"APD19"), Apd20 = S(r,"APD20"),
                Apd21 = S(r,"APD21"), Apd22 = S(r,"APD22"), Apd23 = S(r,"APD23"), Apd24 = S(r,"APD24"),
                Apd25 = S(r,"APD25"), Apd26 = S(r,"APD26"),
                Apd27 = S(r,"APD27"), Apd28 = S(r,"APD28"), Apd29 = S(r,"APD29"), Apd30 = S(r,"APD30"),
                Apd31 = S(r,"APD31"), Apd32 = S(r,"APD32"), Apd33 = S(r,"APD33"),
                Apd34 = S(r,"APD34"), Apd35 = S(r,"APD35"), Apd36 = S(r,"APD36"), Apd37 = S(r,"APD37"),
                Apd38 = S(r,"APD38"), Apd39 = S(r,"APD39"), Apd40 = S(r,"APD40"), Apd41 = S(r,"APD41"),
                Apd42 = S(r,"APD42"), Apd43 = S(r,"APD43"), Apd44 = S(r,"APD44"),

                Sp01 = S(r,"SP01"), Sp02 = S(r,"SP02"), Sp03 = S(r,"SP03"), Sp04 = S(r,"SP04"),
                Sp05 = S(r,"SP05"), Sp06 = S(r,"SP06"), Sp07 = S(r,"SP07"), Sp08 = S(r,"SP08"),
                Sp09 = S(r,"SP09"), Sp10 = S(r,"SP10"), Sp11 = S(r,"SP11"), Sp12 = S(r,"SP12"),
                Sp13 = S(r,"SP13"), Sp14 = S(r,"SP14"), Sp15 = S(r,"SP15"), Sp16 = S(r,"SP16"),
                Sp17 = S(r,"SP17"), Sp18 = S(r,"SP18"), Sp19 = S(r,"SP19"), Sp20 = S(r,"SP20"),
                Sp21 = S(r,"SP21"), Sp22 = S(r,"SP22"), Sp23 = S(r,"SP23"), Sp24 = S(r,"SP24"),
                Sp25 = S(r,"SP25"), Sp26 = S(r,"SP26"), Sp27 = S(r,"SP27"), Sp28 = S(r,"SP28"),
                Sp29 = S(r,"SP29"), Sp30 = S(r,"SP30"),
                Sp31 = S(r,"SP31"), Sp32 = S(r,"SP32"), Sp33 = S(r,"SP33"), Sp34 = S(r,"SP34"),
                Sp35 = S(r,"SP35"), Sp36 = S(r,"SP36"),
                Sp37 = S(r,"SP37"), Sp38 = S(r,"SP38"), Sp39 = S(r,"SP39"), Sp40 = S(r,"SP40"),
                Sp41 = S(r,"SP41"), Sp42 = S(r,"SP42"), Sp43 = S(r,"SP43"), Sp44 = S(r,"SP44"),
                Sp45 = S(r,"SP45"), Sp46 = S(r,"SP46"), Sp47 = S(r,"SP47"), Sp48 = S(r,"SP48"),
                Sp49 = S(r,"SP49"), Sp50 = S(r,"SP50"),
            };
        }
    }
}
