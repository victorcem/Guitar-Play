"""Os 4 músicos da banda no estilo Kenney Toon Characters (CC0): mesmas formas e cores chapadas,
com figurino, cabelo e acessórios redesenhados para cada estilo musical. As formas novas usam
curvas suaves e sombra em meia-lua (nada de cortes retos), para combinar com o desenho orgânico."""
import glob, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from kenney_rig import Character, build_part

root, out = sys.argv[1], sys.argv[2]


def load(name):
    return Character(glob.glob(os.path.join(root, name, 'Vector', '*.svg'))[0])


def shaded(shape, shadow, light, dark):
    """Forma inteira na cor clara + meia-lua de sombra (lado direito) na cor escura."""
    return '<path fill="%s" d="%s"/><path fill="%s" d="%s"/>' % (light, shape, dark, shadow)


def dots(points, color, r):
    return ''.join('<circle cx="%g" cy="%g" r="%g" fill="%s"/>' % (x, y, r, color) for x, y in points)


# ───────────── Guitarrista punk rock (base: Male adventurer) ─────────────
punk = load('Male adventurer')


def spike(x, top, lean):
    """Espeto do moicano: base larga, lados curvos e ponta arredondada, inclinado para trás."""
    tip = x + lean
    shape = ('M%g -23 Q%g %g %g %g Q%g %g %g %g Q%g %g %g -23 Z'
             % (x - 6, x - 5, (top - 23) / 2, tip - 1.2, top + 1.5, tip, top - 0.8, tip + 1.2, top + 1.5,
                x + 5, (top - 23) / 2, x + 6))
    shadow = ('M%g -23 Q%g %g %g %g Q%g %g %g %g Q%g %g %g -23 Z'
              % (x + 0.5, x + 1.5, (top - 23) / 2, tip + 0.2, top + 2, tip, top - 0.8, tip + 1.2, top + 1.5,
                 x + 5, (top - 23) / 2, x + 6))
    return shaded(shape, shadow, '#FF55A8', '#E0388C')


mohawk = ''.join(spike(x, top, lean) for x, top, lean in
                 [(-12, -44, -3), (-5, -57, -2), (3, -62, 0), (10, -54, 2), (16, -43, 3)])
punk_head = (
    # Couro cabeludo raspado: cúpula arredondada acompanhando o topo da cabeça.
    shaded('M-21.5 -21 Q-22 -33 -8 -34.5 Q0 -35.5 8 -34.5 Q22 -33 21.5 -21 Q0 -24 -21.5 -21Z',
           'M8 -34.5 Q22 -33 21.5 -21 Q14 -22.5 10 -23 Q14 -29 8 -34.5Z', '#EBC6A4', '#DDB28D')
    + mohawk
    + '<circle cx="14" cy="-15.5" r="1.3" fill="#E4E7EE"/>'  # piercing na sobrancelha
    + '<circle cx="-25.5" cy="2.5" r="2.3" fill="none" stroke="#E4E7EE" stroke-width="1.2"/>'
    + '<circle cx="24.5" cy="2.5" r="2.3" fill="none" stroke="#E4E7EE" stroke-width="1.2"/>')
punk_body = (
    # Camiseta vermelha entre as lapelas, com um rasgo.
    shaded('M-4.5 -27 Q0 -24 4.5 -27 L4 -2 Q0 -1 -4 -2Z', 'M0.5 -25 Q3 -25.5 4.5 -27 L4 -2 Q2 -1.5 0.5 -1.6Z',
           '#D63A45', '#B32834')
    + '<path fill="#8E1C26" d="M-2.2 -16 Q0 -14.5 1.2 -12.5 Q-0.4 -12.8 -1.6 -11.6 Q-1.2 -13.6 -2.2 -16Z"/>'
    + dots([(-11, -20), (-11.5, -13), (-11.8, -6), (11, -20), (11.5, -13), (11.8, -6)], '#E4E7EE', 1.2))
punk_leg = ('<path stroke="#7A1620" stroke-width="1.1" stroke-linecap="round" d="M-5.5 -3 L5.5 -3 M-5.5 2 L5.5 2"/>'
            '<path stroke="#F2C94C" stroke-width="0.7" stroke-linecap="round" d="M0 -6.5 L0 5.5"/>')
