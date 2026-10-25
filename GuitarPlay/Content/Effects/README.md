# Shaders

O projeto não usa o content pipeline do MonoGame (MGCB). Os arquivos da pasta `Content` são
copiados soltos para a saída e lidos em tempo de execução (`Rendering/Assets.cs`).

O shader `.fx` precisa ser compilado para o formato do MonoGame (`.mgfxo`). Isso é feito com o
**mgfxc**, instalado como ferramenta local do repositório (`.config/dotnet-tools.json`, versão igual
à do `MonoGame.Framework.DesktopGL`, hoje 3.8.6):

```
dotnet tool restore
dotnet mgfxc GuitarPlay/Content/Effects/Bloom.fx GuitarPlay/Content/Effects/Bloom.mgfxo /Profile:OpenGL
```

O `GuitarPlay.csproj` roda esse comando sozinho no build quando o `.fx` fica mais novo que o
`.mgfxo`. O `.mgfxo` também fica no git, então quem só compila o jogo não precisa do mgfxc.
Ao atualizar o MonoGame, atualize junto a versão do `dotnet-mgfxc` e recompile o shader (o formato
do `.mgfxo` acompanha a versão).

`Bloom.fx` tem três técnicas, usadas por `Rendering/Bloom.cs`:

- **Bright**: guarda só o que passa do limiar de brilho (com joelho suave);
- **Blur**: gaussiano separável de 9 amostras (meia e um quarto da resolução);
- **Combine**: soma os dois halos na imagem e escurece levemente as bordas (vinheta).
