"""Port addresses from one Scarlet / Violet build's main module to another's (Violet 4.0.0 -> Scarlet 4.0.0).

The two games are one codebase built twice, so the same function sits in both, maybe at another address. A code
address is found again by its surrounding instructions with the PC-relative fields masked out (adrp / adr / b / bl /
b.cond / cbz / tbz / ldr literal: those change when code moves); it must match exactly once. A data address (a table in
.rodata, a global in .data / .bss) is found through the code that loads it: the first adrp + add / ldr pair in the
source build that computes it, that instruction pair found again in the target, its address decoded there.

    python sv_port.py <from exefs/main> <to exefs/main> [0x7002b7a784 ...]   (no addresses: the built-in list)
    python sv_port.py --compare <main a> <main b>                              (how alike two builds are)
"""
import struct
import sys

from nso import Nso

BASE = 0x7000000000

# Every Pokémon Violet 4.0.0 address the port relies on (tools/sv_patches.py, runtime/hle/*): kind 'code' or 'data'
VIOLET_400 = [
    ('code', 0x7002b7a784, 'sv_patches _sv_frame_clock'),
    ('code', 0x7002b7a858, 'sv_patches _sv_frame_pace'),
    ('code', 0x7002b7b310, 'sv_patches _sv_slot_mode (two slots, a)'),
    ('code', 0x7002b7b33c, 'sv_patches _sv_slot_mode (two slots, b)'),
    ('code', 0x700092dd28, 'sv_patches _sv_slot_mode (seven slots)'),
    ('code', 0x7000cf1168, 'sv_patches _sv_proj_half_fovy (branch 1)'),
    ('code', 0x7000cf11e8, 'sv_patches _sv_proj_aspect_s16'),
    ('code', 0x7000cf182c, 'sv_patches _sv_proj_half_fovy (branch 2)'),
    ('code', 0x7000cf18a4, 'sv_patches _sv_proj_aspect_s4'),
    ('data', 0x7003c2fba8, 'frame mode duration table (sv_patches, framepace.c: +8 = entry 1)'),
    ('data', 0x7004759418, 'CentralHeap pointer (hle/libc.c)'),
    ('data', BASE + 0x473af30, 'NPC_MGR_GLOBAL (hle/npc_shown.c)'),
    ('data', BASE + 0x4468fd8, 'NPC_MGR_VTABLE (hle/npc_shown.c)'),
    ('data', BASE + 0x4763918, 'GLOBAL_HEAP (hle/npc_shown.c)'),
    ('data', BASE + 0x44a6500, 'GLOBAL_HEAP_VT (hle/npc_shown.c)'),
    ('data', BASE + 0x46d4e00, 'LOD_GLOBAL (hle/npc_detail.c)'),
    ('data', BASE + 0x44a00b8, 'LOD_MGR_VTABLE (hle/npc_detail.c)'),
    ('data', 0x7001c51358, 'delegate vtable (hle/delegate_null.c)'),
    ('data', 0x7001c51308, 'lock vtable (hle/delegate_null.c)'),
]


def load(path):
    n = Nso(path)
    t0, tn = n.text
    words = struct.unpack_from(f'<{tn // 4}I', n.image, t0)
    return n, words


def mask(w):
    """The instruction with its PC-relative field cleared (the bits that change when code or data moves)."""
    if (w & 0x9F000000) in (0x90000000, 0x10000000):  # adrp / adr: immlo + immhi
        return w & 0x9F00001F
    if (w & 0x7C000000) == 0x14000000:                # b / bl: imm26
        return w & 0xFC000000
    if (w & 0xFF000010) == 0x54000000:                # b.cond: imm19
        return w & 0xFF00001F
    if (w & 0x7E000000) == 0x34000000:                # cbz / cbnz: imm19
        return w & 0xFF00001F
    if (w & 0x7E000000) == 0x36000000:                # tbz / tbnz: imm14
        return w & 0xFFF8001F
    if (w & 0x3B000000) == 0x18000000:                # ldr (literal): imm19
        return w & 0xFF00001F
    # add / ldr / str immediate right after an adrp: their low 12 bits are the page offset of the data address; they
    # stay in the pattern (a page offset rarely changes alone), and a miss falls back to the shorter windows below
    return w


def find_code(src_words, dst_masked, addr, start=0):
    """The target address of the instruction at addr (source build), or (None, why)."""
    i = (addr - BASE - start) // 4
    for before, after in ((8, 8), (6, 6), (4, 8), (8, 4), (12, 12), (16, 16)):
        lo, hi = i - before, i + after + 1
        if lo < 0 or hi > len(src_words):
            continue
        pat = [mask(w) for w in src_words[lo:hi]]
        hits = find_all(dst_masked, pat)
        if len(hits) == 1:
            return BASE + start + (hits[0] + before) * 4, f'window -{before}/+{after}'
        if len(hits) > 1 and before + after >= 32:
            return None, f'{len(hits)} matches'
    return None, 'not found'


