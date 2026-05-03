오늘 작업일지를 작성하고 GitHub에 push한다.

## 실행 절차

### 1. 컨텍스트 수집
다음 명령으로 오늘 변경된 소스 파일(bin/obj/.vs 제외)과 커밋 이력을 수집한다.

```powershell
# 오늘 날짜
Get-Date -Format "yyyy-MM-dd"

# 마지막 커밋 이후 변경된 소스 파일 (빌드 산출물 제외)
git diff HEAD --name-only | Where-Object { $_ -notmatch '^src/(Bi\.ConSight\.SqlAgent|ConSight\.DONGBO\.DAQ)/(bin|obj)/' -and $_ -notmatch '^src/\.vs/' }

# 오늘 커밋 이력 (없으면 최근 5개)
git log --oneline --since="midnight" --format="%h %s"
# 오늘 커밋이 없으면:
git log --oneline -5

# 미추적/변경 소스 파일
git status --short | Where-Object { $_ -notmatch '(bin|obj|\.vs)/' }
```

PLAN.md와 PROGRESS.md도 읽어서 현재 진행 단계를 파악한다.

### 2. devlog 파일 존재 확인
`devlog/YYYY-MM-DD.md` 파일이 이미 존재하면 덮어쓰지 않고 **내용을 추가(append)**한다.

### 3. 파일 작성
`devlog/YYYY-MM-DD.md`를 아래 형식으로 작성한다.

```markdown
# YYYY-MM-DD 작업일지

## 완료 항목
<!-- 오늘 완료한 작업을 bullet로 정리. git diff 변경 파일과 PROGRESS.md 기준 -->

## 변경 파일
<!-- 소스 파일만 나열. bin/obj/.vs 제외 -->

## 커밋
<!-- git log 결과 hash + message -->

## 다음 할 일
<!-- PLAN.md 미완료 항목 중 가장 가까운 것 1~3개 -->
```

### 4. Git commit & push
```powershell
git add devlog/ .claude/ CLAUDE.md
git commit -m "devlog: YYYY-MM-DD 작업일지"
git push
```

push 전 원격 브랜치 상태를 확인하고, 문제가 있으면 사용자에게 알린다.
push 완료 후 GitHub 브랜치 URL을 출력한다.
