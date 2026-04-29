// Bi.nsExpException 대체 스텁 — 원본 API와 동일한 static 헬퍼 제공
namespace Bi.nsExpException
{
    public class ExpException : Exception
    {
        public string? Location { get; }
        public string? FunctionName { get; }

        public ExpException(string location, string function, string message)
            : base($"[{location}][{function}] {message}")
        {
            Location = location;
            FunctionName = function;
        }

        public ExpException(string message) : base(message) { }

        public static void RaiseExpException(ExpException ex) => throw ex;

        public static void RaiseException(Exception ex)
            => throw new ExpException(ex.Message) { Source = ex.Source };
    }
}
