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
-- EF Core가 생성한 SQL 여기에 붙여넣기
```

## 측정 결과

| 쿼리 | Elapsed Time (ms) | Logical Reads | 행 수 |
|---|---|---|---|
| Q1: 기간 1일 | | | |
| Q2: 기간 1개월 + MODEL | | | |
| Q3: 기간 1개월 + TOTAL_JUDGE NG | | | |

## 전체 비교표

| 쿼리 | Phase A (ADO.NET, 인덱스 없음) | Phase B (ADO.NET, 인덱스) | Phase C (EF Core, 인덱스) |
|---|---|---|---|
| Q1 | ms | ms | ms |
| Q2 | ms | ms | ms |
| Q3 | ms | ms | ms |

## 코드량 비교

| 항목 | ADO.NET | EF Core |
|---|---|---|
| 쿼리 빌딩 | ~80줄 | ~5줄 |
| 결과 매핑 | ~50줄 (DataSet → DTO 수동) | 자동 |
| 파라미터 처리 | 수동 SqlParameter | 자동 (SQL injection 방지) |
| 합계 | ~130줄 | ~15줄 |
