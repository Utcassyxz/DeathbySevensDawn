from pathlib import Path
assets=Path(__file__).resolve().parents[1]/"Assets"
import wave,math,random,array
rate=22050;length=3.6;rng=random.Random(4025);samples=[];low=0
for i in range(int(rate*length)):
 t=i/rate;noise=rng.uniform(-1,1);low=.97*low+.03*noise
 attack=min(1,t/.007);env=attack*math.exp(-t*1.7)*min(1,(length-t)/.3)
 rumble=math.sin(2*math.pi*(54*t-3*t*t))*.22+low*3
 grit=noise*.22*math.exp(-t*5)
 debris=sum(math.exp(-max(0,t-at)*22)*noise*.09 if t>=at else 0 for at in [.14,.31,.54,.81,1.15,1.48])
 samples.append((rumble+grit+debris)*env)
peak=max(abs(x) for x in samples);data=array.array('h',(int(x/peak*.76*32767) for x in samples))
path=assets/'Resources/Audio/BuildingBlast.wav'
with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(data.tobytes())
