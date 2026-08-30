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
- ✅ **Game feel do pulo**: coyote time, jump buffer, altura variável, gravidade de
  queda maior, som de aterrissagem
- ✅ **Dano com peso**: hit stop, empurrão direcional e pisca-pisca de invulnerabilidade
- ✅ Aviso central ao reunir as 6 almas (fase final liberada)
- ✅ Ferramenta de editor `LevelTerrainBuilder` + verificador automático de
  jogabilidade das fases (alcance, vãos, patrulhas, sobreposição de corpos)

**Apresentação / game feel**
- ✅ Câmera seguidora com limites + *screen shake*
- ✅ Cenário coeso: parallax (cemitério/igreja), iluminação 2D, **solo sólido** (fim do efeito "plataforma flutuante")
- ✅ Porta de cripta reharmonizada ao tema frio
- ✅ **HUD de fragmentos** redesenhado: ícone + "N / 6" dourado + pulso ao coletar
- ✅ **Áudio**: música por cena, trilha própria de **vitória** e **derrota**, chefe distinto; SFX de ataque, dano, morte, pulo, coleta (alma) e checkpoint
- ✅ **Fade de transição** entre cenas (fim dos cortes secos)
- ✅ Menus (principal, pausa), telas de Vitória/GameOver e **Créditos** estilizados

**Organização**
- ✅ Projeto versionado no Git/GitHub, com histórico organizado
- ✅ README com integrantes, controles, como rodar/buildar e créditos
- ✅ Tela de Créditos no jogo com integrantes + fontes dos assets
- ✅ Remoção de packs de assets não utilizados

---

## 🐞 Bugs de fundo corrigidos nesta rodada

- **Pulo no ar**: o `GroundCheck` do Ren estava **0,94 u abaixo dos pés**, então o
  jogador contava como "no chão" flutuando quase uma unidade acima dele.
- **Pulo e ataque se anulavam**: estavam numa cadeia `else if`, então não dava para
  atacar no mesmo quadro em que se pulava.
- **Nascimento dentro da rocha**: na Phase2 uma plataforma cobria o ponto de
  nascimento e a física cuspia o jogador para longe.
- **Inimigo colado ao nascimento**: o jogador levava empurrão/dano antes de tocar o teclado.
- **Chefe afundado 0,2 u no chão** (o collider foi recalculado quando o sprite virou o mago).

---

## 🔲 P1 — Obrigatório para a entrega (18/11)

- 🔲 **GDD** entregue (ver `GDD.md`) — revisar com o grupo e exportar em PDF se exigido
- 🔲 **Build final `.exe`** gerada e **testada em uma máquina limpa** (sem o Unity), com as 8 cenas e MainMenu como inicial
- 🔎 **Autoria dos assets nos créditos**: confirmar nas páginas oficiais (itch.io) os autores de **Hero Knight** e **Bandits** — hoje constam como "Sven Thole", mas pode ser **LuizMelo**. Alinhar README + tela de Créditos com a fonte real (Hitspark já confirmado: Jason Lee / jasontomlee)
- 🔲 Conferir no CONNECT+ o formato exato exigido (projeto + build + GDD + README + créditos) e montar o pacote de entrega

---

## 🔲 P2 — Polimento (qualidade e atratividade)

- 🔲 **Playtest de balanceamento**: vida do chefe (hoje 12), dano/alcance do player, dificuldade dos inimigos, número de checkpoints
- 🔲 **Mixagem de áudio**: revisar volumes relativos de música x SFX (nenhum estourando/inaudível)
- ✅ Feedback ao **completar 6/6 fragmentos** (aviso visual/sonoro de que a fase final foi liberada)
- ✅ *Hit stop*/flash também **ao levar dano**
- 🔎 Confirmar em playtest jogado à mão o **percurso completo das 4 fases** com o novo relevo
  (a verificação automática aprova a geometria, mas ritmo e dificuldade só se sentem jogando)
- 🔲 Passe visual final: consistência de escala dos inimigos, emendas de parallax, legibilidade dos textos no escuro

---

## 🔲 P3 — Extras (opcionais, se sobrar tempo)

- 🔲 Menu de **opções** (controle de volume música/SFX)
- 🔲 **Salvar progresso** (fragmentos/último checkpoint) entre sessões
- 🔲 Mais variedade de inimigos ou uma fase extra
- 🔲 Efeitos de partículas nas almas/checkpoints; ataque especial do chefe com sprite dedicado (wizard/Fire já disponível no pack)
- 🔲 Tela de **controles** acessível pelo menu principal

---

## Riscos / pontos de atenção

- **Build em máquina limpa**: testar cedo — problemas de cena ausente no build ou input só aparecem fora do editor.
- **Créditos corretos**: misattribuição de autor é fácil de corrigir agora e chato de explicar depois; confirmar as fontes.
- **Escopo**: o jogo já está jogável e coeso — priorizar a entrega (P1) antes de novos recursos (P3).
