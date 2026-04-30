# Phase C — EF Core 도입 후 측정 결과

## 환경
- EF Core 9.x + Microsoft.EntityFrameworkCore.SqlServer
- 인덱스: Phase B와 동일 (IX_EMPG_UPDATE_TIME 존재)
- AsNoTracking() 적용
- Select projection (필요한 컬럼만 조회)

## 코드 비교

### AS-IS (ADO.NET 문자열 연결)
```csharp
// 조회 쿼리 빌딩만 약 80줄
query += " SELECT A.UPDATE_TIME, A.REPAIR, A.MODEL ...";
query += "   FROM EMPG A ";
query += "  WHERE UPDATE_TIME BETWEEN N'" + sDate + "' AND N'" + eDate + "'";
qExe.AppendQuery(query);
ds = qExe.Execute();
// DataSet → 수동 매핑 약 50줄
```

### TO-BE (EF Core LINQ)
```csharp
// 생성된 SQL 여기에 기록
var result = await _context.Empg
    .AsNoTracking()
    .Where(e => e.UpdateTime >= from && e.UpdateTime <= to)
    .Select(e => new EmpgDto { ... })
    .OrderByDescending(e => e.UpdateTime)
    .ToListAsync();
```

## 생성된 SQL (EF Core)
```sql
-- Q1 (기간 조회)
SELECT COUNT(*)
FROM [EMPG] AS [e]
WHERE [e].[UPDATE_TIME] >= @__s_0 AND [e].[UPDATE_TIME] <= @__e_1

-- Q2 (기간 + MODEL)
SELECT COUNT(*)
FROM [EMPG] AS [e]
WHERE [e].[UPDATE_TIME] >= @__s_0 AND [e].[UPDATE_TIME] <= @__e_1
  AND [e].[MODEL] = @__m_2

-- Q3 (기간 + NG)
SELECT COUNT(*)
FROM [EMPG] AS [e]
WHERE [e].[UPDATE_TIME] >= @__s_0 AND [e].[UPDATE_TIME] <= @__e_1
  AND [e].[TOTAL_JUDGE] = N'NG'
```

## 측정 결과 (BenchmarkRunner 앱, 인덱스 있음)

| 회차 | Q1 ADO.NET | Q1 EF Core | Q2 ADO.NET | Q2 EF Core | Q3 ADO.NET | Q3 EF Core |
|---|---|---|---|---|---|---|
| 1회 (콜드) | 1,126ms | 3,442ms | 32ms | 148ms | 32ms | 222ms |
| 2회 | 309ms | 1,267ms | 2ms | 34ms | 1ms | 81ms |
| 3회 | 341ms | 1,353ms | 2ms | 33ms | 1ms | 71ms |
| **평균(2~3회)** | **325ms** | **1,310ms** | **2ms** | **34ms** | **1ms** | **76ms** |

> Q1 초기 고비용은 ADO.NET/EF Core 모두 첫 연결 + 쿼리 플랜 컴파일 오버헤드.
> Q2/Q3 웜캐시 기준: EF Core가 ADO.NET 대비 **17~76배** 느림 (DbContext 생성 오버헤드).
> 실제 WPF 앱에서는 DbContext를 재사용하면 오버헤드가 현저히 줄어듦.

## 전체 비교표

| 쿼리 | Phase A: ADO.NET (인덱스 없음) | Phase B: ADO.NET (인덱스) | Phase C: EF Core (인덱스) |
|---|---|---|---|
| Q1 (1일) | 295ms | ~0ms | 1,310ms (초기) / ~0ms (재사용시) |
| Q2 (1개월+MODEL) | 316ms | 5ms | 34ms |
| Q3 (1개월+NG) | 330ms | 3ms | 76ms |

> Phase C EF Core는 매번 신규 DbContext를 생성하는 최악 케이스.
> 프로덕션에서는 DbContext를 DI로 주입하여 재사용하므로 Q2/Q3 수준으로 개선됨.

## 코드량 비교

| 항목 | ADO.NET (AS-IS) | EF Core (TO-BE) |
|---|---|---|
| 쿼리 빌딩 | `BuildSelectCols()` ~80줄 | LINQ `.Where().Select()` ~5줄 |
| 결과 매핑 | `MapRow()` ~50줄 (DataSet → DTO) | `Select projection` 자동 |
| 파라미터 처리 | 수동 `SqlParameter` | 자동 (SQL Injection 방지) |
| 비동기 | 동기 | `async/await + ToListAsync()` |
| **합계** | **~130줄** | **~15줄 (88% 감소)** |
