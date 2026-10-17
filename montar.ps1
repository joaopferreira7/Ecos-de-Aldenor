# Verifica o projeto, roda os testes e gera o executavel de "Ecos de Aldenor".
#
# ESTE SCRIPT E PARA QUEM DESENVOLVE O JOGO, NAO PARA QUEM QUER JOGAR.
#
# Ele nao baixa o jogo: ele CONSTROI o jogo a partir do codigo-fonte, e para
# isso a maquina precisa do Unity 6000.4.1f1 instalado e com licenca ativada
# (uns 10 GB), do Git LFS (os ~830 arquivos de arte e audio do projeto estao
# la, ~240 MB) e de dezenas de minutos na primeira importacao. Mandar este
# repositorio para alguem jogar nao funciona - e nao e para funcionar.
#
# PARA JOGAR: pegue o pacote pronto, "Ecos de Aldenor - Windows.zip",
# extraia a pasta inteira e abra o "Ecos de Aldenor.exe" de dentro dela.
# Nao e preciso instalar nada.
#
# Uso:  .\montar.ps1               (verifica e testa)
#       .\montar.ps1 -Build        (verifica, testa e gera o .exe)
#       .\montar.ps1 -Build -Zip   (idem, e ainda compacta o pacote de entrega)
#       .\montar.ps1 -SoZip        (so compacta a pasta Build\ que ja existe -
#                                   para quando a build foi gerada pelo menu
#                                   do Editor)
#       .\montar.ps1 -SoTestes     (so roda os testes)
#       .\montar.ps1 -Diagnostico  (NAO constroi nada: lista o que esta maquina
#                                   tem e o que falta. Se o script nao funcionar
#                                   no seu computador, rode isto e mande a saida)
#
# Em outra maquina:  -Unity "D:\...\Unity.exe"  aponta o Editor na mao, caso a
# busca automatica nao ache.
#
# O Unity so aceita uma instancia por projeto: feche o Editor antes de rodar,
# senao o batchmode morre reclamando de lock.
#
# POR QUE ELE FALHAVA EM ALGUNS COMPUTADORES (corrigido aqui):
#   - Unity instalado em outro disco pelo Hub ("Installs location" trocado nas
#     preferencias): o script procurava em <pasta>\Editor\<versao>, mas o Hub
#     instala direto em <pasta>\<versao>. Agora a lista vem do proprio Hub
#     (unityhub --headless editors -i), que sabe onde cada Editor esta.
#   - Versao diferente da do projeto: o script aceitava qualquer 6000.x, ate
#     uma MAIS ANTIGA (ex.: 6000.0 LTS), que nao abre os pacotes do projeto
#     (URP 17.4) e falha com erro de compilacao. Agora so aceita igual ou mais
#     nova, e mostra o link que instala a versao exata pelo Hub.
#   - Unity fechado a forca (travou, foi morto): sobrava o Temp\UnityLockfile
#     e o script dizia "feche o Unity" para sempre. Agora ele testa se o
#     arquivo esta mesmo em uso.
#   - Repositorio baixado pelo "Download ZIP" do GitHub: vem sem a arte (so os
#     ponteiros do LFS) e sem a pasta .git, entao "git lfs pull" nem roda. O
#     script agora reconhece esse caso e diz para clonar.
#   - Pasta do projeto com [ ] no nome: o PowerShell lia como curinga.
#   - Falha no Unity mostrava so "veja o log". Agora o script le o log e
#     aponta a causa conhecida (licenca, pacotes sem internet, erro de
#     compilacao, versao).

param(
    [switch]$Build,
    [switch]$SoTestes,
    [switch]$Zip,
    [switch]$SoZip,
    [switch]$Diagnostico,
    [string]$Unity
)

$ErrorActionPreference = "Stop"

$projeto = $PSScriptRoot
$logs = Join-Path $projeto "Logs"

