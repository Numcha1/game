"""Original deterministic door latch and quiet indoor room tone; no recordings/dependencies."""
from pathlib import Path
import math,random,struct,wave
destination=Path(__file__).resolve().parents[1]/'Assets'/'Audio'
rate=22050
for name,seconds in [('door',.42),('interior-ambience',8)]:
    rng=random.Random(904);samples=[];smooth=0
    for i in range(int(rate*seconds)):
        t=i/rate;smooth=.985*smooth+.015*rng.uniform(-1,1)
        if name=='door':
            value=0
            for start,freq in [(0,170),(.19,250),(.26,95)]:
                u=t-start
                if u>=0:value+=.25*math.exp(-u*35)*math.sin(2*math.pi*freq*u)
            value+=smooth*.13*math.sin(math.pi*t/seconds)
        else:
            # Integer-cycle frequencies and seam fade keep the repeated room tone unobtrusive.
            value=(smooth*.3+.013*math.sin(2*math.pi*55*t)+.007*math.sin(2*math.pi*82.5*t))*min(1,t/.08,(seconds-t)/.08)
        samples.append(int(max(-1,min(1,value))*32767))
    with wave.open(str(destination/(name+'.wav')),'wb') as output:
        output.setnchannels(1);output.setsampwidth(2);output.setframerate(rate)
        output.writeframes(struct.pack('<'+'h'*len(samples),*samples))
