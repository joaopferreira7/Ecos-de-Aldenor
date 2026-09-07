@echo off
REM Clicar duas vezes num .ps1 abre o Bloco de Notas; este .cmd existe para
REM dar o duplo clique e cair direto no montar.ps1, sem mexer em execution policy.
REM Argumentos passam adiante:  montar.cmd -Build
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0montar.ps1" %*
echo.
pause
