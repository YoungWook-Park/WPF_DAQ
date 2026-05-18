using ConSight.DONGBO.PlcSimulator.Logic;
using ConSight.DONGBO.PlcSimulator.Memory;
using Xunit;

namespace ConSight.DONGBO.DAQ.Tests;

[Trait("Category", "Unit")]
public class HandshakeTests
{
    // ── PlcMemory ────────────────────────────────────────────────────────────

    [Fact]
    public void PlcMemory_Read_ReturnsZeroArray_WhenAddrNotPreInitialized()
    {
        var memory = new PlcMemory();
        var readData = memory.Read("D9999", 5);

        Assert.Equal(5, readData.Length);
        Assert.All(readData, value => Assert.Equal(0, value));
    }

    [Fact]
    public void PlcMemory_Written_FiresAfterWrite()
    {
        var memory = new PlcMemory();
        string? capturedAddress = null;
        short[]? capturedData   = null;

        memory.Written += (address, data) => { capturedAddress = address; capturedData = data; };
        memory.Write("D2000", new short[] { 1, 2, 3 });

        Assert.Equal("D2000", capturedAddress);
        Assert.Equal(new short[] { 1, 2, 3 }, capturedData);
    }

    [Fact]
    public void PlcMemory_Write_StoresClone_NotOriginalReference()
    {
        var memory   = new PlcMemory();
        var original = new short[] { 7, 8 };
        memory.Write("D2000", original);

        original[0] = 99; // 원본 수정

        var readBack = memory.Read("D2000", 2);
        Assert.Equal(7, readBack[0]); // 클론이어야 함
    }

    // ── SimulatorSignalHandler ───────────────────────────────────────────────

    [Fact]
    public void SignalHandler_ResetsBackupStart_OnOp200Complete()
    {
        var memory      = new PlcMemory();
        var processData = new short[100];
        processData[0]  = 1; // BackUp_Start
        memory.Write("D2000", processData);

        _ = new SimulatorSignalHandler(memory);

        // DAQ가 PC_Complete_Flag(D2001[1]) = 1 로 쓰면
        memory.Write("D2001", new short[] { 0, 1, 0 });

        Assert.Equal(0, memory.Read("D2000", 1)[0]); // BackUp_Start 리셋
        Assert.Equal(0, memory.Read("D2001", 3)[1]); // PC_Complete_Flag 리셋
    }

    [Fact]
    public void SignalHandler_ResetsBackupStart_OnOp210Complete()
    {
        var memory      = new PlcMemory();
        var processData = new short[70];
        processData[0]  = 1; // BackUp_Start
        memory.Write("D2200", processData);

        _ = new SimulatorSignalHandler(memory);

        memory.Write("D2201", new short[] { 1 });

        Assert.Equal(0, memory.Read("D2200", 1)[0]);
        Assert.Equal(0, memory.Read("D2201", 1)[0]);
    }

    [Fact]
    public void SignalHandler_NoAction_WhenPcCompleteIsZero()
    {
        var memory      = new PlcMemory();
        var processData = new short[100];
        processData[0]  = 1; // BackUp_Start 유지
        memory.Write("D2000", processData);

        _ = new SimulatorSignalHandler(memory);

        // PC_Complete_Flag = 0 → 핸들러 아무 동작 없음
        memory.Write("D2001", new short[] { 0, 0, 0 });

        Assert.Equal(1, memory.Read("D2000", 1)[0]); // BackUp_Start 여전히 1
    }

    [Fact]
    public void SignalHandler_NoInfiniteRecursion_OnReset()
    {
        var memory = new PlcMemory();
        _ = new SimulatorSignalHandler(memory);

        var exception = Record.Exception(() =>
            memory.Write("D2001", new short[] { 0, 1, 0 }));

        Assert.Null(exception);
    }
}