punk_cfg = dict(
    swaps={
        'head_0_Layer0_3': {'#BF7958': '#5A3426'},
        'body': {'#3DBF7B': '#2E2E36', '#25A863': '#222229', '#BF7958': '#3A3A44', '#EEA160': '#2E2E36'},
        'arm': {'#3DBF7B': '#2E2E36', '#25A863': '#222229', '#FFFFFF': '#3A3A44'},
        'leg': {'#D78B49': '#B3242F', '#EEA160': '#D63A45', '#BF7958': '#26262E'},
    },
    hide=['head_0_Layer0_2_FILL', 'head_0_Layer0_7_FILL'],
    extras={'head': punk_head, 'body': punk_body, 'leg': punk_leg})
punk_svg = punk.build(height=128, **punk_cfg)

# ───────────── Vocalista de jazz (base: Female person) ─────────────
jazz = load('Female person')
petals = [(19, -28.5), (24.5, -24), (22.5, -17.5), (15.5, -18), (13.5, -24)]
gardenia = (''.join('<ellipse cx="%g" cy="%g" rx="4.6" ry="4" fill="#FFF8EC" transform="rotate(%g %g %g)"/>'
                    % (x, y, (i * 72) % 180, x, y) for i, (x, y) in enumerate(petals))
            + '<path fill="#EDE3D0" d="M19 -26 Q23 -24 22.5 -20 Q19 -17.5 15.8 -20.5 Q16 -24.5 19 -26Z"/>'
            + '<circle cx="19.2" cy="-22.2" r="1.7" fill="#F2C94C"/>')
pearls = dots([(-10, 12.5), (-6.8, 15), (-2.4, 16.2), (2.4, 16.2), (6.8, 15), (10, 12.5)], '#F7F4EC', 1.7)
jazz_head = gardenia + pearls  # colar logo abaixo do queixo
jazz_body = (
    # Saia longa que abre em sino até o chão, com barra ondulada, sombra lateral e paetês.
    shaded('M-15 -4 Q-17 8 -25 21 Q-19 24.5 -12 22.5 Q-6 25 0 23 Q6 25 12 22.5 Q19 24.5 25 21 Q17 8 15 -4 Q0 -2 -15 -4Z',
           'M6 -3.4 Q11 -3.2 15 -4 Q17 8 25 21 Q19 24.5 12 22.5 Q9 23.6 6 23.4 Q12 10 6 -3.4Z', '#B8264F', '#97193F')
    + dots([(-12, 4), (-6, 12), (-16, 17), (5, 7), (11, 15), (1, 19), (-3, 1), (8.5, 0), (-19, 20), (18, 19)], '#FFC2D6', 0.8))
jazz_cfg = dict(
    swaps={
        'head_0_Layer0_7': {'#EEA160': '#F7F4EC'}, 'head_0_Layer0_8': {'#EEA160': '#F7F4EC'},
        'head_0_Layer0_1_MEMBER_2_MEMBER_1': {'#A8603F': '#C8233F'},
        'body': {'#3DBF7B': '#B8264F', '#25A863': '#97193F', '#676AC6': '#B8264F', '#3C3E41': '#97193F', '#FFFFFF': '#B8264F'},
        'arm': {'#3DBF7B': '#B8264F', '#25A863': '#97193F', '#BF7958': '#2A2430'},
        'hand': {'#BF7958': '#2A2430', '#A8603F': '#1F1A25'},
    },
    hide=['head_0_Layer0_3_FILL', 'leg'],
    extras={'head': jazz_head, 'body': jazz_body})
jazz_svg = jazz.build(height=128, **jazz_cfg)

# ───────────── Baixista de reggae (base: Male person) ─────────────
reggae = load('Male person')
# Touca rasta "fofa": cúpula larga que cai um pouco para trás, com faixas curvas.
tam = (
    shaded('M-27.5 -19.5 Q-30 -27 -29 -33 Q-27 -55 0 -57 Q27 -55 29 -33 Q30 -27 27.5 -19.5 Q0 -23 -27.5 -19.5Z',
           'M12 -55 Q27 -53 29 -33 Q30 -27 27.5 -19.5 Q22 -20.5 17 -21 Q24 -38 12 -55Z', '#D93A3A', '#BF2C2C')
    + shaded('M-29 -33 Q0 -40 29 -33 Q30 -29 29.6 -26.5 Q0 -33 -29.6 -26.5 Q-30 -29 -29 -33Z',
             'M17 -36 Q24 -35 29 -33 Q30 -29 29.6 -26.5 Q24 -28 19 -28.8 Q19.5 -32 17 -36Z', '#F6C945', '#E0B030')
    + shaded('M-29.6 -26.5 Q0 -33 29.6 -26.5 Q29 -22 27.5 -19.5 Q0 -23 -27.5 -19.5 Q-29 -22 -29.6 -26.5Z',
             'M19 -28.8 Q24 -28 29.6 -26.5 Q29 -22 27.5 -19.5 Q23 -20.3 18.5 -20.8 Q19.5 -24.5 19 -28.8Z', '#2F9E54', '#25874A'))
