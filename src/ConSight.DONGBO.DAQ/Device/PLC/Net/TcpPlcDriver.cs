using System.Net.Sockets;

namespace ConSight.DAQ.Device.PLC.Net
{
    /// <summary>
    /// TCP 소켓 기반 IPlcDriver 구현체. PlcSimulator(localhost:5000)에 연결.
    /// 5회 연속 실패 시 5초 backoff — backoff 중에는 소켓 I/O 없이 즉시 false 반환.
    /// </summary>
    public sealed class TcpPlcDriver : IPlcDriver
    {
        private readonly string _host;
        private readonly int _port;
        private readonly object _lock = new();

        private TcpClient? _client;
        private NetworkStream? _stream;
        private int _consecutiveFailures;
        private DateTime _backoffUntil = DateTime.MinValue;

        private const int BackoffThreshold = 5;
        private static readonly TimeSpan BackoffDuration = TimeSpan.FromSeconds(5);

        public TcpPlcDriver(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public bool IsConnected => _client?.Connected ?? false;

        public bool ReadWords(string deviceAddress, int wordCount, out short[] data)
        {
            data = new short[wordCount];
            lock (_lock)
            {
                if (IsInBackoff()) return false;
                if (!EnsureConnected()) return RecordFailure();

                try
                {
                    var req = PlcWireProtocol.BuildReadRequest(deviceAddress, wordCount);
                    _stream!.Write(req, 0, req.Length);
                    bool ok = PlcWireProtocol.TryReadResponse(_stream, (byte)'R', out var words);
                    if (ok) { data = words; return ResetFailures(); }
                    return RecordFailure();
                }
                catch
                {
                    CloseConnection();
                    return RecordFailure();
                }
            }
        }

        public bool WriteWords(string deviceAddress, short[] data)
        {
            lock (_lock)
            {
                if (IsInBackoff()) return false;
                if (!EnsureConnected()) return RecordFailure();

                try
                {
                    var req = PlcWireProtocol.BuildWriteRequest(deviceAddress, data);
                    _stream!.Write(req, 0, req.Length);
                    bool ok = PlcWireProtocol.TryReadResponse(_stream, (byte)'W', out _);
                    if (ok) return ResetFailures();
                    return RecordFailure();
                }
                catch
                {
                    CloseConnection();
                    return RecordFailure();
                }
            }
        }

        private bool IsInBackoff() => DateTime.UtcNow < _backoffUntil;

        private bool EnsureConnected()
        {
            if (_client?.Connected == true) return true;
            CloseConnection();
            try
            {
                _client = new TcpClient();
                _client.Connect(_host, _port);
                _stream = _client.GetStream();
                return true;
            }
            catch
            {
                CloseConnection();
                return false;
            }
        }

        // 성공: 카운터 리셋 후 true 반환
        private bool ResetFailures()
        {
            _consecutiveFailures = 0;
            return true;
        }

        // 실패: 카운터 증가, 5회 달성 시 backoff 진입 후 false 반환
        private bool RecordFailure()
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= BackoffThreshold)
                _backoffUntil = DateTime.UtcNow + BackoffDuration;
            return false;
        }

        internal void CloseConnection()
        {
            _stream?.Dispose();
            _stream = null;
            _client?.Dispose();
            _client = null;
        }
    }
}