# A versao sai do proprio projeto, nao de uma constante aqui: quem clonar em
# outra maquina precisa do Editor que o projeto pede, e esse arquivo e a fonte
# dessa informacao. A revisao (changeset) monta o link de instalacao do Hub.
$versao = "6000.4.1f1"
$revisao = $null
$arquivoVersao = Join-Path $projeto "ProjectSettings\ProjectVersion.txt"
if (Test-Path -LiteralPath $arquivoVersao) {
    $conteudo = Get-Content -LiteralPath $arquivoVersao -Raw
    $m = [regex]::Match($conteudo, "m_EditorVersion:\s*(\S+)")
    if ($m.Success) { $versao = $m.Groups[1].Value }
    $r = [regex]::Match($conteudo, "m_EditorVersionWithRevision:\s*\S+\s*\((\w+)\)")
    if ($r.Success) { $revisao = $r.Groups[1].Value }
}
$linkInstalar = if ($revisao) { "unityhub://$versao/$revisao" } else { $null }

# "6000.4.1f1" -> 6000.4.1 (para comparar). O sufixo f1/f2 nao muda o que o
# projeto aceita.
function NumeroDaVersao($texto) {
    $m = [regex]::Match($texto, "^(\d+)\.(\d+)\.(\d+)")
    if (-not $m.Success) { return $null }
    return [version]("{0}.{1}.{2}" -f $m.Groups[1].Value, $m.Groups[2].Value, $m.Groups[3].Value)
}

function AcharHub {
    $candidatos = @(
        "$env:ProgramFiles\Unity Hub\Unity Hub.exe",
        "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe",
        "${env:ProgramFiles(x86)}\Unity Hub\Unity Hub.exe"
    )
    foreach ($c in $candidatos) { if ($c -and (Test-Path -LiteralPath $c)) { return $c } }
    return $null
}

