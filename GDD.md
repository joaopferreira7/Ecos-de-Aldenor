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
assombrado para reunir os **6 fragmentos de alma** — os "ecos" de Aldenor —,
faz voltar a tocar os sinos da cidade silenciosa, apaga os círios do manto no
claustro da igreja e enfrenta o guardião corrompido, **A Vigília**.

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

Aldenor foi um reino de sinos e lampiões, vigiado do alto da colina por uma
igreja — e a igreja, por um anjo, **A Vigília**: enquanto ela velasse, nenhuma alma
do reino se perderia. Numa noite, uma **névoa espectral** desceu sobre Aldenor; os
sinos se calaram e as almas do povo foram arrancadas dos corpos e espalhadas pelo
cemitério como **ecos**. A Vigília não as protegeu: cobriu-se com um capuz e passou
a guardá-las como prisioneiras. **Ren**, o último cavaleiro de pé, desperta entre
as lápides para reunir os seis ecos, subir à igreja e encerrar a vigília.

**O prólogo** conta essa história antes do tutorial (cena `Prologo`), em cinco
capítulos encenados com a arte dos próprios pacotes, enquadrados como cinema (faixas
pretas, vinheta, câmera deslizando, nome do capítulo em fonte gótica). A frase de cada
capítulo só começa depois que o momento acontece:

| Capítulo | O que se vê |
|---|---|
| I · Aldenor | a cidade ao entardecer, moradores andando, vaga-lumes, o sol atrás da igreja |
| II · A Vigília | o anjo diante do altar sob colunas de luz, almas douradas |
| III · A Névoa | relâmpago e trovão; os moradores se apagam e se desfazem em almas que sobem; espectros surgem na névoa |
| IV · O Manto | o anjo se dissolve numa fumaça vermelha e no lugar dele surge o mago encapuzado |
| V · O Último Cavaleiro | o Ren se levanta do chão (a animação de morte dele ao contrário) e os seis ecos acendem um a um |

Espaço/Enter/clique avança; ESC pula direto para o tutorial.

O quarto quadro planta a virada da luta final: *"Dizem que o anjo ainda está lá,
sob aquele manto."* Na metade da vida, a Vigília se transforma e revela o anjo.

**O epílogo** é uma cena **dentro do jogo**, com os personagens de verdade, em duas partes
(a primeira versão eram quadros no estilo do prólogo; depois de jogar, o grupo pediu a cena):

1. **A despedida, na própria arena** (`DespedidaDoAnjo`). O chefe cai e as faixas de cinema
   entram; no lugar dele surge o anjo, sem o manto. O controle passa ao roteiro: o Ren caminha
   até ele e os dois conversam em caixas de diálogo (nome em gótico, fala letra por letra). O
   anjo sobe e se desfaz em luz e penas, as seis almas giram em volta dele e sobem pelos vitrais
   enquanto um sino toca longe, e a luz dourada que ficava atrás dele passa para o Ren. Corte em
   branco.
2. **O amanhecer, na cidade** (cena `Epilogo`, a cidade da Fase 3 sem nada de jogo,
   `EpilogoNaCidade`). Noite e névoa; os moradores estão na rua, cinzentos e parados. As almas
   descem do céu, uma para cada um: quem recebe a sua ganha cor e volta a andar, e a câmera
   atravessa a cidade com elas. Amanhece — a luz esquenta, a névoa se levanta e os três
   campanários tocam. O Ren chega com a luz do anjo e para diante da igreja na colina, e os
   **créditos sobem** por cima dessa imagem. Fecha com o título *"Ecos de Aldenor — Os sinos
   tocam outra vez"* e segue para a Vitória.

Espaço avança as falas e acelera os créditos; ESC pula a cena. A trilha da cidade é a mesma da
Vitória, então a música atravessa a troca de cena sem corte.

- **Mundo:** cemitério espectral (fases 1 e 2) → cidade silenciosa (fase 3) → claustro da
  igreja (fase 4) → nave em ruínas (arena final).
- **Tom:** sombrio, frio, melancólico; luz de velas e almas contra a escuridão.
- **Objetivo do herói:** reunir os 6 fragmentos e derrotar A Vigília para libertar Aldenor.

