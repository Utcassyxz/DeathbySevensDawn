using System.Collections.Generic;
using UnityEngine;

namespace DeathBySeventhsDawn.Interaction
{
    // Original pixel scenery drawn at a fixed density. Collision belongs to the room controllers.
    public sealed class PocketSceneArt : MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();
        static readonly Color Ink=new Color32(25,32,38,255),Paper=new Color32(174,164,133,255),Rust=new Color32(122,71,53,255);
        Texture2D canvas;int width,height;
        void Rect(int x,int y,int w,int h,Color c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(height,y+h);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(width,x+w);xx++)canvas.SetPixel(xx,yy,c);}
        void StartCanvas(int w,int h,Color fill){width=w;height=h;canvas=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};owned.Add(canvas);Rect(0,0,w,h,fill);}
        Sprite Finish(float density=20){canvas.Apply();var sprite=Sprite.Create(canvas,new Rect(0,0,width,height),Vector2.one*.5f,density);owned.Add(sprite);return sprite;}
        void Frame(int x,int y,int w,int h,Color fill){Rect(x,y,w,h,Ink);Rect(x+2,y+2,w-4,h-4,fill);Rect(x+3,y+h-4,w-6,1,Paper*.65f);}
        void Window(int x,int y,bool lit){Frame(x,y,27,35,lit?new Color(.38f,.40f,.33f):new Color(.13f,.23f,.28f));Rect(x+13,y+2,2,31,Ink);Rect(x+2,y+17,23,2,Ink);Rect(x-2,y-2,31,3,Paper*.45f);}
        void Door(int x,int y,Color fill){Frame(x,y,26,45,fill);Frame(x+5,y+22,16,17,Ink*.8f);Rect(x+20,y+14,2,2,Paper);}
        public void Backdrop(string region)
        {
            bool neighbor=region=="NeighborRoom2D",service=region=="BuildingService2D";
            if(neighbor)
            {
                StartCanvas(360,240,new Color(.19f,.18f,.16f));
                for(int y=0;y<190;y+=10)for(int x=0;x<360;x+=36){Rect(x-(y/10%2)*18,y,35,9,new Color(.19f+(x%11)*.003f,.17f,.14f));Rect(x,y,35,1,Ink*.5f);}
                Rect(0,190,360,50,new Color(.27f,.29f,.24f));Rect(0,187,360,4,Ink);
                for(int x=8;x<360;x+=14){Rect(x,193,1,43,Paper*.2f);Rect(x+3,211,3,5,Paper*.12f);}
                Window(36,195,false);Window(288,195,false);
                Frame(68,101,24,40,new Color(.25f,.23f,.18f));Rect(87,115,2,3,Paper); // Door at world (-5,0), visually recessed.
                Frame(158,145,44,23,new Color(.34f,.25f,.17f));Rect(166,159,4,5,Paper*.8f);Rect(190,159,5,4,Paper*.65f);Rect(162,150,14,9,Paper*.8f);Rect(183,157,8,4,Ink);
                Frame(218,153,44,24,new Color(.22f,.28f,.24f));Frame(222,163,16,11,new Color(.29f,.34f,.28f));Frame(240,163,17,11,new Color(.29f,.34f,.28f));
                Frame(132,79,98,50,new Color(.27f,.16f,.16f));for(int y=83;y<128;y+=5)Rect(136,y,90,1,Paper*.18f);
                Frame(12,202,17,27,new Color(.21f,.21f,.17f));for(int y=205;y<228;y+=5)Rect(14,y,13,1,Paper*.4f);
                Frame(314,42,26,58,new Color(.16f,.20f,.19f));Rect(317,45,20,1,Paper*.3f);
                for(int i=0;i<9;i++){Rect(270+i*3,190-i*8,2,17,Ink*.65f);}
            }
            else if(service)
            {
                StartCanvas(240,160,new Color(.20f,.23f,.22f));
                for(int y=0;y<160;y+=12)for(int x=0;x<240;x+=30){Rect(x+(y/12%2)*-10,y,29,11,new Color(.23f+(x%9)*.003f,.20f,.17f));Rect(x,y,29,1,new Color(.11f,.14f,.14f));}
                Rect(0,126,240,34,service?new Color(.18f,.27f,.27f):new Color(.31f,.34f,.29f));
                for(int x=5;x<240;x+=12){Rect(x,132,1,25,Paper*.18f);Rect(x,143,3,3,Paper*.25f);}Rect(0,126,240,3,Ink);
                Window(28,124,false);Window(180,124,neighbor);Door(5,62,new Color(.25f,.34f,.31f));
                Frame(84,62,69,43,new Color(.28f,.19f,.17f));for(int y=65;y<104;y+=4)Rect(87,y,63,1,Paper*.15f);
                Frame(100,105,40,20,new Color(.39f,.30f,.20f));Rect(101,105,38,3,Ink);Rect(106,119,11,5,Paper*.8f);Rect(127,112,5,8,Paper*.4f);
                Frame(160,110,40,20,new Color(.22f,.31f,.29f));Frame(163,116,16,11,new Color(.28f,.36f,.31f));Frame(180,116,16,11,new Color(.28f,.36f,.31f));
                Frame(42,7,39,18,new Color(.27f,.28f,.25f));Rect(45,11,32,4,Ink);Rect(47,18,9,3,Paper*.6f);
                if(service){for(int x=18;x<230;x+=36){Rect(x,126,4,32,Rust);Rect(x-3,138,10,5,Ink);}if(DeathBySeventhsDawn.Progression.CrisisRules.LegacyValidation){Frame(72,94,16,20,new Color(.18f,.41f,.39f));Frame(152,94,16,20,Rust);}
                    else {Frame(72,94,23,18,Paper*.7f);Rect(75,107,16,1,Ink);Rect(75,103,12,1,Ink);Rect(75,99,17,1,Ink);Frame(152,94,23,18,Paper*.5f);Rect(155,107,16,1,Ink);Rect(155,103,12,1,Ink);}}
                else {Frame(12,113,30,19,new Color(.31f,.26f,.18f));Rect(16,118,22,2,Ink);Rect(19,122,7,5,Paper*.6f);Rect(213,33,12,43,new Color(.29f,.31f,.22f));for(int y=35;y<77;y+=6)Rect(211,y,16,2,Ink);}
            }
            else
            {
                StartCanvas(360,240,new Color(.23f,.27f,.28f));
                bool street=region=="CityStreet";
                for(int y=0;y<240;y+=12)for(int x=0;x<360;x+=20){Rect(x+(y/12%2)*-8,y,19,11,new Color(.22f+(x%7)*.004f,.26f+(y%9)*.002f,.27f));Rect(x+2,y+2,2,1,Ink*.7f);}
                Rect(0,164,360,76,street?new Color(.28f,.29f,.26f):new Color(.24f,.30f,.29f));Rect(0,161,360,4,Ink);
                for(int y=170;y<240;y+=8)for(int x=0;x<360;x+=22){Rect(x+(y%16==2?11:0),y,21,1,Paper*.15f);}
                if(street)
                {
                    for(int x=16;x<360;x+=57)Window(x,199,x%3==1);
                    Window(47,155,false);Door(167,151,new Color(.26f,.27f,.31f));Window(287,155,false);
                    Frame(161,198,38,9,new Color(.54f,.43f,.23f));
                    for(int y=32;y<120;y+=5){Rect(0,y,19,2,Ink);Rect(341,y,19,2,Ink);}Rect(0,131,360,4,Paper*.35f);
                    Frame(26,63,30,30,new Color(.31f,.19f,.22f));for(int x=28;x<55;x+=6)Rect(x,88,3,10,Paper*.7f);Frame(305,63,30,30,new Color(.22f,.32f,.28f));Rect(306,90,28,7,Rust);
                    for(int x=32;x<340;x+=76){Rect(x,118,3,29,Ink);Rect(x-3,144,9,6,Paper*.65f);}
                    Frame(74,27,38,12,new Color(.25f,.23f,.20f));Rect(77,23,3,6,Ink);Rect(103,23,3,6,Ink);
                    Frame(253,29,28,20,new Color(.23f,.29f,.25f));Rect(255,43,24,3,Paper*.4f);
                    for(int i=0;i<15;i++){int x=28+(i*47)%305,y=15+(i*29)%93;Rect(x,y,5+i%4,1,new Color(.36f,.42f,.41f));}
                    Rect(169,3,22,25,new Color(.14f,.20f,.23f));Rect(169,27,22,2,Paper*.5f);
                }
                else
                {
                    Window(22,187,false);Window(310,187,false);Door(167,5,new Color(.26f,.27f,.24f));
                    if(region=="CityService")
                    {for(int x=25;x<350;x+=40){Rect(x,165,5,70,Rust);Frame(x-4,198,13,8,Paper*.6f);}Frame(64,141,75,28,new Color(.35f,.29f,.19f));Frame(227,141,69,31,new Color(.23f,.39f,.36f));for(int x=232;x<290;x+=10)Frame(x,149,6,12,Rust);}
                    else if(region=="CityBroadcast")
                    {Frame(62,139,74,34,new Color(.31f,.31f,.25f));Frame(223,139,76,34,new Color(.20f,.32f,.33f));for(int x=228;x<294;x+=12){Rect(x,149,7,3,Paper*.7f);Rect(x,156,3,8,Rust);}Frame(151,183,59,37,new Color(.30f,.25f,.20f));for(int y=188;y<214;y+=6)Rect(156,y,49,2,Ink);}
                    else
                    {Frame(61,141,76,28,new Color(.32f,.26f,.36f));Frame(224,141,76,28,new Color(.25f,.33f,.36f));for(int i=0;i<35;i++){int x=95+(i*37)%170,y=182+(i*19)%51;Rect(x,y,1,1,Paper*.8f);}Frame(164,190,34,28,new Color(.31f,.36f,.39f));Rect(174,204,16,5,Ink);}
                    for(int x=43;x<320;x+=62){Frame(x,30,18,22,new Color(.21f,.24f,.24f));Rect(x+4,45,10,2,Paper*.3f);}
                }
            }
            var go=new GameObject("Hand-drawn pocket scenery");go.transform.SetParent(transform);go.AddComponent<SpriteRenderer>().sprite=Finish();go.GetComponent<SpriteRenderer>().sortingOrder=-4;
        }
        public Sprite Person(bool hat=false,bool exposed=false)
        {
            StartCanvas(18,32,Color.clear);Rect(4,2,4,10,Ink);Rect(10,2,4,10,Ink);Rect(4,1,5,3,Ink*.7f);Rect(10,1,5,3,Ink*.7f);
            Frame(4,11,10,12,new Color(.28f,.35f,.31f));Rect(8,12,2,11,Paper*.4f);Rect(2,12,3,10,new Color(.63f,.47f,.36f));Rect(14,12,2,10,new Color(.63f,.47f,.36f));
            Rect(5,23,8,7,new Color(.64f,.49f,.39f));Rect(6,26,2,1,Ink);Rect(11,26,1,1,Ink);Rect(8,23,4,1,Rust);
            if(exposed){Rect(5,28,6,3,new Color(.53f,.14f,.22f));Rect(6,30,2,1,new Color(.78f,.37f,.39f));Rect(9,29,2,1,new Color(.78f,.37f,.39f));}
            else Rect(5,29,8,2,Ink);
            if(hat){Rect(2,28,14,2,Ink);Frame(5,29,9,3,new Color(.28f,.25f,.20f));}
            return Finish(20);
        }
        public Sprite Hat()
        {StartCanvas(22,9,Color.clear);Rect(0,0,22,2,Ink);Frame(4,2,14,7,new Color(.24f,.25f,.20f));Rect(5,3,12,2,new Color(.37f,.31f,.21f));return Finish(20);}
        public void Marker(string id,Vector3 p,bool portal)
        {
            bool person=id=="caretaker"||id=="night-clerk"||id=="cat-feeder";StartCanvas(22,28,Color.clear);
            if(person){var sprite=Person(true);var npc=new GameObject("Caretaker silhouette");npc.transform.position=p;npc.AddComponent<SpriteRenderer>().sprite=sprite;npc.GetComponent<SpriteRenderer>().sortingOrder=3;return;}
            if(portal){Rect(10,0,3,23,Ink);Frame(0,16,22,10,new Color(.32f,.37f,.31f));Rect(4,20,14,2,Paper);Rect(id=="service"?3:16,18,3,6,Paper);}
            else {Frame(0,1,22,15,new Color(.33f,.27f,.19f));Rect(3,11,16,7,Paper*.8f);Rect(5,13,11,1,Ink);Rect(5,16,7,1,Ink);}
            var go=new GameObject(id+" fixture");go.transform.position=p+Vector3.up*.15f;go.AddComponent<SpriteRenderer>().sprite=Finish();go.GetComponent<SpriteRenderer>().sortingOrder=2;
        }
        void OnDestroy(){foreach(var item in owned)Destroy(item);}
    }
}
