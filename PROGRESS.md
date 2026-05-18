# 진행 현황

## 완료 이력

- DB 스키마, SqlAgent 라이브러리, WPF 앱 구성, DevExpress 제거
- ControlUnit_DAQ 전면 리팩토링 (Phase A~F: DTO·파서·EmpgRow·DB레이어·WriteRegion·파이프라인)
- IProcessEventBus (Phase G), PLC Simulator TCP 통합 (featureA C2~C7)
- xUnit 테스트 18개 작성 및 통과 (Unit 16 / Integration 2)
- **2026-05-18**: `MainCore` 싱글톤 도입 — `MainWindow` 코드비하인드 분리, 레거시 컨벤션(`cFunc_`, `partial`, 로그 형식) 적용

## 미결 사항

- `ProcessData_Op200` 내 `_eventBus.Publish` 위치 수정 (DB 실패 시에도 모니터 표시)
- TCP 연결 상태 UI 상태바 반영

상세 이력: `git log` 참조.
