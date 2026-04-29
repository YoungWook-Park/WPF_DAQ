// AS-IS: Bi.SqlServerAgent + 문자열 연결 쿼리
// TO-BE: Bi.ConSight.SqlAgent + SqlParameter (SQL Injection 방어)
using Bi.ConSight.SqlAgent;
using ConSight.DAQ.Data;

namespace ConSight.DAQ.Device
{
    public class SSMS_Op200
    {
        private readonly string _connectionString;

        public SSMS_Op200(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool Insert(OP200_Process_DTO processData)
        {
            if (processData == null) throw new ArgumentNullException(nameof(processData));

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "INSERT INTO EMPG " +
                "  (RESULT_ID, UPDATE_TIME, REPAIR, MODEL, CYCLETIME, CREATE_DAYTIME, " +
                "   MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE) " +
                "VALUES " +
                "  (@RESULT_ID, @UPDATE_TIME, @REPAIR, @MODEL, @CYCLETIME, @CREATE_DAYTIME, " +
                "   @MAT_SERIAL01, @MAT_SERIAL02, @TOTAL_JUDGE)");

            nqExe.AddParameter("@RESULT_ID",      $"R-{now}");
            nqExe.AddParameter("@UPDATE_TIME",     DateTime.Now);   // datetime 타입 (Step 1 변경 적용)
            nqExe.AddParameter("@REPAIR",          processData.PLC_Repair);
            nqExe.AddParameter("@MODEL",           processData.PLC_Model_Name);
            nqExe.AddParameter("@CYCLETIME",       0);
            nqExe.AddParameter("@CREATE_DAYTIME",  now);
            nqExe.AddParameter("@MAT_SERIAL01",    processData.PLC_Shaft_Serial_No);
            nqExe.AddParameter("@MAT_SERIAL02",    processData.PLC_Gear_Serial_No);
            nqExe.AddParameter("@TOTAL_JUDGE",     processData.PLC_Total_Judgement);
            nqExe.Execute();
            return true;
        }
    }
}
