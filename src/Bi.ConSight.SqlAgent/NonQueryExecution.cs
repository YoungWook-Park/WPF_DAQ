using Microsoft.Data.SqlClient;
using System.Text;

namespace Bi.ConSight.SqlAgent
{
    /// <summary>
    /// INSERT / UPDATE / DELETE 전용 실행기. 기존 Bi.SqlServerAgent.NonQueryExecution과 동일한
    /// API 형태를 유지하되 AddParameter()로 파라미터화 쿼리를 지원한다.
    /// AppendQuery를 여러 번 호출하면 단일 트랜잭션 안에서 순서대로 실행된다.
    /// </summary>
    public class NonQueryExecution
    {
        private readonly string _connectionString;
        private readonly List<(StringBuilder Query, List<SqlParameter> Parameters)> _queryList = new();

        public NonQueryExecution(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>로깅용 — AppendQuery로 추가된 쿼리 목록</summary>
        public IEnumerable<StringBuilder> QueryCollection => _queryList.Select(x => x.Query);

        public void AppendQuery(string sql)
        {
            _queryList.Add((new StringBuilder(sql), new List<SqlParameter>()));
        }

        /// <summary>
        /// 마지막으로 AppendQuery한 쿼리에 파라미터를 추가한다.
        /// value가 null이면 DBNull.Value로 변환된다.
        /// </summary>
        public void AddParameter(string name, object? value)
        {
            if (_queryList.Count == 0)
                throw new InvalidOperationException("AddParameter 전에 AppendQuery를 호출하세요.");
            _queryList[^1].Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));
        }

        /// <summary>
        /// AppendQuery된 모든 쿼리를 단일 트랜잭션 안에서 순서대로 실행한다.
        /// 하나라도 실패하면 전체 롤백된다.
        /// </summary>
        public void Execute()
        {
            using var conn = SqlConnectionFactory.CreateConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                foreach (var (query, parameters) in _queryList)
                {
                    using var cmd = new SqlCommand(query.ToString(), conn, tran);
                    if (parameters.Count > 0)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}
