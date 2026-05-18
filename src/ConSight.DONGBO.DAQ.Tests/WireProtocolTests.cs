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
        var requestBytes  = PlcWireProtocol.BuildReadRequest("D2000", 100);
        int addressLength = "D2000".Length;

        Assert.Equal((byte)'R',     requestBytes[0]);
        Assert.Equal(addressLength, requestBytes[1]);
        Assert.Equal("D2000",       Encoding.ASCII.GetString(requestBytes, 2, addressLength));
        Assert.Equal(0,             requestBytes[2 + addressLength]); // 100 >> 8
        Assert.Equal(100,           requestBytes[3 + addressLength]); // 100 & 0xFF
        Assert.Equal(4 + addressLength, requestBytes.Length);
    }

    [Fact]
    public void BuildWriteRequest_EncodesPayloadBigEndian()
    {
        var writeData     = new short[] { 0x0102, -1 };
        var requestBytes  = PlcWireProtocol.BuildWriteRequest("D2001", writeData);
        int addressLength = "D2001".Length;
        int headerLength  = 4 + addressLength;

        Assert.Equal((byte)'W', requestBytes[0]);
        Assert.Equal(addressLength, requestBytes[1]);
        Assert.Equal(0, requestBytes[2 + addressLength]); // wordCount(2) >> 8
        Assert.Equal(2, requestBytes[3 + addressLength]); // wordCount(2) & 0xFF

        // word[0] = 0x0102 → [0x01, 0x02]
        Assert.Equal(0x01, requestBytes[headerLength]);
        Assert.Equal(0x02, requestBytes[headerLength + 1]);

        // word[1] = -1 = 0xFFFF → [0xFF, 0xFF]
        Assert.Equal(0xFF, (byte)requestBytes[headerLength + 2]);
        Assert.Equal(0xFF, (byte)requestBytes[headerLength + 3]);
    }

    [Fact]
    public void TryReadResponse_ReturnsFalse_OnUnexpectedOp()
    {
        using var responseStream = new MemoryStream(new byte[] { (byte)'W', 0, 0, 0 });
        var result = PlcWireProtocol.TryReadResponse(responseStream, (byte)'R', out _);

        Assert.False(result);
    }

    [Fact]
    public void TryReadResponse_ReturnsFalse_OnErrorStatus()
    {
        using var responseStream = new MemoryStream(new byte[] { (byte)'R', 1, 0, 3 });
        var result = PlcWireProtocol.TryReadResponse(responseStream, (byte)'R', out _);

        Assert.False(result);
    }

    [Fact]
    public void TryReadResponse_DecodesWords_OnSuccessfulReadResponse()
    {
        var responseBytes = new byte[]
        {
            (byte)'R', 0,   // op='R', status=OK
            0, 3,           // wordCount=3
            0, 1,           // word[0] = 1
            0, 2,           // word[1] = 2
            0, 3,           // word[2] = 3
        };
        using var responseStream = new MemoryStream(responseBytes);
        var success = PlcWireProtocol.TryReadResponse(responseStream, (byte)'R', out var words);

        Assert.True(success);
        Assert.Equal(3, words.Length);
        Assert.Equal((short)1, words[0]);
        Assert.Equal((short)2, words[1]);
        Assert.Equal((short)3, words[2]);
    }

    [Fact]
    public void TryReadResponse_ReturnsTrue_OnSuccessfulWriteResponse()
    {
        using var responseStream = new MemoryStream(new byte[] { (byte)'W', 0, 0, 0 });
        var result = PlcWireProtocol.TryReadResponse(responseStream, (byte)'W', out var words);

        Assert.True(result);
        Assert.Empty(words);
    }
}
