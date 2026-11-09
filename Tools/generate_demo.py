"""Original deterministic synth examples and charts. No recordings or samples.

Requires Python 3.10+, numpy and ffmpeg on PATH (or --ffmpeg PATH).
The note grid is authored together with the musical phrase, not inferred from peaks.
"""
from pathlib import Path
import argparse, hashlib, json, math, subprocess
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
RATE=44100
NAMES=['初光','薄荷时钟','玻璃雨','晴空信号','周末散步','暖风来信','星光漫步','云间回声','微风回廊','月色涟漪','银河列车','远方灯火','再次出发']
BPMS=[128,112,124,140,104,116,132,108,120,136,144,126,138]
KEYS=[60,62,65,67,57,60,64,62,65,59,64,67,60]
KINDS=['入门','简单','普通','困难','极限']

def meta(path,kind='DefaultImporter'):
    target=Path(str(path)+'.meta')
    if target.exists(): return
    guid=hashlib.md5(path.relative_to(ROOT).as_posix().encode()).hexdigest()
    target.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+kind+':\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf8')

def tone(midi,seconds,style,seed=1):
    t=np.arange(int(seconds*RATE),dtype=np.float64)/RATE
    f=440*2**((midi-69)/12)
    if style=='pluck':
        x=(np.sin(2*np.pi*f*t)+.32*np.sin(4*np.pi*f*t)+.10*np.sin(6*np.pi*f*t))*np.exp(-t*6)
        x*=np.minimum(t/.006,1)
    elif style=='bass':
        x=(np.sin(2*np.pi*f*t)+.16*np.sin(4*np.pi*f*t))*np.exp(-t*3)*np.minimum(t/.008,1)
    elif style=='pad':
        x=(np.sin(2*np.pi*f*t)+.2*np.sin(2*np.pi*f*1.003*t))*np.sin(np.pi*np.clip(t/seconds,0,1))**1.5
    elif style=='kick':
        phase=2*np.pi*(44*t+70*.035*(1-np.exp(-t/.035)))
        x=np.sin(phase)*np.exp(-t*13)*np.minimum(t/.0015,1)
    else:
        rng=np.random.default_rng(seed);noise=rng.uniform(-1,1,len(t))
        high=np.concatenate(([0],np.diff(noise)))*.48
        if style=='snare': x=(high*.55+np.sin(2*np.pi*185*t)*.35)*np.exp(-t*23)
        elif style=='tom': x=np.sin(2*np.pi*(155*t+38*.04*(1-np.exp(-t/.04))))*np.exp(-t*10)
        else:
            metal=sum(np.sin(2*np.pi*q*t) for q in [2413,3191,4567,6179,8093])/5
            decay={'hat':22,'openhat':8,'ride':5,'crash':3.6}[style]
            x=(high*.55+metal*.32)*np.exp(-t*decay)
        x*=np.minimum(t/.001,1)
    x[-min(len(x),220):]*=np.linspace(1,0,min(len(x),220))
    return x.astype(np.float32)

def write_ogg(ffmpeg,path,audio):
    path.parent.mkdir(parents=True,exist_ok=True)
    p=subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','f32le','-ar',str(RATE),'-ac','2','-i','pipe:0','-c:a','libvorbis','-q:a','5',str(path)],input=audio.astype('<f4').tobytes(),capture_output=True)
    if p.returncode: raise RuntimeError(p.stderr.decode())
    meta(path)

