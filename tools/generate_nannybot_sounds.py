import math
import os
import struct
import wave

ROOT = r"E:\RimWorld-WorkSpace\sources\ColonyKit\Sounds\NannyBot"
os.makedirs(ROOT, exist_ok=True)


def beep(name, freq, duration=0.13, volume=0.32, rate=44100):
    count = int(duration * rate)
    path = os.path.join(ROOT, name)
    with wave.open(path, "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(rate)
        frames = []
        for index in range(count):
            t = index / rate
            envelope = min(1.0, index / (0.012 * rate), (count - index) / (0.03 * rate))
            value = volume * envelope * (
                math.sin(2 * math.pi * freq * t) + 0.35 * math.sin(4 * math.pi * freq * t)
            )
            value = max(-1.0, min(1.0, value))
            frames.append(struct.pack("<h", int(value * 32767)))
        handle.writeframes(b"".join(frames))
    print("wrote", path)


beep("Beep.wav", 880)
beep("LowBattery.wav", 440, duration=0.18, volume=0.30)
