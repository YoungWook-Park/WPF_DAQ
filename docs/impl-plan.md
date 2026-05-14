# featureA 구현 계획 (C2~C7)

커밋 전 필수: `dotnet build src/ConSight.DONGBO.slnx` 성공 확인.  
커밋 형식: `feat(CX): 설명`.

---

## C2 — PlcSimulator 인프라

**신규 파일 4개 + slnx 수정**

### ConSight.DONGBO.PlcSimulator.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>ConSight.DONGBO.PlcSimulator</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>
</Project>
```

### Memory/PlcMemory.cs

```csharp
namespace ConSight.DONGBO.PlcSimulator.Memory
{
    internal sealed class PlcMemory
    {
        private readonly Dictionary<string, short[]> _store = new();
        private readonly object _lock = new();

        // 앱 시작 시 각 OP 영역을 0으로 pre-initialize
        internal PlcMemory()
        {
            foreach (var (addr, count) in new[] {
                ("D2000",100), ("D2200",70), ("D2300",70), ("D2400",80),
                ("D1900",100), ("D1800",24),
                ("D2001",3), ("D2201",1), ("D2301",1), ("D2401",1) })
                _store[addr] = new short[count];
        }

        internal event Action<string, short[]>? Written;

        internal short[] Read(string addr, int count)
        {
            lock (_lock)
                return _store.TryGetValue(addr, out var d)
                    ? (short[])d.Clone()
                    : new short[count];
        }

        internal void Write(string addr, short[] data)
        {
            lock (_lock)
                _store[addr] = (short[])data.Clone();
            Written?.Invoke(addr, (short[])data.Clone()); // lock 밖에서 발화 (handler 재진입 허용)
        }
    }
}
```

### Net/PlcWireProtocol.cs

`Device/PLC/Net/PlcWireProtocol.cs` 전체 복사, namespace만 변경:
```csharp
namespace ConSight.DONGBO.PlcSimulator.Net
```

### Net/PlcSimulatorServer.cs

```csharp
namespace ConSight.DONGBO.PlcSimulator.Net
{
    internal sealed class PlcSimulatorServer
    {
        private readonly TcpListener _listener;
        private readonly Memory.PlcMemory _memory;
        private readonly CancellationTokenSource _cts = new();
        private TcpClient? _activeClient;

