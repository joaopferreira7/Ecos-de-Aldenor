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

### Pelo terminal (recomendado)

Com o **Editor fechado**, na raiz do projeto:

```powershell
.\montar.ps1           # só verifica o projeto e as fases
.\montar.ps1 -Build    # verifica e gera o executável
```

O script chama o Unity em modo batch e faz, nesta ordem: confere a lista de
cenas (8, com **MainMenu** em primeiro), roda o verificador de geometria das
fases (alcance de pulo, vãos, atores presos na rocha, patrulha válida), roda os
testes automatizados quando houver alguma suíte, e só então constrói. Qualquer
etapa que falhe interrompe a build e aponta o log em `Logs\`.

Saída: `Build\Ecos de Aldenor.exe` (a pasta é recriada do zero a cada build, para
não levar sobra da anterior no pacote da entrega).

Em outra máquina, o script acha o Editor sozinho: lê a versão em
`ProjectSettings/ProjectVersion.txt` e procura nos caminhos do Unity Hub, em
todos os drives. Se o Hub estiver num lugar incomum, aponte na mão com
`-Unity "D:\...\Unity.exe"`. Quem preferir duplo clique: `montar.cmd` chama o
`montar.ps1` sem esbarrar na *execution policy* e mantém a janela aberta no fim.

### Pelo Editor

Menu **Ecos de Aldenor → Gerar build Windows** (mesmas verificações), ou
**Ecos de Aldenor → Verificar projeto** para só conferir.

O caminho manual por **File → Build Settings** também funciona, mas pula as
verificações.

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
