# Créditos da arte

Toda a arte de terceiros do jogo está em `GuitarPlay/Content/` e é **CC0 1.0** (domínio público:
pode usar, modificar e distribuir, inclusive comercialmente, sem pedir permissão). Crédito não é
obrigatório, mas fica registrado aqui. As licenças originais estão em `GuitarPlay/Content/Licenses/`.

| Pacote | Autor | Origem | Licença | Usado em |
|---|---|---|---|---|
| LowPoly Concert Props (`lowpoly_concert_pack.zip`) | iPoly3D | <https://opengameart.org/content/lowpoly-concert-props> | CC0 1.0 | `Content/Stage/`: palco, parede, treliça, caixas de som, refletores, microfone, grade e as texturas `albedo.png`/`glow.png` (OBJ, sem alteração) |
| Toon Characters 1 (`kenney_toonCharacters1.zip`) | Kenney (kenney.nl) | <https://opengameart.org/content/toon-characters-1> | CC0 1.0 | `Content/Crowd/`: poses HD de 4 personagens, desenhadas como silhuetas da plateia. `Content/Band/`: os 4 músicos da banda, gerados a partir dos SVGs do pacote com figurinos novos (punk, jazz, reggae, hip-hop) — ver `tools/band-art/` |
| Particle Pack (`kenney_particle-pack.zip`) | Kenney (kenney.nl) | <https://kenney.nl/assets/particle-pack> | CC0 1.0 | `Content/Fx/`: chamas (`muzzle_*`, `flame_*`), fumaça, raios (`spark_*`), brilhos e estrelas (versões com fundo preto, para luz aditiva) |

Só os arquivos usados foram copiados para o projeto; os nomes dos modelos com espaço foram trocados
por `_` (por exemplo `truss structure.001.obj` → `truss_structure.001.obj`).

O shader de bloom (`Content/Effects/Bloom.fx`) e o resto do código são do próprio projeto.
