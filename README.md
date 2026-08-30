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
| Pausar | `ESC` |

**Objetivo:** colete todos os **6 fragmentos de alma** ao longo das fases para liberar a fase final
e derrote o chefe **A Vigília**. Cuidado com quedas e com os inimigos — a barra de corações no topo
mostra sua vida, e os checkpoints (altares) restauram você ao longo do caminho.

---

## 🗺️ Estrutura do jogo

O jogo tem **8 cenas** encadeadas:

`MainMenu → Tutorial → Phase1 → Phase2 → FinalPhase → Victory / GameOver / Credits`

- **Tutorial:** área segura com placas ensinando os controles.
- **Phase1 / Phase2:** cemitério espectral, com espectros (*Sombra Rastejante*) e esqueletos (*Guardião de Pedra*).
- **FinalPhase:** arena na igreja, com o chefe **A Vigília** (barra de vida própria e fases de combate).

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

1. Menu **File → Build Settings**.
2. Confirme que as 8 cenas estão listadas em **Scenes In Build**, com **MainMenu** em primeiro.
3. Plataforma **Windows** → **Build** e escolha uma pasta de saída.
4. O executável gerado (`.exe`) roda o jogo de forma independente.

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