_index = {}


def find_all(hay, pat):
    """Positions where pat occurs in hay (both lists of masked words). Keyed on the pattern's first two words."""
    key = (id(hay), pat[0], pat[1])
    if key not in _index:
        _index[key] = [j for j in range(len(hay) - 1) if hay[j] == pat[0] and hay[j + 1] == pat[1]]
    n = len(pat)
    return [j for j in _index[key] if list(hay[j:j + n]) == pat][:4]


def decode_adrp(w, pc):
    imm = ((w >> 29) & 3) | (((w >> 5) & 0x7FFFF) << 2)
    if imm & (1 << 20):
        imm -= 1 << 21
    return (pc & ~0xFFF) + (imm << 12)


def lo12(w):
    """The low 12 bits an add / ldr / str (unsigned immediate) adds to an adrp's page, scaled by its access size."""
    if (w & 0xFF800000) in (0x91000000, 0x11000000):  # add (immediate), 64 / 32 bit
        return (w >> 10) & 0xFFF
    if (w & 0x3B000000) == 0x39000000:                # ldr / str (unsigned offset), any size
        size = w >> 30
        if (w & 0x04800000) == 0x04800000:            # SIMD&FP 128-bit
            size = 4
        return ((w >> 10) & 0xFFF) << size
    return None


def refs_to(words, target):
    """(index of the adrp, index of its add / ldr) pairs in words that compute target."""
    out = []
    page = target & ~0xFFF
    for i, w in enumerate(words):
        if (w & 0x9F000000) != 0x90000000:
            continue
        pc = BASE + i * 4
        if decode_adrp(w, pc) != page:
            continue
        rd = w & 0x1F
        for j in range(i + 1, min(i + 6, len(words))):
            o = lo12(words[j])
            if o is not None and ((words[j] >> 5) & 0x1F) == rd and page + o == target:
                out.append((i, j))
                break
        if len(out) >= 6:
            break
    return out


def find_data(src_words, dst_words, dst_masked, addr):
    refs = refs_to(src_words, addr)
    if not refs:
        return None, 'no adrp reference in the source code'
    seen = {}
    for i, j in refs:
        got, how = find_code(src_words, dst_masked, BASE + i * 4)
        if got is None:
            continue
        ti = (got - BASE) // 4
        tj = ti + (j - i)
        o = lo12(dst_words[tj])
        if o is None:
            continue
        new = decode_adrp(dst_words[ti], got) + o
        seen[new] = seen.get(new, 0) + 1
    if not seen:
        return None, f'{len(refs)} references, none found in the target'
    best = max(seen, key=seen.get)
    agree = f'{seen[best]}/{sum(seen.values())} references agree'
    return (best, agree) if len(seen) == 1 else (best, agree + ' (DISAGREE: check by hand)')


def compare(a, b):
    na, wa = load(a)
    nb, wb = load(b)
    print(f'a: build {na.build_id[:16]}  text {na.text[1]:#x}  rodata {na.rodata[1]:#x}  data {na.data[1]:#x}')
    print(f'b: build {nb.build_id[:16]}  text {nb.text[1]:#x}  rodata {nb.rodata[1]:#x}  data {nb.data[1]:#x}')
    n = min(len(wa), len(wb))
    same = sum(1 for i in range(n) if wa[i] == wb[i])
    print(f'text words identical at the same offset: {same}/{n} ({100 * same / n:.2f}%)')


def main():
    if sys.argv[1] == '--compare':
        return compare(sys.argv[2], sys.argv[3])
    src_n, src_words = load(sys.argv[1])
    dst_n, dst_words = load(sys.argv[2])
    dst_masked = [mask(w) for w in dst_words]
    print(f'from build {src_n.build_id[:16]} to build {dst_n.build_id[:16]}')
    todo = VIOLET_400 if len(sys.argv) < 4 else [('code', int(a, 16), '') for a in sys.argv[3:]]
    for kind, addr, what in todo:
        if kind == 'code':
            got, how = find_code(src_words, dst_masked, addr)
            if got is not None:
                ok = dst_words[(got - BASE) // 4] == src_words[(addr - BASE) // 4]
                how += '' if ok else ' (instruction itself differs!)'
        else:
            got, how = find_data(src_words, dst_words, dst_masked, addr)
        shown = f'{got:#x}' if got is not None else '-'
        delta = f'{got - addr:+#x}' if got is not None else ''
        print(f'{kind:4} {addr:#x} -> {shown:14} {delta:>10}  {how:40} {what}')


if __name__ == '__main__':
    main()
