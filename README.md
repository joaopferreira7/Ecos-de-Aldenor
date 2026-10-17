# Ecos de Aldenor

Jogo 2D de plataforma e ação, de temática **gótica/espectral**, desenvolvido na disciplina
**Desenvolvimento de Jogos Digitais** (Ciência da Computação — trabalho em grupo P2).

O jogador controla **Ren**, que atravessa um cemitério assombrado coletando **fragmentos de alma**
até enfrentar o chefe final, **A Vigília**, dentro de uma igreja em ruínas.

> **Entrega/apresentação:** 18/11/2026 (via CONNECT+).

---

## 👥 Integrantes

<!-- Preencher com o nome completo (e RA/matrícula, se exigido) de cada integrante -->
- Gabriel Quirino Bressanin
- Guilherme Barros Oliveira
- Heitor Lupino
- João Pedro Ferreira
- Matheus Risso

---

## 🎮 Como jogar

| Ação | Tecla / Controle |
|------|------------------|
| Mover | `A` / `D` ou setas `←` `→` |
| Pular | `Espaço` |
| Atacar | Botão **esquerdo** do mouse |
| Esquivar (rolamento) | `Shift` ou botão **direito** do mouse |
| Pausar | `ESC` |

**Objetivo:** colete todos os **6 fragmentos de alma** ao longo das fases para liberar a fase final
e derrote o chefe **A Vigília**. Cuidado com quedas e com os inimigos — a barra de corações no topo
mostra sua vida, e os checkpoints (altares) restauram você ao longo do caminho.

Ao apertar **Jogar**, uma **tela de tutorial** resume esses controles e o objetivo antes da primeira
fase começar; o jogo só destrava quando você escolhe *Começar* (ou pressiona `Espaço`).

O **rolamento** dá alguns quadros de invulnerabilidade: é com ele que se sai da marca de impacto
d'A Vigília a tempo.

### Dificuldade

O Menu Principal tem três níveis, guardados entre partidas:

| Nível | O que muda |
|-------|------------|
| **Fácil** | 4 corações, invulnerabilidade mais longa após o dano, inimigos com 30% menos vida, mais lentos e batendo com menos frequência; os avisos do chefe duram 60% mais |
| **Médio** | A experiência original do jogo, sem nenhum multiplicador |
| **Difícil** | Inimigos com 35% mais vida, 20% mais rápidos, dano maior e menos tempo entre golpes; o chefe avisa 30% mais rápido, respira menos entre ataques e parte mais para o golpe forte |

Nenhum golpe mata com a vida cheia, em nenhum nível.

---

## 🗺️ Estrutura do jogo

O jogo tem **8 cenas** encadeadas:

`MainMenu → Tutorial → Phase1 → Phase2 → FinalPhase → Victory / GameOver / Credits`

- **Tutorial:** abre com a **tela de tutorial** (controles e objetivo) e depois entrega uma área
  segura, com placas reforçando cada comando no lugar onde ele é usado.
- **Phase1 / Phase2:** cemitério espectral, com espectros (*Sombra Rastejante*) e esqueletos (*Guardião de Pedra*).
- **FinalPhase:** arena na igreja, com o chefe **A Vigília** (barra de vida própria e fases de combate).
  Todo golpe dele é anunciado antes: o mago **conjura** (animação de 10 quadros), o corpo muda de cor
  e incha, e um indicador mostra onde vai bater — faixa âmbar à frente no golpe curto, marca vermelha
  no chão no golpe forte. No golpe forte uma **bola de fogo** sai das mãos dele e pousa na marca no
  instante exato em que o círculo se completa. Sair da marca a tempo faz o golpe errar.

---

## 🛠️ Tecnologias

- **Unity 6000.4.1f1** (Universal Render Pipeline — 2D, com iluminação 2D)
- **C#** — código em `Assets/_Project/Scripts/` (namespaces `EcosDeAldenor.*`),
  organizado com Singletons, eventos e princípios SOLID.

---

## ▶️ Como abrir e rodar (no editor)

1. Instale o **Unity Hub** e a versão **6000.4.1f1** (com módulo de build para *Windows*).
2. No Unity Hub: **Add → Add project from disk** e selecione a pasta do projeto
   (`Ecos de Aldenor`, a que contém as pastas `Assets/`, `Packages/` e `ProjectSettings/`).
3. Abra o projeto e carregue a cena `Assets/_Project/Scenes/MainMenu.unity`.
4. Pressione **Play** ▶️ para jogar no editor.

## 📦 Como gerar a build (executável)

> ⚠️ **Isto é para quem desenvolve o jogo.** Para alguém apenas **jogar**, não mande
> este repositório nem o `montar.ps1`: mande o **`Ecos de Aldenor - Windows.zip`**
> (veja *Como entregar/enviar a build*, logo abaixo). Construir o jogo do código exige
> o Unity 6000.4.1f1 instalado e licenciado (~10 GB), o **Git LFS** (827 arquivos de
> arte e áudio do projeto estão lá) e dezenas de minutos na primeira importação.

