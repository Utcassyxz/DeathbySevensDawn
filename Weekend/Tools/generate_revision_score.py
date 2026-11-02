from pathlib import Path
import numpy as np,wave,json,uuid
folder=Path(__file__).resolve().parents[1]/"Assets/Resources/Audio"
sr=22050
def save(name,y):
 y=np.nan_to_num(y);peak=float(np.max(np.abs(y)));y=y*((.67/max(.001,peak)) if name in ['EndingGood','EndingBad','EndingTrue'] else (.78/max(.78,peak)))
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

# Identical harmony and melody in all endings. Mood comes from register, articulation,
# instrumental combinations and four developing statements, rather than major/minor swaps.
def ensemble(note,duration,kind='strings'):
 t=np.arange(max(1,int(sr*duration)))/sr;f=hz(note);end=np.minimum(1,np.maximum(0,duration-t)/(.30 if kind!='brass' else .10))
 if kind=='strings':
  y=np.zeros(len(t))
  for detune,phase in [(-.003,0),(.001,1.7),(.004,3.1)]:
   for h in range(1,9):y+=np.sin(2*np.pi*f*(1+detune)*h*t+.05*h*np.sin(2*np.pi*4.8*t+phase))/(h**1.45)*.23
  env=np.minimum(1,t/.16)*end
 elif kind=='brass':
  y=sum(np.sin(2*np.pi*f*h*t+.018*h*np.sin(2*np.pi*5*t))/(h**.85) for h in range(1,8))*.28
  env=(1-np.exp(-t*45))*(.65+.35*np.exp(-t*5))*end
 elif kind=='flute':
  y=np.sin(2*np.pi*f*t+.04*np.sin(2*np.pi*5.1*t))+.13*np.sin(2*np.pi*2*f*t);env=np.minimum(1,t/.09)*end
 elif kind=='cello':
  y=sum(np.sin(2*np.pi*f*h*t+.02*h*np.sin(2*np.pi*4.5*t))/(h**1.5) for h in range(1,7))*.5;env=np.minimum(1,t/.12)*end
 else:return voice(note,duration,'bell')
 return y*env

def snare(duration=.16,seed=0):
 t=np.arange(int(sr*duration))/sr;rng=np.random.default_rng(seed);n=rng.normal(0,1,len(t));bright=n-np.roll(n,1)
 return (bright*.40+np.sin(2*np.pi*185*t)*.2)*np.exp(-t*34)

sections=['prologue','statement','development','return_and_coda']
chords=[[r+12,r+19,r+27] for r in roots] # same exact voicings in every version
arrangements={
 'good':[['solo strings','harp','bass'],['strings','brass melody','march snare','bass'],['high strings','brass countermelody','march snare','timpani'],['full strings','brass','snare','timpani','harp']],
 'bad':[['music box','low pad'],['music box','cello','bass'],['music box octave','cello','low pad'],['music box','cello','bell echoes']],
 'true':[['bell','low strings'],['strings','flute','harp'],['strings','horn','bell counterline'],['strings','flute','horn','harp']]
}
for name,bpm,mood in [('EndingGood',112,'good'),('EndingBad',64,'bad'),('EndingTrue',84,'true')]:
 beat=60/bpm;lead_in=6;total_beats=lead_in+64*3+6;duration=total_beats*beat+3;y=np.zeros(int(duration*sr))
 for n in [38,50,57,65]:put(y,0,voice(n,6.5*beat,'pad'),.025)
 put(y,beat,voice(74,4*beat,'bell'),.065)
 for section in range(4):
  for bar,events in enumerate(bars):
   at=(lead_in+(section*16+bar)*3)*beat;root=roots[bar];chord=chords[bar]
   # Kept identical in every arrangement; printed into the score metadata.
   for n in chord:put(y,at,voice(n,3.35*beat,'pad'),.024 if mood=='bad' else .025)
   put(y,at,voice(root,2.2*beat,'bass'),.095 if mood=='bad' else .10)
   for off,n in [(1,chord[0]),(2,chord[1])]:put(y,at+off*beat,voice(n,1.1*beat,'bell'),.032)
   for off,d,n in events:
    note=74+n;length=d*beat+.1
    if mood=='bad':
     put(y,at+off*beat,voice(note,length+.18,'bell'),.115)
     if section in [1,2,3]:put(y,at+off*beat,ensemble(note-24,length+.35,'cello'),.040)
     if section==2:put(y,at+off*beat+.06,voice(note+12,length,'bell'),.023)
     if section==3:put(y,at+off*beat+.52*beat,voice(note,length,'bell'),.025)
    elif mood=='good':
     put(y,at+off*beat,ensemble(note,length,'strings' if section==0 else 'brass'),.105 if section!=2 else .080)
     put(y,at+off*beat,ensemble(note-12,length+.25,'strings'),.085)
     if section>=2:put(y,at+off*beat,ensemble(note+12,length,'strings'),.045)
    else:
     put(y,at+off*beat,voice(note,length,'bell') if section==0 else ensemble(note,length,'flute'),.085)
     if section>=1:put(y,at+off*beat,ensemble(note-12,length+.3,'strings'),.070)
     if section>=2:put(y,at+off*beat+.5*beat,ensemble(note-24,max(.15,length*.7),'brass'),.035)
   if mood=='good':
    for n in chord:put(y,at,ensemble(n+(12 if section==2 else 0),3.15*beat,'strings'),.032+section*.004)
    if section>=1:
     for off,gain in [(0,.024),(1,.035),(1.75,.012),(2,.035),(2.75,.012)]:put(y,at+off*beat,snare(seed=section*100+bar*7+int(off*4)),gain)
     if bar%4==3:
      for off in [2,2.25,2.5,2.75]:put(y,at+off*beat,snare(.12,bar+int(off*9)),.016)
     if bar%4==0:
      t=np.arange(int(sr*beat*1.2))/sr;put(y,at,np.sin(2*np.pi*(62*t-8*t*t))*np.exp(-t*6),.10)
   elif mood=='true':
    for n in chord:put(y,at,ensemble(n,3.2*beat,'strings'),.022+section*.006)
    if section in [1,3]:
     for off,n in enumerate(chord*2):put(y,at+off*.5*beat,voice(n+12,beat*1.3,'bell'),.025)
   elif section>=1:
    put(y,at,ensemble(root+12,3.2*beat,'cello'),.050)
 # A real cadence, not a cut-off: tonic resolves over six beats before reverb release.
 at=(lead_in+64*3)*beat
 for n in [38,50,57,65,74]:put(y,at,ensemble(n,6*beat,'strings') if mood!='bad' else voice(n,6*beat,'pad'),.040)
 put(y,at,voice(74,5*beat,'bell'),.065)
 y=room(y);fade=np.minimum(1,np.arange(len(y))/(sr*.65))*np.minimum(1,np.arange(len(y))[::-1]/(sr*2.5));save(name,y*fade)
metadata={'name':'Seven Day · One More Morning','meter':'3/4','motif':bars,'chords':chords,'sharedHarmony':True,'tempos':{'good':112,'bad':64,'true':84},'sections':sections,'barsPerSection':16,'arrangements':arrangements,'lyrics':None}
(folder/'EndingTheme.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')

# Existing one-shots / Bubble generation retained for reproducibility.
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
