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
        var mem = new PlcMemory();
        // D9999는 생성자에서 초기화되지 않은 주소
        var data = mem.Read("D9999", 5);

        Assert.Equal(5, data.Length);
        Assert.All(data, v => Assert.Equal(0, v));
    }

    [Fact]
    public void PlcMemory_Written_FiresAfterWrite()
    {
        var mem = new PlcMemory();
        string? firedAddr = null;
        short[]? firedData = null;

        mem.Written += (addr, data) => { firedAddr = addr; firedData = data; };
        mem.Write("D2000", new short[] { 1, 2, 3 });

        Assert.Equal("D2000", firedAddr);
        Assert.Equal(new short[] { 1, 2, 3 }, firedData);
    }

    [Fact]
    public void PlcMemory_Write_StoresClone_NotOriginalReference()
    {
        var mem = new PlcMemory();
        var original = new short[] { 7, 8 };
        mem.Write("D2000", original);

        original[0] = 99; // 원본 수정

        var readBack = mem.Read("D2000", 2);
        Assert.Equal(7, readBack[0]); // 클론이어야 함
    }

    // ── SimulatorSignalHandler ───────────────────────────────────────────────

    [Fact]
    public void SignalHandler_ResetsBackupStart_OnOp200Complete()
    {
        var mem = new PlcMemory();
        var proc = new short[100];
        proc[0] = 1; // BackUp_Start
        mem.Write("D2000", proc);

        _ = new SimulatorSignalHandler(mem);

        // DAQ가 PC_Complete_Flag(D2001[1]) = 1 로 쓰면
        mem.Write("D2001", new short[] { 0, 1, 0 });

        Assert.Equal(0, mem.Read("D2000", 1)[0]); // BackUp_Start 리셋
        Assert.Equal(0, mem.Read("D2001", 3)[1]); // PC_Complete_Flag 리셋
    }

    [Fact]
    public void SignalHandler_ResetsBackupStart_OnOp210Complete()
    {
        var mem = new PlcMemory();
        var proc = new short[70];
        proc[0] = 1; // BackUp_Start
        mem.Write("D2200", proc);

        _ = new SimulatorSignalHandler(mem);

        mem.Write("D2201", new short[] { 1 });

        Assert.Equal(0, mem.Read("D2200", 1)[0]);
        Assert.Equal(0, mem.Read("D2201", 1)[0]);
    }

    [Fact]
    public void SignalHandler_NoAction_WhenPcCompleteIsZero()
    {
        var mem = new PlcMemory();
        var proc = new short[100];
        proc[0] = 1; // BackUp_Start 유지
        mem.Write("D2000", proc);

        _ = new SimulatorSignalHandler(mem);

        // PC_Complete_Flag = 0 → 핸들러 아무 동작 없음
        mem.Write("D2001", new short[] { 0, 0, 0 });

        Assert.Equal(1, mem.Read("D2000", 1)[0]); // BackUp_Start 여전히 1
    }

    [Fact]
    public void SignalHandler_NoInfiniteRecursion_OnReset()
    {
        // ResetWord가 Written을 다시 발화해도 값이 0이므로 재귀 없음
        var mem = new PlcMemory();
        _ = new SimulatorSignalHandler(mem);

        var ex = Record.Exception(() =>
            mem.Write("D2001", new short[] { 0, 1, 0 }));

        Assert.Null(ex);
    }
}
