# Phase A — 인덱스 없음 측정 결과

## 환경
- DB: SQL Server Express (.\SQLEXPRESS)
- 데이터: EMPG 500,022건 / EMPG_HIS 500,000건
- 인덱스: PK NONCLUSTERED on RESULT_ID 만 존재 (Heap 테이블)
- UPDATE_TIME 타입: datetime (Step 1 스키마 수정 완료)
- 측정 방식: 매 쿼리 전 DBCC DROPCLEANBUFFERS + FREEPROCCACHE (콜드 캐시)

## 실행계획 특징
- Table Scan (전체 Heap 스캔) — 모든 쿼리 동일
- Logical Reads = 50,004 (전체 페이지 수) — 범위 필터링 없음
- Physical Reads = 50,004 (콜드 캐시 조건)

## 측정 결과

| 쿼리 | Elapsed Time (ms) | CPU Time (ms) | Logical Reads | 행 수 |
|---|---|---|---|---|
| Q1: 기간 1일 | **295** | 172 | 50,004 | 452 |
| Q2: 기간 1개월 + MODEL | **316** | 171 | 50,004 | 2,797 |
| Q3: 기간 1개월 + TOTAL_JUDGE NG | **330** | 78 | 50,004 | 1,430 |

## 실행계획 요약
- Q1: Table Scan (EMPG) — UPDATE_TIME 범위 조건에도 인덱스 없음 → 전체 스캔
- Q2: Table Scan (EMPG) — MODEL 필터 추가해도 전체 스캔
- Q3: Table Scan (EMPG) — TOTAL_JUDGE 필터 추가해도 전체 스캔
- 공통: PK가 NONCLUSTERED이므로 데이터 페이지 = Heap, Index Seek 불가능