beard = ('<path fill="#2B2B30" d="M-19.5 -7 Q-21 4 -14 10 Q-7 15 0 15 Q7 15 14 10 Q20.5 4 19 -7 '
         'Q15.5 -7.5 14 -6 Q13.5 3 7 5.5 Q0 7.5 -7 5.5 Q-13.5 3 -14 -6 Q-15.5 -7.5 -19.5 -7Z"/>'
         '<path fill="#FFFFFF" d="M-5 2.6 Q0 3.6 5 2.6 Q4.5 6 0 6.3 Q-4.5 6 -5 2.6Z"/>')
dreads = ''.join('<path fill="%s" d="M%g -24 Q%g -4 %g %g Q%g %g %g %g Q%g %g %g -4 Q%g -14 %g -24Z"/>'
                 % (c, x, x - 1, x + w * 0.1 + sway, len_, x + w * 0.5 + sway, len_ + 3, x + w + sway, len_,
                    x + w + 1, len_ - 6, x + w + 1, x + w, x + w)
                 for x, w, len_, sway, c in [(-31.5, 5, 16, -1.5, '#3A2A1E'), (-26.5, 5, 21, -0.8, '#4A3626'),
                                             (21.5, 5, 21, 0.8, '#3A2A1E'), (26.5, 5, 16, 1.5, '#2F2218')])
reggae_body = (dots([(-13, -18), (-10, -8), (-14, -2), (12, -18), (9, -8), (13, -2)], '#F6C945', 1.1)
               + dots([(-7, -24), (-5, -21), (-2, -19.3), (1.5, -19.3), (4.5, -21), (6.5, -24)], '#9B6A3C', 1.45))
reggae_cfg = dict(
    swaps={
        'body_0_Layer0_0_MEMBER_1': {'#D78B49': '#25874A', '#EEA160': '#2F9E54'},
        'body_0_Layer0_0_MEMBER_2': {'#D78B49': '#25874A', '#EEA160': '#2F9E54'},
        'body_0_Layer0_0': {'#BFD4E5': '#F2EFE6', '#E7F3FC': '#FFFFFF', '#676AC6': '#B39860'},
        'arm': {'#BFD4E5': '#2F9E54', '#E7F3FC': '#25874A'},
        'leg': {'#4C4EAA': '#B39860', '#676AC6': '#C9B07A', '#FFFFFF': '#8A5A33', '#EEA160': '#7A4A2C'},
    },
    hide=['head_0_Layer0_0_FILL', 'head_0_Layer0_4_FILL', 'head_0_Layer0_5_FILL'],
    extras_before={'head': dreads},
    extras={'head': beard + tam, 'body': reggae_body})
reggae_svg = reggae.build(height=128, **reggae_cfg)

# ───────────── Baterista de hip-hop (base: Female adventurer) ─────────────
hiphop = load('Female adventurer')
skin = {'#FFD7B1': '#C98D5E', '#F0C49A': '#B57A4D', '#E7B687': '#A8703F'}
cap = (
    # Aba virada para trás: aparece como uma meia-lua por cima da copa.
    shaded('M-13 -38 Q-12 -45 0 -45.5 Q12 -45 13 -38 Q0 -40.5 -13 -38Z',
           'M5 -45 Q12 -44.6 13 -38 Q9.5 -38.8 6.5 -39.2 Q8 -42 5 -45Z', '#B52B38', '#9A2230')
    + shaded('M-24.5 -19.5 Q-25.5 -42 0 -42.5 Q25.5 -42 24.5 -19.5 Q0 -22.5 -24.5 -19.5Z',
             'M9 -41.5 Q25 -39 24.5 -19.5 Q19 -20.5 15 -20.8 Q19 -32 9 -41.5Z', '#E0414F', '#C73442')
    + '<path fill="#B52B38" d="M-24.6 -22.5 Q0 -25.8 24.6 -22.5 Q24.8 -20.8 24.5 -19.5 Q0 -22.5 -24.5 -19.5 Q-24.8 -20.8 -24.6 -22.5Z"/>'
    + '<circle cx="0" cy="-32" r="2.5" fill="#FFFFFF"/>')
