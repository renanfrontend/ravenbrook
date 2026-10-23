# Usando modelos do Sketchfab no Ravenbrook

O jogo já vem com modelos gerados pelo Blender (`Blender/gerar_modelos.py`). Você pode trocar qualquer um deles por um modelo do Sketchfab sem mexer em código: basta colocar o arquivo com o nome certo em `Assets/Resources/Modelos`.

## 1. Baixar no Sketchfab

1. Entre em [sketchfab.com](https://sketchfab.com) com a sua conta (o download exige login).
2. Pesquise, por exemplo, `zombie low poly`, `wooden crate`, `office desk`, `typewriter`, `statue`, `barricade`.
3. Filtre por **Downloadable** e confira a licença na página do modelo:
   - **CC0**: pode usar sem dar crédito.
   - **CC-BY**: pode usar, mas **precisa dar crédito** (veja o passo 4).
   - Evite modelos "Editorial", "NoDerivatives" ou "NonCommercial" se pensa em publicar o jogo.
4. Clique em **Download 3D Model** e escolha **glTF (.glb)** ou **FBX**.

> Não baixe modelos que copiem personagens da Capcom (Mr. X, Nemesis, Leon etc.). Além do risco de direitos autorais, o jogo é original.

## 2. Nomes dos arquivos (vagas)

| Arquivo em `Assets/Resources/Modelos` | Onde aparece | Tamanho no jogo |
|---|---|---|
| `Zumbi.fbx` ou `Zumbi.glb` | todos os zumbis comuns e corredores | 1,8 m de altura |
| `Brutamonte.fbx/.glb` | o perseguidor | 2,1 m (antes da escala 1,3×) |
| `Jogador.fbx/.glb` | a protagonista | 1,8 m |
| `Caixa` | caixas de madeira | 1,05 m |
| `Mesa` | mesas dos escritórios | 1,6 m |
| `Barricada` | barricadas | 2 m |
| `Estatua` | estátua do saguão | 3 m |
| `MaquinaEscrever` | máquina de escrever (salvar) | 0,5 m |
| `Bau` | baú de itens | 1,1 m |
| `Erva` | ervas verde e vermelha (material com "folha" no nome recebe a cor da erva) | 0,55 m |

O jogo ajusta o tamanho sozinho (pela maior dimensão) e apoia o modelo no chão.

**Arquivos `.glb`**: o projeto já inclui o pacote glTFast, que importa `.glb` direto no Unity.

## 3. Personagens: modelo inteiro ou articulado

- **Modelo inteiro** (o normal do Sketchfab): o jogo usa o modelo como um bloco, com balanço ao andar e queda ao morrer. Se o modelo tiver um `Animator` com o parâmetro `Speed` (float), o jogo envia a velocidade para ele.
- **Modelo articulado** (como os do Blender): se o arquivo tiver peças chamadas `Tronco`, `Cabeca`, `BracoE`, `BracoD`, `PernaE` e `PernaD`, cada uma com o pivô na articulação, o jogo anima braços e pernas por código. Você pode separar um modelo do Sketchfab em peças no Blender e exportar assim.

## 4. Créditos (obrigatório para CC-BY)

Anote cada modelo em `Assets/Resources/Modelos/CREDITOS.txt`, uma linha por modelo:

```
"Old Wooden Crate" por NomeDoAutor (sketchfab.com/NomeDoAutor) — CC-BY 4.0 — usado em Caixa
```

O texto aparece na tela de **Créditos** do jogo e é copiado para a pasta do build como `CREDITOS-MODELOS.txt`.

## 5. Testar

1. Abra o projeto no Unity e espere a importação.
2. Menu **Ravenbrook > Gerar jogo para Windows**, ou aperte Play na cena `Assets/Scenes/Ravenbrook.unity`.
3. Para voltar aos modelos do Blender, apague o arquivo do Sketchfab e rode **Ravenbrook > Gerar modelos no Blender**.
