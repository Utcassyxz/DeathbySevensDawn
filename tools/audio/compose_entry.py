"""Original 80 BPM title arrangement and a wordless synthetic crescendo; no external samples."""
from pathlib import Path
import numpy as np,wave,json,uuid
OUT=Path(__file__).resolve().parents[2]/'Weekend/Assets/Resources/Audio'
SR=32000;N=SR*36;beat=.75
music=np.zeros((N,2),dtype=np.float64)
def add(start,midi,duration,gain,pan=0,pad=False):
 t=np.arange(int(duration*SR))/SR;freq=440*2**((midi-69)/12)
 env=np.minimum(1,t/(.7 if pad else .02))*np.minimum(1,(duration-t)/(.8 if pad else .12))*np.exp(-t/(duration*(1.8 if pad else .24)))
 s=(np.sin(2*np.pi*freq*t)+.12*np.sin(2*np.pi*freq*2.001*t))*env*gain
 v=s[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])
 np.add.at(music,(np.arange(len(t))+int(start*beat*SR))%N,v)
chords=[[48,55,60,64],[45,52,59,64],[50,57,60,65],[43,55,57,62]]
for bar in range(16):
 for n in chords[bar%4]:add(bar*3,n-12,4.8,.022,.25 if n%2 else -.25,True)
 if bar%4==0:
  for off,n in zip([0,1,1.5,2.5],[76,79,81,79]):add(bar*3+off,n,3.8,.047,-.2 if off<1.5 else .2)
 if bar%4==2:add(bar*3,74,4,.025,.1)
dry=music.copy()
for delay,g in [(.227,.18),(.613,.13),(1.113,.08)]:music+=np.roll(dry,int(SR*delay),axis=0)[:,::-1]*g
def save(name,a,peak):
 a=a*min(1,peak/max(float(np.max(np.abs(a))),1e-9));f=OUT/(name+'.wav')
 with wave.open(str(f),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((a*32767).astype('<i2').tobytes())
 if not Path(str(f)+'.meta').exists():Path(str(f)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
 return {'seconds':len(a)/SR,'peak':float(np.max(np.abs(a))),'rms':float(np.sqrt(np.mean(a*a)))}
music*=1.65
stats={'TitleOrbit':save('TitleOrbit',music,.32)}
t=np.arange(SR*12)/SR;p=np.clip((t-.25)/10,0,1);rng=np.random.default_rng(709)
voices=np.zeros((len(t),2))
for v in range(5):
 f=(180+v*37)+(210+v*19)*p+11*np.sin(t*(4.7+v*.17));phase=2*np.pi*np.cumsum(f)/SR
 vocal=np.zeros(len(t))
 for harmonic in range(1,13):
  # Moving formant bands give this a breathy, wordless vowel instead of a plain siren.
  hz=f*harmonic;weight=np.exp(-((hz-900)/340)**2)+.65*np.exp(-((hz-2400)/650)**2)
  vocal+=np.sin(phase*harmonic+v*.41)*weight/harmonic
 vocal+=rng.standard_normal(len(t))*.018*p
 envelope=(p**2.1)*np.minimum(1,np.maximum(0,(12-t)/1.1))*(.72+.14*np.sin(t*7+v))
 voices[:,v%2]+=vocal*envelope*.17
voices[:,0]+=np.roll(voices[:,1],int(.029*SR))*.2
voices[:,1]+=np.roll(voices[:,0],int(.041*SR))*.2
stats['OpeningScream']=save('OpeningScream',voices,.38)
stats['crescendo_rms']=[float(np.sqrt(np.mean(voices[int(a*SR):int(b*SR)]**2))) for a,b in [(0,2),(4,6),(8,10)]]
(OUT/'ENTRY_SCORE.md').write_text('# Entry audio\n\nTitleOrbit is a distinct 36-second, 80 BPM, 3/4 arrangement. Its chord cycle and E–G–A–G motif match the apartment score. All three loops start on the same DSP timestamp and keep running silently across menus. Equal-power gains crossfade for 4.5 seconds; beginning a journey never resets the shared music clock.\n\nOpeningScream is a layered, wordless synthetic vocal effect, with rising pitch and loudness, followed by a release tail. It is not a recording of a person. Both assets are original synthesis without external samples.\n\n'+json.dumps(stats,indent=2),encoding='utf-8')
print(json.dumps(stats))
