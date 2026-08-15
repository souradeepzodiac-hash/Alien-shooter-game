"""Chroma-key sprites, clean backgrounds, and synthesize audio for VOID HUNTER."""

from __future__ import annotations

import colorsys
import math
import os
import struct
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = Path(
    r"C:\Users\KIIT0001\.grok\sessions\C%3A%5CUsers%5CKIIT0001\019ffc9d-ad8b-7b20-b30c-99574fa2f3f2\images"
)
ROOT = Path(r"C:\Users\KIIT0001\VoidHunter")
SPR = ROOT / "Assets" / "Sprites"
AUD = ROOT / "Assets" / "Audio"
SPR.mkdir(parents=True, exist_ok=True)
AUD.mkdir(parents=True, exist_ok=True)


def is_magenta(r: int, g: int, b: int) -> bool:
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
    # Hot pink / magenta only. Do not treat red (hue ~0) as backdrop.
    return 0.78 <= h <= 0.96 and s >= 0.28 and v >= 0.22


def chroma_key(im: Image.Image) -> Image.Image:
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    alpha = Image.new("L", (w, h), 0)
    ap = alpha.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if is_magenta(r, g, b):
                ap[x, y] = 0
            else:
                # Soft-key leftover pink fringe.
                hh, ss, vv = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
                if 0.80 <= hh <= 0.95 and ss >= 0.16 and vv >= 0.25:
                    t = min(1.0, (ss - 0.16) / 0.35)
                    ap[x, y] = int(255 * (1.0 - t * 0.85))
                else:
                    ap[x, y] = 255

    # Flood from the border so leftover backdrop is gone.
    stack = []
    for x in range(w):
        stack.append((x, 0))
        stack.append((x, h - 1))
    for y in range(h):
        stack.append((0, y))
        stack.append((w - 1, y))
    seen = set()
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or x < 0 or y < 0 or x >= w or y >= h:
            continue
        seen.add((x, y))
        r, g, b, _ = px[x, y]
        if ap[x, y] == 0 or is_magenta(r, g, b):
            ap[x, y] = 0
            stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))

    alpha = alpha.filter(ImageFilter.GaussianBlur(radius=0.8))
    im.putalpha(alpha)
    return im


def crop_alpha(im: Image.Image, pad: int = 10) -> Image.Image:
    bbox = im.getchannel("A").point(lambda p: 255 if p > 12 else 0).getbbox()
    if not bbox:
        return im
    l, t, r, b = bbox
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(im.width, r + pad)
    b = min(im.height, b + pad)
    return im.crop((l, t, r, b))


def fit_max(im: Image.Image, longest: int) -> Image.Image:
    w, h = im.size
    scale = longest / float(max(w, h))
    nw, nh = max(1, int(w * scale)), max(1, int(h * scale))
    return im.resize((nw, nh), Image.Resampling.LANCZOS)


