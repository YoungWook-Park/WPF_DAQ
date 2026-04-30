@echo off
:: ============================================================
:: EMPG 테스트 데이터 INSERT - 윈도우 스케줄러 작업용
:: 실행 위치: 이 .bat 파일이 있는 폴더에서 실행하거나
::             스케줄러 [시작 위치]를 이 폴더로 설정.
:: ============================================================
setlocal

:: ── 설정 ──────────────────────────────────────────────────
set SERVER=.\SQLEXPRESS
set DATABASE=DB_eM
set SQL_FILE=%~dp0\05_mock_insert_empg.sql
set LOG_FILE=%~dp0\mock_insert_empg.log

:: ── 실행 ──────────────────────────────────────────────────
echo [%DATE% %TIME%] INSERT 시작 >> "%LOG_FILE%"

sqlcmd -S "%SERVER%" -d "%DATABASE%" -E -No -i "%SQL_FILE%" >> "%LOG_FILE%" 2>&1

if %ERRORLEVEL% EQU 0 (
    echo [%DATE% %TIME%] INSERT 성공 >> "%LOG_FILE%"
) else (
    echo [%DATE% %TIME%] INSERT 실패 (ERRORLEVEL=%ERRORLEVEL%^) >> "%LOG_FILE%"
)

endlocal
