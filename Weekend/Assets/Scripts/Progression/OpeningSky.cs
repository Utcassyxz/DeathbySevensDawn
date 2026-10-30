using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    // Render at a fixed low resolution; point sampling keeps every effect on the same pixel grid.
    public sealed class OpeningSky : System.IDisposable
    {
        const int W=480,H=270;
        Texture2D image; readonly Color32[] pixels=new Color32[W*H];
        float last=-1;
        static readonly Vector2[] Eyes={new Vector2(42,239),new Vector2(461,61),new Vector2(290,277),new Vector2(157,193),new Vector2(359,191),new Vector2(86,111),new Vector2(381,110),new Vector2(191,33),new Vector2(61,35),new Vector2(320,37),new Vector2(256,218),new Vector2(428,247),new Vector2(22,176)};
        static Color32 C(float r,float g,float b)=>new Color(r,g,b,1);
        void Dot(int x,int y,Color32 c){if(x>=0&&y>=0&&x<W&&y<H){c.a=255;pixels[y*W+x]=c;}}
        public Texture2D Render(float clock,float progress)
        {
            if(image==null)image=new Texture2D(W,H,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            if(clock-last<.065f)return image;last=clock;
            float grow=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress*1.8f));
            float radius=12+grow*13,cx=240,cy=100;
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                int hash=unchecked(x*73856093^y*19349663);float noise=(hash&255)/255f;
                float darkness=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.08f)/.18f));
                pixels[y*W+x]=C(.001f*darkness,.002f*darkness,.006f*darkness);
                if((hash&2047)==37&&darkness>.01f)Dot(x,y,C((.24f+noise*.3f)*darkness,(.29f+noise*.3f)*darkness,(.36f+noise*.3f)*darkness));
                float dx=(x-cx)/radius,dy=(y-cy)/radius,d=dx*dx+dy*dy;
                if(d>1)continue;
                float z=Mathf.Sqrt(1-d),light=Mathf.Clamp01((-.6f*dx+.25f*dy+z*.6f));
                light=Mathf.Floor(light*5)/5f;
                float land=Mathf.PerlinNoise(dx*2.2f+5+clock*.008f,dy*2.4f+7);
                Color col=land>.52f?new Color(.23f,.36f,.28f):new Color(.055f,.19f,.31f);
                if(Mathf.Abs(dy)>.83f)col=new Color(.5f,.57f,.59f);
                float corruption=Mathf.Clamp01((progress-.16f)*1.5f);
                col=Color.Lerp(col,new Color(.23f,.13f,.28f),corruption)*(.16f+light);
                Dot(x,y,col);
            }
            float spread=Mathf.Clamp01((progress-.23f)*2);
            for(int arm=0;arm<9;arm++)
            {
                float angle=arm*2.399f;
                for(int s=0;s<80*spread;s++)
                {
                    float t=s/79f,rad=radius*.65f+t*radius*2.4f;
                    float a=angle+Mathf.Sin(t*6+arm+clock*.23f)*t*.9f;
                    int x=(int)(cx+Mathf.Cos(a)*rad),y=(int)(cy+Mathf.Sin(a)*rad*.8f);
                    int thick=Mathf.Max(1,(int)(4*(1-t)));
                    for(int yy=-thick;yy<=thick;yy++)for(int xx=-thick;xx<=thick;xx++)Dot(x+xx,y+yy,C(.12f+arm*.005f,.20f,.19f));
                }
            }
            float burn=Mathf.Clamp01((progress-.35f)*2.3f);
            for(int i=0;i<22;i++)
            {
                float a=i*2.399f;int bx=(int)(cx+Mathf.Cos(a)*radius*.72f),by=(int)(cy+Mathf.Sin(a)*radius*.58f);
                int height=(int)(burn*(10+9*(.5f+.5f*Mathf.Sin(clock*9+i*3))));
                for(int j=0;j<height+9*burn;j++)
                {int drift=(int)(Mathf.Sin(i*7+j*.23f+clock*3)*j*.23f);int width=Mathf.Max(1,3-j/7);bool smoke=j>=height;
                    for(int k=-width;k<=width;k++)Dot(bx+drift+k,by+j,smoke?C(.08f,.075f,.085f):j<height*.32f?C(.95f,.69f,.24f):C(.65f,.22f,.065f));}
            }
            for(int eye=0;eye<Eyes.Length;eye++)
            {
                float onset=eye<3?.40f+eye*.075f:.22f+(eye%5)*.023f;
                float appear=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-onset)*26));
                if(appear<=0)continue;
                int ex=(int)Eyes[eye].x,ey=(int)Eyes[eye].y;
                // Foreground eyes are cropped by the screen. No face, fog or visible body.
                int rw=eye<3?108-eye*8:16+(eye*17)%24;
                int rh=Mathf.Max(1,(int)((eye<3?27:7+eye%5)*appear));
                float gazeX=(cx-ex)*.035f,gazeY=(cy-ey)*.025f;
                for(int y=-rh;y<=rh;y++)for(int x=-rw;x<=rw;x++)
                {
                    float almond=Mathf.Abs(y)/(float)rh+Mathf.Pow(Mathf.Abs(x)/(float)rw,1.65f);
                    if(almond>1)continue;
                    float shade=Mathf.Pow(1-almond,.8f)*appear;
                    Color col=new Color(.035f,.008f,.003f)*shade;
                    float ix=x-gazeX,iy=y-gazeY,ir=rh*.85f;
                    float d=(ix*ix+iy*iy)/(ir*ir);
                    if(d<1){float spokes=.55f+.45f*Mathf.Sin(Mathf.Atan2(iy,ix)*23+eye);float rim=Mathf.SmoothStep(1,.65f,d);col=new Color(.74f,.24f,.025f)*(.35f+.65f*spokes)*rim*appear;}
                    if(d<.16f)col=Color.black;
                    if(ix>ir*.16f&&ix<ir*.31f&&iy>ir*.16f&&iy<ir*.31f)col=new Color(.95f,.59f,.18f)*appear;
                    Dot(ex+x,ey+y,col);
                }
            }
            float fade=Mathf.Clamp01((progress-.88f)/.12f);
            if(fade>0)for(int i=0;i<pixels.Length;i++)pixels[i]=Color.Lerp(pixels[i],Color.black,fade);
            image.SetPixels32(pixels);image.Apply(false);return image;
        }
        public void Dispose(){if(image!=null){if(Application.isPlaying)Object.Destroy(image);else Object.DestroyImmediate(image);}}
    }
}
