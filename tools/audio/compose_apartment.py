"""Original deterministic score for DeathbySevensDawn. No samples or borrowed melodies."""
from pathlib import Path
import numpy as np, wave, json
OUT=Path(__file__).resolve().parents[2]/'Weekend/Assets/Resources/Audio'
OUT.mkdir(parents=True,exist_ok=True)
SR=32000;BEAT=.75;LENGTH=36.;N=round(SR*LENGTH)
def tone(track,beat,midi,duration,gain,pan=0,bell=False,detune=0):
 t=np.arange(int(duration*SR))/SR;f=440*2**((midi-69+detune)/12)
 env=(1-np.exp(-t/0.012))*np.exp(-t/(duration*.24))
 env*=np.minimum(1,np.maximum(0,(duration-t)/.08))
 sig=np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*(2.002 if bell else 2)*t)*np.exp(-t*2)+.075*np.sin(2*np.pi*f*3*t)
 sig*=env*gain
 stereo=sig[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])
 np.add.at(track,(np.arange(len(t))+round(beat*BEAT*SR))%N,stereo)
def room(track):
 dry=track.copy()
 for delay,gain in [(.113,.12),(.227,.10),(.397,.07),(.613,.04)]:track+=np.roll(dry,round(delay*SR),axis=0)[:,::-1]*gain
 return track
warm=np.zeros((N,2));truth=np.zeros_like(warm)
chords=[[48,55,60,64],[45,52,59,64],[50,57,60,65],[43,55,57,62]]
melody=[[76,79,81,79],[76,74,71,72],[74,77,76,69],[71,74,72,67],
        [76,79,83,81],[79,76,74,72],[77,76,74,69],[71,74,79,74]]
for bar in range(16):
 chord=chords[bar%4];b=bar*3
 tone(warm,b,chord[0],2.6,.12,-.22)
 for k in range(6):tone(warm,b+k*.5,chord[1+k%3],1.8,.065,.27 if k%2 else -.27)
 phrase=melody[bar%8]
 for k,n in enumerate(phrase):
  offset=[0,1,1.5,2.5][k]
  tone(warm,b+offset,n+(0 if bar<8 else -12),2.9,.095,(-.10 if bar%2 else .1),True)
  tone(truth,b+offset,n+(-1 if k==2 else 0),4.5,.065,-.2 if bar%2 else .2,True,detune=.08)
 tone(truth,b,chord[0]-12,4.2,.11,-.15,detune=-.055)
 tone(truth,b+1.5,chord[0]+6,3.0,.022,.2,detune=.10)
warm=room(warm);truth=room(truth)
def save(name,a,peak=.32):
 a=a*min(1,peak/max(1e-9,float(np.max(np.abs(a)))))
 path=OUT/(name+'.wav')
 with wave.open(str(path),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes((a*32767).astype('<i2').tobytes())
 return {'seconds':len(a)/SR,'sample_rate':SR,'peak_dbfs':round(20*np.log10(np.max(np.abs(a))),2),'rms_dbfs':round(20*np.log10(np.sqrt(np.mean(a*a))),2),'loop_boundary_delta':float(np.max(np.abs(a[0]-a[-1])))}
stats={'ApartmentHome':save('ApartmentHome',warm),'BehindTheFilter':save('BehindTheFilter',truth)}
t=np.arange(SR*5)/SR;envelope=(1-np.exp(-t*3))*np.exp(-t*1.15)*np.minimum(1,(5-t)/.2)
sig=(np.sin(2*np.pi*65.406*t)+.28*np.sin(2*np.pi*92.499*t)+.14*np.sin(2*np.pi*261.625*t))*envelope*.18
stats['Revelation']=save('Revelation',np.column_stack((sig,np.roll(sig,430))),.32)
(OUT/'SCORE.md').write_text('# 原创原型配乐\n\n80 BPM，3/4 拍，16 小节，36 秒循环；双声道 32 kHz PCM。\n\nApartmentHome：柔和拨弦、钟琴与低音。BehindTheFilter：共享节拍与主题轮廓，加入错位音程和轻微失谐。Revelation：5 秒低音和声事件音。全部由项目脚本合成，无第三方采样或既有游戏旋律。\n\n两个循环采用环形混响尾音，播放从同一个 DSP 时间启动，跨场景保持同步。表中 RMS 是幅度指标，不是 LUFS。仍需在实际耳机和扬声器上做主观混音复核。\n\n```json\n'+json.dumps(stats,indent=2)+'\n```\n',encoding='utf-8')
print(json.dumps(stats,indent=2))
