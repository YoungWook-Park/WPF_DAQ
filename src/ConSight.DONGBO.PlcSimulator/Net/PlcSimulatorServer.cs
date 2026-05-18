using System.Net;
using System.Net.Sockets;
using System.Text;
using ConSight.DONGBO.PlcSimulator.Memory;

namespace ConSight.DONGBO.PlcSimulator.Net
{
    internal sealed class PlcSimulatorServer
    {
        private readonly TcpListener _listener;
        private readonly PlcMemory _memory;
        private readonly CancellationTokenSource _cts = new();
        private TcpClient? _activeClient;

        internal PlcSimulatorServer(PlcMemory memory, int port)
        {
            _memory = memory;
            _listener = new TcpListener(IPAddress.Loopback, port);
        }

        internal void Start()
        {
            _listener.Start();
            Task.Run(AcceptLoop);
        }

        internal void Stop()
        {
            _cts.Cancel();
            _activeClient?.Dispose();
            try { _listener.Stop(); } catch { }
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _activeClient?.Dispose(); // 기존 클라이언트 명시적 해제 (단일 클라이언트 유지)
                    _activeClient = client;
                    _ = Task.Run(() => ClientLoop(client));
                }
                catch (OperationCanceledException) { break; }
                catch { break; }
            }
        }

        private void ClientLoop(TcpClient client)
        {
            using var _ = client;
            try
            {
                var stream = client.GetStream();
                while (!_cts.IsCancellationRequested)
                {
                    // [op:1, addrLen:1]
                    var header = new byte[2];
                    if (!ReadExact(stream, header, 2)) break;
                    byte op     = header[0];
                    int addrLen = header[1];

                    // [addr:addrLen bytes ASCII]
                    var addrBuf = new byte[addrLen];
                    if (!ReadExact(stream, addrBuf, addrLen)) break;
                    string addr = Encoding.ASCII.GetString(addrBuf);

                    // [wordCount:2 BE]
                    var wcBuf = new byte[2];
                    if (!ReadExact(stream, wcBuf, 2)) break;
                    int wordCount = (wcBuf[0] << 8) | wcBuf[1];

                    if (op == (byte)'R')
                    {
                        var words = _memory.Read(addr, wordCount);
                        SendResponse(stream, (byte)'R', words);
                    }
                    else if (op == (byte)'W')
                    {
                        var payload = new byte[wordCount * 2];
                        if (!ReadExact(stream, payload, payload.Length)) break;
                        var words = new short[wordCount];
                        for (int i = 0; i < wordCount; i++)
                            words[i] = (short)((payload[i * 2] << 8) | payload[i * 2 + 1]);
                        _memory.Write(addr, words);
                        SendResponse(stream, (byte)'W', Array.Empty<short>());
                    }
                }
            }
            catch { /* 클라이언트 연결 종료 또는 서버 중단 */ }
        }

        private static void SendResponse(NetworkStream stream, byte op, short[] words)
        {
            var buf = new byte[4 + words.Length * 2];
            buf[0] = op;
            buf[1] = 0; // status: OK
            buf[2] = (byte)(words.Length >> 8);
            buf[3] = (byte)(words.Length & 0xFF);
            for (int i = 0; i < words.Length; i++)
            {
                buf[4 + i * 2]     = (byte)(words[i] >> 8);
                buf[4 + i * 2 + 1] = (byte)(words[i] & 0xFF);
            }
            stream.Write(buf, 0, buf.Length);
        }

        private static bool ReadExact(NetworkStream stream, byte[] buf, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int n = stream.Read(buf, offset, count - offset);
                if (n == 0) return false;
                offset += n;
            }
            return true;
        }
    }
}
