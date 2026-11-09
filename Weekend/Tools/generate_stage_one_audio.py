import wave, math, struct, random, uuid
from pathlib import Path
r=Path('Weekend/Assets/Resources/Audio');rate=24000
def save(name,seconds,fn):
 data=[max(-.92,min(.92,fn(i/rate))) for i in range(int(seconds*rate))]
 with wave.open(str(r/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(struct.pack('<%dh'%len(data),*(int(v*32767) for v in data)))
 meta=r/(name+'.wav.meta')
 if not meta.exists():
  template=(r/'DoorKnock.wav.meta').read_text(encoding='utf-8-sig');old=next(x for x in template.splitlines() if x.startswith('guid:'));meta.write_text(template.replace(old,'guid: '+uuid.uuid4().hex),encoding='utf-8')
 print(name,round(max(abs(x) for x in data),3))
random.seed(711)
save('NightHeartbeat',4,lambda t:sum(.32*math.exp(-((t-(b+d))/.055)**2)*math.sin(2*math.pi*52*t) for b in [0.35,1.35,2.35,3.35] for d in [0,.19]))
save('NeighborBreath',6,lambda t:(.055*random.uniform(-1,1)+.04*math.sin(2*math.pi*103*t))*max(0,math.sin(math.pi*t/3))**2*(.5+.5*math.sin(math.pi*t/6)))
save('ChairScrape',1.25,lambda t:(.12*random.uniform(-1,1)+.10*math.sin(2*math.pi*(180*t+75*t*t)))*math.sin(math.pi*t/1.25)**2)
# An original eight-bar fragment, band-limited and fluttered like a small radio; no sampled song.
notes=[220,261.63,293.66,261.63,196,220,174.61,196,164.81,196,220,196,174.61,164.81,146.83,164.81]
def radio(t):
 x=(t%.75)/.75;f=notes[int(t/.75)%len(notes)];fade=min(1,t/1.5,(12-t)/1.5)
 return fade*(.11*math.sin(2*math.pi*f*t+.012*math.sin(2*math.pi*4*t))*math.sin(math.pi*x)**.5+.018*random.uniform(-1,1))
save('NeighborRadio',12,radio)
