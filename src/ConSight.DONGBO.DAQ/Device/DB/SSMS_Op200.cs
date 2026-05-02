// Phase C: SSMS_Op200 — OP200 공정 완료 시 EMPG 행 INSERT / UPDATE
//
// EmpgRow (APD01~26, SP01~30) 를 받아 DB 에 한 행을 삽입하거나,
// 이미 존재하는 행을 OP200 컬럼 기준으로 갱신한다.
//
// 공개 메서드:
//   Insert()          — 신규 행 INSERT
//   FindBySerial()    — MAT_SERIAL01/02 로 기존 행 조회
//   UpdateOp200Cols() — APD01~26, SP01~30, 식별자 컬럼 UPDATE

using System.Data;
using Bi.ConSight.SqlAgent;
using ConSight.DAQ.Device.DB;

namespace ConSight.DAQ.Device
{
    public sealed class SSMS_Op200
    {
        private readonly string _connectionString;

        public SSMS_Op200(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ── INSERT ──────────────────────────────────────────────────────────

        /// <summary>EMPG 테이블에 OP200 공정 행을 삽입한다.</summary>
        public void Insert(EmpgRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "INSERT INTO EMPG (" +
                "  RESULT_ID, UPDATE_TIME, REPAIR, MODEL, CYCLETIME, CREATE_DAYTIME," +
                "  MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE, OP200_TOTAL_JUDGE," +
                "  APD01,APD02,APD03,APD04,APD05,APD06,APD07,APD08," +
                "  APD09,APD10,APD11,APD12,APD13,APD14,APD15,APD16," +
                "  APD17,APD18,APD19,APD20,APD21,APD22,APD23,APD24," +
                "  APD25,APD26," +
                "  SP01,SP02,SP03,SP04,SP05,SP06,SP07,SP08," +
                "  SP09,SP10,SP11,SP12,SP13,SP14,SP15,SP16," +
                "  SP17,SP18,SP19,SP20,SP21,SP22,SP23,SP24," +
                "  SP25,SP26,SP27,SP28,SP29,SP30" +
                ") VALUES (" +
                "  @RESULT_ID,@UPDATE_TIME,@REPAIR,@MODEL,@CYCLETIME,@CREATE_DAYTIME," +
                "  @MAT_SERIAL01,@MAT_SERIAL02,@TOTAL_JUDGE,@OP200_TOTAL_JUDGE," +
                "  @APD01,@APD02,@APD03,@APD04,@APD05,@APD06,@APD07,@APD08," +
                "  @APD09,@APD10,@APD11,@APD12,@APD13,@APD14,@APD15,@APD16," +
                "  @APD17,@APD18,@APD19,@APD20,@APD21,@APD22,@APD23,@APD24," +
                "  @APD25,@APD26," +
                "  @SP01,@SP02,@SP03,@SP04,@SP05,@SP06,@SP07,@SP08," +
                "  @SP09,@SP10,@SP11,@SP12,@SP13,@SP14,@SP15,@SP16," +
                "  @SP17,@SP18,@SP19,@SP20,@SP21,@SP22,@SP23,@SP24," +
                "  @SP25,@SP26,@SP27,@SP28,@SP29,@SP30" +
                ")");

            // ── 식별자 ─────────────────────────────────────────────────────
            nqExe.AddParameter("@RESULT_ID",         row.ResultId);
            nqExe.AddParameter("@UPDATE_TIME",        row.UpdateTime);
            nqExe.AddParameter("@REPAIR",             row.Repair);
            nqExe.AddParameter("@MODEL",              row.Model);
            nqExe.AddParameter("@CYCLETIME",          0);
            nqExe.AddParameter("@CREATE_DAYTIME",     now);
            nqExe.AddParameter("@MAT_SERIAL01",       row.MatSerial01);
            nqExe.AddParameter("@MAT_SERIAL02",       row.MatSerial02);
            nqExe.AddParameter("@TOTAL_JUDGE",        row.TotalJudge);
            nqExe.AddParameter("@OP200_TOTAL_JUDGE",  row.TotalJudge);   // INSERT 시점 OP200 판정 스냅샷

            // ── APD01~26 ───────────────────────────────────────────────────
            nqExe.AddParameter("@APD01", row.Apd01); nqExe.AddParameter("@APD02", row.Apd02);
            nqExe.AddParameter("@APD03", row.Apd03); nqExe.AddParameter("@APD04", row.Apd04);
            nqExe.AddParameter("@APD05", row.Apd05); nqExe.AddParameter("@APD06", row.Apd06);
            nqExe.AddParameter("@APD07", row.Apd07); nqExe.AddParameter("@APD08", row.Apd08);
            nqExe.AddParameter("@APD09", row.Apd09); nqExe.AddParameter("@APD10", row.Apd10);
            nqExe.AddParameter("@APD11", row.Apd11); nqExe.AddParameter("@APD12", row.Apd12);
            nqExe.AddParameter("@APD13", row.Apd13); nqExe.AddParameter("@APD14", row.Apd14);
            nqExe.AddParameter("@APD15", row.Apd15); nqExe.AddParameter("@APD16", row.Apd16);
            nqExe.AddParameter("@APD17", row.Apd17); nqExe.AddParameter("@APD18", row.Apd18);
            nqExe.AddParameter("@APD19", row.Apd19); nqExe.AddParameter("@APD20", row.Apd20);
            nqExe.AddParameter("@APD21", row.Apd21); nqExe.AddParameter("@APD22", row.Apd22);
            nqExe.AddParameter("@APD23", row.Apd23); nqExe.AddParameter("@APD24", row.Apd24);
            nqExe.AddParameter("@APD25", row.Apd25); nqExe.AddParameter("@APD26", row.Apd26);

            // ── SP01~30 ────────────────────────────────────────────────────
            nqExe.AddParameter("@SP01", row.Sp01); nqExe.AddParameter("@SP02", row.Sp02);
            nqExe.AddParameter("@SP03", row.Sp03); nqExe.AddParameter("@SP04", row.Sp04);
            nqExe.AddParameter("@SP05", row.Sp05); nqExe.AddParameter("@SP06", row.Sp06);
            nqExe.AddParameter("@SP07", row.Sp07); nqExe.AddParameter("@SP08", row.Sp08);
            nqExe.AddParameter("@SP09", row.Sp09); nqExe.AddParameter("@SP10", row.Sp10);
            nqExe.AddParameter("@SP11", row.Sp11); nqExe.AddParameter("@SP12", row.Sp12);
            nqExe.AddParameter("@SP13", row.Sp13); nqExe.AddParameter("@SP14", row.Sp14);
            nqExe.AddParameter("@SP15", row.Sp15); nqExe.AddParameter("@SP16", row.Sp16);
            nqExe.AddParameter("@SP17", row.Sp17); nqExe.AddParameter("@SP18", row.Sp18);
            nqExe.AddParameter("@SP19", row.Sp19); nqExe.AddParameter("@SP20", row.Sp20);
            nqExe.AddParameter("@SP21", row.Sp21); nqExe.AddParameter("@SP22", row.Sp22);
            nqExe.AddParameter("@SP23", row.Sp23); nqExe.AddParameter("@SP24", row.Sp24);
            nqExe.AddParameter("@SP25", row.Sp25); nqExe.AddParameter("@SP26", row.Sp26);
            nqExe.AddParameter("@SP27", row.Sp27); nqExe.AddParameter("@SP28", row.Sp28);
            nqExe.AddParameter("@SP29", row.Sp29); nqExe.AddParameter("@SP30", row.Sp30);

            nqExe.Execute();
        }

