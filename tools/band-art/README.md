# Arte da banda

Gera os 4 músicos da banda (punk, jazz, reggae e hip-hop) no estilo do pacote
[Toon Characters 1 da Kenney](https://kenney.nl/assets/toon-characters-1) (CC0), a partir dos SVGs do próprio
pacote: recolore as roupas e acrescenta cabelo, chapéus e acessórios com o mesmo traço.

- `kenney_rig.py`: lê o SVG de um personagem, monta a pose parada e exporta peças soltas (cabeça e
  expressões, tronco, braço, mão, perna) numa moldura fixa com o pivô conhecido.
- `design.py`: o figurino de cada músico e os ajustes por expressão.
- `render.ps1`: transforma os SVGs em PNG transparente usando o Edge sem janela.
- `sheet.ps1`: monta a prancha de apresentação.

## Como regenerar

```powershell
# Pasta do pacote Kenney descompactado (a que tem "Male person", "Female person"...).
$kenney = "C:\caminho\para\toon"
python -I design.py $kenney .\saida\inteiros .\saida\pecas
.\render.ps1 -Folder .\saida\pecas -Recurse
# Copie .\saida\pecas\<músico>\*.png para GuitarPlay\Content\Band\<músico>\
```

As molduras das peças (em `kenney_rig.FRAMES`) e a escala de 3 px por unidade precisam bater com as
constantes de `GuitarPlay/Rendering/Band.cs`.
