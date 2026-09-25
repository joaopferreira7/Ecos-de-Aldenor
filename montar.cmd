@echo off
REM ============================================================
REM  ECOS DE ALDENOR - gerar o jogo (para quem DESENVOLVE)
REM ============================================================
REM
REM  CLIQUE DUAS VEZES NESTE ARQUIVO (montar.cmd).
REM
REM  Nao clique no montar.ps1: o Windows associa .ps1 ao Bloco de
REM  Notas, entao o duplo clique abre o codigo do script num editor
REM  e nada acontece. Este .cmd existe para contornar isso - ele
REM  chama o PowerShell direito e ainda pula a execution policy.
REM
REM  Sem argumentos, faz o que quem da duplo clique quer: verifica,
REM  GERA o executavel e compacta o pacote de entrega. Antes ele
REM  rodava so a verificacao, e terminava com "Tudo certo" sem ter
REM  gerado jogo nenhum.
REM
REM  Com argumentos, repassa:   montar.cmd -SoTestes
REM
REM  PARA APENAS JOGAR voce nao precisa disto: peca o arquivo
REM  "Ecos de Aldenor - Windows.zip", extraia a pasta inteira e
REM  abra o "Ecos de Aldenor.exe" de dentro dela.
REM ============================================================

if "%~1"=="" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0montar.ps1" -Build -Zip
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0montar.ps1" %*
)

echo.
pause