        internal PlcSimulatorServer(Memory.PlcMemory memory, int port)
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
            _listener.Stop();
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _activeClient?.Dispose();
                    _activeClient = client;
                    Task.Run(() => ClientLoop(client));
                }
                catch (OperationCanceledException) { break; }
                catch { /* listener closed */ break; }
            }
        }

        private void ClientLoop(TcpClient client)
        {
            using var stream = client.GetStream();
            var header = new byte[4];
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (!ReadExact(stream, header, 4)) break;
                    byte op       = header[0];
                    int addrLen   = header[1];
                    int wordCount = (header[2] << 8) | header[3];

                    var addrBuf = new byte[addrLen];
                    if (!ReadExact(stream, addrBuf, addrLen)) break;
                    string addr = Encoding.ASCII.GetString(addrBuf);

                    if (op == (byte)'R')
                    {
                        var words = _memory.Read(addr, wordCount);
                        WriteResponse(stream, (byte)'R', words);
                    }
                    else if (op == (byte)'W')
                    {
                        var payload = new byte[wordCount * 2];
                        if (!ReadExact(stream, payload, payload.Length)) break;
                        var words = new short[wordCount];
                        for (int i = 0; i < wordCount; i++)
                            words[i] = (short)((payload[i*2] << 8) | payload[i*2+1]);
                        _memory.Write(addr, words);
                        WriteResponse(stream, (byte)'W', Array.Empty<short>());
                    }
                }
            }
            catch { /* client disconnected */ }
            finally { client.Dispose(); }
        }

        private static void WriteResponse(NetworkStream stream, byte op, short[] words)
        {
            var buf = new byte[4 + words.Length * 2];
            buf[0] = op; buf[1] = 0;
            buf[2] = (byte)(words.Length >> 8); buf[3] = (byte)(words.Length & 0xFF);
            for (int i = 0; i < words.Length; i++)
            {
                buf[4 + i*2]   = (byte)(words[i] >> 8);
                buf[4 + i*2+1] = (byte)(words[i] & 0xFF);
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
```

### slnx 수정

```xml
<Solution>
  <Project Path="Bi.ConSight.SqlAgent/Bi.ConSight.SqlAgent.csproj" />
  <Project Path="ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj" />
  <Project Path="ConSight.DONGBO.PlcSimulator/ConSight.DONGBO.PlcSimulator.csproj" />
</Solution>
```

---

## C3 — PlcSimulator 로직+UI

**신규 파일 4개 (Logic 2 + MainWindow + App)**

### Logic/SimulatorSignalHandler.cs

```csharp
namespace ConSight.DONGBO.PlcSimulator.Logic
{
    internal sealed class SimulatorSignalHandler
    {
        private readonly Memory.PlcMemory _memory;

        internal SimulatorSignalHandler(Memory.PlcMemory memory)
        {
            _memory = memory;
            memory.Written += OnWritten;
        }

        private void OnWritten(string addr, short[] data)
        {
            // PC_Complete_Flag 감지 → BackUp_Start + PC_Complete 리셋
            // OP200: write 영역 D2001 (3 words), index 1 = PC_Complete_Flag
            if (addr == "D2001" && data.Length > 1 && data[1] == 1)
            {
                ResetProc("D2000", 0);
                ResetWrite("D2001", 1);
            }
            // OP210/220/230: write 영역 1 word, index 0 = PC_Complete_Flag
            else if (addr == "D2201" && data.Length > 0 && data[0] == 1)
            { ResetProc("D2200", 0); ResetWrite("D2201", 0); }
            else if (addr == "D2301" && data.Length > 0 && data[0] == 1)
            { ResetProc("D2300", 0); ResetWrite("D2301", 0); }
            else if (addr == "D2401" && data.Length > 0 && data[0] == 1)
            { ResetProc("D2400", 0); ResetWrite("D2401", 0); }
        }

        private void ResetProc(string addr, int index)
        {
            var d = _memory.Read(addr, index + 1);
            d[index] = 0;
            _memory.Write(addr, d);
        }

        private void ResetWrite(string addr, int index)
        {
            // 실제 저장 크기를 유지하기 위해 기존 배열 크기 사용
            var d = _memory.Read(addr, index + 1);
            if (d.Length <= index) return;
            d[index] = 0;
            _memory.Write(addr, d);
        }
    }
}
```

> `ResetProc/ResetWrite`가 `PlcMemory.Write()`를 호출해 `Written`이 다시 발화되지만,  
> 리셋 시 값이 0이므로 조건(data[x] == 1)에 걸리지 않아 무한 재귀 없음.

### Logic/MockArrayBuilder.cs

`ProcessPipelineTestView.xaml.cs` 349~537줄의 빌더 로직 복제. 6개 정적 메서드:
- `BuildOp200ProcArray()` → `short[100]`
- `BuildOp200SettingArray()` → `short[100]`
- `BuildOp210ProcArray()` → `short[70]`
- `BuildOp220ProcArray()` → `short[70]`
- `BuildOp230ProcArray()` → `short[80]`
- `BuildOp230SettingArray()` → `short[24]`

`EncodeAscii`, `SetInt32` 헬퍼도 함께 복사. namespace: `ConSight.DONGBO.PlcSimulator.Logic`.

> **동기화 주의**: DAQ 측 파서 오프셋(`Op200Parser.cs` 등)과 항상 일치 유지.

### App.xaml.cs

```csharp
internal sealed partial class App : Application
{
    internal PlcMemory Memory { get; } = new();
    internal PlcSimulatorServer Server { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Server = new PlcSimulatorServer(Memory, port: 5000);
        _ = new SimulatorSignalHandler(Memory); // Written 이벤트 구독
        Server.Start();
        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Server.Stop();
        base.OnExit(e);
    }
}
```

### MainWindow.xaml (PlcSimulator)

레이아웃: 상단 4개 트리거 버튼, 중단 메모리 스냅샷(각 OP 주소 현황), 하단 통신 로그 TextBox.

버튼 클릭 핸들러 패턴 (OP200 예시):
```csharp
private void BtnTriggerOp200_Click(object sender, RoutedEventArgs e)
{
    var app = (App)Application.Current;
    app.Memory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
    app.Memory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray()); // proc[0]=1 포함
    AppendLog($"[{DateTime.Now:HH:mm:ss}] OP200 Triggered");
}
```

---

## C4 — PlcReadLoop + DAQ MainWindow.xaml.cs 수정

### Sequence/PlcReadLoop.cs (신규)

```csharp
namespace ConSight.DAQ.Sequence
{
    public sealed class PlcReadLoop
    {
        private sealed record OpMeta(
            string ProcAddr, int ProcCount,
            string SettingAddr, int SettingCount,
            Func<short[], short[], object> Parse,
            Action<object> Process);

        private readonly IPlcDriver _driver;
        private readonly OpMeta[] _ops;
        private readonly short[] _prevProc0 = new short[4]; // edge detection

        public PlcReadLoop(IPlcDriver driver, ControlUnit_DAQ controlUnit)
        {
            _driver = driver;
            _ops =
            [
                new("D2000", 100, "D1900", 100,
                    (p, s) => new Op200Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op200((Op200ProcessDto)dto)),
                new("D2200",  70, "D1900", 100,
                    (p, s) => new Op210Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op210((Op210ProcessDto)dto)),
                new("D2300",  70, "D1900", 100,
                    (p, s) => new Op220Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op220((Op220ProcessDto)dto)),
                new("D2400",  80, "D1800",  24,
                    (p, s) => new Op230Parser().Parse(p, s),
                    dto    => controlUnit.ProcessData_Op230((Op230ProcessDto)dto)),
            ];
        }

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < _ops.Length; i++)
                {
                    var op = _ops[i];
                    if (!_driver.ReadWords(op.ProcAddr, op.ProcCount, out var proc))
                        continue; // backoff 중이거나 연결 끊김 → skip

                    if (proc[0] == 1 && _prevProc0[i] == 0) // rising edge
                    {
                        if (_driver.ReadWords(op.SettingAddr, op.SettingCount, out var setting))
                        {
                            var dto = op.Parse(proc, setting);
                            op.Process(dto); // ControlUnit_DAQ 동기 blocking 호출
                        }
                    }
                    _prevProc0[i] = proc[0];
                }
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }
    }
}
```

using 추가 필요: `ConSight.DAQ.Device` (파서·DTO), `ConSight.DAQ.Device.PLC` (IPlcDriver)

### Device/PLC/Net/TcpPlcDriver.cs 수정

`CloseConnection()` 을 `private` → `internal` 로 변경. (MainWindow.Window_Closed 에서 호출)

### MainWindow.xaml.cs 수정

```csharp
// 추가 필드
private TcpPlcDriver _tcpDriver = null!;
private PlcReadLoop _plcLoop = null!;
private readonly CancellationTokenSource _cts = new();
```

`InitViews()` 하단에 추가:
```csharp
// PLC 인프라 wire-up
_tcpDriver = new TcpPlcDriver("localhost", 5000);

