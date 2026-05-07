# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```powershell
# Build
cd src
dotnet build ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj

# Run
dotnet run --project ConSight.DONGBO.DAQ/ConSight.DONGBO.DAQ.csproj
```

Runtime requires SQL Server Express at `Server=.\SQLEXPRESS;Database=DB_eM;Integrated Security=SSPI;TrustServerCertificate=True`.

## Testing

There is no unit test project. Testing is done through the **Pipeline Test** tab in the running application:

1. **Parser Test** — Click OP200/210/220/230 buttons to parse hardcoded mock `short[]` arrays and display DTO fields. No DB required.
2. **Full Pipeline Test** — Click "OP200 파이프라인" to run Parser → ControlUnit_DAQ → DB INSERT + MockPlcDriver log. Requires SQL Server.

Test view source: `src/ConSight.DONGBO.DAQ/Views/99_Test/ProcessPipelineTestView.xaml.cs`

The mock `short[]` builders in that file mirror parser offsets exactly (ASCII strings, 2-word Int32, float-as-scaled-int). When modifying parsers, keep the mock builders in sync.

## Solution Structure

```
src/
  ConSight.DONGBO.slnx                     # Solution file
  Bi.ConSight.SqlAgent/                    # Class library: custom SqlConnection wrapper
  ConSight.DONGBO.DAQ/                     # Main WPF application (.NET 10-windows)
```

**Key NuGet packages in the WPF project:**
- `CommunityToolkit.Mvvm 8.4.2` — RelayCommand, ObservableObject
- `Microsoft.EntityFrameworkCore.SqlServer 9.0.4` — inquiry/history views
- `CsvHelper 33.0.1` — EMPG row export
- `Microsoft.Xaml.Behaviors.Wpf 1.1.142` — EventToCommand in XAML

## Architecture Overview

### 4-Stage Manufacturing Pipeline

The core orchestrator is `Sequence/Controller/ControlUnit_DAQ.cs`. It processes PLC signals through four sequential operations:

```
PLC Signal (short[])
  → Op200Parser → Op200ProcessDto → EmpgRow.From()        [INSERT to DB]
  → Op210Parser → Op210ProcessDto → EmpgRow.ApplyOp210()  [UPDATE in DB]
  → Op220Parser → Op220ProcessDto → EmpgRow.ApplyOp220()  [UPDATE in DB]
  → Op230Parser → Op230ProcessDto → EmpgRow.ApplyOp230()  [UPDATE in DB]
  → CSV append + IProcessEventBus.Publish(row)
```

### Key Patterns

**Immutable DTOs (Phase A)** — `Op200ProcessDto`, `Op210ProcessDto`, etc. use `init`-only setters. 44 measurement fields (APD01–44) and 50 config snapshot fields (SP01–50).

**Parser Strategy (Phase B)** — One parser class per operation (`Op200Parser`, etc.) converts raw `short[]` PLC memory into strongly-typed DTOs. Offsets are documented in comments (D2000, D2010, etc.). Common conversions live in `PlcParseHelper` (F2, F2Int, F4Int, Judge, Serial).

PLC setting array addressing:
- **D1900** — shared by OP200, OP210, OP220 (SP01–36)
- **D1800** — used exclusively by OP230 (SP37–50)

**EmpgRow as Aggregate Root (Phase C)** — Single domain object holding all 91 fields for one manufacturing record. Lifecycle: `From(dto)` creates it at OP200 completion; `ApplyOp2X0()` mutates it as subprocesses complete. `TotalJudge` recalculates on each apply — once NG, stays NG.

**OP210–230 Fallback** — If a subprocess signal arrives without a preceding OP200, `BuildFallback()` creates a phantom row (NG judge) and calls `InsertFallback()` to prevent data loss in manufacturing anomalies.

**Write Region Abstraction (Phase E)** — `IPlcWriteRegion` implementations per operation. All four are collected in `_allRegions[]` for a single `foreach` in `RunTimeTriggerLoopAsync()`. The TimeTrigger pattern: enqueue pulse → 1000ms delay → dequeue reset (PLC handshake protocol).

**Type-Safe EventBus (Phase G)** — `IProcessEventBus` replaces legacy `NormValueDictionary[string]` boxing. `ProcessEventBus` uses `event Action<EmpgRow>?` with a lock/snapshot pattern for thread safety. Subscribers receive rows on a background thread — UI updates require `Dispatcher.InvokeAsync()`.

### Directory Map

| Path | Contents |
|------|----------|
| `AppEvent/` | `IProcessEventBus`, `ProcessEventBus` |
| `Common/` | `Constants_App`, `ObservableRangeCollection` |
| `Compat/` | Stubs for legacy libs (MxComponent, LogWriter, ExpException) |
| `Data/DriverDataRead/` | Legacy parser stub (superseded by `Device/PLC/` parsers) |
| `Data/DriverDataWrite/` | `TimeTriggerQueue`, `Write_TimeTriggerDataArgs` |
| `Define/` | Enums for result types and PLC write words |
| `Device/DB/` | `EmpgRow`, `SSMS_Op200`, `SSMS_SubProcess`, `SSMS_Model`, `EmpgCsvWriter` |
| `Device/DB/EfCore/` | `DongBoDbContext`, `EmpgEntity` (EMPG table), `EmpgHisEntity` (EMPG_HIS table); `HasBaseType(null)` disables EF Core TPH |
| `Device/PLC/` | Parsers, DTOs, `IPlcDriver`, `IPlcWriteRegion`, `PlcWriteBuffer` |
| `Device/PLC/OP200–230/` | Per-operation write region implementations |
| `Sequence/Controller/` | `ControlUnit_DAQ` — main pipeline orchestrator |
| `Views/03_Inquiry/` | History inquiry view — dual implementations: ADO.NET (legacy) and EF Core (`_EfCore` suffix) |
| `Views/99_Test/` | `ProcessPipelineTestView` — manual pipeline test UI |

### Composition

There is no DI container. Dependencies are wired manually in `MainWindow.InitViews()` and `ProcessPipelineTestView` constructor. The `IPlcDriver` interface has a `MockPlcDriver` for testing and expects a real `MxComponentPlcDriver` for production (external `Bi.ConSightCommon` library).

### Bi.ConSight.SqlAgent

The sibling class library provides three types for raw ADO.NET access:
- `SqlConnectionFactory` — creates `SqlConnection` from a connection string
- `QueryExecution` — executes SELECT queries, returns `DataTable`
- `NonQueryExecution` — executes INSERT/UPDATE/DELETE

The inquiry views use this library for the ADO.NET path; EF Core views use `DongBoDbContext` directly.

### Database Performance

The EMPG table has a covering index on `UPDATE_TIME` (nvarchar, not datetime) that must exist for acceptable query performance:

```sql
CREATE NONCLUSTERED INDEX IX_EMPG_UPDATE_TIME
ON EMPG (UPDATE_TIME)
INCLUDE (TOTAL_JUDGE, MODEL, MAT_SERIAL01, MAT_SERIAL02, RESULT_ID);
```

Without this index, queries degrade from ~1ms to 295–330ms on 500K+ rows.
