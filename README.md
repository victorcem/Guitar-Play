# Guitar Play

Jogo estilo Guitar Hero para aprender pentatônicas na guitarra, seguindo a estrutura de estudo do ebook
*Pentatônicas* (Pedro Bernardi): cada capítulo vira um mundo, cada exercício um nível, e só se avança com estrelas.

Feito em C# com MonoGame (.NET 8).

## Rodar

```bash
dotnet run --project GuitarPlay
```

## Controles

| Tela | Teclas |
|---|---|
| Menu | ↑ ↓ escolhe o nível · ← → muda o BPM · Enter começa · Esc sai |
| Pista | 1 a 6 (número da corda) ou A S D F G H (6ª corda à esquerda) · Esc volta |
| Resultado | Enter toca de novo · Esc volta ao menu |

## Pontuação

- Perfeito: até 60 ms do tempo · Bom: até 130 ms.
- Multiplicador sobe a cada 10 notas de combo (até x4).
- Estrelas pela precisão: 1★ 60% · 2★ 80% · 3★ 95%. São necessárias 2★ para passar de nível.

## Etapas

1. **Pista** (feito): pista em perspectiva, metrônomo, entrada pelo teclado, Mundo 1 (5 desenhos de Sol menor).
2. **Ouvir a guitarra**: entrada pela Pocket Master / interface / microfone, afinador, calibração de latência (5 batidas).
3. **Níveis**: mundos do livro, desbloqueio por estrelas, progresso salvo.

O PDF do ebook não faz parte do repositório (direitos autorais).