var buf200 = new PlcWriteBuffer(_tcpDriver, "D2001", 3);
var buf210 = new PlcWriteBuffer(_tcpDriver, "D2201", 1);
var buf220 = new PlcWriteBuffer(_tcpDriver, "D2301", 1);
var buf230 = new PlcWriteBuffer(_tcpDriver, "D2401", 1);

var op200Write = new Op200WriteRegion(buf200);
var op210Write = new Op210WriteRegion(buf210);
var op220Write = new Op220WriteRegion(buf220);
var op230Write = new Op230WriteRegion(buf230);

var csvWriter = new EmpgCsvWriter();  // 기존 생성자 확인 필요
var controlUnit = new ControlUnit_DAQ(
    ConnectionString, op200Write, op210Write, op220Write, op230Write, csvWriter, _eventBus);

_plcLoop = new PlcReadLoop(_tcpDriver, controlUnit);

// MonitoringView
var monVm = new MonitoringViewModel(_eventBus);
MonitoringViewHost.Content = new MonitoringView(monVm);

// 백그라운드 루프 시작
_ = controlUnit.RunTimeTriggerLoopAsync(_cts.Token);
_ = _plcLoop.RunAsync(_cts.Token);
```

`Window_Closed` 핸들러 추가 (xaml 에서 `Closed="Window_Closed"` 연결):
```csharp
private void Window_Closed(object? sender, EventArgs e)
{
    _cts.Cancel();
    _tcpDriver?.CloseConnection();
}
```

### MainWindow.xaml 수정

TabControl 첫 자식으로 Monitoring 탭 추가:
```xml
<TabItem Header="Monitoring">
    <ContentControl x:Name="MonitoringViewHost"/>
