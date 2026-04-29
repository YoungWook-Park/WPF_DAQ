// Step 6에서 Op200WriteRegion으로 대체됨.
// Device/PLC/OP200/Op200WriteRegion.cs 참조.
//
// AS-IS (이 파일):
//   - static Int16[] WriteDataList  → 전역 상태, 테스트 불가
//   - PC_Complete_Flag = 0 (word 값)
//   - PC_Power_On = 2 (word 값)
//   - IPlcDriver 미추상화 → Mock 불가
//
// TO-BE (Op200WriteRegion):
//   - PlcWriteBuffer 캡슐화 (IPlcDriver 주입)
//   - Word 0: PC_Response (비트 필드, BitPos_PC_Response enum)
//   - Word 1: PC_Complete_Flag (word 값)
//   - Word 2: PC_Power_On (word 값)
//   - SetWord/SetBit → PC 메모리만 변경
//   - Cmd_Write() → 배열 단위 드라이버 전송