shades = ('<path fill="#1F1F27" d="M-17.5 -12 Q-17.5 -15.5 -10.5 -15.5 Q-3 -15.5 -3 -12 Q-3.5 -6.2 -10.5 -6.2 Q-17 -6.2 -17.5 -12Z"/>'
          '<path fill="#1F1F27" d="M2 -12 Q2 -15.5 9 -15.5 Q16.5 -15.5 16.5 -12 Q16 -6.2 9 -6.2 Q2.5 -6.2 2 -12Z"/>'
          '<path stroke="#1F1F27" stroke-width="2" fill="none" d="M-3.5 -12.5 Q-0.5 -14 2.5 -12.5"/>'
          '<path stroke="#7A7A90" stroke-width="1.2" stroke-linecap="round" fill="none" d="M-14 -12.2 Q-11 -13.4 -8.5 -12.6 M5 -12.2 Q8 -13.4 10.5 -12.6"/>'
          '<circle cx="-25.5" cy="5.5" r="3.6" fill="none" stroke="#F6C945" stroke-width="1.4"/>'
          '<circle cx="24.5" cy="5.5" r="3.6" fill="none" stroke="#F6C945" stroke-width="1.4"/>')
jersey = ('<path stroke="#F6C945" stroke-width="1.3" stroke-linecap="round" fill="none" d="M-9 -27 Q-6 -16 0 -16 Q6 -16 9 -27"/>'
          + '<circle cx="0" cy="-16.5" r="2.7" fill="#F6C945"/><circle cx="0" cy="-16.5" r="1.2" fill="#D9A21E"/>'
          + ''.join('<ellipse cx="%g" cy="%g" rx="2.2" ry="2.6" fill="none" stroke="#F6C945" stroke-width="1.2"/>' % (x, y)
                    for x, y in [(-4, -9.5), (-4, -4), (4, -9.5), (4, -4)]))
sneaker = '<path stroke="#E0414F" stroke-width="1.4" stroke-linecap="round" fill="none" d="M-5.5 8.5 Q0 6.5 5.5 8.5"/>'
hiphop_cfg = dict(
    swaps={
        '': skin,
        'head_0_Layer0_1': {'#DB855C': '#2B2B33', '#BF7958': '#202028'},
        'body': {'#79ADCB': '#7B4BD1', '#5A93B4': '#6339B3', '#69A1C2': '#6E42C2', '#EEA160': '#F6C945'},
        'arm': {'#79ADCB': '#F5F5F5', '#5A93B4': '#DADDE5'},
        'leg': {'#C98D5E': '#3A63B0', '#EEA160': '#2C4F8F', '#BF7958': '#F7F7F9'},
    },
    hide=['head_0_Layer0_2_FILL', 'body_0_Layer0_1_FILL', 'body_0_Layer0_2_FILL', 'body_0_Layer0_4_FILL'],
    extras={'head': shades + cap, 'body': jersey, 'leg': sneaker})
hiphop_svg = hiphop.build(height=128, **hiphop_cfg)

for name, svg in [('1_punk', punk_svg), ('2_jazz', jazz_svg), ('3_reggae', reggae_svg), ('4_hiphop', hiphop_svg)]:
    open(os.path.join(out, name + '.svg'), 'w', encoding='utf-8').write(svg)

# Ajustes por expressão: nas variantes "focus"/"shock" as camadas têm outra numeração.
VARIANT_HIDE = {
    'guitarist': {'headFocus': ['headFocus_0_Layer0_4_FILL'], 'headShock': ['headShock_0_Layer0_4_FILL']},
    'vocalist': {'headFocus': ['headFocus_0_Layer0_7_FILL']},
}
VARIANT_SWAPS = {
    'vocalist': {'headFocus': {'headFocus': {'#EEA160': '#F7F4EC'}}},
}

MUSICIANS = [('guitarist', punk, punk_cfg), ('vocalist', jazz, jazz_cfg), ('bassist', reggae, reggae_cfg), ('drummer', hiphop, hiphop_cfg)]

# Peças separadas para o jogo (3x a escala do SVG), uma pasta por músico.
if len(sys.argv) > 3:
    parts_dir = sys.argv[3]
    for key, ch, cfg in MUSICIANS:
        folder = os.path.join(parts_dir, key)
        os.makedirs(folder, exist_ok=True)
        for variant in ['head', 'headFocus', 'headShock', 'body', 'arm', 'hand', 'leg']:
            if variant == 'leg' and 'leg' in cfg.get('hide', ()):
                continue
            svg = build_part(ch, variant, 3, cfg.get('swaps'), cfg.get('hide', ()), cfg.get('extras'), cfg.get('extras_before'),
                             VARIANT_SWAPS.get(key), VARIANT_HIDE.get(key))
            open(os.path.join(folder, variant + '.svg'), 'w', encoding='utf-8').write(svg)
print('ok')
