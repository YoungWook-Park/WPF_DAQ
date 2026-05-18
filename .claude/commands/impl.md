주어진 구현 단계(C2~C7)를 실행한다.

## 절차

### 1. 계획 읽기
`docs/impl-plan.md`에서 `## <step>` 섹션을 읽고 사전 체크리스트를 출력한다:
- 신규/수정 파일 목록
- 작성할 테스트 목록
- 빌드 의존성 변경 여부 (csproj, slnx 수정 포함 시 명시)

### 2. 구현
체크리스트 순서대로 파일 작성/수정한다.

### 3. 빌드 확인
```powershell
dotnet build src/ConSight.DONGBO.slnx -v q 2>&1 | Select-String "error|warning|Build succeeded" | Select-Object -Last 5
```
오류가 있으면 수정 후 재시도. 경고도 가능하면 제거한다.

### 4. 단위 테스트 작성
해당 단계에서 추가/변경된 내부 로직에 대해 Unit 테스트를 작성한다.
- 파일: `src/ConSight.DONGBO.DAQ.Tests/<ClassName>Tests.cs`
- `[Trait("Category","Unit")]` 필수
- 메서드명: `메서드_조건_기대결과`
- DB·네트워크 불필요

### 5. 테스트 실행
```powershell
dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit -v n
```
실패가 있으면 수정 후 재실행한다.

### 6. 결과 보고
다음을 출력한다:
- 구현 파일 수 + 목록
- 테스트 통과/실패 수
- 커밋 메시지 제안: `feat(<step>): 한 줄 설명`