### Pelo terminal (recomendado)

Com o **Editor fechado**, na raiz do projeto:

```powershell
.\montar.ps1                # só verifica o projeto e as fases
.\montar.ps1 -Build         # verifica e gera o executável
.\montar.ps1 -Build -Zip    # idem, e ainda compacta o pacote de entrega
.\montar.ps1 -SoZip        # só compacta a Build\ gerada pelo menu do Editor
.\montar.ps1 -Diagnostico  # não constrói nada: lista o que a máquina tem e o que falta
```

> 🩺 **O script não funcionou no seu computador?** Rode `montar.cmd -Diagnostico`
> e mande a saída para o grupo: ela mostra versão do Windows e do PowerShell, se a
> pasta é um clone com a arte do LFS baixada, quais Editores do Unity o Hub conhece
> e qual seria usado, se há um Editor aberto no projeto e o espaço livre em disco.

Prefere duplo clique? Use o **`montar.cmd`** — ele verifica, gera o executável e
compacta o pacote, tudo de uma vez.

> ⚠️ **Não clique duas vezes no `montar.ps1`.** O Windows associa arquivos `.ps1` ao
> Bloco de Notas: o duplo clique abre o código do script num editor e **nada acontece**.
> É para isso que existe o `montar.cmd`.

**Antes do primeiro build em uma máquina nova**, garanta os arquivos do Git LFS:

```powershell
git lfs install
git lfs pull
```

Sem isso, cada PNG e cada WAV do projeto é um arquivo de texto de ~130 bytes
apontando para o conteúdo real — e o Unity constrói assim mesmo, **sem erro
nenhum**, gerando um jogo sem sprites e sem som. O `montar.ps1` detecta esse
estado e para antes de construir.

O script chama o Unity em modo batch e faz, nesta ordem: confere a lista de
cenas (8, com **MainMenu** em primeiro), roda o verificador de geometria das
fases (alcance de pulo, vãos, atores presos na rocha, patrulha válida), roda os
testes automatizados quando houver alguma suíte, e só então constrói. Qualquer
etapa que falhe interrompe a build e aponta o log em `Logs\`.

Saída: `Build\Ecos de Aldenor.exe` (a pasta é recriada do zero a cada build, para
não levar sobra da anterior no pacote da entrega).

Em outra máquina, o script acha o Editor sozinho: lê a versão em
`ProjectSettings/ProjectVersion.txt` e **pergunta ao próprio Unity Hub** onde cada
Editor está instalado (`Unity Hub.exe -- --headless editors -i`); sem o Hub, procura
nas pastas de instalação conhecidas. Se não houver a versão exata, só aceita uma
**mais nova** da mesma série — uma mais antiga não abre os pacotes do projeto — e
mostra o link `unityhub://` que instala a versão certa. Ainda dá para apontar na
mão com `-Unity "D:\...\Unity.exe"`. Quem preferir duplo clique: `montar.cmd` chama
o `montar.ps1` sem esbarrar na *execution policy* e mantém a janela aberta no fim.

**Por que o script falhava em alguns computadores** (corrigido):

| Sintoma | Causa | O que mudou |
|---|---|---|
| "Unity não encontrado", com ele instalado | Hub com a pasta de instalação trocada (outro disco): o script procurava em `<pasta>\Editor\<versão>`, mas o Hub instala em `<pasta>\<versão>` | a lista vem do próprio Hub |
| Erros de compilação na primeira abertura | outra versão do Unity aceita, até mais antiga (ex.: 6000.0 LTS) | só a exata ou uma mais nova; link de instalação |
| "Feche o Unity" com ele fechado | Unity encerrado à força deixa o `Temp\UnityLockfile` | testa se o arquivo está mesmo em uso |
| Jogo sem arte / "git lfs pull" não roda | projeto baixado pelo *Download ZIP* do GitHub (sem LFS e sem `.git`) | reconhece o caso e manda clonar |
| `.cmd` instável | `montar.cmd` estava salvo com quebra de linha LF | `.gitattributes` força CRLF em `.cmd`/`.ps1` |
| Só "veja o log" | — | lê o log e aponta licença, pacotes sem internet, compilação, versão, módulo ou disco |

O que o script **não** tem como resolver sozinho: Unity instalado sem licença ativa
(entre no Hub e ative a Personal), máquina sem internet na primeira abertura (os
pacotes vêm de `packages.unity.com`), e a cota gratuita de banda do Git LFS no
GitHub — cada clone baixa ~240 MB de arte e áudio.

### Pelo Editor

Menu **Ecos de Aldenor → Gerar build Windows** (mesmas verificações), ou
**Ecos de Aldenor → Verificar projeto** para só conferir.

O caminho manual por **File → Build Settings** também funciona, mas pula as
verificações.

### Como entregar/enviar a build ⚠️

**Envie sempre a pasta inteira, compactada — nunca só o `.exe`.**

