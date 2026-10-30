using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    // Render at a fixed low resolution; point sampling keeps every effect on the same pixel grid.
    public sealed class OpeningSky : System.IDisposable
    {
        const int W=480,H=270;
        Texture2D image; readonly Color32[] pixels=new Color32[W*H];
        float last=-1;
        static Color32 C(float r,float g,float b)=>new Color(r,g,b,1);
        void Dot(int x,int y,Color32 c){if(x>=0&&y>=0&&x<W&&y<H)pixels[y*W+x]=c;}
        public Texture2D Render(float clock,float progress)
        {
            if(image==null)image=new Texture2D(W,H,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            if(clock-last<.065f)return image;last=clock;
            float grow=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress*1.8f));
            float radius=12+grow*13,cx=240,cy=100;
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                int hash=unchecked(x*73856093^y*19349663);float noise=(hash&255)/255f;
                pixels[y*W+x]=C(.012f+noise*.006f,.018f+noise*.008f,.037f+noise*.014f);
                if((hash&2047)==37)Dot(x,y,C(.24f+noise*.3f,.29f+noise*.3f,.36f+noise*.3f));
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
            float burn=Mathf.Clamp01((progress-.48f)*2.5f);
            for(int i=0;i<15;i++)
            {
                float a=i*2.399f;int bx=(int)(cx+Mathf.Cos(a)*radius*.75f),by=(int)(cy+Mathf.Sin(a)*radius*.65f);
                for(int j=0;j<(int)(22*burn);j++)
                {
                    int drift=(int)(Mathf.Sin(i*7+j*.2f+clock)*j*.2f);
                    int x=bx+drift,y=by+j;
                    Color32 c=j<7?C(.68f,.29f,.10f):C(.14f,.13f,.16f);
                    for(int k=-(j/7+1);k<=j/7+1;k++)Dot(x+k,y,c);
                }
            }
            float approach=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.18f)*1.8f));
            if(approach>0)for(int y=0;y<H;y++)for(int x=285;x<W;x++)
            {
                float mx=560-approach*58,my=220,rx=210,ry=190;
                float dx=(x-mx)/rx,dy=(y-my)/ry,d=dx*dx+dy*dy;
                if(d>1)continue;
                float grain=Mathf.PerlinNoise(x*.065f,y*.065f);
                bool ridge=Mathf.Abs(Mathf.Sin((x+y*.4f)*.055f+grain*2))<.10f;
                Dot(x,y,ridge?C(.095f,.115f,.105f):C(.022f+grain*.025f,.035f+grain*.02f,.045f));
                float ex=(x-420)/33f,ey=(y-170)/12f;
                if(ex*ex+ey*ey<1){float pupil=Mathf.Abs(x-420);Dot(x,y,pupil<3?C(.005f,.009f,.01f):C(.26f,.29f,.17f));}
            }
            float fade=Mathf.Clamp01((progress-.88f)/.12f);
            if(fade>0)for(int i=0;i<pixels.Length;i++)pixels[i]=Color.Lerp(pixels[i],Color.black,fade);
            image.SetPixels32(pixels);image.Apply(false);return image;
        }
        public void Dispose(){if(image!=null){if(Application.isPlaying)Object.Destroy(image);else Object.DestroyImmediate(image);}}
    }
}
