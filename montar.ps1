# Verifica o projeto, roda os testes e gera o executavel de "Ecos de Aldenor".
# Uso:  .\montar.ps1            (verifica e testa)
#       .\montar.ps1 -Build     (verifica, testa e gera o .exe)
#       .\montar.ps1 -SoTestes  (so roda os testes)
#
# O Unity so aceita uma instancia por projeto: feche o Editor antes de rodar,
# senao o batchmode morre reclamando de lock.

param(
    [switch]$Build,
    [switch]$SoTestes
)

$ErrorActionPreference = "Stop"

$unity = "C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe"
$projeto = $PSScriptRoot
$logs = Join-Path $projeto "Logs"

if (-not (Test-Path $unity)) {
    Write-Host "Unity 6000.4.1f1 nao encontrado em:" -ForegroundColor Red
    Write-Host "  $unity"
    Write-Host "Ajuste a variavel `$unity no topo deste script."
    exit 1
}

# O lockfile fica enquanto o Editor esta aberto neste projeto. Avisar aqui e
# mais claro do que deixar o Unity falhar la na frente com log enigmatico.
if (Test-Path (Join-Path $projeto "Temp\UnityLockfile")) {
    Write-Host "O Editor parece estar aberto neste projeto (Temp\UnityLockfile)." -ForegroundColor Yellow
    Write-Host "Feche o Unity antes de rodar este script."
    exit 1
}

if (-not (Test-Path $logs)) { New-Item -ItemType Directory $logs | Out-Null }

function Executar($titulo, $argumentos, $arquivoLog) {
    Write-Host ""
    Write-Host "=== $titulo ===" -ForegroundColor Cyan
    $p = Start-Process -FilePath $unity -ArgumentList $argumentos -PassThru -Wait -NoNewWindow
    Write-Host "  codigo de saida: $($p.ExitCode)   log: Logs\$arquivoLog"
    return $p.ExitCode
}

$falhou = $false

# --- verificacao ---
# Nao e so "compila?": BuildEntrega.Verificar confere a lista de cenas (8, com o
# MainMenu na frente) e roda o verificador de geometria das fases, que mede
# alcance de pulo, vaos, atores presos na rocha e patrulha invalida. Esses erros
# nao aparecem na build; aparecem jogando, tarde demais.
if (-not $SoTestes) {
    $argsVerif = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", "`"$projeto`"",
        "-executeMethod", "EcosDeAldenor.EditorTools.BuildEntrega.Verificar",
        "-logFile", "`"$logs\verificar.log`""
    )
    if ((Executar "Verificando projeto e fases" $argsVerif "verificar.log") -ne 0) {
        Write-Host "Verificacao falhou. Veja Logs\verificar.log" -ForegroundColor Red
        exit 1
    }
}

# --- testes ---
# O projeto ainda nao tem suites automatizadas. Subir o Unity duas vezes para
# rodar zero teste custa minutos e nao diz nada, entao o script procura por
# assemblies de teste e so paga esse custo quando existir alguma.
$temTestes = $false
Get-ChildItem -Path (Join-Path $projeto "Assets") -Filter *.asmdef -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
        if ((Get-Content $_.FullName -Raw) -match "UnityEngine\.TestRunner") { $temTestes = $true }
    }

if ($temTestes) {
    $argsEdit = @(
        "-batchmode", "-nographics",
        "-projectPath", "`"$projeto`"",
        "-runTests", "-testPlatform", "EditMode",
        "-testResults", "`"$logs\resultado-editmode.xml`"",
        "-logFile", "`"$logs\testes-editmode.log`""
    )
    if ((Executar "Testes EditMode" $argsEdit "testes-editmode.log") -ne 0) { $falhou = $true }

    # Sem "-nographics": os testes de PlayMode carregam as cenas de verdade, com
    # Light2D da URP e captura de camera. Sem contexto grafico o processo cai no
    # meio da suite, e o resultado parece falha de teste quando e de ambiente.
    $argsPlay = @(
        "-batchmode",
        "-projectPath", "`"$projeto`"",
        "-runTests", "-testPlatform", "PlayMode",
        "-testResults", "`"$logs\resultado-playmode.xml`"",
        "-logFile", "`"$logs\testes-playmode.log`""
    )
    if ((Executar "Testes PlayMode" $argsPlay "testes-playmode.log") -ne 0) { $falhou = $true }

    # --- resumo dos resultados ---
    Write-Host ""
    Write-Host "=== Resumo ===" -ForegroundColor Cyan
    foreach ($nome in @("editmode", "playmode")) {
        $xml = Join-Path $logs "resultado-$nome.xml"
        if (Test-Path $xml) {
            [xml]$r = Get-Content $xml
            $t = $r.'test-run'
            Write-Host ("  {0,-9} total={1}  passou={2}  falhou={3}  pulou={4}" -f `
                $nome, $t.total, $t.passed, $t.failed, $t.skipped)

            if ([int]$t.failed -gt 0) {
                $r.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
                    Write-Host ("     X {0}" -f $_.fullname) -ForegroundColor Red
                }
            }
        } else {
            Write-Host "  $nome : sem arquivo de resultado" -ForegroundColor Yellow
        }
    }
} elseif (-not $SoTestes) {
    Write-Host ""
    Write-Host "Sem suites de teste no projeto: a validacao automatica e a verificacao de fases acima." -ForegroundColor Yellow
} else {
    Write-Host "Sem suites de teste no projeto - nada a rodar." -ForegroundColor Yellow
}

if ($Build -and -not $falhou) {
    $argsBuild = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", "`"$projeto`"",
        "-executeMethod", "EcosDeAldenor.EditorTools.BuildEntrega.GerarExecutavel",
        "-logFile", "`"$logs\build.log`""
    )
    if ((Executar "Gerando executavel" $argsBuild "build.log") -eq 0) {
        Write-Host ""
        Write-Host "Pronto: Build\Ecos de Aldenor.exe" -ForegroundColor Green
    } else {
        Write-Host "Falhou ao gerar a build. Veja Logs\build.log" -ForegroundColor Red
        $falhou = $true
    }
}

if ($falhou) {
    Write-Host ""
    Write-Host "Algo falhou. Veja os logs em Logs\." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Tudo certo." -ForegroundColor Green
