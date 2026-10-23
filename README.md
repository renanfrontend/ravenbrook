# Ravenbrook: O Surto

Jogo de survival horror em terceira pessoa, inspirado em Resident Evil, feito com **Unity 6 + C#**, **Unity UI**, **Blender** e suporte a modelos do **Sketchfab**. É um fan game original: não usa marcas, nomes nem personagens da Capcom.

Você é uma policial novata presa na Delegacia de Ravenbrook durante o surto do Vírus-V. Encontre as chaves, junte os três medalhões da estátua do saguão, abra o portão da garagem e fuja, enquanto um perseguidor de máscara de gás caça você pelos corredores.

## Como jogar

| Tecla | Ação |
|---|---|
| WASD | andar |
| Shift | correr |
| Botão direito | mirar (a mira fecha se você ficar parado) |
| Botão esquerdo | atirar / golpe de faca |
| R | recarregar |
| E | interagir (portas, itens, máquina de escrever, baú, estátua) |
| Q | virada rápida de 180° |
| F | golpe rápido de faca |
| 1 a 4, rodinha | trocar arma (faca, pistola, escopeta, magnum) |
| Tab | inventário (usar, combinar ervas, arquivos lidos) |
| M | mapa (salas vermelhas ainda têm itens; azuis estão vazias) |
| L | lanterna |
| Esc | pausa |
| F1 | Painel Overwatch (modo de teste) |

## O que tem no jogo

- **Delegacia com 9 salas**: saguão com estátua, escritórios, arquivo, biblioteca, registros, celas, sala segura e garagem.
- **Inimigos com NavMesh**: zumbis lentos, corredores, zumbis caídos que levantam quando você chega perto, e o **Brutamonte**, que aparece depois do 2º medalhão, segue seus passos, não morre (só cai de joelhos) e não entra na sala segura.
- **Combate estilo RE**: tiro na cabeça causa dano 2,6× e faz o zumbi cambalear, a escopeta empurra, a magnum atravessa, e a faca gasta durabilidade.
- **Saúde BEM / CUIDADO / PERIGO** com eletrocardiograma na tela, mancar, batimento cardíaco e vinheta vermelha.
- **Inventário de 8 espaços**, baú compartilhado, ervas combináveis, máquina de escrever para salvar, 4 documentos e mapa.
- **13 idiomas**: português, inglês, espanhol, francês, alemão, italiano, russo, polonês, turco, holandês, japonês, coreano e chinês.
- **Tema HAZMAT** escuro e interface que se adapta a qualquer resolução (Canvas Scaler).
- **Gráficos Baixo / Médio / Alto** (sombras da lanterna, pós-processamento e número de luzes).
- **Classificação final** (S, A, B ou C) por tempo e número de salvamentos.

### Painel Overwatch (F1)

Ferramentas de teste do próprio jogo, organizadas como no documento de referência:

- **Guardian**: invulnerável, restaurar vida, telemetria na tela.
- **Ordnance**: munição infinita, faca indestrutível, arsenal completo, trocar arma.
- **Phantom**: congelar inimigos, devolvê-los à origem, velocidade do tempo de 0,1× a 2×, teletransporte para qualquer sala.

Usar o painel marca a partida como "de teste" (sem classificação). Dá para desligar o painel em Opções.

## Estrutura

```
Assets/
  Scripts/Core        GameManager, idiomas (Loc), sons sintetizados (Sfx), materiais, salvar, autoteste
  Scripts/Player      jogador, câmera sobre o ombro, armas, inventário
  Scripts/Characters  corpo articulado animado por código
  Scripts/Enemies     zumbis e Brutamonte (NavMesh)
  Scripts/World       delegacia gerada por mapa de texto, portas, itens, documentos
  Scripts/UI          Unity UI: HUD, menus, inventário, mapa, Painel Overwatch
  Resources/Shaders   shaders próprios (superfície, paredes, manchas, brilho, pós-processamento)
  Resources/Modelos   modelos FBX do Blender (e do Sketchfab, se você adicionar)
  Editor              menu "Ravenbrook": configurar, gerar modelos no Blender, compilar
Blender/gerar_modelos.py  gera os modelos no Blender
```

O jogo se monta sozinho ao abrir a cena (`RuntimeInitializeOnLoadMethod`): não há nada arrastado no Inspector.

### Editar o mapa

O mapa fica em `LevelBuilder.Blocks`: cada sala tem 9 × 7 caracteres e cada caractere ocupa 2,5 m. A legenda está no topo de `LevelBuilder.cs` (`Z` zumbi, `h` erva, `L` lâmpada, etc.).

## Compilar

Pelo Unity: menu **Ravenbrook > Gerar jogo para Windows**. O resultado sai em `Build/Ravenbrook.exe`.

Pela linha de comando:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" -batchmode -quit -projectPath C:/Projetos/Ravenbrook -executeMethod Ravenbrook.EditorTools.BuildTools.BuildWindows -logFile Logs/build.log
```

Teste automático (joga sozinho, salva prints `autotest_*.png` em `Build/` e registra FPS e erros no log):

```bash
Build/Ravenbrook.exe -autotest -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile Build/autotest.log
```

## Modelos 3D

- **Blender**: menu **Ravenbrook > Gerar modelos no Blender** (ou `blender -b -P Blender/gerar_modelos.py -- Assets/Resources/Modelos`). Gera a protagonista, o zumbi, o Brutamonte e os objetos.
- **Sketchfab**: veja [TUTORIAL-SKETCHFAB.md](TUTORIAL-SKETCHFAB.md).
- Sem nenhum modelo, o jogo monta tudo com peças primitivas.
