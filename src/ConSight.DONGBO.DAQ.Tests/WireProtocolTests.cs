using System.IO;
using System.Text;
using ConSight.DAQ.Device.PLC.Net;
using Xunit;

namespace ConSight.DONGBO.DAQ.Tests;

[Trait("Category", "Unit")]
public class WireProtocolTests
{
    // Request 포맷: [op:1B] [addrLen:1B] [addr:N ASCII] [wordCount:2B BE]
    // + ('W') [payload: wordCount×2B BE]

    [Fact]
    public void BuildReadRequest_EncodesAddrAndWordCount()
    {
        var buf = PlcWireProtocol.BuildReadRequest("D2000", 100);

        int addrLen = "D2000".Length; // 5
        Assert.Equal((byte)'R',  buf[0]);
        Assert.Equal(addrLen,    buf[1]);
        Assert.Equal("D2000",    Encoding.ASCII.GetString(buf, 2, addrLen));
        Assert.Equal(0,          buf[2 + addrLen]); // 100 >> 8
        Assert.Equal(100,        buf[3 + addrLen]); // 100 & 0xFF
        Assert.Equal(4 + addrLen, buf.Length);
    }

    [Fact]
    public void BuildWriteRequest_EncodesPayloadBigEndian()
    {
        var data = new short[] { 0x0102, -1 };
        var buf = PlcWireProtocol.BuildWriteRequest("D2001", data);

        int addrLen   = "D2001".Length; // 5
        int headerLen = 4 + addrLen;

        Assert.Equal((byte)'W', buf[0]);
        Assert.Equal(addrLen,   buf[1]);
        Assert.Equal(0,         buf[2 + addrLen]); // wordCount(2) >> 8
        Assert.Equal(2,         buf[3 + addrLen]); // wordCount(2) & 0xFF

        // word[0] = 0x0102 → [0x01, 0x02]
        Assert.Equal(0x01, buf[headerLen]);
        Assert.Equal(0x02, buf[headerLen + 1]);

        // word[1] = -1 = 0xFFFF → [0xFF, 0xFF]
        Assert.Equal(0xFF, (byte)buf[headerLen + 2]);
        Assert.Equal(0xFF, (byte)buf[headerLen + 3]);
    }

    [Fact]
    public void TryReadResponse_ReturnsFalse_OnUnexpectedOp()
    {
        // 서버가 'W' 응답을 보냈지만 클라이언트는 'R' 기대
        using var ms = new MemoryStream(new byte[] { (byte)'W', 0, 0, 0 });
        var result = PlcWireProtocol.TryReadResponse(ms, (byte)'R', out var words);

        Assert.False(result);
    }

    [Fact]
    public void TryReadResponse_ReturnsFalse_OnErrorStatus()
    {
        // status=1(ERR) 응답
        using var ms = new MemoryStream(new byte[] { (byte)'R', 1, 0, 3 });
        var result = PlcWireProtocol.TryReadResponse(ms, (byte)'R', out var words);

        Assert.False(result);
    }

    [Fact]
    public void TryReadResponse_DecodesWords_OnSuccessfulReadResponse()
    {
        // 'R' 성공 응답: status=0, wordCount=3, data=[1,2,3]
        var responseBytes = new byte[]
        {
            (byte)'R', 0,   // op='R', status=OK
            0, 3,           // wordCount=3
            0, 1,           // word[0] = 1
            0, 2,           // word[1] = 2
            0, 3,           // word[2] = 3
        };
        using var ms = new MemoryStream(responseBytes);
        var success = PlcWireProtocol.TryReadResponse(ms, (byte)'R', out var words);

        Assert.True(success);
        Assert.Equal(3, words.Length);
        Assert.Equal((short)1, words[0]);
        Assert.Equal((short)2, words[1]);
        Assert.Equal((short)3, words[2]);
    }

    [Fact]
    public void TryReadResponse_ReturnsTrue_OnSuccessfulWriteResponse()
    {
        // 'W' 성공 응답: status=0, wordCount=0, payload 없음
        using var ms = new MemoryStream(new byte[] { (byte)'W', 0, 0, 0 });
        var result = PlcWireProtocol.TryReadResponse(ms, (byte)'W', out var words);

        Assert.True(result);
        Assert.Empty(words);
    }
}