</TabItem>
```

---

## C5 — MonitoringView + ViewModel

### Views/01_Monitoring/MonitoringViewModel.cs (신규)

```csharp
namespace ConSight.DAQ.Views.Monitoring
{
    public sealed class MonitoringViewModel
    {
        public ObservableCollection<EmpgRow> Rows { get; } = [];
        private const int MaxRows = 200;

        public MonitoringViewModel(IProcessEventBus eventBus)
        {
            eventBus.Subscribe(OnRow);
        }

        private void OnRow(EmpgRow row)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int idx = FindRow(row);
                if (idx >= 0)
                    Rows[idx] = row;           // OP210/220/230: 기존 행 갱신
                else
                {
                    Rows.Insert(0, row);        // OP200: 신규 행 최상단 삽입
                    if (Rows.Count > MaxRows)
                        Rows.RemoveAt(Rows.Count - 1);
                }
            });
        }

        // MatSerial01 기준 기존 행 탐색. 없으면 -1.
        private int FindRow(EmpgRow row)
        {
            if (string.IsNullOrEmpty(row.MatSerial01)) return -1;
            for (int i = 0; i < Rows.Count; i++)
                if (Rows[i].MatSerial01 == row.MatSerial01 ||
                    Rows[i].MatSerial02 == row.MatSerial01)
                    return i;
            return -1;
        }
    }
}
```

### Views/01_Monitoring/MonitoringView.xaml (신규)

DataGrid 컬럼 (AutoGenerateColumns="False"):

| 컬럼 Header | Binding | 비고 |
|------------|---------|------|
| 시간 | UpdateTime | StringFormat=HH:mm:ss |
| 모델 | Model | |
| Serial1 | MatSerial01 | |
| Serial2 | MatSerial02 | |
| 판정 | TotalJudge | |
| GR판정 | Apd07 | OP200 Guide Ring |
| BR판정 | Apd15 | OP200 Bearing |
| SR판정 | Apd24 | OP200 Snap Ring |
| EP판정 | Apd26 | OP200 End Plate |
| RunOut입력 | Apd28 | OP210 |
| RunOut공간 | Apd30 | OP210 |
| Guiding | Apd33 | OP220 |
| SOCP판정 | Apd42 | OP230 |
| SOC판정 | Apd44 | OP230 |

NG 행 강조 RowStyle:
```xml
<DataGrid.RowStyle>
    <Style TargetType="DataGridRow">
        <Style.Triggers>
            <DataTrigger Binding="{Binding TotalJudge}" Value="NG">
                <Setter Property="Background" Value="#FFEEEE"/>
                <Setter Property="Foreground" Value="#CC0000"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>
</DataGrid.RowStyle>
```

---

## C6 — xUnit Unit 테스트 프로젝트

### ConSight.DONGBO.DAQ.Tests.csproj (신규)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>  <!-- DAQ ProjectReference가 WPF이므로 필수 -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ConSight.DONGBO.DAQ\ConSight.DONGBO.DAQ.csproj" />
    <ProjectReference Include="..\ConSight.DONGBO.PlcSimulator\ConSight.DONGBO.PlcSimulator.csproj" />
  </ItemGroup>
</Project>
```

### DAQ AssemblyInfo.cs 수정

```csharp
[assembly: InternalsVisibleTo("ConSight.DONGBO.DAQ.Tests")]
```

### slnx 수정

```xml
<Project Path="ConSight.DONGBO.DAQ.Tests/ConSight.DONGBO.DAQ.Tests.csproj" />
```

### WireProtocolTests.cs

```csharp
[Trait("Category", "Unit")]
public class WireProtocolTests
{
    [Fact] public void BuildReadRequest_EncodesAddrAndWordCount() { ... }
    [Fact] public void BuildWriteRequest_EncodesPayloadBigEndian() { ... }
    [Fact] public void TryReadResponse_ReturnsFalse_OnUnexpectedOp() { ... }
    [Fact] public void ReadWrite_RoundTrip_ViaMemoryStream() { ... }
}
```