# Pergunta ao Hub quais Editores estao instalados e onde. E a unica fonte que
# acerta sempre: o Hub deixa instalar em qualquer pasta e "localizar" um
# Editor que ja estava no disco, e nenhuma lista fixa de pastas cobre isso.
$script:cacheDoHub = $null
function EditoresDoHub {
    if ($null -ne $script:cacheDoHub) { return $script:cacheDoHub }
    $lista = @()
    $hub = AcharHub
    if (-not $hub) { $script:cacheDoHub = $lista; return $lista }
    $base = Join-Path ([System.IO.Path]::GetTempPath()) ("EcosHub_" + [guid]::NewGuid().ToString("N"))
    $saida = "$base.txt"
    $erros = "$base.err"   # o Hub (Electron) despeja avisos de cache aqui; nao interessam
    try {
        $p = Start-Process -FilePath $hub -ArgumentList "--", "--headless", "editors", "-i" `
                 -RedirectStandardOutput $saida -RedirectStandardError $erros -PassThru -WindowStyle Hidden
        # O Hub e um app Electron: se travar, nao vale segurar o script.
        if (-not $p.WaitForExit(60000)) { try { $p.Kill() } catch {} ; return $lista }
        foreach ($linha in (Get-Content -LiteralPath $saida -ErrorAction SilentlyContinue)) {
            $m = [regex]::Match($linha, "^\s*(\d+\.\d+\.\d+\w*)\s*,?\s*installed at\s+(.+?Unity\.exe)\s*$")
            if ($m.Success) {
                $lista += [pscustomobject]@{ Versao = $m.Groups[1].Value; Exe = $m.Groups[2].Value.Trim() }
            }
        }
    } catch {
    } finally {
        Remove-Item -LiteralPath $saida, $erros -Force -ErrorAction SilentlyContinue
    }
    $script:cacheDoHub = $lista
    return $lista
}

# Lugares plausiveis, para quando o Hub nao esta instalado (Editor instalado
# avulso) ou nao responde.
function EditoresNasPastas {
    $raizes = @(
        "$env:ProgramFiles\Unity\Hub\Editor",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor",
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor"
    )

    # Pasta de instalacao trocada nas preferencias do Hub. Os Editores ficam
    # DIRETO nela (<pasta>\6000.4.1f1\Editor\Unity.exe). A versao antiga deste
    # script acrescentava um "\Editor" a mais e nunca achava nada ali.
    $secundario = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path -LiteralPath $secundario) {
        $caminho = (Get-Content -LiteralPath $secundario -Raw).Trim().Trim('"') -replace '\\\\', '\'
        if ($caminho) { $raizes += $caminho; $raizes += (Join-Path $caminho "Editor") }
    }

    # So discos locais conectados: uma unidade de rede desconectada faz cada
    # Test-Path esperar o timeout.
    foreach ($d in (Get-PSDrive -PSProvider FileSystem | Where-Object { $null -ne $_.Used })) {
        $raizes += "$($d.Root)Unity\Hub\Editor"
        $raizes += "$($d.Root)Program Files\Unity\Hub\Editor"
        $raizes += "$($d.Root)Unity"
    }

    $lista = @()
    foreach ($raiz in ($raizes | Where-Object { $_ } | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $raiz)) { continue }
        foreach ($dir in (Get-ChildItem -LiteralPath $raiz -Directory -ErrorAction SilentlyContinue)) {
            $exe = Join-Path $dir.FullName "Editor\Unity.exe"
            if ((NumeroDaVersao $dir.Name) -and (Test-Path -LiteralPath $exe)) {
                $lista += [pscustomobject]@{ Versao = $dir.Name; Exe = $exe }
            }
        }
    }
    return $lista
}

function AcharUnity($versao) {
    if ($Unity) { return $Unity }
    if ($env:UNITY_PATH) { return $env:UNITY_PATH }

    $editores = @(EditoresDoHub) + @(EditoresNasPastas)
    $exato = $editores | Where-Object { $_.Versao -eq $versao } | Select-Object -First 1
    if ($exato) { return $exato.Exe }

    # Sem a versao exata: so serve um Editor da mesma major e MAIS NOVO (ele
    # atualiza o projeto ao abrir). Um mais antigo nao tem as versoes de
    # pacote que o projeto usa e quebra na compilacao.
    $pedida = NumeroDaVersao $versao
    $major = $versao.Split('.')[0]
    $alt = $editores |
        Where-Object { $_.Versao.StartsWith("$major.") -and (NumeroDaVersao $_.Versao) -ge $pedida } |
        Sort-Object { NumeroDaVersao $_.Versao } |
        Select-Object -First 1
    if ($alt) {
        Write-Host "Unity $versao nao encontrado; usando o $($alt.Versao), mais novo:" -ForegroundColor Yellow
        Write-Host "  $($alt.Exe)"
        Write-Host "  Ele vai atualizar arquivos do projeto ao abrir - nao faca commit disso sem combinar com o grupo." -ForegroundColor Yellow
        return $alt.Exe
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
    $suspeitos = Get-ChildItem -LiteralPath (Join-Path $projeto "Assets") -Recurse -Include *.png,*.wav,*.mp3,*.ttf `
                     -File -ErrorAction SilentlyContinue |
                 Where-Object { $_.Length -lt 1kb } |
                 Select-Object -First 40

    foreach ($arq in $suspeitos) {
        $primeira = Get-Content -LiteralPath $arq.FullName -TotalCount 1 -ErrorAction SilentlyContinue
        if ($primeira -like "version https://git-lfs.github.com*") { return $arq }
    }
    return $null
}

function ExplicarLFS($ponteiro) {
    Write-Host ""
    Write-Host "Os arquivos de arte e audio nao foram baixados de verdade." -ForegroundColor Red
    Write-Host "Exemplo: $($ponteiro.Name) tem $($ponteiro.Length) bytes e e um ponteiro do Git LFS."
    Write-Host ""
    Write-Host "Construir assim gera um jogo sem sprites e sem som."
    if (-not (Test-Path -LiteralPath (Join-Path $projeto ".git"))) {
        # "Code > Download ZIP" do GitHub nao inclui o conteudo do LFS.
        Write-Host "Esta pasta nao e um clone do Git (sem .git): parece ter vindo do" -ForegroundColor Yellow
        Write-Host "'Download ZIP' do GitHub, que NAO traz a arte. Baixe com:" -ForegroundColor Yellow
        Write-Host "  git lfs install" -ForegroundColor Cyan
        Write-Host "  git clone https://github.com/joaopferreira7/Ecos-de-Aldenor.git" -ForegroundColor Cyan
    } else {
        Write-Host "Resolva com:"
        Write-Host "  git lfs install" -ForegroundColor Cyan
        Write-Host "  git lfs pull"    -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Se o 'git lfs pull' disser que o repositorio passou da cota (budget/quota)," -ForegroundColor Yellow
        Write-Host "o limite gratuito de banda do LFS no GitHub acabou neste mes: peca a pasta"
        Write-Host "do projeto a quem ja tem ou espere o limite renovar."
    }
    Write-Host ""
    Write-Host "(o Git LFS se instala em https://git-lfs.com)"
}

# O lockfile existe enquanto o Editor esta aberto neste projeto - mas tambem
# sobra quando o Unity foi fechado a forca. So o Editor vivo segura o arquivo
# aberto, entao tentar abrir com exclusividade separa um caso do outro.
function EditorAberto($projeto) {
    $lock = Join-Path $projeto "Temp\UnityLockfile"
    if (-not (Test-Path -LiteralPath $lock)) { return $false }
    try {
        $f = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $f.Close()
        return $false   # arquivo velho: ninguem esta usando
    } catch {
        return $true
    }
}

# Compacta a pasta da build no formato em que ela deve ser entregue: um zip que
# abre numa pasta unica "Ecos de Aldenor", com o executavel e o LEIAME dentro.
# Existe porque o passo de compactar era manual - e foi exatamente o passo que
# se perdeu quando a build foi enviada a colegas: mandou-se o .exe solto, que
# nao roda sem a pasta _Data e a UnityPlayer.dll ao lado.
function Empacotar($projeto) {
    $origem = Join-Path $projeto "Build"
    $saida  = Join-Path $projeto "Entrega"
    $zipFinal = Join-Path $saida "Ecos de Aldenor - Windows.zip"

    if (-not (Test-Path -LiteralPath (Join-Path $origem "Ecos de Aldenor.exe"))) {
        Write-Host "Nada em Build\ para empacotar (falta o Ecos de Aldenor.exe)." -ForegroundColor Yellow
        return $false
    }
    if (-not (Test-Path -LiteralPath $saida)) { New-Item -ItemType Directory $saida | Out-Null }

    # O zip precisa conter UMA pasta raiz, e nao os arquivos soltos: quem extrai
    # um zip de arquivos soltos na area de trabalho espalha 140 MB por ela e
    # depois nao sabe qual .exe abrir.
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("EcosPack_" + [guid]::NewGuid().ToString("N"))
    $pasta = Join-Path $temp "Ecos de Aldenor"
    New-Item -ItemType Directory $pasta -Force | Out-Null

    try {
        Get-ChildItem -LiteralPath $origem | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $pasta -Recurse -Force }
        if (Test-Path -LiteralPath $zipFinal) { Remove-Item -LiteralPath $zipFinal -Force }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $temp, $zipFinal, [System.IO.Compression.CompressionLevel]::Optimal, $false)

        $mb = [math]::Round((Get-Item -LiteralPath $zipFinal).Length / 1mb, 1)
        Write-Host ""
        Write-Host "Pacote de entrega: Entrega\Ecos de Aldenor - Windows.zip ($mb MB)" -ForegroundColor Green
        return $true
    }
    finally {
        if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

# Le o log do Unity e traduz as falhas conhecidas. "Veja o log" nao ajuda
# quem nunca abriu um log do Unity - e quase sempre e uma destas.
function ExplicarFalha($arquivoLog) {
    $log = Join-Path $logs $arquivoLog
    if (-not (Test-Path -LiteralPath $log)) {
        Write-Host "  O Unity nao chegou a escrever o log: ele nem abriu. Confira o caminho do Editor" -ForegroundColor Yellow
        Write-Host "  e rode .\montar.ps1 -Diagnostico"
        return
    }
    $texto = Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue
    if (-not $texto) { return }

    $causas = @(
        @{ p = "No valid Unity Editor license|License is not active|licens\w* (was )?not found|Licensing.*(error|fail)|entitlement";
           m = "LICENCA: o Unity desta maquina nao tem licenca ativa. Abra o Unity Hub, entre na sua conta" +
               " e ative a licenca Personal (Preferences > Licenses). Sem isso o modo batch nao roda." },
        @{ p = "Scripts have compiler errors|error CS\d{4}";
           m = "COMPILACAO: o codigo nao compilou nesta maquina. Se a versao do Unity e diferente de $versao," +
               " e quase certo que seja isso; senao, procure 'error CS' no log." },
        @{ p = "Cannot connect to|Unable to connect|Failed to resolve packages|An error occurred while resolving packages|ECONNREFUSED|ETIMEDOUT|getaddrinfo";
           m = "PACOTES: na primeira abertura o Unity baixa os pacotes do projeto (URP, 2D, Input System)" +
               " e precisou de internet. Conecte a maquina (rede de laboratorio as vezes bloqueia" +
               " packages.unity.com) e rode de novo." },
        @{ p = "It looks like another Unity instance is running|Multiple Unity instances cannot open the same project";
           m = "LOCK: outro Unity esta com este projeto aberto. Feche todos os Editores (e o Unity Hub, se" +
               " ele abriu o projeto) e rode de novo." },
        @{ p = "Unity Editor version mismatch|project was last saved with a (newer|different) version";
           m = "VERSAO: este Editor e de outra versao. Instale a $versao pelo Hub" +
               $(if ($linkInstalar) { " (cole no navegador: $linkInstalar)." } else { "." }) },
        @{ p = "Switching to .* is disabled|Build target .* not supported|module.*not (installed|loaded)|BuildTarget.*not available";
           m = "MODULO: falta o 'Windows Build Support' no Editor. No Hub: Installs > engrenagem > Add modules." },
        @{ p = "There is not enough space on the disk|No space left";
           m = "DISCO: acabou o espaco. A primeira importacao precisa de alguns GB livres." }
    )
    $achou = $false
    foreach ($c in $causas) {
        if ($texto -match $c.p) {
            Write-Host ""
            Write-Host ("  " + $c.m) -ForegroundColor Yellow
            $achou = $true
        }
    }
    if (-not $achou) {
        Write-Host "  Causa nao reconhecida. As ultimas linhas com 'error' no log:" -ForegroundColor Yellow
        Select-String -LiteralPath $log -Pattern "error|exception|failed" -SimpleMatch:$false |
            Select-Object -Last 8 | ForEach-Object { Write-Host ("    " + $_.Line.Trim()) }
    }
}

# Start-Process com -Wait devolve o codigo de saida - mas em algumas maquinas o
# PowerShell 5.1 devolve $null, e o script tratava isso como falha. Na duvida,
# vale o que o proprio Unity escreveu no fim do log.
function Executar($titulo, $argumentos, $arquivoLog) {
    Write-Host ""
    Write-Host "=== $titulo ===" -ForegroundColor Cyan
    Write-Host "  (na primeira vez numa maquina o Unity importa o projeto inteiro: pode levar 10-30 min)" -ForegroundColor DarkGray
    $p = Start-Process -FilePath $unity -ArgumentList $argumentos -PassThru -NoNewWindow
    $null = $p.Handle   # garante que o codigo de saida fique disponivel
    $p.WaitForExit()
    $codigo = $p.ExitCode
    if ($null -eq $codigo) {
        $log = Join-Path $logs $arquivoLog
        $ok = (Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -Pattern "Exiting batchmode successfully" -Quiet)
        $codigo = if ($ok) { 0 } else { 1 }
    }
    Write-Host "  codigo de saida: $codigo   log: Logs\$arquivoLog"
    return $codigo
}

# --------------------------------------------------------------------------
# Diagnostico: o que esta maquina tem
# --------------------------------------------------------------------------
if ($Diagnostico) {
    Write-Host ""
    Write-Host "=== Diagnostico do montar.ps1 ===" -ForegroundColor Cyan
    Write-Host ("  Windows ............ " + [System.Environment]::OSVersion.VersionString)
    Write-Host ("  PowerShell ......... " + $PSVersionTable.PSVersion)
    Write-Host ("  Execution policy ... " + (Get-ExecutionPolicy) + "  (pelo montar.cmd isso nao importa, salvo politica de grupo: " + (Get-ExecutionPolicy -Scope MachinePolicy) + ")")
    Write-Host ("  Pasta do projeto ... " + $projeto)
    Write-Host ("  Clone do Git ....... " + $(if (Test-Path -LiteralPath (Join-Path $projeto ".git")) { "sim" } else { "NAO (veio de um zip?)" }))
    $git = Get-Command git -ErrorAction SilentlyContinue
    Write-Host ("  git ................ " + $(if ($git) { (& git --version) } else { "NAO instalado" }))
    $lfsOk = $false
    if ($git) { try { $v = & git lfs version 2>$null; if ($LASTEXITCODE -eq 0) { $lfsOk = $true; Write-Host "  git lfs ............ $v" } } catch {} }
    if (-not $lfsOk) { Write-Host "  git lfs ............ NAO instalado" -ForegroundColor Yellow }
    $pont = ConferirLFS $projeto
    Write-Host ("  Arte baixada ....... " + $(if ($pont) { "NAO - $($pont.Name) e ponteiro do LFS" } else { "sim" }))
    Write-Host ("  Versao pedida ...... " + $versao + $(if ($linkInstalar) { "   (instalar: $linkInstalar)" } else { "" }))
    $hub = AcharHub
    Write-Host ("  Unity Hub .......... " + $(if ($hub) { $hub } else { "nao encontrado" }))
    $editores = @(@(EditoresDoHub) + @(EditoresNasPastas) | Sort-Object Exe -Unique)
    if ($editores.Count -eq 0) { Write-Host "  Editores .......... nenhum encontrado" -ForegroundColor Yellow }
    foreach ($e in $editores) { Write-Host ("  Editor ............. {0,-12} {1}" -f $e.Versao, $e.Exe) }
    $escolhido = AcharUnity $versao
    Write-Host ("  Editor escolhido ... " + $(if ($escolhido) { $escolhido } else { "NENHUM serve" }))
    Write-Host ("  Editor aberto ...... " + $(if (EditorAberto $projeto) { "sim - feche antes de montar" } else { "nao" }))
    $livre = try { (Get-PSDrive -Name ($projeto.Substring(0, 1)) -ErrorAction Stop).Free } catch { 0 }
    Write-Host ("  Espaco livre ....... {0:N1} GB" -f ($livre / 1gb))
    Write-Host ""
    exit 0
}

# Para quando a build foi gerada pelo menu do Editor: so empacota, sem abrir
# outro Unity (o que nem daria, com o Editor aberto no projeto).
if ($SoZip) {
    if (Empacotar $projeto) { exit 0 } else { exit 1 }
}

$ponteiro = ConferirLFS $projeto
if ($ponteiro) { ExplicarLFS $ponteiro; exit 1 }

$unity = AcharUnity $versao

if (-not $unity -or -not (Test-Path -LiteralPath $unity)) {
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
    if ($linkInstalar) {
        Write-Host "Com o Hub instalado, este link abre a instalacao da versao exata:"
        Write-Host "  $linkInstalar" -ForegroundColor Cyan
    }
    Write-Host "Se o Editor ja estiver instalado num lugar incomum, aponte na mao:"
    Write-Host "  .\montar.ps1 -Unity `"D:\Unity\Hub\Editor\$versao\Editor\Unity.exe`""
    Write-Host ""
    Write-Host "Para ver tudo o que esta maquina tem:  .\montar.cmd -Diagnostico"
    exit 1
}

if (EditorAberto $projeto) {
    Write-Host "O Editor esta aberto neste projeto (Temp\UnityLockfile em uso)." -ForegroundColor Yellow
    Write-Host "Feche o Unity antes de rodar este script - ou, com ele aberto, gere a build"
    Write-Host "pelo menu 'Ecos de Aldenor > Gerar build Windows' e depois rode:  .\montar.cmd -SoZip"
    exit 1
}

if (-not (Test-Path -LiteralPath $logs)) { New-Item -ItemType Directory $logs | Out-Null }

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
        ExplicarFalha "verificar.log"
        exit 1
    }
}

# --- testes ---
# O projeto ainda nao tem suites automatizadas. Subir o Unity duas vezes para
# rodar zero teste custa minutos e nao diz nada, entao o script procura por
# assemblies de teste e so paga esse custo quando existir alguma.
$temTestes = $false
Get-ChildItem -LiteralPath (Join-Path $projeto "Assets") -Filter *.asmdef -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
        if ((Get-Content -LiteralPath $_.FullName -Raw) -match "UnityEngine\.TestRunner") { $temTestes = $true }
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
        if (Test-Path -LiteralPath $xml) {
            [xml]$r = Get-Content -LiteralPath $xml
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

        if ($Zip) { $null = Empacotar $projeto }
    } else {
        Write-Host "Falhou ao gerar a build. Veja Logs\build.log" -ForegroundColor Red
        ExplicarFalha "build.log"
        $falhou = $true
    }
}

if ($falhou) {
    Write-Host ""
    Write-Host "Algo falhou. Veja os logs em Logs\ - ou rode .\montar.cmd -Diagnostico e mande a saida." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Tudo certo." -ForegroundColor Green
