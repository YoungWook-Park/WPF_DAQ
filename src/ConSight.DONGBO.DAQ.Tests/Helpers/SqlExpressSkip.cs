using Microsoft.Data.SqlClient;

namespace ConSight.DONGBO.DAQ.Tests.Helpers;

internal static class SqlExpressSkip
{
    internal const string ConnectionString =
        "Server=.\\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;" +
        "TrustServerCertificate=True;Connect Timeout=3";

    // null = 사용 가능 / 문자열 = skip 사유
    // OP200 파이프라인에 필요한 테이블·컬럼이 존재하는지 확인
    internal static string? GetSkipReason()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            // EMPG 핵심 컬럼 + STS_MODEL_TB 모두 확인
            using var cmd = new SqlCommand(
                "SELECT TOP 0 RESULT_ID, MAT_SERIAL01, MAT_SERIAL02, TOTAL_JUDGE FROM EMPG;" +
                "SELECT TOP 0 MODEL FROM STS_MODEL_TB", conn);
            using var reader = cmd.ExecuteReader();
            do { } while (reader.NextResult());

            return null;
        }
        catch
        {
            return "SQLEXPRESS 미가동 또는 필요 테이블/컬럼 없음 — Integration 테스트 건너뜀";
        }
    }
}