### HandshakeTests.cs

```csharp
[Trait("Category", "Unit")]
public class HandshakeTests
{
    [Fact] public void PlcMemory_Read_ReturnsZeroArray_WhenAddrMissing() { ... }
    [Fact] public void PlcMemory_Written_FiresAfterWrite() { ... }
    [Fact] public void SignalHandler_ResetsBackupStart_OnOp200Complete() { ... }
    [Fact] public void SignalHandler_ResetsBackupStart_OnOp210Complete() { ... }
    [Fact] public void SignalHandler_NoAction_WhenPcCompleteIsZero() { ... }
}
```

### ReadLoopTests.cs

PlcReadLoop edge detection 검증. `ControlUnit_DAQ`의 `ProcessData_*`가 `internal`이므로 호출 여부를 직접 assert하기 어려움 → **ProcessData 호출 결과를 MockPlcDriver의 WriteWords 기록으로 간접 검증** (WriteRegion이 Cmd_Write를 호출하므로).

```csharp
[Trait("Category", "Unit")]
public class ReadLoopTests
{
    [Fact] public async Task PlcReadLoop_TriggersOnce_OnRisingEdge() { ... }
    [Fact] public async Task PlcReadLoop_NoRetrigger_WhenBackupStartStaysHigh() { ... }
    [Fact] public async Task PlcReadLoop_SkipsCycle_OnDriverReadFailure() { ... }
}
```

---

## C7 — Integration 테스트 + 빌드 정리

### Helpers/SqlExpressSkip.cs

```csharp
// SQLEXPRESS 미가동 시 Integration 테스트 skip 유틸
internal static class SqlExpressSkip
{
    internal static void SkipIfUnavailable()
    {
        try { new SqlConnection(TestConnectionString).Open(); }
        catch { Assert.Skip("SQLEXPRESS 미가동 — Integration 테스트 건너뜀"); }
    }
}
```

### Op200PipelineTests.cs

```csharp
[Trait("Category", "Integration")]
public class Op200PipelineTests : IAsyncLifetime
{
    // PlcMemory + PlcSimulatorServer(랜덤 포트) + TcpPlcDriver + PlcReadLoop + ControlUnit_DAQ
    // InitializeAsync: SqlExpressSkip.SkipIfUnavailable(), 서버 시작, 루프 Task 시작
    // DisposeAsync: cts.Cancel(), 서버 Stop

    [Fact]
    public async Task TriggerOp200_InsertsRowToDb()
    {
        // _memory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
        // _memory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());
        // await WaitForPcComplete("D2001", timeoutMs: 3000);
        // var row = new SSMS_Op200(TestConnectionString).FindBySerial("SN-00001");
        // Assert.NotNull(row);
        // Assert.Equal("OK", row.TotalJudge);
    }

    [Fact]
    public async Task TriggerOp200_WithNgJudge_PropagatesNgToDb() { ... }
}
```

### 빌드 정리

- `dotnet build src/ConSight.DONGBO.slnx` — 경고 0건 목표
- `ProcessPipelineTestView.xaml.cs` 상단에 MockArrayBuilder 출처 주석 추가
- README 또는 devlog에 end-to-end 수동 검증 순서 기록

---

## 설계 결정 사항

| 항목 | 결정 | 근거 |
|------|------|------|
| MonitoringViewModel OP 구분 | MatSerial01 존재 여부로 Insert/Replace 결정 (A안) | EmpgRow에 OP 타입 필드 없음. 동일 Serial OP200 재진입은 드문 케이스. |
| PlcReadLoop 파이프라인 호출 | `ProcessData_*` 동기 blocking | edge detection이 중복 트리거를 막고, OP 순서 보장 필요 |
| PlcSimulatorServer 클라이언트 정책 | 신규 accept 시 기존 클라이언트 Dispose (단일 유지) | DAQ는 연결 끊기면 즉시 재연결 시도 (TcpPlcDriver EnsureConnected) |
| TestProject TFM | `net10.0-windows` + `UseWPF` | DAQ ProjectReference가 WPF TFM 요구 |
| PlcWireProtocol 공유 방식 | 파일 복사 (namespace만 변경) | 소스 링크 대신 단순 복사 — namespace 충돌 없음. 프로토콜 변경 시 두 파일 동기화 필요. |