**Personagens**
- **Ren** — o cavaleiro jogável. Ágil, ataca com a espada em combos.
- **Sombra Rastejante** — espectro que patrulha o cemitério.
- **Guardião de Pedra** — esqueleto guardião, mais resistente.
- **Gato Infernal** — fera da névoa que ronda a cidade; salta sobre a presa.
- **Carniçal em Chamas** — morto de cabeça em chamas que guarda o claustro.
- **A Vigília** — o chefe: o anjo guardião da igreja, escondido sob o manto de um
  mago encapuzado. Na metade da luta, o manto se desfaz (*A Vigília Desvelada*).

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
  caminho até a igreja se abriu") aparece com som próprio — sem ele, o objetivo do
  jogo se cumpriria sem o jogador perceber.

### 4.5.1 Objetivos das fases 3 e 4
- Além das almas, a porta de saída dessas fases cobra um **objetivo da fase**, ativado com a
  espada (o golpe já sabe acertá-los: eles têm 1 ponto de vida na camada dos inimigos, com
  faísca e *hit stop*):
  - **Phase3 — três sinos.** Cada badalada balança o sino, acende a luz dele e solta uma onda
    que **atordoa os inimigos** num raio de 4 u por 1,6 s. O som do sino é sintetizado em
    código (`RuntimeAudio`), porque nenhum pacote trazia um.
  - **Phase4 — três círios do manto.** Velas com a faixa vermelha do manto da Vigília; o golpe
    apaga a chama.
