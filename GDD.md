# Game Design Document — *Ecos de Aldenor*

> Documento de design do jogo. Descreve a visão, as mecânicas e os sistemas
> **conforme implementados** no projeto, servindo de referência para o grupo e
> para a entrega da disciplina.

| | |
|---|---|
| **Título** | Ecos de Aldenor |
| **Gênero** | Plataforma de ação 2D (*action-platformer*) |
| **Tema** | Gótico / espectral (*dark gothic*) |
| **Plataforma** | PC (Windows), standalone `.exe` |
| **Engine** | Unity 6000.4.1f1 — Universal Render Pipeline (2D + iluminação 2D) |
| **Perspectiva** | 2D lateral (*side-scroller*) |
| **Público** | Livre / adolescentes+ (violência estilizada em pixel art) |
| **Disciplina** | Desenvolvimento de Jogos Digitais — Ciência da Computação (P2) |
| **Entrega** | 18/11/2026 (CONNECT+) |

---

## 1. Visão geral (pitch)

*Ecos de Aldenor* é um jogo de plataforma e ação em pixel art de ambientação
gótica. O jogador controla **Ren**, um cavaleiro que atravessa um cemitério
assombrado e uma igreja em ruínas para reunir os **6 fragmentos de alma** —
os "ecos" de Aldenor — e enfrentar o guardião corrompido, **A Vigília**.

O jogo combina **exploração de fases**, **combate corpo a corpo** com
*game feel* (recuo, congelamento de impacto e faíscas), **coleta** como
condição de progressão e uma **luta de chefe** com fases e barra de vida própria.

---

## 2. Equipe

- Gabriel Quirino Bressanin
- Guilherme Barros Oliveira
- Heitor Lupino
- João Pedro Ferreira
- Matheus Risso

---

## 3. Narrativa e ambientação

> *Premissa de trabalho — o grupo pode expandir/ajustar conforme a visão narrativa.*

Aldenor foi um reino tomado por uma névoa espectral. As almas de seus habitantes
ficaram presas como **fragmentos** dispersos pelo cemitério e pela velha igreja.
**Ren**, o último cavaleiro desperto, precisa recolher esses ecos para quebrar a
vigília que mantém o reino aprisionado — um guardião corrompido conhecido como
**A Vigília**, que aguarda no altar da igreja.

- **Mundo:** cemitério espectral (fases iniciais) → igreja em ruínas (arena final).
- **Tom:** sombrio, frio, melancólico; luz de velas e almas contra a escuridão.
- **Objetivo do herói:** reunir os 6 fragmentos e derrotar A Vigília para libertar Aldenor.

**Personagens**
- **Ren** — o cavaleiro jogável. Ágil, ataca com a espada em combos.
- **Sombra Rastejante** — espectro que patrulha o cemitério.
- **Guardião de Pedra** — esqueleto guardião, mais resistente.
- **A Vigília** — o chefe: um mago encapuzado que protege o altar final.

---

## 4. Mecânicas centrais (*core loop*)

**Loop de jogo:** explorar a fase → derrotar/evitar inimigos → coletar fragmentos
→ ativar checkpoints → alcançar a saída → repetir até reunir 6 fragmentos →
enfrentar o chefe → vitória.

### 4.1 Movimento e pulo
- Movimento horizontal com aceleração alta (resposta imediata) e um pouco menor no ar.
- Direção do personagem espelha o sprite (sem inverter escala, evitando efeitos na física).
- O pulo usa as quatro tolerâncias clássicas de plataforma, para o comando nunca
  parecer "engolido" pelo jogo:
  - **Coyote time (0,12 s)** — o pulo ainda vale por alguns quadros depois de sair da borda.
  - **Jump buffer (0,12 s)** — apertar pouco antes de aterrissar não perde o pulo.
  - **Altura variável** — soltar o botão durante a subida corta o salto.
  - **Gravidade de queda maior (1,9×)** — a descida é mais rápida que a subida, o
    que elimina a sensação de flutuar.
- Aterrissagem tem som; quedas fortes também sacodem a câmera.

**Medidas do salto (base do level design):** altura máxima **1,73 u**, alcance
horizontal **3,00 u** no mesmo nível. Daí as regras de projeto das fases:
**degrau até 1,3 u** e **vão até 2,3 u**.

### 4.2 Combate
- Ataque corpo a corpo por clique, com **combo de até 3 golpes** (reinicia se demorar).
- Detecção por área (*overlap circle*) no ponto de ataque; cada inimigo atingido:
  - recebe dano,
  - sofre **recuo (knockback)** na direção do golpe,
  - gera uma **faísca de impacto** (Hitspark) no ponto de contato.
- **Hit stop**: breve congelamento do tempo ao acertar, dando "peso" ao golpe.

### 4.3 Vida, dano e morte
- Vida do jogador exibida como **corações** no HUD (sistema discreto de poucos pontos).
- Ao levar dano o golpe tem peso e legibilidade:
  - *hit stop* curto e *shake* de câmera;
  - **empurrão na direção oposta à do agressor** (o `HealthSystem` guarda de onde
    veio o dano), com breve perda de controle para o recuo ser percebido;
  - o personagem **pisca em vermelho enquanto dura a invulnerabilidade**, então o
    jogador sabe exatamente quando volta a ser vulnerável.
