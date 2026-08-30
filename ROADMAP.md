# Ecos de Aldenor — Roadmap (o que deve ser feito)

> Estado geral: **jogo jogável de ponta a ponta (~95%)**. O que falta é sobretudo
> **entregáveis da disciplina**, **verificações** e **polimento fino**.
> Entrega/apresentação: **18/11/2026** (CONNECT+).

Legenda: ✅ feito · 🔲 a fazer · 🔎 verificar

---

## ✅ Já concluído

**Jogabilidade**
- ✅ Movimento, pulo e ataque (combo de 3 golpes) do Ren
- ✅ Vidas em corações (HUD) + dano, knockback e *hit stop* no golpe
- ✅ Faísca de impacto (Hitspark) ao acertar inimigos
- ✅ Patrulha dos inimigos corrigida (pontos fixos) + IA do chefe (state machine)
- ✅ Chefe "A Vigília": barra de vida própria (com *chip damage*), telegrafo, fases de combate
- ✅ Coleta dos 6 fragmentos de alma (regra da fase final)
- ✅ Checkpoints (altares) + queda reposiciona no checkpoint em vez de morte instantânea
- ✅ **Level design de verdade**: as fases deixaram de ser corredores planos — chão
  segmentado com abismos, plataformas em degrau e almas que exigem subir
- ✅ **Ritmo dos abismos**: os vãos crescem ao longo do jogo (1,60 → 2,30 na Phase1;
  1,40 → 1,90 → 2,30 na Phase2) em vez de serem todos o vão máximo
- ✅ **Game feel do pulo**: coyote time, jump buffer, altura variável, gravidade de
  queda maior, som de aterrissagem
- ✅ **Dano com peso**: hit stop, empurrão direcional e pisca-pisca de invulnerabilidade

**Apresentação / game feel**
- ✅ Câmera seguidora com limites + *screen shake*
- ✅ Cenário coeso: parallax (cemitério/igreja), iluminação 2D, **solo sólido**
- ✅ **Parallax sem rasgo**: as camadas cobrem toda a faixa que a câmera mostra
- ✅ **HUD de fragmentos** redesenhado: ícone + "N / 6" dourado + pulso ao coletar
- ✅ **Áudio**: música por cena, trilha própria de **vitória** e **derrota**, chefe distinto;
  SFX de ataque, dano, morte, pulo, coleta (alma) e checkpoint
- ✅ **Mixagem**: música em 0,55 e SFX em 0,90 — antes as duas fontes ficavam em 1,0
- ✅ **Fade de transição** entre cenas
- ✅ Menus (principal, pausa), telas de Vitória/GameOver e **Créditos** estilizados

**Ferramentas e organização**
- ✅ **Relevo das fases como dado versionado** (`LevelLayouts.cs`): segmentos, plataformas
  e patrulhas de cada fase num só lugar, com o menu **Ecos de Aldenor → Reconstruir fases**
- ✅ **Verificador automático** (**Ecos de Aldenor → Verificar fases**): vão, degrau,
  alcance das almas, patrulha presa em rocha ou cruzando abismo, nascimento dentro de
  pedra, cenário encavalado ou cobrindo altar, cobertura do parallax e total de almas
- ✅ **Comando de build** (**Ecos de Aldenor → Gerar build Windows**), que recusa
  construir se a lista de cenas estiver errada ou o verificador achar problema
- ✅ Projeto versionado no Git/GitHub, com histórico organizado
- ✅ README com integrantes, controles, como rodar/buildar e créditos
- ✅ Tela de Créditos no jogo com integrantes + fontes dos assets
- ✅ Remoção de packs de assets não utilizados

---

## 🐞 Bugs de fundo corrigidos

**Rodada anterior**
- **Pulo no ar**: o `GroundCheck` do Ren estava 0,94 u abaixo dos pés.
- **Pulo e ataque se anulavam**: estavam numa cadeia `else if`.
- **Nascimento dentro da rocha** na Phase2, e **inimigo colado ao nascimento**.
- **Chefe afundado 0,2 u no chão**.

**Esta rodada**
- **Altar curava para sempre**: cair saía de graça (perdia-se um coração e recuperava-se
  ao reaparecer sobre o altar) e dava para recuperar vida andando de um lado para o outro
  em cima dele. Agora o altar se consome ao ser aceso.
- **Renascer no abismo em ciclo**: o dano de queda era aplicado *depois* de reposicionar,
  então o empurrão do dano era gasto em cima do ponto de renascimento e podia arremessar
  o jogador de volta ao vão, até perder todas as vidas. Dano primeiro, e 1,5 s de
  invulnerabilidade ao voltar.
- **Inimigo empurrava o jogador para o vazio**: a perseguição não respeitava a faixa de
  patrulha; o inimigo saía da borda e caía junto.
- **Sete almas para uma meta de seis**: quem coletasse tudo via "7 / 6" no HUD.
- **Rasgo preto no céu** nas duas pontas de cada fase de cemitério — inclusive onde o
  jogador nasce: o céu era uma única peça de 22,29 u para 29,64 u de percurso de câmera.
- **Lápide em cima do altar** (Phase1 e Phase2): a distribuição de cenário não reservava
  lugar para altares nem para a saída da fase.
