"""Extrai a pose 'idle' de um personagem da Kenney (SVG CC0) e permite recolorir camadas e
acrescentar formas (cabelo, chapéu, roupa) no referencial de cada peça."""
import re

CELL_W, CELL_H = 96, 128


class Character:
    def __init__(self, svg_path):
        text = open(svg_path, encoding='utf-8').read()
        self.defs = text[text.index('<defs>') + 6:text.index('</defs>')]
        poses = text[text.index('<g id="poses"'):]
        self.instances = []  # (matrix, [layer ids]) na ordem de desenho
        for m in re.finditer(r'<g transform="matrix\(([^)]*)\) ">\s*((?:<g transform="matrix\( 1, 0, 0, 1, 0,0\) ">\s*<use xlink:href="#[^"]+"/>\s*</g>\s*)+)</g>', poses):
            nums = [float(x) for x in m.group(1).split(',')]
            e, f = nums[4], nums[5]
            if 0 <= e < CELL_W and 0 <= f < CELL_H:
                ids = re.findall(r'#([^"]+)"', m.group(2))
                self.instances.append((nums, ids))
        self.filter = re.search(r'<filter id="Filter_1".*?</filter>', text, re.S).group(0)
        self.gradient = re.search(r'<radialGradient id="Gradient_1".*?</radialGradient>', text, re.S).group(0)

    def part_of(self, layer_id):
        return layer_id.split('_0_')[0]

    def build(self, swaps=None, hide=(), extras=None, extras_before=None, scale=4, height=None):
        """swaps: {prefixo de camada: {'#cor velha': '#cor nova'}} (prefixo '' = todas);
        hide: ids ou peças a esconder; extras/extras_before: {peça: svg} desenhado depois/antes
        da peça, no referencial dela."""
        swaps = swaps or {}
        hide = tuple(hide) + ('shadow',)
        extras = extras or {}
        extras_before = extras_before or {}
        defs = self.defs

        def swap_group(m):
            gid, inner = m.group(1), m.group(2)
            for prefix, table in swaps.items():
                if gid.startswith(prefix):
                    for old, new in table.items():
                        inner = re.sub(re.escape(old), new, inner, flags=re.I)
            return '<g id="%s">%s</g>' % (gid, inner)
        defs = re.sub(r'<g id="([^"]+)">(.*?)</g>', swap_group, defs, flags=re.S)
        out = []
        done_before, done_after = set(), set()
        for nums, ids in self.instances:
            part = self.part_of(ids[0])
            matrix = 'matrix(%s)' % ','.join('%g' % n for n in nums)
            if part in extras_before and (part, tuple(nums)) not in done_before:
                out.append('<g transform="%s">%s</g>' % (matrix, extras_before[part]))
                done_before.add((part, tuple(nums)))
            uses = ''.join('<use xlink:href="#%s"/>' % i for i in ids if i not in hide and self.part_of(i) not in hide)
            out.append('<g transform="%s">%s</g>' % (matrix, uses))
            if part in extras and (part, tuple(nums)) not in done_after:
                out.append('<g transform="%s">%s</g>' % (matrix, extras[part]))
                done_after.add((part, tuple(nums)))
        body = ''.join(out)
        top = CELL_H - (height or CELL_H)
        return ('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
                'width="%d" height="%d" viewBox="0 %d %d %d"><defs>%s%s</defs>'
                '<g>%s</g></svg>') % (CELL_W * scale, (height or CELL_H) * scale, top, CELL_W, height or CELL_H,
                                      self.gradient, defs, body)

    def layers(self):
        return [(self.part_of(ids[0]), ids, nums) for nums, ids in self.instances]


# Moldura fixa de cada peça exportada (unidades da Kenney, origem = pivô da peça no SVG).
FRAMES = {
    'head': (-42, -70, 42, 28),
    'body': (-34, -36, 34, 30),
    'arm': (-10, -10, 26, 30),
    'hand': (-16, -18, 14, 14),
    'leg': (-12, -10, 12, 20),
}


def _retarget(key, base, variant):
    return variant + key[len(base):] if key.startswith(base + '_') else key


def build_part(ch, variant, scale, swaps=None, hide=(), extras=None, extras_before=None,
               variant_swaps=None, variant_hide=None):
    """SVG de uma peça só (ex.: 'headFocus'), com as mesmas trocas de cor e extras da peça base.
    variant_swaps/variant_hide: ajustes só daquela variante (a numeração das camadas muda entre expressões)."""
    base = re.match(r'(head|body|arm|hand|leg)', variant).group(1)
    swaps = {_retarget(k, base, variant): v for k, v in (swaps or {}).items()}
    swaps.update((variant_swaps or {}).get(variant, {}))
    hidden = {_retarget(h, base, variant) for h in hide} | set((variant_hide or {}).get(variant, ()))
    defs = ch.defs

    def swap_group(m):
        gid, inner = m.group(1), m.group(2)
        for prefix, table in swaps.items():
            if gid.startswith(prefix):
                for old, new in table.items():
                    inner = re.sub(re.escape(old), new, inner, flags=re.I)
        return '<g id="%s">%s</g>' % (gid, inner)
    defs = re.sub(r'<g id="([^"]+)">(.*?)</g>', swap_group, defs, flags=re.S)

    ids = [i for i in re.findall(r'<g id="(%s_0_[^"]+_FILL)">' % variant, ch.defs) if i not in hidden]
    x0, y0, x1, y1 = FRAMES[base]
    body = ((extras_before or {}).get(base, '') + ''.join('<use xlink:href="#%s"/>' % i for i in ids)
            + (extras or {}).get(base, ''))
    return ('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
            'width="%d" height="%d" viewBox="%g %g %g %g"><defs>%s</defs>%s</svg>'
            % ((x1 - x0) * scale, (y1 - y0) * scale, x0, y0, x1 - x0, y1 - y0, defs, body))