        // ── FindBySerial ────────────────────────────────────────────────────

        /// <summary>
        /// MAT_SERIAL01 또는 MAT_SERIAL02 가 serial 과 일치하는 가장 최근 EMPG 행을 반환한다.
        /// 없으면 null.
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

        // ── UpdateOp200Cols ─────────────────────────────────────────────────

        /// <summary>
        /// EMPG 행의 OP200 컬럼(식별자·APD01~26·SP01~30·TotalJudge)을
        /// EmpgRow 기준으로 UPDATE 한다. WHERE 조건은 RESULT_ID.
        /// </summary>
        public void UpdateOp200Cols(EmpgRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "UPDATE EMPG SET " +
                "UPDATE_TIME = @UPDATE_TIME," +
                "REPAIR = @REPAIR," +
                "MODEL = @MODEL," +
                "MAT_SERIAL01 = @MAT_SERIAL01," +
                "MAT_SERIAL02 = @MAT_SERIAL02," +
                "TOTAL_JUDGE = @TOTAL_JUDGE," +
                "OP200_TOTAL_JUDGE = @OP200_TOTAL_JUDGE," +
                "APD01 = @APD01,APD02 = @APD02,APD03 = @APD03,APD04 = @APD04," +
                "APD05 = @APD05,APD06 = @APD06,APD07 = @APD07,APD08 = @APD08," +
                "APD09 = @APD09,APD10 = @APD10,APD11 = @APD11,APD12 = @APD12," +
                "APD13 = @APD13,APD14 = @APD14,APD15 = @APD15,APD16 = @APD16," +
                "APD17 = @APD17,APD18 = @APD18,APD19 = @APD19,APD20 = @APD20," +
                "APD21 = @APD21,APD22 = @APD22,APD23 = @APD23,APD24 = @APD24," +
                "APD25 = @APD25,APD26 = @APD26," +
                "SP01 = @SP01,SP02 = @SP02,SP03 = @SP03,SP04 = @SP04," +
                "SP05 = @SP05,SP06 = @SP06,SP07 = @SP07,SP08 = @SP08," +
                "SP09 = @SP09,SP10 = @SP10,SP11 = @SP11,SP12 = @SP12," +
                "SP13 = @SP13,SP14 = @SP14,SP15 = @SP15,SP16 = @SP16," +
                "SP17 = @SP17,SP18 = @SP18,SP19 = @SP19,SP20 = @SP20," +
                "SP21 = @SP21,SP22 = @SP22,SP23 = @SP23,SP24 = @SP24," +
                "SP25 = @SP25,SP26 = @SP26,SP27 = @SP27,SP28 = @SP28," +
                "SP29 = @SP29,SP30 = @SP30 " +
                "WHERE RESULT_ID = @RESULT_ID");