def wipe_watermark(im: Image.Image) -> Image.Image:
    """Paint out the usual bottom-right generator mark."""
    im = im.convert("RGB")
    w, h = im.size
    rw, rh = max(90, w // 8), max(36, h // 16)
    x0, y0 = w - rw - 8, h - rh - 6
    sample = im.crop((x0 - 24, y0 - 8, x0, y0 + rh))
    avg = tuple(int(c) for c in np.array(sample).mean(axis=(0, 1)))
    draw = ImageDraw.Draw(im)
    draw.rectangle((x0, y0, w, h), fill=avg)
    return im.filter(ImageFilter.GaussianBlur(radius=0.2))


def process_sprite(src_name: str, dest_name: str, longest: int) -> None:
    src = SRC / src_name
    im = Image.open(src)
    im = chroma_key(im)
    im = crop_alpha(im)
    im = fit_max(im, longest)
    dest = SPR / dest_name
    im.save(dest, "PNG")
    print(f"sprite {dest.name} {im.size}")


def process_bg(src_name: str, dest_name: str, size: tuple[int, int]) -> None:
    im = wipe_watermark(Image.open(SRC / src_name))
    im = im.resize(size, Image.Resampling.LANCZOS)
    dest = SPR / dest_name
    im.save(dest, "PNG", optimize=True)
    print(f"bg {dest.name} {im.size}")


def write_wav(path: Path, samples: np.ndarray, rate: int = 44100) -> None:
    samples = np.clip(samples, -1.0, 1.0)
    pcm = (samples * 32767.0).astype(np.int16)
    with wave.open(str(path), "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(rate)
        wf.writeframes(pcm.tobytes())
    print(f"wav {path.name} {len(pcm) / rate:.2f}s")


def env(n: int, attack: float, release: float, rate: int = 44100) -> np.ndarray:
    a = max(1, int(attack * rate))
    r = max(1, int(release * rate))
    e = np.ones(n, dtype=np.float64)
    a = min(a, n)
    r = min(r, n)
    e[:a] = np.linspace(0.0, 1.0, a)
    if r < n:
        e[n - r :] = np.linspace(1.0, 0.0, r)
    return e


def tone(freq: float, dur: float, kind: str = "sine", rate: int = 44100) -> np.ndarray:
    n = int(dur * rate)
    t = np.arange(n) / rate
    ph = 2 * math.pi * freq * t
    if kind == "sine":
        return np.sin(ph)
    if kind == "square":
        return np.sign(np.sin(ph) + 1e-9)
    if kind == "saw":
        return 2.0 * ((t * freq) % 1.0) - 1.0
    if kind == "tri":
        return 2.0 * np.abs(2.0 * ((t * freq) % 1.0) - 1.0) - 1.0
    if kind == "noise":
        return np.random.default_rng(int(freq * 10)).uniform(-1.0, 1.0, n)
    return np.sin(ph)


def mix(*parts: np.ndarray) -> np.ndarray:
    n = max(len(p) for p in parts)
    out = np.zeros(n, dtype=np.float64)
    for p in parts:
        out[: len(p)] += p
    return out


def sfx_shoot(freq: float = 880) -> np.ndarray:
    a = tone(freq, 0.07, "square") * env(int(0.07 * 44100), 0.002, 0.06) * 0.22
    b = tone(freq * 0.5, 0.09, "sine") * env(int(0.09 * 44100), 0.001, 0.08) * 0.18
    click = tone(1800, 0.02, "noise") * env(int(0.02 * 44100), 0.001, 0.018) * 0.08
    return mix(a, b, click)


def sfx_rail() -> np.ndarray:
    sweep_n = int(0.22 * 44100)
    t = np.arange(sweep_n) / 44100
    freq = np.linspace(220, 1400, sweep_n)
    ph = 2 * math.pi * np.cumsum(freq) / 44100
    body = np.sin(ph) * env(sweep_n, 0.01, 0.12) * 0.28
    noise = tone(100, 0.18, "noise") * env(int(0.18 * 44100), 0.005, 0.12) * 0.08
    return mix(body, noise)


def sfx_explode(big: bool) -> np.ndarray:
    dur = 0.55 if big else 0.28
    n = int(dur * 44100)
    noise = tone(40, dur, "noise") * env(n, 0.002, dur * 0.85)
    boom = tone(70 if big else 110, dur, "sine") * env(n, 0.004, dur * 0.9)
    crack = tone(600, dur * 0.35, "noise") * env(int(dur * 0.35 * 44100), 0.001, dur * 0.3)
    gain = 0.38 if big else 0.28
    return mix(noise * gain, boom * (0.32 if big else 0.22), crack * 0.12)


def sfx_hit() -> np.ndarray:
    a = tone(420, 0.06, "square") * env(int(0.06 * 44100), 0.001, 0.05) * 0.16
    b = tone(160, 0.08, "sine") * env(int(0.08 * 44100), 0.001, 0.07) * 0.14
    return mix(a, b)


def sfx_hurt() -> np.ndarray:
    n = int(0.28 * 44100)
    t = np.arange(n) / 44100
    freq = np.linspace(320, 90, n)
    ph = 2 * math.pi * np.cumsum(freq) / 44100
    return np.sin(ph) * env(n, 0.005, 0.2) * 0.32


def sfx_pickup() -> np.ndarray:
    a = tone(523, 0.09, "sine") * env(int(0.09 * 44100), 0.004, 0.06) * 0.22
    b = tone(784, 0.12, "sine") * env(int(0.12 * 44100), 0.01, 0.08) * 0.2
    c = tone(1046, 0.16, "sine") * env(int(0.16 * 44100), 0.02, 0.1) * 0.16
    # stagger
    z = np.zeros(int(0.04 * 44100))
    return mix(a, np.concatenate([z, b]), np.concatenate([z * 2, c]))


def sfx_dash() -> np.ndarray:
    n = int(0.16 * 44100)
    t = np.arange(n) / 44100
    freq = np.linspace(200, 900, n)
    ph = 2 * math.pi * np.cumsum(freq) / 44100
    whoosh = np.sin(ph) * env(n, 0.01, 0.08) * 0.22
    air = tone(80, 0.16, "noise") * env(n, 0.005, 0.12) * 0.1
    return mix(whoosh, air)


def sfx_ui(high: bool) -> np.ndarray:
    f = 880 if high else 520
    return tone(f, 0.06, "sine") * env(int(0.06 * 44100), 0.002, 0.05) * 0.2


def sfx_wave() -> np.ndarray:
    parts = []
    for i, f in enumerate((220, 330, 440, 660)):
        pad = np.zeros(int(0.07 * i * 44100))
        body = tone(f, 0.22, "sine") * env(int(0.22 * 44100), 0.01, 0.16) * 0.16
        parts.append(np.concatenate([pad, body]))
    return mix(*parts)


def sfx_boss() -> np.ndarray:
    n = int(1.1 * 44100)
    t = np.arange(n) / 44100
    bass = np.sin(2 * math.pi * 55 * t) * env(n, 0.05, 0.4) * 0.3
    growl = np.sin(2 * math.pi * 73 * t + 0.4 * np.sin(2 * math.pi * 6 * t)) * env(n, 0.08, 0.5) * 0.18
    brass = tone(110, 0.7, "saw") * env(int(0.7 * 44100), 0.08, 0.4) * 0.08
    return mix(bass, growl, brass)


def sfx_shield() -> np.ndarray:
    a = tone(640, 0.08, "tri") * env(int(0.08 * 44100), 0.002, 0.06) * 0.16
    b = tone(1280, 0.1, "sine") * env(int(0.1 * 44100), 0.004, 0.08) * 0.1
    return mix(a, b)


def make_music(path: Path, battle: bool) -> None:
    rate = 44100
    bpm = 118 if battle else 92
    beat = 60.0 / bpm
    bars = 16
    beats_per_bar = 4
    total_beats = bars * beats_per_bar
    n = int(total_beats * beat * rate)
    out = np.zeros(n, dtype=np.float64)
    rng = np.random.default_rng(7 if battle else 3)

    # A minor-ish dark progression looping cleanly.
    chords = [
        [110.00, 130.81, 164.81],  # Am
        [87.31, 130.81, 174.61],  # F
        [98.00, 123.47, 146.83],  # G
        [82.41, 123.47, 164.81],  # E
    ]

    def place(sig: np.ndarray, at: float, gain: float = 1.0) -> None:
        i = int(at * rate)
        if i >= n:
            return
        sl = min(len(sig), n - i)
        out[i : i + sl] += sig[:sl] * gain

    for bar in range(bars):
        chord = chords[bar % 4]
        start = bar * beats_per_bar * beat
        # pad
        pad_n = int(beats_per_bar * beat * rate)
        t = np.arange(pad_n) / rate
        pad = np.zeros(pad_n)
        for f in chord:
            pad += np.sin(2 * math.pi * f * t) * 0.10
            pad += np.sin(2 * math.pi * f * 2 * t) * 0.03
        pad *= env(pad_n, 0.04, 0.18)
        place(pad, start, 0.9 if battle else 1.1)

        # bass on beats
        for b in range(4):
            bass = tone(chord[0] / 2, beat * 0.7, "sine" if not battle else "tri")
            bass *= env(len(bass), 0.01, beat * 0.45)
            place(bass, start + b * beat, 0.28 if battle else 0.18)

        if battle:
            # kick
            for b in range(4):
                kn = int(0.18 * rate)
                kt = np.arange(kn) / rate
                kf = np.linspace(140, 42, kn)
                kick = np.sin(2 * math.pi * np.cumsum(kf) / rate) * env(kn, 0.002, 0.15)
                place(kick, start + b * beat, 0.34)
            # hat
            for b in range(8):
                hat = tone(8000, 0.04, "noise") * env(int(0.04 * rate), 0.001, 0.03)
                place(hat, start + b * (beat / 2), 0.045 if b % 2 else 0.03)
            # arp 16ths
            arp = [chord[0] * 2, chord[1] * 2, chord[2] * 2, chord[1] * 2]
            for s in range(16):
                note = tone(arp[s % 4], beat / 4 * 0.85, "sine")
                note *= env(len(note), 0.004, beat / 6)
                place(note, start + s * (beat / 4), 0.09)
            # lead every other bar
            if bar % 2 == 0:
                melody = [chord[2] * 2, chord[1] * 2, chord[2] * 2, chord[0] * 4]
                for i, f in enumerate(melody):
                    lead = tone(f, beat * 0.85, "tri") * env(int(beat * 0.85 * rate), 0.02, beat * 0.5)
                    place(lead, start + i * beat, 0.08)
        else:
            # gentle arp
            arp = [chord[0] * 2, chord[1] * 2, chord[2] * 2, chord[1] * 2]
            for s in range(8):
                note = tone(arp[s % 4], beat / 2 * 0.9, "sine")
                note *= env(len(note), 0.01, beat / 3)
                place(note, start + s * (beat / 2), 0.07)

    # soft limiter
    peak = np.max(np.abs(out)) + 1e-6
    out = out / peak * 0.72
    write_wav(path, out, rate)


def make_icon() -> None:
    src = SPR / "player.png"
    im = Image.open(src).convert("RGBA")
    # square canvas
    side = max(im.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(im, ((side - im.width) // 2, (side - im.height) // 2), im)
    ico = canvas.resize((256, 256), Image.Resampling.LANCZOS)
    dest = ROOT / "Assets" / "icon.ico"
    ico.save(dest, sizes=[(256, 256), (128, 128), (64, 64), (32, 32), (16, 16)])
    print(f"icon {dest}")


def main() -> None:
    process_sprite("3.jpg", "player.png", 160)
    process_sprite("2.jpg", "scout.png", 118)
    process_sprite("1.jpg", "strafer.png", 140)
    process_sprite("4.jpg", "bruiser.png", 168)
    process_sprite("5.jpg", "wasp.png", 92)
    process_sprite("6.jpg", "spitter.png", 148)
    process_sprite("7.jpg", "boss.png", 280)
    process_sprite("10.jpg", "health.png", 92)
    process_sprite("9.jpg", "weapon.png", 96)
    process_sprite("8.jpg", "shield.png", 100)
    process_sprite("11.jpg", "overdrive.png", 96)
    process_bg("12.jpg", "nebula.png", (1920, 1080))
    process_bg("13.jpg", "menu.png", (1920, 1080))
    make_icon()

    write_wav(AUD / "shoot_pulse.wav", sfx_shoot(920))
    write_wav(AUD / "shoot_spread.wav", sfx_shoot(640))
    write_wav(AUD / "shoot_rail.wav", sfx_rail())
    write_wav(AUD / "shoot_nova.wav", mix(sfx_shoot(280) * 0.6, sfx_explode(False) * 0.25))
    write_wav(AUD / "hit.wav", sfx_hit())
    write_wav(AUD / "explode.wav", sfx_explode(False))
    write_wav(AUD / "explode_big.wav", sfx_explode(True))
    write_wav(AUD / "hurt.wav", sfx_hurt())
    write_wav(AUD / "pickup.wav", sfx_pickup())
    write_wav(AUD / "dash.wav", sfx_dash())
    write_wav(AUD / "ui.wav", sfx_ui(False))
    write_wav(AUD / "ui_ok.wav", sfx_ui(True))
    write_wav(AUD / "wave.wav", sfx_wave())
    write_wav(AUD / "boss.wav", sfx_boss())
    write_wav(AUD / "shield.wav", sfx_shield())
    make_music(AUD / "music_theme.wav", battle=False)
    make_music(AUD / "music_battle.wav", battle=True)
    print("done")


if __name__ == "__main__":
    main()
