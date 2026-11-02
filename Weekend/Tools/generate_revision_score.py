from pathlib import Path
import numpy as np,wave,json,uuid
folder=Path(__file__).resolve().parents[1]/"Assets/Resources/Audio"
sr=22050
def save(name,y):
 y=np.nan_to_num(y);y=y*(.78/max(.78,float(np.max(np.abs(y)))))
 pcm=np.asarray(np.clip(y,-.94,.94)*32767,dtype='<i2')
 with wave.open(str(folder/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes(pcm.tobytes())
 p=folder/(name+'.wav.meta')
 if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nAudioImporter:\n  serializedVersion: 7\n  defaultSettings:\n    loadType: 0\n    compressionFormat: 1\n    quality: 0.8\n  preloadAudioData: 1\n',encoding='utf-8')
 print(name,round(len(y)/sr,2),'sec',round(float(np.max(np.abs(y))),3),'peak')
def hz(n):return 440*2**((n-69)/12)
def voice(note,duration,kind='bell'):
 t=np.arange(max(1,int(sr*duration)))/sr;f=hz(note)
 if kind=='lead':y=np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*2*t)+.08*np.sin(2*np.pi*f*3*t);env=(1-np.exp(-t*130))*np.exp(-t*2.4)
 elif kind=='pad':y=np.sin(2*np.pi*f*t+ .04*np.sin(2*np.pi*.31*t))+.28*np.sin(2*np.pi*f*2.002*t)+.12*np.sin(2*np.pi*f*3*t);env=np.minimum(1,t/.65)*np.minimum(1,np.maximum(0,duration-t)/.7)
 elif kind=='bass':y=np.sin(2*np.pi*f*t)+.14*np.sin(2*np.pi*f*2*t);env=(1-np.exp(-t*90))*np.exp(-t*3.5)
 else:y=np.sin(2*np.pi*f*t)*np.exp(-t*2.8)+.25*np.sin(2*np.pi*f*2.76*t)*np.exp(-t*8);env=(1-np.exp(-t*240))
 return y*env
def put(y,at,v,gain):
 i=int(at*sr)
 if i>=len(y):return
 if i<0:v=v[-i:];i=0
 n=min(len(v),len(y)-i);y[i:i+n]+=v[:n]*gain
def room(y):
 dry=y.copy()
 for delay,gain in[(.113,.13),(.257,.10),(.419,.065),(.731,.04)]:
  i=int(sr*delay);y[i:]+=dry[:-i]*gain
 return y
# A new, shared singable 16-bar melody. No borrowed game melody or recording.
bars=[[(0,1,0),(1,.5,2),(1.5,.5,3),(2,1,7)],[(0,1,5),(1,1,3),(2,1,2)],[(0,1.5,0),(1.5,.5,-2),(2,1,0)],[(0,2,7),(2,1,5)],[(0,1,3),(1,.5,5),(1.5,.5,7),(2,1,10)],[(0,1.5,7),(1.5,.5,5),(2,1,3)],[(0,1,2),(1,1,0),(2,1,-2)],[(0,3,0)],[(0,1,12),(1,.5,10),(1.5,.5,7),(2,1,5)],[(0,1,7),(1,1,3),(2,1,2)],[(0,1.5,0),(1.5,.5,2),(2,1,3)],[(0,2,7),(2,1,10)],[(0,1,12),(1,1,7),(2,1,5)],[(0,1,3),(1,1,2),(2,1,0)],[(0,1,-2),(1,1,2),(2,1,7)],[(0,3,0)]]
roots=[38,34,36,33,38,34,31,33]*2
for name,bpm,mood in [('EndingGood',112,'good'),('EndingBad',64,'bad'),('EndingTrue',84,'true')]:
 beat=60/bpm;duration=54*beat+2.8;y=np.zeros(int(duration*sr))
 for n in [38,50,57,62,66 if mood=='good' else 65]:put(y,0,voice(n,3.4*beat,'pad'),.035)
 put(y,.15*beat,voice(74,2.8*beat,'bell'),.08)
 for bar,events in enumerate(bars):
  at=(bar*3+3)*beat;major=mood=='good'
  root=roots[bar]+(1 if major and roots[bar]==34 else 0)
  chord=[root+12,root+19,root+24+(4 if major else 3)]
  for n in chord:put(y,at,voice(n,3.4*beat,'pad'),.028 if mood=='bad' else .044)
  put(y,at,voice(root,beat*2,'bass'),.13)
  for off,n in [(1,chord[0]),(2,chord[1])]:put(y,at+off*beat,voice(n,.9*beat),.036)
  for off,d,n in events:
   if major:n={3:4,10:11,-2:-1}.get(n,n)
   put(y,at+off*beat,voice(74+n,d*beat+.18,'lead' if mood!='bad' else 'bell'),.13)
   if mood=='true':put(y,at+off*beat+.025,voice(62+n,d*beat+.8,'pad'),.025)
  if mood=='good':
   for off in [0,1,2]:
    t=np.arange(int(.09*sr))/sr;rng=np.random.default_rng(bar*3+off);put(y,at+off*beat,np.diff(rng.normal(0,1,len(t)+1))*np.exp(-t*65),.006)
 for n in [38,50,57,62,65 if mood!='good' else 66,74]:put(y,51*beat,voice(n,3*beat+2.8,'pad'),.045)
 y=room(y);fade=np.minimum(1,np.arange(len(y))/(sr*.35))*np.minimum(1,np.arange(len(y))[::-1]/(sr*1.7));save(name,y*fade)
# Bubble's entry: monumental low organ, wide register bells, ascending sustained layers.
duration=42;y=np.zeros(int(sr*duration))
for at,chord in [(0,[26,38,45,50]),(7,[29,41,48,53]),(14,[31,43,50,57]),(21,[26,38,45,62]),(28,[33,45,52,64]),(35,[26,38,50,62])]:
 for n in chord:put(y,at,voice(n,8,'pad'),.052)
 for k,n in enumerate([74,81,86,89]):put(y,at+1+k*.75,voice(n,4),.033)
 t=np.arange(int(sr*2))/sr;put(y,at+4,np.sin(2*np.pi*(48*t-9*t*t))*np.exp(-t*2.8),.055)
y=room(y);fade=np.minimum(1,np.arange(len(y))/(sr*2))*np.minimum(1,np.arange(len(y))[::-1]/(sr*3));save('BubbleThreshold',y*fade)
t=np.arange(int(sr*.045))/sr;rng=np.random.default_rng(1997);noise=np.diff(rng.normal(size=len(t)+1));click=(noise*.16+np.sin(2*np.pi*4100*t)*.16)*np.exp(-t*220);click+=np.sin(2*np.pi*2700*t)*np.exp(-np.maximum(0,t-.012)*350)*(t>=.012)*.09;save('UiPress',click)
y=np.zeros(int(sr*.9))
for i,n in enumerate([74,78,81,86]):put(y,i*.085,voice(n,.48),.16)
save('DeductionSuccess',room(y)*np.minimum(1,np.arange(len(y))[::-1]/(sr*.16)))
y=np.zeros(int(sr*6))
for at,notes in [(0,[38,50,57,64,67]),(1.3,[36,48,55,62,65]),(2.8,[38,50,57,62,65,74])]:
 for n in notes:put(y,at,voice(n,3,'pad'),.055);put(y,at,voice(n,1.4),.025)
 t=np.arange(int(sr*1.4))/sr;put(y,at,np.sin(2*np.pi*(63*t-16*t*t))*np.exp(-t*5),.13)
save('EndingCurtain',room(y)*np.minimum(1,np.arange(len(y))[::-1]/(sr*1.5)))
(folder/'EndingTheme.json').write_text(json.dumps({'name':'Seven Day · One More Morning','meter':'3/4','motif':bars,'tempos':{'good':112,'bad':64,'true':84},'lyrics':None},indent=2)+'\n',encoding='utf-8')
p=folder/'EndingTheme.json.meta'
if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
