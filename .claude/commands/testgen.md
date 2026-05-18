마지막 커밋의 변경 파일을 읽고, 대응하는 xUnit 테스트를 작성한 뒤 실행한다.

## 절차

1. `git show --stat HEAD` — 변경된 `.cs` 파일 목록 확인
2. 변경 파일 읽기 — public·internal 시그니처, 경계 조건 파악
3. `src/ConSight.DONGBO.DAQ.Tests/<클래스명>Tests.cs` 작성
   - DB·네트워크 불필요: `[Trait("Category","Unit")]`
   - 메서드명: `메서드_조건_기대결과`
4. `dotnet test src/ConSight.DONGBO.DAQ.Tests --filter Category=Unit -v n`
5. 결과 보고: 테스트 수 + 통과/실패
