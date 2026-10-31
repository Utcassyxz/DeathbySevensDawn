from pathlib import Path
import uuid
root=Path(__file__).resolve().parents[1]/"Assets"
import numpy as np,wave
sr=22050
folder=root/'Resources/Audio'
def save(name,y):
 y=np.nan_to_num(y);peak=float(np.max(np.abs(y)));y=y*(.76/max(peak,.76));y=np.tanh(y*1.05)/1.05
 pcm=np.asarray(np.clip(y,-1,1)*32767,dtype='<i2')
 with wave.open(str(folder/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes(pcm.tobytes())
 meta=folder/(name+'.wav.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nAudioImporter:\n  serializedVersion: 7\n  defaultSettings:\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 22050\n    compressionFormat: 1\n    quality: 0.7\n  forceToMono: 1\n  preloadAudioData: 1\n  loadInBackground: 0\n',encoding='utf-8')
 print(name,'seconds',round(len(y)/sr,2),'peak',round(float(np.max(np.abs(y))),3))
t=np.arange(int(sr*.14))/sr;rng=np.random.default_rng(731)
click=.5*np.sin(2*np.pi*(1650*t+1500*t*t))*np.exp(-65*t)+.18*rng.standard_normal(len(t))*np.exp(-190*t)+.22*np.sin(2*np.pi*3100*t)*np.exp(-90*t)
save('UiPress',click)
def hz(m):return 440*2**((m-69)/12)
def score(name,chords,melody,tempo,kind):
 beat=60/tempo;dur=beat*32;n=int(dur*sr);y=np.zeros(n)
 def add(note,start,length,gain,bell=False):
  a=int(start*sr);count=min(int(length*sr),n-a)
  if count<=0:return
  tt=np.arange(count)/sr;freq=hz(note)
  if bell:
   sound=np.sin(2*np.pi*freq*tt+1.4*np.sin(2*np.pi*freq*2.001*tt)*np.exp(-tt*2))
   env=(1-np.exp(-tt*70))*np.exp(-tt*1.7)*np.minimum(1,(length-tt)/.15)
  else:
   sound=np.sin(2*np.pi*freq*tt)+.19*np.sin(2*np.pi*freq*2.003*tt)+.06*np.sin(2*np.pi*freq*3*tt)
   env=np.minimum(1,tt/1.1)*np.minimum(1,np.maximum(0,(length-tt)/1.8))*(.85+.15*np.sin(tt*.6))
  y[a:a+count]+=sound*env*gain
 for bar,chord in enumerate(chords):
  start=bar*4*beat
  for note in chord:add(note,start,5.5*beat,.062 if kind!='bad' else .052)
  add(chord[0]-12,start,4*beat,.045)
 for i,note in enumerate(melody):
  if note>0:add(note,(i*2+.5)*beat,3.2*beat,.105 if kind=='bubble' else .09,True)
 if kind=='bad':
  tt=np.arange(n)/sr;y+=.025*np.sin(2*np.pi*hz(35)*tt)*(1+np.sin(2*np.pi*1.1*tt))*.5
 # Short, diminishing room/space reflections; loop ends at silence, never at a click.
 dry=y.copy()
 for delay,gain in [(.19,.16),(.43,.12),(.79,.08),(1.13,.05)]:
  k=int(delay*sr);y[k:]+=dry[:-k]*gain
 fade=np.minimum(1,np.arange(n)/(sr*1.6))*np.minimum(1,np.arange(n)[::-1]/(sr*2.8));save(name,y*fade)
score('BubbleThreshold',[[45,52,59,62],[41,48,57,60],[43,50,57,62],[40,47,55,59],[45,52,59,62],[41,48,57,60],[43,50,59,62],[45,52,57,64]],[81,76,0,79,74,0,76,72,81,83,76,0,79,74,76,0],64,'bubble')
score('EndingGood',[[48,55,60,64],[45,52,57,60],[41,48,53,57],[43,50,55,59],[48,55,60,64],[45,52,57,60],[41,48,55,60],[48,55,60,64]],[72,76,79,76,77,76,74,71,72,76,79,84,81,79,76,72],72,'good')
score('EndingBad',[[36,43,48,51],[35,42,47,50],[32,39,44,47],[34,41,46,49],[36,43,48,51],[35,42,47,50],[32,39,44,47],[36,43,48,50]],[75,74,72,0,71,70,67,0,75,74,72,71,68,67,63,0],58,'bad')
score('EndingTrue',[[41,48,53,60],[43,50,55,62],[45,52,57,64],[48,55,60,67],[41,48,57,60],[43,50,59,62],[48,55,60,64],[48,55,62,67]],[77,79,81,84,86,84,81,79,77,79,84,88,86,84,79,84],68,'true')
