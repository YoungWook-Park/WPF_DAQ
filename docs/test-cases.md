# xUnit 테스트 케이스 목록

> 최종 갱신: 2026-05-18  
> 환경: .NET 10, ConSight.DONGBO.DAQ.Tests

---

## 요약

| 카테고리    | 파일                     | 테스트 수 | 상태 |
|-------------|--------------------------|-----------|------|
| Unit        | WireProtocolTests.cs     | 6         | ✅ 전부 통과 |
| Unit        | HandshakeTests.cs        | 7         | ✅ 전부 통과 |
| Unit        | ReadLoopTests.cs         | 3         | ✅ 전부 통과 |
| Integration | Op200PipelineTests.cs    | 2         | ✅ 전부 통과 |
| **합계**    |                          | **18**    | **18 / 18** |

---

## Unit 테스트

Unit 테스트는 DB·TCP 없이 실행됩니다.  
실행 명령: `dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit`

### WireProtocolTests — PLC TCP 프로토콜 직렬화

| #   | 테스트 메서드                                                 | 검증 내용                                                    | 상태  |
| --- | ------------------------------------------------------- | -------------------------------------------------------- | --- |
| 1   | `BuildReadRequest_EncodesAddrAndWordCount`              | R 요청: op코드 · addrLen · ASCII addr · wordCount(2B BE) 인코딩 | ✅   |
| 2   | `BuildWriteRequest_EncodesPayloadBigEndian`             | W 요청: 페이로드 Big-Endian 2바이트/word 인코딩                      | ✅   |
| 3   | `TryReadResponse_ReturnsFalse_OnUnexpectedOp`           | 서버 응답 op코드 불일치 → false 반환                                | ✅   |
| 4   | `TryReadResponse_ReturnsFalse_OnErrorStatus`            | status=1(ERR) 응답 → false 반환                              | ✅   |
| 5   | `TryReadResponse_DecodesWords_OnSuccessfulReadResponse` | R 성공 응답: short[] 디코딩 정확성                                 | ✅   |
| 6   | `TryReadResponse_ReturnsTrue_OnSuccessfulWriteResponse` | W 성공 응답: wordCount=0, payload 없음 처리                      | ✅   |

### HandshakeTests — PlcMemory · SimulatorSignalHandler

| #   | 테스트 메서드                                                     | 검증 내용                                                       | 상태  |
| --- | ----------------------------------------------------------- | ----------------------------------------------------------- | --- |
| 7   | `PlcMemory_Read_ReturnsZeroArray_WhenAddrNotPreInitialized` | 미초기화 주소 Read → 0 배열 반환                                      | ✅   |
| 8   | `PlcMemory_Written_FiresAfterWrite`                         | Write 후 Written 이벤트 발화                                      | ✅   |
| 9   | `PlcMemory_Write_StoresClone_NotOriginalReference`          | 원본 배열 변경이 저장된 값에 영향 없음                                      | ✅   |
| 10  | `SignalHandler_ResetsBackupStart_OnOp200Complete`           | PC_Complete_Flag(D2001[1])=1 수신 시 BackUp_Start(D2000[0]) 리셋 | ✅   |
| 11  | `SignalHandler_ResetsBackupStart_OnOp210Complete`           | PC_Complete_Flag(D2201[0])=1 수신 시 BackUp_Start(D2200[0]) 리셋 | ✅   |
| 12  | `SignalHandler_NoAction_WhenPcCompleteIsZero`               | PC_Complete_Flag=0 → 핸들러 아무 동작 없음                           | ✅   |
| 13  | `SignalHandler_NoInfiniteRecursion_OnReset`                 | ReSet이 Written을 재발화해도 무한 재귀 없음                              | ✅   |

### ReadLoopTests — PlcReadLoop 동작

| #   | 테스트 메서드                                            | 검증 내용                                             | 상태  |
| --- | -------------------------------------------------- | ------------------------------------------------- | --- |
| 14  | `PlcReadLoop_TriggersOnce_OnRisingEdge`            | BackUp_Start 상승 에지에서 D2001 Write 1회 발생            | ✅   |
| 15  | `PlcReadLoop_NoRetrigger_WhenBackupStartStaysHigh` | BackUp_Start 유지(시뮬레이터 리셋 없음) → D2001 Write 정확히 1회 | ✅   |
| 16  | `PlcReadLoop_SkipsCycle_OnDriverReadFailure`       | ReadWords 실패 → 예외 없이 루프 continue                  | ✅   |

---

## Integration 테스트

Integration 테스트는 SQLEXPRESS + `DB_eM` 데이터베이스가 필요합니다.  
SQLEXPRESS 미가동 시 vacuous pass (early return)로 건너뜁니다.  
실행 명령: `dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Integration`

### Op200PipelineTests — OP200 파이프라인 End-to-End

| # | 테스트 메서드 | 검증 내용 | 상태 |
|---|--------------|-----------|------|
| 17 | `TriggerOp200_WritesPcCompleteFlag_AfterProcessing` | D2000(BackUp_Start=1) 트리거 후 10초 내 D2001[1]>0 기록 | ✅ |
| 18 | `TriggerOp200_InsertsOrUpdatesRow_InDb` | 파이프라인 완료 후 EMPG 테이블에 해당 시리얼 행 존재 | ✅ |

**통합 테스트 선행 조건**

- `STS_MODEL_TB`에 `MockArrayBuilder`가 사용하는 `MODEL` 값이 존재해야 함  
  (FK: `EMPG.MODEL → STS_MODEL_TB.MODEL`). `ControlUnit_DAQ.ProcessData_Op200()` 내부에서 upsert 처리.
- `DisposeAsync`에서 테스트 행 자동 정리: `EMPG WHERE MAT_SERIAL01='SN-00001'`, `STS_MODEL_TB WHERE MODEL='MODEL-A'`

---

## 테스트 자동화 명령어

CLAUDE.md 에 정의된 커스텀 명령어:

| 명령어 | 설명 |
|--------|------|
| `/testgen` | 마지막 커밋 변경 파일 기준으로 xUnit 테스트 자동 작성 → 실행 → 결과 보고 |
| `/review` | 마지막 커밋 구현 요약 + 코드 품질 검토 |

단순 실행만 필요할 경우 아래 명령을 직접 사용:

```powershell
# Unit only (SQLEXPRESS 불필요)
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit

# Integration only (SQLEXPRESS 필요)
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Integration

# 전체
dotnet test src/ConSight.DONGBO.DAQ.Tests
```
