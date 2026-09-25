# Verifica o projeto, roda os testes e gera o executavel de "Ecos de Aldenor".
#
# ESTE SCRIPT E PARA QUEM DESENVOLVE O JOGO, NAO PARA QUEM QUER JOGAR.
#
# Ele nao baixa o jogo: ele CONSTROI o jogo a partir do codigo-fonte, e para
# isso a maquina precisa do Unity 6000.4.1f1 instalado e com licenca ativada
# (uns 10 GB), do Git LFS (827 arquivos de arte e audio do projeto estao la) e
# de dezenas de minutos na primeira importacao. Mandar este repositorio para
# alguem jogar nao funciona - e nao e para funcionar.
#
# PARA JOGAR: pegue o pacote pronto, "Ecos de Aldenor - Windows.zip",
# extraia a pasta inteira e abra o "Ecos de Aldenor.exe" de dentro dela.
# Nao e preciso instalar nada.
#
# Uso:  .\montar.ps1              (verifica e testa)
#       .\montar.ps1 -Build       (verifica, testa e gera o .exe)
#       .\montar.ps1 -Build -Zip  (idem, e ainda compacta o pacote de entrega)
#       .\montar.ps1 -SoTestes    (so roda os testes)
#
# Em outra maquina:  -Unity "D:\...\Unity.exe"  aponta o Editor na mao, caso a
# busca automatica nao ache (Hub instalado num lugar incomum, por exemplo).
#
# O Unity so aceita uma instancia por projeto: feche o Editor antes de rodar,
# senao o batchmode morre reclamando de lock.

param(
    [switch]$Build,
    [switch]$SoTestes,
    [switch]$Zip,
    [string]$Unity
)

$ErrorActionPreference = "Stop"

$projeto = $PSScriptRoot
$logs = Join-Path $projeto "Logs"

# A versao sai do proprio projeto, nao de uma constante aqui: quem clonar em
# outra maquina precisa do Editor que o projeto pede, e esse arquivo e a fonte
# dessa informacao.
$versao = "6000.4.1f1"
$arquivoVersao = Join-Path $projeto "ProjectSettings\ProjectVersion.txt"
if (Test-Path $arquivoVersao) {
    $m = [regex]::Match((Get-Content $arquivoVersao -Raw), "m_EditorVersion:\s*(\S+)")
    if ($m.Success) { $versao = $m.Groups[1].Value }
}

# O Hub instala em qualquer drive e aceita um caminho secundario configurado
# pelo usuario. Procurar nos lugares plausiveis evita ter que editar o script
# a cada maquina nova.
function AcharUnity($versao) {
    if ($Unity) { return $Unity }
    if ($env:UNITY_PATH) { return $env:UNITY_PATH }

    $raizes = @(
        "$env:ProgramFiles\Unity\Hub\Editor",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor",
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor"
    )

    $secundario = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $secundario) {
        $caminho = (Get-Content $secundario -Raw).Trim().Trim('"')
        if ($caminho) { $raizes += (Join-Path $caminho "Editor") }
    }

    foreach ($letra in (Get-PSDrive -PSProvider FileSystem).Name) {
        $raizes += "${letra}:\Unity\Hub\Editor"
        $raizes += "${letra}:\Program Files\Unity\Hub\Editor"
    }

    $raizes = $raizes | Where-Object { $_ } | Select-Object -Unique

    foreach ($raiz in $raizes) {
        $exe = Join-Path $raiz "$versao\Editor\Unity.exe"
        if (Test-Path $exe) { return $exe }
    }

    # Sem a versao exata: outro Editor da mesma major costuma abrir o projeto
    # (fazendo upgrade dos assets), entao vale avisar e usar em vez de so falhar.
    $major = $versao.Split('.')[0]
    foreach ($raiz in $raizes) {
        if (-not (Test-Path $raiz)) { continue }
        $alt = Get-ChildItem $raiz -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name.StartsWith("$major.") } |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($alt) {
            Write-Host "Unity $versao nao encontrado; usando $alt" -ForegroundColor Yellow
            return $alt
        }
    }

    return $null
}

