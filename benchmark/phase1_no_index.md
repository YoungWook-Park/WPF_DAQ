# Phase A — 인덱스 없음 측정 결과

## 환경
- DB: SQL Server (로컬)
- 데이터: EMPG 50만건, EMPG_HIS 50만건
- 인덱스: PK NONCLUSTERED on RESULT_ID 만 존재 (Heap 테이블)
- UPDATE_TIME 타입: datetime (스키마 수정 후)

## 실행계획 특징
- Table Scan (전체 테이블 스캔)
- 예상 실행계획 캡처 후 여기에 기술

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

## 실행계획 요약
- Q1: 
- Q2: 
- Q3: 
