using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    // A shared reading surface: measure prose separately from choices and controls.
    public static class NarrativeCard
    {
        static Vector2 scroll;static string previous;
        public static void Caption(string title,string body)
        {
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-120;float w=Screen.width/scale,h=Screen.height/scale;
            try
            {
                bool brief=string.IsNullOrEmpty(title);var area=new Rect((w-880)/2,h-(brief?92:194),880,brief?58:162);Fill(area,new Color(.035f,.045f,.05f,.96f));Fill(new Rect(area.x,area.y,2,area.height),new Color(.57f,.19f,.22f));
                var style=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=20,wordWrap=true};style.normal.textColor=new Color(.85f,.82f,.72f);
                if(!brief)GUI.Label(new Rect(area.x+25,area.y+16,830,35),title,style);style.fontSize=18;GUI.Label(new Rect(area.x+25,area.y+(brief?16:57),830,brief?32:94),body,style);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
        }
        public static int Draw(string title,string body,string notice,string[] choices,string footer)
        {
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;int chosen=-1;
            float scale=Mathf.Min(Screen.width/1200f,Screen.height/800f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.depth=-120;
            float w=Screen.width/scale,h=Screen.height/scale;
            var ink=new Color(.87f,.85f,.76f);var muted=new Color(.55f,.61f,.58f);var gold=new Color(.69f,.53f,.32f);
            try
            {
                if(previous!=title){previous=title;scroll=Vector2.zero;}
                var prose=new GUIStyle(GUI.skin.label){font=GameTypography.Current,fontSize=22,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};prose.normal.textColor=ink;
                var heading=new GUIStyle(prose){fontSize=30,fontStyle=FontStyle.Bold};
                var quiet=new GUIStyle(prose){fontSize=16};quiet.normal.textColor=muted;
                var option=new GUIStyle(prose){fontSize=20,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(16,12,8,8)};
                int count=choices?.Length??0;float[] heights=new float[count];float buttonHeight=0;
                for(int i=0;i<count;i++){heights[i]=Mathf.Max(43,option.CalcHeight(new GUIContent(choices[i]),780));buttonHeight+=heights[i]+7;}
                float proseHeight=prose.CalcHeight(new GUIContent(body??""),758);
                float cardHeight=Mathf.Clamp(150+buttonHeight+(string.IsNullOrEmpty(notice)?0:40)+Mathf.Max(64,proseHeight),260,568);
                var area=new Rect((w-840)/2,h-cardHeight-36,840,cardHeight);
                Fill(new Rect(0,0,w,h),new Color(.015f,.02f,.025f,.50f));
                Fill(new Rect(area.x+8,area.y+9,area.width,area.height),new Color(0,0,0,.65f));Fill(area,new Color(.055f,.071f,.073f,.98f));
                Fill(new Rect(area.x,area.y,area.width,2),gold);Fill(new Rect(area.x+28,area.y+70,area.width-56,1),new Color(.23f,.27f,.25f));
                GUI.Label(new Rect(area.x+30,area.y+20,780,44),title,heading);
                float bodyHeight=area.height-150-buttonHeight-(string.IsNullOrEmpty(notice)?0:40);
                var viewport=new Rect(area.x+30,area.y+88,780,Mathf.Max(60,bodyHeight));float contentHeight=prose.CalcHeight(new GUIContent(body??""),758);
                scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,758,Mathf.Max(viewport.height,contentHeight)),false,false);
                GUI.Label(new Rect(0,0,758,contentHeight),body,prose);GUI.EndScrollView();
                float y=viewport.yMax+12;
                if(!string.IsNullOrEmpty(notice)){quiet.normal.textColor=gold;GUI.Label(new Rect(area.x+30,y,780,36),notice,quiet);y+=40;quiet.normal.textColor=muted;}
                for(int i=0;i<count;i++)
                {
                    var row=new Rect(area.x+30,y,780,heights[i]);Fill(row,row.Contains(Event.current.mousePosition)?new Color(.17f,.21f,.20f):new Color(.10f,.13f,.13f));
                    if(PrototypeButtons.Button(row,choices[i],option))chosen=i;y+=heights[i]+7;
                }
                GUI.Label(new Rect(area.x+30,area.yMax-34,780,25),footer,quiet);
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
            return chosen;
        }
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