- Morte → tela de **Game Over**.

### 4.4 Quedas e checkpoints
- **Checkpoints** são altares de alma; ao ativar, salvam a posição.
- **Cair num abismo nunca é Game Over instantâneo**: custa um coração e devolve o
  jogador ao último altar ativado — ou ao início da fase, se ele ainda não passou
  por nenhum. A partida só termina quando a vida acaba.
- O dano de queda ignora a invulnerabilidade, para cair logo após levar um golpe
  não sair de graça.

### 4.5 Fragmentos de alma (progressão)
- Espalhados pelas fases; total de **6 fragmentos** no jogo.
- O acesso à **fase final** exige os 6 fragmentos (regra em `GameManager`).
- HUD mostra o progresso como **ícone + "N / 6"**, com pulso ao coletar.
- Ao reunir a sexta alma, um **anúncio central** ("AS ALMAS FORAM REUNIDAS / O
  santuário final se abriu") aparece com som próprio — sem ele, o objetivo do
  jogo se cumpriria sem o jogador perceber.

---

## 4.6 Level design (relevo das fases)

Cada fase ocupa a faixa `x ∈ [-15, 15]`, com o solo em `y = -0,4`. O relevo é
descrito por **dados** e montado pela ferramenta de editor `LevelTerrainBuilder`
(menu do projeto), o que torna o balanceamento reproduzível: muda-se o número e
reconstrói-se a fase.

**Regras de projeto** (derivadas das medidas do salto):

| Regra | Valor | Motivo |
|---|---|---|
| Degrau máximo | 1,3 u | altura útil do pulo é 1,73 u |
| Vão máximo | 2,3 u | alcance é 3,00 u — sobra folga |
| Faixa de nascimento | `x < -8,5` livre | o jogador não pode nascer colado a um inimigo nem dentro de rocha |
| Plataforma baixa (topo 0,9 u) | maciça (0,9 u) | não há altura para passar por baixo; é um degrau para subir |
| Plataforma alta (topo ≥ 1,8 u) | fina (0,6 u) | deixa passagem livre por baixo para jogador e inimigos |
| Segmento de chão | **ou** sala de combate **ou** trecho de plataforma | empilhar inimigo sob plataforma baixa prende o inimigo na rocha |

**Relevo por fase:**

- **Tutorial** — chão inteiro (espaço seguro, sem abismo). 4 plataformas; a alma
  só é alcançável subindo dois degraus, então a lição de pulo é praticada, não lida.
- **Phase1** — 3 segmentos de chão, **2 abismos de 2,3 u**, 4 plataformas.
  Segmento A é sala de combate + primeira subida; B é puro trecho de plataforma
  (com uma saliência alta); C é a reta final guardada. Checkpoint logo antes do
  primeiro abismo.
- **Phase2** — 4 segmentos, **3 abismos**, 4 plataformas, 3 inimigos. Alterna
  corredor de aproximação → combate → plataforma → escalada final. 2 checkpoints.
- **FinalPhase** — **arena limpa**, sem abismos nem plataformas: o combate com A
  Vigília é horizontal, e qualquer degrau maciço travaria a perseguição do chefe.

Um verificador automático confere, antes de cada entrega, que toda plataforma é
alcançável, que nenhum vão passa do alcance, que nenhum corpo nasce dentro de
rocha, que nenhuma patrulha cruza abismo ou esbarra em plataforma, e que toda
alma está ao alcance de quem pisa na superfície sob ela.

---

## 5. Controles

| Ação | Tecla / Controle |
|------|------------------|
| Mover | `A` / `D` ou setas `←` `→` |
| Pular | `Espaço` |
| Atacar | Botão **esquerdo** do mouse |
| Pausar | `ESC` |

---

## 6. Inimigos e chefe

### 6.1 Inimigos comuns
- Base compartilhada (`EnemyBase`): patrulham entre dois pontos fixos, causam dano
  por contato, têm vida própria e reagem a golpes (knockback + *poof* de morte).
- **Sombra Rastejante** (espectro) — mais frágil, presente nas fases iniciais.
- **Guardião de Pedra** (esqueleto) — mais resistente, aparece a partir da Phase2.

### 6.2 Chefe — A Vigília
- Máquina de estados: **Idle → Perseguir → Ataque básico → Telegrafo → Ataque especial → Recuar**.
- **Barra de vida de chefe** própria no topo, com **trilha de dano** (*chip damage*) e
  divisores marcando as **3 fases** do combate.
- **Telegrafo** visível (piscar) antes do ataque especial; efeitos de explosão e *screen shake*.
- Sequência de morte com atraso para leitura antes da tela de Vitória.

---

## 7. Progressão e fluxo de cenas

O jogo tem **8 cenas** encadeadas:

```
MainMenu → Tutorial → Phase1 → Phase2 → FinalPhase → Victory
                                              ↘ GameOver ↘  (→ MainMenu)
                                    Credits (a partir do MainMenu)
```

- **MainMenu** — título estilizado e navegação (Jogar, Créditos, Sair).
- **Tutorial** — área segura, sem inimigos, com placas ensinando os controles.
- **Phase1 / Phase2** — cemitério espectral com plataformas, inimigos, fragmentos e checkpoints.
- **FinalPhase** — arena na igreja com o chefe A Vigília.
- **VictoryScreen / GameOverScreen** — telas de fim, com botão de voltar ao menu.
- **Credits** — integrantes e fontes dos assets.

---

## 8. Interface (UI/HUD)

- **HUD** (prefab compartilhado entre fases): fileira de **corações** (vida) e o
  **contador de fragmentos** (ícone de alma + "N / 6" em dourado, com pulso ao coletar).
- **Menu de pausa** (`ESC`): retomar / voltar ao menu.
- **Menus e telas de fim** estilizados no tema gótico (títulos com sombra/contorno).
- **Transições com fade** (preto) entre todas as cenas, sem cortes secos.

---

## 9. Direção de arte

- **Estilo:** pixel art, paleta **fria** (roxos/azuis escuros) com destaques quentes
  pontuais (velas, tochas) e as almas em tom espectral/dourado.
- **Cenário:** parallax em camadas (fundo, montanhas, cemitério / mural da igreja),
  **iluminação 2D URP** (luz global fria + luzes pontuais quentes) para clima de cripta,
  **solo sólido** que desce para fora da tela (evita a sensação de plataforma flutuante).
- **Coerência:** elementos que destoavam do frio (ex.: chão e porta) foram reharmonizados.

**Pacotes de arte** (ver Créditos): Gothicvania (ansimuz), Hero Knight, Brackeys 2D Mega Pack,
2D Dungeon Tilemap, Hitspark FX.

---

## 10. Áudio

- **Música por cena** (componente `SceneMusicPlayer` + `AudioManager` persistente):
  - Menu: tema calmo;
  - Fases: temas de exploração distintos;
  - **Chefe (FinalPhase):** trilha própria, mais intensa;
  - **Vitória:** trilha de "paz restaurada" (em loop);
  - **Derrota:** *sting* que toca uma única vez.
- **SFX** (RPG Essentials — Leohpaz): ataque, dano, **morte**, **pulo**, **coleta de alma**
  (absorção) e **ativação de checkpoint** (revive).

---

## 11. Arquitetura técnica

- **Código** em `Assets/_Project/Scripts/` (namespaces `EcosDeAldenor.*`), organizado por
  domínio: `Core`, `Player`, `Enemies`, `Systems`, `UI`, `ScriptableObjects`.
- **Padrões:** Singletons persistentes (`GameManager`, `AudioManager`, `SceneController`),
  **eventos C#** para desacoplar UI e lógica, princípios SOLID e comentários em PT-BR.
- **Sistemas de apresentação:** câmera seguidora com limites, `CameraShake`, `ParallaxLayer`,
  `SimpleSpriteAnimator`, `OneShotVFX`, `HitStop`, fade de cena.
- **Gerência de estado:** fragmentos, checkpoint atual, contagem de mortes e pausa no `GameManager`.

---

## 12. Requisitos da entrega (checklist)

- [x] Build jogável (a gerar/testar em máquina limpa — ver `ROADMAP.md`)
- [x] Projeto Unity organizado e versionado
- [x] GDD (este documento)
- [x] README com integrantes
- [x] Tela de créditos com fontes dos assets
- [ ] Confirmar autoria dos assets nas fontes oficiais (ver `ROADMAP.md`)

---

## 13. Créditos e fontes dos assets

Todos os assets de terceiros são gratuitos; as atribuições exigidas por licença
estão na **tela de Créditos** do jogo e no README.

| Asset | Uso | Autor / Fonte | Licença |
|-------|-----|---------------|---------|
| **Gothicvania** (Cemetery, Church, Town) | Cenários, inimigos e chefe | Luis Zuno "Ansimuz" | CC0 |
| **Hero Knight – Pixel Art** | Personagem jogável (Ren) | *confirmar (LuizMelo / Sven Thole)* | gratuito |
| **Bandits – Pixel Art** | Animações/sensor auxiliares | *confirmar (LuizMelo / Sven Thole)* | gratuito |
| **Hitspark FX** | Faísca de impacto no golpe | Jason Lee (jasontomlee.itch.io) | gratuito |
| **Brackeys 2D Mega Pack** | Decoração e efeitos | Brackeys | CC0 |
| **2D Dungeon Tilemap & músicas CC0** | Tiles e trilhas | Unity Asset Store | CC0 |
| **RPG Essentials SFX** | Efeitos sonoros | Leohpaz | gratuito |
| **Música (vila / vitória)** | Trilha da tela de Vitória | Pascal Belisle | crédito exigido |

> **Atenção:** confirmar nas páginas oficiais a autoria de *Hero Knight* e *Bandits*
> e a licença exata de cada pacote, alinhando README e tela de Créditos (ver `ROADMAP.md`).
