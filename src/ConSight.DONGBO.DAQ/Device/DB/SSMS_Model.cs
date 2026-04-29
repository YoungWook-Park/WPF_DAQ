// AS-IS: Bi.SqlServerAgent + 문자열 연결 쿼리
// TO-BE: Bi.ConSight.SqlAgent + SqlParameter (SQL Injection 방어)
using Bi.ConSight.SqlAgent;
using ConSight.DAQ.Data;
using System.Data;
using System.Text;

namespace ConSight.DAQ.Device
{
    public sealed class SSMS_Model
    {
        private readonly string _connectionString;

        public SSMS_Model(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ModelProduction? GetByModel(string model)
        {
            var qExe = new QueryExecution(_connectionString);
            qExe.AppendQuery("SELECT * FROM STS_MODEL_TB WHERE MODEL = @MODEL");
            qExe.AddParameter("@MODEL", model);

            DataSet ds = qExe.Execute();
            if (ds.Tables[0].Rows.Count == 0) return null;

            var row = ds.Tables[0].Rows[0];
            return new ModelProduction(
                row["MODEL"].ToString()!,
                Convert.ToDouble(row["PRODUCTION_QTY"]),
                Convert.ToDouble(row["FINISHED_QTY"]),
                Convert.ToDouble(row["DEFECTIVE_QTY"]));
        }

        public void Insert(ModelProduction model)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "INSERT INTO STS_MODEL_TB " +
                "  (MODEL, MODEL_DESC, PRODUCTION_QTY, FINISHED_QTY, DEFECTIVE_QTY, YIELD, CREATED_TIME, UPDATED_TIME) " +
                "VALUES " +
                "  (@MODEL, @DESC, @PROD, @FIN, @DEF, @YIELD, @CREATED, @UPDATED)");
            nqExe.AddParameter("@MODEL",   model.Model);
            nqExe.AddParameter("@DESC",    " - ");
            nqExe.AddParameter("@PROD",    model.ProductionQty);
            nqExe.AddParameter("@FIN",     model.FinishedQty);
            nqExe.AddParameter("@DEF",     model.DefectiveQty);
            nqExe.AddParameter("@YIELD",   model.Yield.ToString("0.00"));
            nqExe.AddParameter("@CREATED", now);
            nqExe.AddParameter("@UPDATED", now);
            nqExe.Execute();
        }

        public void Update(ModelProduction model)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var nqExe = new NonQueryExecution(_connectionString);
            nqExe.AppendQuery(
                "UPDATE STS_MODEL_TB SET " +
                "  PRODUCTION_QTY = @PROD, FINISHED_QTY = @FIN, " +
                "  DEFECTIVE_QTY = @DEF, YIELD = @YIELD, UPDATED_TIME = @UPDATED " +
                "WHERE MODEL = @MODEL");
            nqExe.AddParameter("@PROD",    model.ProductionQty);
            nqExe.AddParameter("@FIN",     model.FinishedQty);
            nqExe.AddParameter("@DEF",     model.DefectiveQty);
            nqExe.AddParameter("@YIELD",   model.Yield.ToString("0.00"));
            nqExe.AddParameter("@UPDATED", now);
            nqExe.AddParameter("@MODEL",   model.Model);
            nqExe.Execute();
        }
    }
}
