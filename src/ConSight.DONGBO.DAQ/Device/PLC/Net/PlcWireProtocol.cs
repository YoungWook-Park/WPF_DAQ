using System.IO;

namespace ConSight.DAQ.Device.PLC.Net
{
    // 자체 바이너리 프로토콜 (빅엔디언)
    // Request:  [op:1B 'R'/'W'] [addrLen:1B] [addr:N B ASCII] [wordCount:2B BE]
    //           + (op='W') [payload: wordCount×2B BE shorts]
    // Response: [op:1B] [status:1B 0=OK/1=ERR] [wordCount:2B BE]
    //           + (op='R' && status=0) [payload: wordCount×2B BE shorts]
    internal static class PlcWireProtocol
    {
        internal static byte[] BuildReadRequest(string addr, int wordCount)
        {
            var addrBytes = System.Text.Encoding.ASCII.GetBytes(addr);
            var buf = new byte[4 + addrBytes.Length];
            buf[0] = (byte)'R';
            buf[1] = (byte)addrBytes.Length;
            addrBytes.CopyTo(buf, 2);
            buf[2 + addrBytes.Length] = (byte)(wordCount >> 8);
            buf[3 + addrBytes.Length] = (byte)(wordCount & 0xFF);
            return buf;
        }

        internal static byte[] BuildWriteRequest(string addr, short[] data)
        {
            var addrBytes = System.Text.Encoding.ASCII.GetBytes(addr);
            int headerLen = 4 + addrBytes.Length;
            var buf = new byte[headerLen + data.Length * 2];
            buf[0] = (byte)'W';
            buf[1] = (byte)addrBytes.Length;
            addrBytes.CopyTo(buf, 2);
            buf[2 + addrBytes.Length] = (byte)(data.Length >> 8);
            buf[3 + addrBytes.Length] = (byte)(data.Length & 0xFF);
            for (int i = 0; i < data.Length; i++)
            {
                buf[headerLen + i * 2]     = (byte)(data[i] >> 8);
                buf[headerLen + i * 2 + 1] = (byte)(data[i] & 0xFF);
            }
            return buf;
        }

        // 'R' 성공 시 words에 payload 반환. 'W' 성공 시 words는 빈 배열.
        internal static bool TryReadResponse(Stream stream, byte expectedOp, out short[] words)
        {
            words = Array.Empty<short>();
            var header = new byte[4];
            if (!ReadExact(stream, header, 4)) return false;

            byte op     = header[0];
            byte status = header[1];
            int wordCount = (header[2] << 8) | header[3];

            if (op != expectedOp || status != 0) return false;

            if (op == (byte)'R' && wordCount > 0)
            {
                var payload = new byte[wordCount * 2];
                if (!ReadExact(stream, payload, payload.Length)) return false;
                words = new short[wordCount];
                for (int i = 0; i < wordCount; i++)
                    words[i] = (short)((payload[i * 2] << 8) | payload[i * 2 + 1]);
            }
            return true;
        }

        private static bool ReadExact(Stream stream, byte[] buf, int count)
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
