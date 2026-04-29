using Microsoft.Data.SqlClient;

namespace Bi.ConSight.SqlAgent
{
    public static class SqlConnectionFactory
    {
        public static SqlConnection CreateConnection(string connectionString)
            => new SqlConnection(connectionString);
    }
}
