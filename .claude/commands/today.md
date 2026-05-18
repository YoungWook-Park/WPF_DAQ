작업 시작 전 오늘의 목표를 설정한다.

## 실행 절차

### 1. 현재 상태 파악

```powershell
# 오늘 날짜
Get-Date -Format "yyyy-MM-dd"

# 최근 커밋 이력 (오늘 + 어제)
git log --oneline --since="2 days ago" --format="%h %ad %s" --date=short

# 미커밋 변경 파일 (빌드 산출물 제외)
git status --short | Where-Object { $_ -notmatch '(bin|obj|\.vs)/' }

# 브랜치 확인
git branch --show-current
```

어제 devlog(`devlog/YYYY-MM-DD.md`)가 있으면 읽어 "다음 할 일" 섹션을 확인한다.

`docs/featureA-work-plan.md`를 읽어 미완료 Task(T1~T6)의 현황을 파악한다.

### 2. 오늘 목표 결정

위 정보를 바탕으로 다음을 사용자에게 제안한다:

- **어제 이어받을 작업**: devlog "다음 할 일" 또는 미커밋 변경 파일 기준
- **오늘 권장 Task**: featureA-work-plan.md 커밋 전략(C1→C7) 중 다음 단계
- **예상 소요 시간**: 파일 수 / 복잡도 기준 간략 추정

사용자가 목표를 확정하면 아래 형식으로 `devlog/YYYY-MM-DD.md` 상단에 오늘 계획 섹션을 추가(파일 없으면 신규 생성)한다.

```markdown
# YYYY-MM-DD 작업일지

## 오늘 목표
<!-- 확정된 Task 번호 + 한 줄 설명 -->

## 완료 항목
<!-- 작업 후 /devlog 실행 시 채움 -->

## 변경 파일
<!-- /devlog 실행 시 채움 -->

## 커밋
<!-- /devlog 실행 시 채움 -->

## 다음 할 일
<!-- /devlog 실행 시 채움 -->
```

### 3. 빌드 상태 확인 (선택)

```powershell
dotnet build src/ConSight.DONGBO.slnx --no-incremental -v q 2>&1 | Select-String -Pattern "error|warning|Build succeeded" | Select-Object -Last 5
```

빌드 오류가 있으면 작업 시작 전에 사용자에게 알린다.

### 4. 출력 요약

다음을 짧게 출력한다:
- 현재 브랜치
- 마지막 커밋 (hash + message)
- 미커밋 파일 수
- **오늘 목표** (확정된 Task)
- 다음 커밋 목표 (Cx)