# Os arquivos de arte e audio ficam em Git LFS. Quem clona sem o LFS instalado
# recebe, no lugar de cada PNG e cada WAV, um arquivo de texto de ~130 bytes
# apontando para o conteudo real. O Unity importa esses arquivos como texturas
# quebradas e a build SAI, sem erro nenhum - so que sem sprite, sem cenario e
# sem som. Falhar aqui, com a instrucao do que fazer, e muito melhor do que
# entregar um jogo invisivel.
function ConferirLFS($projeto) {
    $suspeitos = Get-ChildItem (Join-Path $projeto "Assets") -Recurse -Include *.png,*.wav,*.mp3 `
                     -File -ErrorAction SilentlyContinue |
                 Where-Object { $_.Length -lt 1kb } |
                 Select-Object -First 40

    foreach ($arq in $suspeitos) {
        $primeira = Get-Content $arq.FullName -TotalCount 1 -ErrorAction SilentlyContinue
        if ($primeira -like "version https://git-lfs.github.com*") { return $arq }
    }
    return $null
}

$ponteiro = ConferirLFS $projeto
if ($ponteiro) {
    Write-Host ""
    Write-Host "Os arquivos de arte e audio nao foram baixados de verdade." -ForegroundColor Red
    Write-Host "Exemplo: $($ponteiro.Name) tem $($ponteiro.Length) bytes e e um ponteiro do Git LFS."
    Write-Host ""
    Write-Host "Construir assim gera um jogo sem sprites e sem som. Resolva com:"
    Write-Host "  git lfs install" -ForegroundColor Cyan
    Write-Host "  git lfs pull"    -ForegroundColor Cyan
    Write-Host ""
    Write-Host "(o Git LFS se instala em https://git-lfs.com)"
    exit 1
}

$unity = AcharUnity $versao

if (-not $unity -or -not (Test-Path $unity)) {
    Write-Host ""
    Write-Host "Unity $versao nao encontrado nesta maquina." -ForegroundColor Red
    Write-Host ""
    Write-Host "SE VOCE SO QUER JOGAR, nao precisa de nada disto:" -ForegroundColor Yellow
    Write-Host "  peca o 'Ecos de Aldenor - Windows.zip', extraia a pasta inteira"
    Write-Host "  e abra o 'Ecos de Aldenor.exe' de dentro dela."
    Write-Host ""
    Write-Host "Para CONSTRUIR o jogo a partir do codigo, instale o Unity $versao"
    Write-Host "pelo Unity Hub (com Windows Build Support) e entre na sua conta Unity"
    Write-Host "para ativar a licenca - sem licenca ativa o modo batch nao roda."
    Write-Host "Se o Editor ja estiver instalado num lugar incomum, aponte na mao:"
    Write-Host "  .\montar.ps1 -Unity `"D:\Unity\Hub\Editor\$versao\Editor\Unity.exe`""
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

# Compacta a pasta da build no formato em que ela deve ser entregue: um zip que
# abre numa pasta unica "Ecos de Aldenor", com o executavel e o LEIAME dentro.
# Existe porque o passo de compactar era manual - e foi exatamente o passo que
# se perdeu quando a build foi enviada a colegas: mandou-se o .exe solto, que
# nao roda sem a pasta _Data e a UnityPlayer.dll ao lado.
function Empacotar($projeto) {
    $origem = Join-Path $projeto "Build"
    $saida  = Join-Path $projeto "Entrega"
    $zipFinal = Join-Path $saida "Ecos de Aldenor - Windows.zip"

    if (-not (Test-Path $origem)) { Write-Host "Nada em Build\ para empacotar." -ForegroundColor Yellow; return }
    if (-not (Test-Path $saida)) { New-Item -ItemType Directory $saida | Out-Null }

    # O zip precisa conter UMA pasta raiz, e nao os arquivos soltos: quem extrai
    # um zip de arquivos soltos na area de trabalho espalha 141 MB por ela e
    # depois nao sabe qual .exe abrir.
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("EcosPack_" + [guid]::NewGuid().ToString("N"))
    $pasta = Join-Path $temp "Ecos de Aldenor"
    New-Item -ItemType Directory $pasta -Force | Out-Null

    try {
        Get-ChildItem $origem | ForEach-Object { Copy-Item $_.FullName -Destination $pasta -Recurse -Force }
        if (Test-Path $zipFinal) { Remove-Item -LiteralPath $zipFinal -Force }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $temp, $zipFinal, [System.IO.Compression.CompressionLevel]::Optimal, $false)

        $mb = [math]::Round((Get-Item $zipFinal).Length / 1mb, 1)
        Write-Host ""
        Write-Host "Pacote de entrega: Entrega\Ecos de Aldenor - Windows.zip ($mb MB)" -ForegroundColor Green
    }
    finally {
        if (Test-Path $temp) { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

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
        Write-Host "Para ENVIAR o jogo, mande a pasta Build inteira compactada." -ForegroundColor Yellow
        Write-Host "O .exe sozinho tem menos de 1 MB e nao roda: ele e so o carregador."

        if ($Zip) { Empacotar $projeto }
    } else {
        Write-Host "Falhou ao gerar a build. Veja Logs\build.log" -ForegroundColor Red

        # A causa mais comum em maquina nova nao aparece como erro de compilacao:
        # o Unity sobe, nao acha licenca ativa e para. Vale apontar o dedo.
        $log = Join-Path $logs "build.log"
        if ((Test-Path $log) -and (Select-String -Path $log -Pattern "License|Licens" -Quiet)) {
            Write-Host ""
            Write-Host "O log menciona licenca: entre na sua conta pelo Unity Hub e" -ForegroundColor Yellow
            Write-Host "ative a licenca Personal antes de rodar o script de novo."
        }
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