def chart(index,difficulty,bars,beat,duration):
    notes=[]
    # Separate rhythmic arrangements for each level, all tied to the composition grid.
    phrases=[[(0,0),(2,1)],[(0,0),(1.5,1),(2,0),(3,1)],
             [(0,0),(1,1),(2,0),(2.5,0),(3,1)],
             [(0,0),(.5,0),(1,1),(1.5,0),(2,0),(2.75,1),(3,1),(3.5,0)],
             [(0,0),(.5,1),(.75,0),(1,1),(1.5,0),(2,0),(2.5,1),(2.75,0),(3,1),(3.5,0)]]
    for bar in range(2,bars-1):
        start=.5+bar*4*beat
        if difficulty>=1 and bar%8==7:
            # A dedicated fill with no competing single or big notes.
            length=(2.5 if difficulty<3 else 3.0)*beat
            notes.append({'kind':4,'time':round(start,6),'duration':round(length,6),'source':'fill'})
            continue
        pattern=phrases[difficulty]
        if bar%8==0: pattern=pattern[:2] # Leave room at phrase entrances.
        for i,(b,color) in enumerate(pattern):
            kind=color
            if difficulty>=2 and i==0 and bar%4==2: kind=2+color
            notes.append({'kind':kind,'time':round(start+b*beat,6),'duration':0,'source':'authored grid'})
    return {'version':2,'revision':1,'difficulty':difficulty,'songId':f'yyou_demo_{index:02}',
            'title':NAMES[index],'status':'open-demo','label':KINDS[difficulty]+' · 合成演示',
            'audioPath':f'DemoMusic/yyou_demo_{index:02}.ogg','audioDuration':duration,
            'travelSeconds':3,'bpm':BPMS[index],'gridOffset':.5,'reviewOnly':False,'notes':notes}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--ffmpeg',default='ffmpeg');args=parser.parse_args()
    assets=ROOT/'Assets/GameMain';songs=[]
    for index,name in enumerate(NAMES):
        beat=60/BPMS[index];bars=32 if index==0 else 16;duration=round(.5+bars*4*beat+1.75,6)
        out=np.zeros((round(duration*RATE),2),np.float32)
        def add(t,signal,gain=.15,pan=0):
            offset=round(t*RATE);n=min(len(signal),len(out)-offset)
            if n<=0:return
            out[offset:offset+n,0]+=signal[:n]*gain*math.sqrt((1-pan)/2)
            out[offset:offset+n,1]+=signal[:n]*gain*math.sqrt((1+pan)/2)
        chord_roots=[0,5,9,7];melodies=[[0,4,7,12,7,4,2,4],[5,9,12,14,12,9,7,5],[9,12,16,19,16,12,11,9],[7,11,14,19,14,11,9,7]]
        for bar in range(bars):
            root=KEYS[index]+chord_roots[(bar//2)%4];t0=.5+bar*4*beat
            for degree in [0,4 if (bar//2)%4!=2 else 3,7]:add(t0,tone(root+degree,beat*3.8,'pad'),.032,degree/14-.25)
            for b in range(4):
                add(t0+b*beat,tone(root-24,beat*.8,'bass'),.18)
                add(t0+b*beat,tone(40,.32,'kick'),.32 if b%2==0 else .16)
                if b%2:add(t0+b*beat,tone(60,.23,'snare',index*99+bar*4+b),.14)
            for b in range(8):
                add(t0+b*.5*beat,tone(60,.14,'hat',index*11+bar*8+b),.047,-.2 if b%2 else .2)
                note=KEYS[index]+12+melodies[(bar//2)%4][b]
                # The same musical line offers eighth notes and distinct pickup sixteenths.
                add(t0+b*.5*beat,tone(note,beat*.74,'pluck'),.13 if bar>=2 else .06,(b%3-1)*.35)
                if bar>=2 and b in [1,5]:add(t0+(b*.5+.25)*beat,tone(note+2,beat*.32,'pluck'),.065,.3)
            if bar%8==7:
                for b in np.arange(0,3.01,.25):add(t0+b*beat,tone(root,.24,'tom'),.11)
        out=np.tanh(out*1.12)*.65
        out[-RATE:]*=np.linspace(1,0,RATE)[:,None]
        path=ROOT/f'Assets/StreamingAssets/DemoMusic/yyou_demo_{index:02}.ogg';write_ogg(args.ffmpeg,path,out)
        chart_dir=assets/f'Resources/DrumCharts/yyou_demo_{index:02}';chart_dir.mkdir(parents=True,exist_ok=True)
        for difficulty in range(5):
            p=chart_dir/f'{difficulty}.json';p.write_text(json.dumps(chart(index,difficulty,bars,beat,duration),ensure_ascii=False,indent=2),encoding='utf8');meta(p)
        category=8 if index==0 else [1,2,16,4][(index-1)//3]
        songs.append({'id':f'yyou_demo_{index:02}','title':name,'artist':'YYOU · 合成演示','duration':f'{int(duration)//60}:{int(duration)%60:02}',
                      'previewStart':round(.5+8*beat,3),'audioPath':f'DemoMusic/yyou_demo_{index:02}.ogg','category':category,'availableDifficulties':31})
        print('Generated',name,duration)
    # Independently synthesized replacements. No copied drum-machine code or samples.
    import wave
    combos={'DrumBlue':[('openhat',.24),('snare',.38)],'DrumGreen':[('ride',.24),('snare',.38)],'DrumBig':[('crash',.30),('snare',.40)],'DrumRoll':[('crash',.20),('tom',.44)],'Miss':[('tom',.14)],'WrongTap':[('snare',.12)]}
    for name,parts in combos.items():
        samples=np.zeros(int(RATE*.65),np.float32)
        for kind,gain in parts:samples+=tone(36,.65,kind,17)*gain
        p=assets/f'Resources/Audio/{name}.wav';p.parent.mkdir(parents=True,exist_ok=True)
        with wave.open(str(p),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes((np.clip(samples,-.8,.8)*32767).astype('<i2').tobytes())
        # Keep existing resource GUIDs supplied in the snapshot when present.
        meta(p,'AudioImporter')
    p=ROOT/'Tools/demo-catalog.json';p.write_text(json.dumps({'songs':songs},ensure_ascii=False,indent=2),encoding='utf8')
    print('Generated 13 demo tracks, 65 independent charts, 6 synthesized effects.')

if __name__=='__main__': main()
