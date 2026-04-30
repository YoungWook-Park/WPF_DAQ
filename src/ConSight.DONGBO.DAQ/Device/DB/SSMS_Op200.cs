// Phase C: SSMS_Op200 — OP200 공정 완료 시 EMPG 행 신규 INSERT
//
// EmpgRow (APD01~26, SP01~30) 를 받아 DB에 한 행을 삽입한다.
// APD27~44, SP31~50 은 서브공정 완료 후 SSMS_SubProcess.UpdateSubCols() 로 채워진다.

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
    }
}