- A cada um cumprido aparece "N / 3"; ao último, um anúncio ("Os sinos voltaram a tocar / O
  portão da colina se abriu"). A porta trancada diz quantos faltam e o que fazer.
- **Arte dos objetivos:** o que o pacote tem sai do pacote. O **círio** é o pedestal com vela do
  tileset da igreja (a mesma arte das balaustradas da arena), e apagado vira o recorte da vela sem
  chama. O **sino** pende de um campanário montado com as peças de madeira do tileset da cidade
  (módulos com mão-francesa, viga, postes e sapata). Só o sino de bronze e o braseiro são desenhados
  em código (`ArteGerada`), porque nenhum pacote os traz — e na **mesma densidade de pixel e paleta**
  do pacote ao lado: o sino a 100 px/u como os props da cidade, o braseiro a 24 px/u como o tileset
  da igreja, contorno roxo-escuro e sombras puxadas para o roxo. A primeira versão tinha pixels
  quatro vezes maiores e cores saturadas, e destoava do cenário.

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
- **Phase3 (Cidade)** — 4 segmentos, vãos de 1,6 / 2,0 / 2,3 u, um degrau e um telhado alto
  sobre o segundo vão. Os gatos ficam em chão firme e comprido, longe dos abismos. Sinos no
  começo (lugar seguro, para ensinar), no telhado e diante da saída. 2 checkpoints.
- **Phase4 (Claustro)** — 4 segmentos, vãos de 1,4 / 1,6 / 1,9 u e uma subida em três níveis
  (0,9 → 2,2 → 3,5). Um guardião, dois carniçais nos corredores retos, um braseiro no degrau e
  outro no alto, sobre o corredor do segundo carniçal. 2 checkpoints. **Nenhuma faixa de patrulha
  fica a menos de 1,6 u de um abismo**, e o primeiro carniçal dispara contra a rocha do degrau, não
  contra o vão: na primeira versão os inimigos ficavam colados às beiras, e o empurrão de um golpe
  (ou o fim da disparada) jogava o Ren no vazio a toda hora. A reta final não tem inimigo.
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
- **Gato Infernal** (`hell-gato`, Phase3) — 2 de vida. Ao ver o Ren, **para e se encolhe**
  por 0,5 s (corpo achatado e avermelhado: o aviso), **salta num arco** de até 3,2 u até onde o
  Ren estava quando o aviso começou e fica **atordoado 1 s** ao pousar. O salto nunca sai da
  faixa de patrulha. Golpear durante o aviso cancela o salto. Ensina a **esquivar**.
- **Carniçal em Chamas** (`burning-ghoul`, Phase4) — 2 de vida. Ao ver o Ren, a chama se
  **atiça** por 0,4 s e ele **dispara** em linha reta até o fim da própria faixa, sem virar,
  deixando um **rastro de fogo** que queima por 1,5 s; depois fica **ofegante** 1 s. Ensina a
  **pular por cima**.
- **Braseiro** (Phase4, armadilha) — quando o Ren passa a até 5 u, marca o chão sob ele com a
  **marca vermelha** e, 1 s depois, a bola de fogo cai ali. Mesmo indicador, projétil e cores
  do chefe: a fase ensina a ler o golpe antes da luta.

Avisos e respiros dos inimigos novos seguem a dificuldade (os mesmos multiplicadores dos
avisos do chefe). Nenhum dos dois pacotes traz quadros de ataque ou de morte: o aviso é feito
por cor e deformação, e a morte usa a baforada comum aos inimigos.

### 6.2 Chefe — A Vigília (duas formas, quatro atos)

| Vida | Forma | Golpes |
|------|-------|--------|
| 100% – 75% | **Mago** | golpe curto (faixa âmbar, corte de fogo) · bola de fogo numa marca vermelha · **passo sombrio** (some em fumaça e reaparece perto do Ren; a marca do destino aparece antes) |
| **75%** | **Invocação** | ergue os braços sobre um círculo verde e dois Guardiões de Pedra se levantam do chão, longe do Ren |
| 75% – 50% | **Mago** | + **Salva**: três marcas em fila na direção do Ren, as bolas caem em ordem |
| **50%** | **Transformação** | cena de ~4 s: golpes cancelados, controle travado, convulsão com fumaça, trovão, clarão branco — por trás dele o mago vira anjo; cartão *"A Vigília Desvelada — O anjo que velava Aldenor"* |
| 50% – 25% | **Anjo** | golpe de asa (corte de luz e penas) · **Investida** (faixa lilás no chão; ele a atravessa — e atravessa o Ren, que leva o golpe — deixando imagens residuais, com onda de choque na chegada) · **Chuva de fogo** (3 marcas com colunas de luz descendo do alto, fogo caindo uma por vez) |
| 25% – 0% | **Anjo em fúria** | avisos e respiros 15% mais curtos, 4 marcas na chuva, mais investidas, corpo avermelhado |

- Todo golpe segue **carga → impacto → respiro**: o chefe para, toca a animação de ataque
  esticada para caber no aviso, e um indicador mostra *onde* vai bater. O dano só vale dentro
  da área mostrada — rolar para fora (ou por cima no tempo certo) faz o golpe errar.
- **Código de cores fixo:** âmbar = golpe de perto; vermelho = fogo numa marca; lilás = caminho
  da investida.
- **Mago:** conjura sobre um círculo de runas que gira no chão, solta brasas pelas mãos; as bolas
  de fogo deixam rastro de brasas e o chão fica chamuscado onde caem. Acompanha o Ren com o corpo
  nas cargas de golpes à distância (nos de perto a direção fica travada no aviso).
- **Anjo:** luz dourada atrás dele o tempo todo e penas se soltando das asas; em fúria a luz fica
  vermelha e brasas sobem do corpo. Na transformação, os mortos invocados desabam.
- **Barra de vida** própria com trilha de dano; os divisores marcam os limiares reais (75%, a
  transformação em dourado nos 50%, 25%). Na transformação ela troca de nome e de cor.
- **Arte:** o mago e o anjo são do mesmo pacote (*GothicVania Church*), os dois de manto
  vermelho — a transformação lê como "a mesma criatura, sem disfarce". Animações usadas: mago
  parado (5) e conjuração (10); anjo batendo asas (8) e ataque (3); bola de fogo (3); morte (9).
- **Vida:** 16 no Médio (11 no Fácil, 22 no Difícil) — oito golpes por forma.
- Sequência de morte com atraso para leitura antes da tela de Vitória.

---

## 7. Progressão e fluxo de cenas

O jogo tem **12 cenas** encadeadas:

```
MainMenu → Prologo → Tutorial → Phase1 → Phase2 → Phase3 → Phase4 → FinalPhase → Epilogo → Victory
                                              ↘ GameOver ↘  (→ MainMenu)
                                    Credits (a partir do MainMenu)
```

- **MainMenu** — título estilizado e navegação (Jogar, Créditos, Sair).
- **Prologo** — a história de Aldenor em cinco quadros ilustrados (ver §3). Só aparece ao
  começar um jogo novo; ESC pula.
- **Tutorial** — área segura, sem inimigos, com placas ensinando os controles.
- **Phase1 / Phase2** — cemitério espectral com plataformas, inimigos, fragmentos e checkpoints.
  As seis almas ficam nessas duas fases.
- **Phase3 / Phase4** — a cidade silenciosa (pacote *Town*) e o claustro da igreja (pacote
  *Church*), com os inimigos novos e os objetivos das fases (§4.5.1). A Phase4 é a porta da arena:
  é ela que chama `LoadFinalPhase`, que confere as seis almas.
- **Epilogo** — a cidade ao amanhecer e os créditos (§3). Antes dela, a despedida do anjo
  acontece na própria arena: `BossDeathTrigger` a inicia e ela carrega o `Epilogo`; se a cena não
  estiver na build, o jogo cai direto na Vitória.
- **FinalPhase** — arena na igreja com o chefe A Vigília. É a única fase **sem porta de
  saída**: a passagem para a Vitória é derrubar o chefe (`BossDeathTrigger`), e não
  alcançar a borda direita. Um `PhaseExit` aqui seria um atalho que vence o jogo sem
  luta — o verificador de fases recusa.
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
  - Fases: temas de exploração distintos (a Cidade e o Claustro dividem a *Music 3*, que até então não era usada, e a música atravessa a troca entre as duas sem corte);
  - **Chefe (FinalPhase):** *Epic Boss Battle*, trilha orquestral de combate (172 BPM) —
    as trilhas das fases têm 64–100 BPM e servem à exploração, não ao confronto;
  - **Epílogo e Vitória:** trilha de "paz restaurada" (em loop), contínua entre as duas cenas;
  - **Derrota:** *sting* que toca uma única vez.
- **Escalada na luta do chefe:** a cada terço de vida perdido, A Vigília muda de fase e a
  trilha sobe de intensidade (×1,12 e ×1,25), com um toque de virada e um tremor de câmera.
  As fases já mudavam o comportamento do chefe, mas em silêncio; agora a virada se ouve.
- **Encadeamento de ganho:** volume final = escolha do jogador × ganho da faixa × intensidade.
  O ganho por faixa existe porque as trilhas vêm de pacotes diferentes e não foram
  masterizadas juntas — a do chefe é gravada ~3,5× mais alta que as das fases e entra com
  ganho 0,36, para ficar presente sem virar um susto de volume.
- **SFX** (RPG Essentials — Leohpaz): ataque, dano, **morte**, **pulo**, **coleta de alma**
  (absorção), **ativação de checkpoint** (revive) e **virada de fase do chefe** (carga mágica).

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
| **Hero Knight – Pixel Art** | Personagem jogável (Ren) | **LuizMelo** (luizmelo.itch.io) | CC0 |
| **Bandits – Pixel Art** | Base dos prefabs de inimigos | **Sven Thole** (sventhole.itch.io) | livre em jogos; não revendável como asset |
| **Hitspark FX** | Faísca de impacto no golpe | Jason Lee (jasontomlee.itch.io) | gratuito |
| **Brackeys 2D Mega Pack** | Decoração e efeitos | Brackeys | CC0 |
| **Free 2D Dungeon Platformer Tilemap** | Tiles, Free GameUI e trilhas das fases | **Aether2D** (aether2d.itch.io) | CC0 |
| **Music 1 / 2 / 3** | Música das fases (vem no pacote da Aether2D) | Seth_Makes_Sounds (freesound.org) | CC0 |
| **Epic Boss Battle** | Trilha da luta contra A Vigília | Juhani Junkala / SubspaceAudio (opengameart.org) | CC0 |
| **RPG Essentials SFX** | Efeitos sonoros | Leohpaz | gratuito |
| **Música (vila / vitória)** | Trilha da tela de Vitória | Pascal Belisle | crédito exigido |

> Autoria conferida nas páginas oficiais em 30/08/2026. *Hero Knight* e *Bandits* são de
> **autores diferentes** — os dois já constaram como "Sven Thole" no README e na tela de
> Créditos, mas o sprite do Ren é do **LuizMelo**. README, GDD e tela de Créditos estão alinhados.
