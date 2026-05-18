# 프로젝트 목적

레거시 .NET Framework WPF DAQ 앱을 .NET 10으로 재작성하면서, 3가지 개선 목표를 측정·기록한다.

| 항목 | AS-IS | TO-BE |
|------|-------|-------|
| 런타임 | .NET Framework 4.x, DevExpress WPF | .NET 10 WPF, 순수 WPF |
| DB 접근 | 블랙박스 DLL, 문자열 연결 쿼리 | 자체 SqlAgent, 파라미터화 쿼리 |
| 인덱스 | 없음 (Heap, Full Scan) | 클러스터형 인덱스 (UPDATE_TIME) |
| PLC 결합 | 하드코딩 직접 호출 | IPlcDriver 인터페이스 + Mock/TCP 구현 |
| 테스트 | 없음 | xUnit Unit + Integration |

## 성능 결과

| 단계 | 조건 | 응답시간 | Logical Reads |
|------|------|---------|---------------|
| Phase 1 | 인덱스 없음 (Full Scan) | 295~330ms | 50,004 |
| Phase 2 | Index Seek | 0~5ms | 13~261 |
| Phase 3 | EF Core + AsNoTracking | 1~3ms | 13~261 |

상세 측정값: `benchmark/` 폴더 참조.
