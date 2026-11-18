#!/usr/bin/env python3
"""Generate the small sound effects for Whack-a-Mole.

The game needs four short sounds and no outside audio pack, so they are
synthesised here: a pop when a mole rises, a click when it is hit, a low boom
for a bomb and a short cheer for a new high score. Output: 44.1 kHz, 16-bit mono
WAV files in Assets/Audio.
"""
import math, os, random, struct, wave

RATE = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio")


def write_wav(name, samples):
    """Write float samples (-1..1) to Assets/Audio/<name>.wav."""
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".wav")
    frames = bytearray()
    for s in samples:
        v = max(-1.0, min(1.0, s))
        frames += struct.pack("<h", int(v * 32000))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(bytes(frames))
    print("wrote", path, len(samples) / RATE, "s")


def env(i, n, attack=0.01, curve=3.0):
    """Quick attack, exponential decay, silent at the ends."""
    t = i / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    return a * math.exp(-curve * i / n)


def pop():
    """Short upward blip: a mole popping out of its hole."""
    n = int(0.13 * RATE)
    out = []
    phase = 0.0
    for i in range(n):
        f = 380 + 900 * (i / n) ** 0.6          # pitch rises
        phase += 2 * math.pi * f / RATE
        out.append(0.42 * math.sin(phase) * env(i, n, 0.004, 5.0))
    return out


def hit():
    """Soft whack: a click plus a quick mid tone."""
    n = int(0.12 * RATE)
    random.seed(7)
    out = []
    for i in range(n):
        t = i / RATE
        noise = random.uniform(-1, 1) * math.exp(-90 * t) * 0.5
        tone = math.sin(2 * math.pi * 520 * t) * math.exp(-28 * t) * 0.7
        out.append(0.5 * (noise + tone))
    return out


def bomb():
    """Low boom for the bomb: falling noise plus a 70 Hz thump."""
    n = int(0.45 * RATE)
    random.seed(11)
    out = []
    low = 0.0
    for i in range(n):
        t = i / RATE
        raw = random.uniform(-1, 1)
        low = low * 0.82 + raw * 0.18            # simple low-pass
        rumble = low * math.exp(-6 * t) * 1.6
        thump = math.sin(2 * math.pi * 68 * t) * math.exp(-11 * t) * 0.9
        out.append(0.55 * (rumble + thump))
    return out


def cheer():
    """Three rising notes for a new high score."""
    notes = [523.25, 659.25, 783.99, 1046.5]      # C E G C
    out = []
    for k, f in enumerate(notes):
        n = int(0.11 * RATE)
        for i in range(n):
            amp = env(i, n, 0.008, 3.2) * 0.3
            out.append(amp * (math.sin(2 * math.pi * f * i / RATE)
                              + 0.35 * math.sin(4 * math.pi * f * i / RATE)))
    return out


if __name__ == "__main__":
    write_wav("SFX_Pop", pop())
    write_wav("SFX_Hit", hit())
    write_wav("SFX_Bomb", bomb())
    write_wav("SFX_Cheer", cheer())