- **Guardião virou estátua**: patrulhava 0,4 u e, com a perseguição agora limitada à
  própria faixa, deixara de conseguir avançar.
- **Música abafando os efeitos**: nada no jogo chamava `SetMusicVolume`/`SetSfxVolume`, e
  as duas fontes tocavam no volume máximo.
- **Crédito errado no sprite do herói**: Hero Knight constava como "Sven Thole"; é do
  **LuizMelo**. Sven Thole é o autor do Bandits, que nem aparecia na tabela do README.
- **Vitória sem lutar**: a arena do chefe tinha um `PhaseExit` invisível em x=14 apontando
  para a VictoryScreen — bastava correr para a direita e o jogo era vencido sem encostar
  n'A Vigília. Removido; a vitória é do `BossDeathTrigger`, e o verificador agora recusa
  qualquer porta de saída na arena.
- **Preso na arena depois de matar o chefe**: o `BossDeathTrigger` agendava a Vitória com
  `Invoke(..., 1,6 s)` **no próprio objeto do chefe**, mas `EnemyBase.HandleDeath` destrói
  esse objeto em 0,6 s — e destruir um objeto cancela os `Invoke` pendentes dele, sem erro
  no console. O jogo acabava sem saída. A contagem passou para o `SceneController`, que é
  persistente. O defeito era antigo, mascarado pelo `PhaseExit` que existia na arena.
- **Barra do chefe no meio da tela**: o `BossHealthBar` ancora o painel no topo do próprio
  RectTransform, que na cena era um quadrado de 100×100 preso ao centro do canvas — a barra
  aparecia atravessada no rosto do jogador durante a luta. Agora ocupa 30,6%–69,4% da
  largura, a 5,5% do topo.

---

## 🔲 P1 — Obrigatório para a entrega (18/11)

- 🔲 **GDD** entregue (ver `GDD.md`) — revisar com o grupo e exportar em PDF se exigido
- 🔲 **Build final `.exe`** gerada e **testada em uma máquina limpa** (sem o Unity).
  O comando **Ecos de Aldenor → Gerar build Windows** monta o pacote com as 8 cenas e
  MainMenu como inicial; falta rodar o `.exe` num PC sem Unity.
- ✅ **Autoria dos assets nos créditos** conferida nas páginas oficiais (30/08/2026):
  Hero Knight → LuizMelo (CC0); Bandits → Sven Thole; 2D Dungeon Tilemap → Aether2D (CC0);
  trilhas das fases → Seth_Makes_Sounds (CC0); Hitspark → Jason Lee. README, GDD e tela de
  Créditos alinhados.
- 🔲 Conferir no CONNECT+ o formato exato exigido (projeto + build + GDD + README + créditos)
  e montar o pacote de entrega

---

## 🔲 P2 — Polimento (qualidade e atratividade)

- 🔎 **Playtest de balanceamento jogado à mão**. O que já foi medido, para orientar o teste:
  - o pulo sobe **1,73 u** e alcança **2,97 u** no mesmo nível (simulado com a física real
    do jogo: `jumpForce` 11, `gravityScale` 3, damping 0,5);
  - o **Guardião de Pedra causa 2 de dano** e o Ren tem **3 corações**: dois toques matam, e
    com o altar curando uma única vez a Phase2 pode ter ficado dura demais — é o primeiro
    número a observar no teste;
  - chefe com **12 de vida** e golpe do jogador em **1 por 0,4 s**: cerca de 5 s de acertos
    limpos, fora o tempo de desviar.
- ✅ Mixagem de áudio revisada (música 0,55 × SFX 0,90)
- ✅ Feedback ao completar 6/6 fragmentos
- ✅ *Hit stop*/flash também ao levar dano
- ✅ Percurso das 4 fases verificado por geometria (*Verificar fases*) — ritmo e
  dificuldade seguem dependendo do playtest acima
- 🔲 Passe visual final: **legibilidade das plataformas no escuro** (são o que o jogador
  precisa enxergar de relance e hoje contrastam pouco com o fundo), consistência de escala
  dos inimigos, legibilidade dos textos no escuro

---

## 🔲 P3 — Extras (opcionais, se sobrar tempo)

- 🔲 Menu de **opções** (controle de volume música/SFX) — o `AudioManager` já expõe
  `SetMusicVolume`/`SetSfxVolume` e guarda os valores; falta só a tela
- 🔲 **Salvar progresso** (fragmentos/último checkpoint) entre sessões
- 🔲 Mais variedade de inimigos ou uma fase extra
- 🔲 Efeitos de partículas nas almas/checkpoints; ataque especial do chefe com sprite
  dedicado (wizard/Fire já disponível no pack)
- 🔲 Tela de **controles** acessível pelo menu principal
- 🔲 Remover a `Assets/Scenes/SampleScene.unity`, sobra do template

---

## Riscos / pontos de atenção

- **Build em máquina limpa**: testar cedo — problemas de cena ausente no build ou input só
  aparecem fora do editor.
- **Escopo**: o jogo já está jogável e coeso — priorizar a entrega (P1) antes de novos
  recursos (P3).
- **Mexer no relevo**: alterar uma fase é alterar a tabela em `LevelLayouts.cs` e rodar
  *Reconstruir fases*; arrastar objeto na mão faz a cena e os dados discordarem em silêncio.
