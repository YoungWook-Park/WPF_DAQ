# Phase B — 인덱스 적용 후 측정 결과

## 환경
- 인덱스:
  - `IX_EMPG_UPDATE_TIME` ON EMPG(UPDATE_TIME) INCLUDE(TOTAL_JUDGE, MODEL, MAT_SERIAL01, MAT_SERIAL02, RESULT_ID, REPAIR, OP200_TOTAL_JUDGE)
  - `IX_EMPG_HIS_UPDATE_TIME` ON EMPG_HIS(UPDATE_TIME) INCLUDE(...)
- 데이터, DB 환경 Phase A와 동일
- 측정 방식: 매 쿼리 전 DBCC DROPCLEANBUFFERS + FREEPROCCACHE (콜드 캐시)

## 실행계획 특징
- Q1/Q2/Q3 모두: Index Seek (IX_EMPG_UPDATE_TIME) → Covering Index
- Key Lookup 없음 (INCLUDE 컬럼으로 커버링 완성)
- Logical Reads = 13~261 (Phase A의 50,004 대비 99.5% 감소)

## 측정 결과

| 쿼리 | Elapsed Time (ms) | CPU Time (ms) | Logical Reads | 행 수 |
|---|---|---|---|---|
| Q1: 기간 1일 | **< 1** | 0 | 13 | 452 |
| Q2: 기간 1개월 + MODEL | **5** | 0 | 261 | 2,797 |
| Q3: 기간 1개월 + TOTAL_JUDGE NG | **3** | 0 | 261 | 1,430 |

## Phase A 대비 개선율

| 쿼리 | Phase A Elapsed | Phase B Elapsed | Logical Reads 감소 | 개선율 |
|---|---|---|---|---|
| Q1 | 295ms | ~0ms | 50,004 → 13 | **99.7%** |
| Q2 | 316ms | 5ms | 50,004 → 261 | **98.4%** |
| Q3 | 330ms | 3ms | 50,004 → 261 | **99.1%** |
