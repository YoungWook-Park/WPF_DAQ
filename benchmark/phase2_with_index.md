# Phase B — 인덱스 적용 후 측정 결과

## 환경
- 인덱스: `IX_EMPG_UPDATE_TIME` ON EMPG(UPDATE_TIME) INCLUDE(...)
- 나머지 환경 Phase A와 동일

## 실행계획 특징
- Index Seek → Key Lookup (또는 Covering Index Scan)
- 실행계획 캡처 후 기술

## 측정 결과

| 쿼리 | Elapsed Time (ms) | CPU Time (ms) | Logical Reads | 행 수 |
|---|---|---|---|---|
| Q1: 기간 1일 | | | | |
| Q2: 기간 1개월 + MODEL | | | | |
| Q3: 기간 1개월 + TOTAL_JUDGE NG | | | | |

## 앱 레벨 Stopwatch (ms)

| 쿼리 | 1회차 | 2회차 | 3회차 | 평균 |
|---|---|---|---|---|
| Q1 | | | | |
| Q2 | | | | |
| Q3 | | | | |

## Phase A 대비 개선율

| 쿼리 | Phase A 평균 | Phase B 평균 | 개선율 |
|---|---|---|---|
| Q1 | ms | ms | % |
| Q2 | ms | ms | % |
| Q3 | ms | ms | % |