O `Ecos de Aldenor.exe` tem **0,6 MB** de uma build de **141 MB**: ele é apenas o
carregador. Sem a pasta `Ecos de Aldenor_Data` (as 8 cenas e os assets), a
`UnityPlayer.dll` e a `MonoBleedingEdge` ao lado dele, ele **abre e fecha na hora** —
nem chega a escrever um log. Foi o que aconteceu quando a build foi enviada a colegas.

1. Gere a build (o comando já coloca o `LEIAME.txt` dentro dela e remove a pasta de
   símbolos do Burst, que o próprio Unity marca como *DoNotShip*).
2. Compacte a pasta `Build/` inteira em um `.zip` (~60 MB).
3. Quem receber deve **extrair tudo** e abrir o `.exe` de dentro da pasta extraída.

O `LEIAME-jogador.txt` na raiz do repositório é a fonte desse arquivo: ele explica
isso ao jogador, junto dos controles e do aviso do SmartScreen ("O Windows protegeu o
seu PC" → *Mais informações* → *Executar assim mesmo*, porque o executável não tem
assinatura digital paga).

O `.zip` pronto fica em `Entrega/`, que o `.gitignore` ignora.

### Compatibilidade

O jogo é construído para **Windows 64 bits** com **Direct3D 11** em primeiro lugar e
Direct3D 12 como reserva. A ordem importa: com o DX12 na frente — o padrão do Unity —
a inicialização falha em placas integradas com driver antigo, que é o caso comum de
notebook de laboratório.

---

## 🎨 Créditos e fontes dos assets

Todos os assets de terceiros são gratuitos. As atribuições exigidas por licença estão listadas
abaixo (e também na **tela de Créditos** dentro do jogo):

| Asset | Uso no jogo | Autor / Fonte | Licença |
|-------|-------------|---------------|---------|
| **Hero Knight – Pixel Art** | Personagem jogável (Ren) | **LuizMelo** — [luizmelo.itch.io](https://luizmelo.itch.io/hero-knight) | CC0 (crédito não exigido, mas apreciado) |
| **Bandits – Pixel Art** | Base dos prefabs de inimigos (estrutura e animações) | **Sven Thole** — [sventhole.itch.io](https://sventhole.itch.io/bandits) | livre para qualquer jogo, comercial ou não; **não** pode ser revendido como asset |
| **Gothicvania** (Church, Cemetery, Town) | Cenários, inimigos (fantasma, esqueleto) e o chefe (mago) | Luis Zuno "Ansimuz" — [ansimuz.itch.io](https://ansimuz.itch.io/) | CC0 |
| **Free 2D Dungeon Platformer Tilemap** (tiles, Free GameUI, CoinSystem) | Tiles, elementos de UI e trilhas das fases | **Aether2D** — [aether2d.itch.io](https://aether2d.itch.io/dungeon) | CC0 |
| **Music 1 / 2 / 3** (trilha das fases) | Música das fases, via pacote da Aether2D | Seth_Makes_Sounds — [freesound.org](https://freesound.org/people/Seth_Makes_Sounds/) | CC0 |
| **Epic Boss Battle** *(Seamlessly Looping)* | Trilha da luta contra A Vigília | Juhani Junkala (SubspaceAudio) — [opengameart.org](https://opengameart.org/content/boss-battle-music) | CC0 |
| **Brackeys 2D Mega Pack** | Elementos e decoração de cenário | Brackeys | gratuito / CC0 |
| **Hitspark FX** | Efeito de faísca ao acertar golpes | Jason Lee — [jasontomlee.itch.io](https://jasontomlee.itch.io/) | gratuito |
| **RPG Essentials SFX** | Efeitos sonoros | Leohpaz — [leohpaz.itch.io](https://leohpaz.itch.io/) | gratuito |
| **Música (vila / vitória)** | Trilha da tela de Vitória | Pascal Belisle | crédito exigido |
| **New Rocker** (fonte) | Títulos (menu, telas, nome do chefe) | Pablo Impallari — [Google Fonts](https://fonts.google.com/specimen/New+Rocker) | SIL OFL 1.1 (texto em `Assets/_Project/Resources/Fonts`) |
| **Pixelify Sans** (fonte) | Todos os demais textos e as placas do Tutorial | Stefie Justprince — [Google Fonts](https://fonts.google.com/specimen/Pixelify+Sans) | SIL OFL 1.1 (texto em `Assets/_Project/Resources/Fonts`) |

> Autoria conferida nas páginas oficiais em 30/08/2026. **Hero Knight e Bandits são de autores
> diferentes**: os dois já constaram como "Sven Thole", mas Hero Knight é do **LuizMelo** — dele é o
> sprite do Ren. A tela de Créditos dentro do jogo reflete exatamente esta tabela.

---

## 📁 Organização do projeto

```
Assets/
├── _Project/            # Código, prefabs e cenas próprias do jogo
│   ├── Scripts/         # C# (Core, Player, Enemies, Systems, UI)
│   ├── Prefabs/         # Player, inimigos, itens, UI, ambiente
│   └── Scenes/          # As 8 cenas do jogo, MainMenu inclusive
└── ...                  # Pacotes de assets de terceiros (ver Créditos)
```
