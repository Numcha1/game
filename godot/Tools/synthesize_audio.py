"""Original short cozy game effects; no third-party recordings or dependencies."""
from pathlib import Path
import math, random, struct, wave

output = Path(__file__).resolve().parents[1] / 'Assets' / 'Audio'
output.mkdir(exist_ok=True)
rate = 22050
for name, seconds in [('click', .12), ('water', .38), ('harvest', .28), ('coin', .32), ('error', .22)]:
    randomizer = random.Random(713)
    samples = []
    smooth = 0
    for i in range(int(rate * seconds)):
        t = i / rate
        attack = min(1, t / .008)
        release = max(0, 1 - t / seconds) ** 2
        noise = randomizer.uniform(-1, 1)
        smooth = .84 * smooth + .16 * noise
        if name == 'click':
            value = .3 * math.sin(2 * math.pi * 440 * t) * math.exp(-t * 40) + smooth * .35 * math.exp(-t * 60)
        elif name == 'water':
            value = smooth * .65
            for start, frequency in [(.02, 1050), (.09, 1320), (.19, 870), (.26, 1550)]:
                d = t - start
                if d >= 0: value += .12 * math.sin(2 * math.pi * frequency * d) * math.exp(-d * 55)
        elif name == 'harvest':
            value = smooth * .65 * math.sin(math.pi * t / seconds) + .1 * math.sin(2 * math.pi * 660 * t) * math.exp(-t * 20)
        elif name == 'coin':
            value = sum(math.sin(2 * math.pi * f * t) * a for f, a in [(1318.51, .17), (1975.53, .08), (2637.02, .03)]) * math.exp(-t * 8)
        else:
            value = .18 * math.sin(2 * math.pi * (330 - t * 250) * t) * math.exp(-t * 15)
        samples.append(int(max(-.9, min(.9, value * attack * release)) * 32767))
    with wave.open(str(output / (name + '.wav')), 'wb') as stream:
        stream.setnchannels(1); stream.setsampwidth(2); stream.setframerate(rate)
        stream.writeframes(struct.pack('<' + 'h' * len(samples), *samples))
