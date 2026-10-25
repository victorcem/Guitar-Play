# Guitar Play

Jogo estilo Guitar Hero para aprender pentatônicas na guitarra, seguindo a estrutura de estudo do ebook
*Pentatônicas* (Pedro Bernardi): cada capítulo vira um mundo, cada exercício um nível, e só se avança com estrelas.

Feito em C# com MonoGame (.NET 8).

## Rodar

```bash
dotnet run --project GuitarPlay
```

## Atividades (por desenho)

1. **Constelações**: lição guiada. Primeiro as tônicas, depois cada intervalo desenhado como um caminho
   a partir da tônica mais próxima ("na corda vizinha mais aguda, 2 casas à frente"), com som e uma dica do caráter da nota.
2. **Caça ao intervalo**: 60 s para clicar em todas as b3, depois 4, 5 e b7. Só as tônicas aparecem.
3. **Qual é essa nota?**: uma nota acende e toca depois da tônica; responda o grau (teclas 1 a 4). 12 perguntas.
4. **Pista**: as notas descem no tempo do metrônomo, estilo Guitar Hero.

## Controles

| Tela | Teclas |
|---|---|
| Menu | ↑ ↓ desenho · ← → atividade · + − BPM da pista · Enter começa · Esc sai |
| Constelações | → próximo · ← voltar · R ouvir de novo · Esc menu |
| Caça ao intervalo | clique nas notas · Esc menu |
| Qual é essa nota? | 1 a 4 ou clique · R ouvir de novo · Esc menu |
| Pista | 1 a 6 (número da corda) ou A S D F G H (6ª corda à esquerda) · Esc volta |
| Resultado | Enter joga de novo · Esc volta ao menu |

## Pontuação

- Perfeito: até 60 ms do tempo · Bom: até 130 ms.
- Multiplicador sobe a cada 10 notas de combo (até x4).
- Estrelas pela precisão: 1★ 60% · 2★ 80% · 3★ 95%. São necessárias 2★ para passar de nível.

## Etapas

1. **Pista** (feito): pista em perspectiva, metrônomo, entrada pelo teclado, Mundo 1 (5 desenhos de Sol menor).
2. **Ouvir a guitarra**: entrada pela Pocket Master / interface / microfone, afinador, calibração de latência (5 batidas).
3. **Níveis**: mundos do livro, desbloqueio por estrelas, progresso salvo.

O PDF do ebook não faz parte do repositório (direitos autorais).
