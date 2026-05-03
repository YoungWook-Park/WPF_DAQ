using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace Bi.ConSight.SqlAgent
{
    /// <summary>
    /// SELECT 전용 실행기. 기존 Bi.SqlServerAgent.QueryExecution과 동일한 API 형태를 유지하되
    /// AddParameter()로 파라미터화 쿼리를 지원한다.
    /// </summary>
    public class QueryExecution
    {
        private readonly string _connectionString;
        private readonly List<(StringBuilder Query, List<SqlParameter> Parameters)> _queryList = new();

        public QueryExecution(string connectionString)
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
            // DateTime을 object로 전달하면 SqlDbType.DateTime으로 추론됨.
            // DB 컬럼이 datetime2인 경우 타입 불일치로 암묵적 변환 → 인덱스 미사용.
            // DateTime 값은 명시적으로 DateTime2 타입으로 지정한다.
            SqlParameter param = value is DateTime dt
                ? new SqlParameter(name, SqlDbType.DateTime2) { Value = dt }
                : new SqlParameter(name, value ?? DBNull.Value);
            _queryList[^1].Parameters.Add(param);
        }

        /// <summary>
        /// SqlDataReader로 스트리밍 읽기: DataSet/DataRow 중간 버퍼 없이 mapper로 직접 변환.
        /// DataSet.Fill 대비 메모리 사용 약 50% 감소 (DataRow 이중 버퍼·row state 제거).
        /// </summary>
        public List<T> ExecuteReader<T>(Func<IDataRecord, T> mapper, int timeoutSeconds = 300)
        {
            var result = new List<T>();
            using var conn = SqlConnectionFactory.CreateConnection(_connectionString);
            conn.Open();
            foreach (var (query, parameters) in _queryList)
            {
                using var cmd = new SqlCommand(query.ToString(), conn);
                cmd.CommandTimeout = timeoutSeconds;
                if (parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(mapper(reader));
            }
            return result;
        }

        /// <summary>AppendQuery된 모든 쿼리를 순서대로 실행해 DataSet으로 반환한다.</summary>
        public DataSet Execute()
        {
            var ds = new DataSet();
            using var conn = SqlConnectionFactory.CreateConnection(_connectionString);
            conn.Open();

            foreach (var (query, parameters) in _queryList)
            {
                using var cmd = new SqlCommand(query.ToString(), conn);
                if (parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
            }

            return ds;
        }
    }
}