            nqExe.AddParameter("@UPDATE_TIME", row.UpdateTime);
            nqExe.AddParameter("@REPAIR", row.Repair);
            nqExe.AddParameter("@MODEL", row.Model);
            nqExe.AddParameter("@MAT_SERIAL01", row.MatSerial01);
            nqExe.AddParameter("@MAT_SERIAL02", row.MatSerial02);
            nqExe.AddParameter("@TOTAL_JUDGE", row.TotalJudge);
            nqExe.AddParameter("@OP200_TOTAL_JUDGE", row.TotalJudge);

            nqExe.AddParameter("@APD01", row.Apd01); nqExe.AddParameter("@APD02", row.Apd02);
            nqExe.AddParameter("@APD03", row.Apd03); nqExe.AddParameter("@APD04", row.Apd04);
            nqExe.AddParameter("@APD05", row.Apd05); nqExe.AddParameter("@APD06", row.Apd06);
            nqExe.AddParameter("@APD07", row.Apd07); nqExe.AddParameter("@APD08", row.Apd08);
            nqExe.AddParameter("@APD09", row.Apd09); nqExe.AddParameter("@APD10", row.Apd10);
            nqExe.AddParameter("@APD11", row.Apd11); nqExe.AddParameter("@APD12", row.Apd12);
            nqExe.AddParameter("@APD13", row.Apd13); nqExe.AddParameter("@APD14", row.Apd14);
            nqExe.AddParameter("@APD15", row.Apd15); nqExe.AddParameter("@APD16", row.Apd16);
            nqExe.AddParameter("@APD17", row.Apd17); nqExe.AddParameter("@APD18", row.Apd18);
            nqExe.AddParameter("@APD19", row.Apd19); nqExe.AddParameter("@APD20", row.Apd20);
            nqExe.AddParameter("@APD21", row.Apd21); nqExe.AddParameter("@APD22", row.Apd22);
            nqExe.AddParameter("@APD23", row.Apd23); nqExe.AddParameter("@APD24", row.Apd24);
            nqExe.AddParameter("@APD25", row.Apd25); nqExe.AddParameter("@APD26", row.Apd26);

            nqExe.AddParameter("@SP01", row.Sp01); nqExe.AddParameter("@SP02", row.Sp02);
            nqExe.AddParameter("@SP03", row.Sp03); nqExe.AddParameter("@SP04", row.Sp04);
            nqExe.AddParameter("@SP05", row.Sp05); nqExe.AddParameter("@SP06", row.Sp06);
            nqExe.AddParameter("@SP07", row.Sp07); nqExe.AddParameter("@SP08", row.Sp08);
            nqExe.AddParameter("@SP09", row.Sp09); nqExe.AddParameter("@SP10", row.Sp10);
            nqExe.AddParameter("@SP11", row.Sp11); nqExe.AddParameter("@SP12", row.Sp12);
            nqExe.AddParameter("@SP13", row.Sp13); nqExe.AddParameter("@SP14", row.Sp14);
            nqExe.AddParameter("@SP15", row.Sp15); nqExe.AddParameter("@SP16", row.Sp16);
            nqExe.AddParameter("@SP17", row.Sp17); nqExe.AddParameter("@SP18", row.Sp18);
            nqExe.AddParameter("@SP19", row.Sp19); nqExe.AddParameter("@SP20", row.Sp20);
            nqExe.AddParameter("@SP21", row.Sp21); nqExe.AddParameter("@SP22", row.Sp22);
            nqExe.AddParameter("@SP23", row.Sp23); nqExe.AddParameter("@SP24", row.Sp24);
            nqExe.AddParameter("@SP25", row.Sp25); nqExe.AddParameter("@SP26", row.Sp26);
            nqExe.AddParameter("@SP27", row.Sp27); nqExe.AddParameter("@SP28", row.Sp28);
            nqExe.AddParameter("@SP29", row.Sp29); nqExe.AddParameter("@SP30", row.Sp30);

            nqExe.AddParameter("@RESULT_ID", row.ResultId);

            nqExe.Execute();
        }

        // ── 내부 헬퍼 ───────────────────────────────────────────────────────

        private static string SqlQuotedString(string? value) =>
            "'" + (value ?? string.Empty).Replace("'", "''") + "'";

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
