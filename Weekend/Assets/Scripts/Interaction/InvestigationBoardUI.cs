using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DeathBySeventhsDawn.Knowledge;

namespace DeathBySeventhsDawn.Interaction
{
    public sealed class InvestigationBoardUI
    {
        string first,second,message="先选两张已经收录的卡片，再判断它们有什么关系。";
        int relation,focus;
        bool reviewed;
        Vector2 boardScroll,detailScroll;
        GUIStyle title,body,small,button,cardTitle;
        static readonly Color Ink=new Color(.16f,.22f,.21f),Green=new Color(.22f,.42f,.36f),Paper=new Color(.92f,.88f,.75f);
        public void Reset(){first=second=null;relation=focus=0;reviewed=false;boardScroll=detailScroll=Vector2.zero;message="先选两张已经收录的卡片，再判断它们有什么关系。";}
        void Styles()
        {
            if(body!=null)return;var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei"},24);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=true};body.normal.textColor=Ink;
            title=new GUIStyle(body){fontSize=30,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=16};
            cardTitle=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};
            button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,padding=new RectOffset(10,10,8,8)};
            foreach(var style in new[]{body,title,small,cardTitle,button})
                foreach(var state in new[]{style.normal,style.hover,style.active,style.focused,style.onNormal,style.onHover,style.onActive,style.onFocused})state.textColor=Ink;
            button.hover.textColor=Green;
        }
        public void Select(string id)
        {
            if(id==first){first=second;second=null;}
            else if(id==second)second=null;
            else if(first==null)first=id;
            else if(second==null)second=id;
            else{first=second;second=id;}
            reviewed=false;detailScroll=Vector2.zero;
        }
        public void KeyboardInput()
        {
            var k=Keyboard.current;if(k==null)return;var nodes=Investigation.Nodes();
            if(nodes.Count>0)
            {
                int step=(k.rightArrowKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame?1:0)-(k.leftArrowKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame?1:0);
                if(step!=0){focus=(focus+step+nodes.Count)%nodes.Count;boardScroll.y=Mathf.Max(0,(focus/2)*190-150);}
                focus=Mathf.Clamp(focus,0,nodes.Count-1);if(k.spaceKey.wasPressedThisFrame)Select(nodes[focus].id);
            }
            if(k.tabKey.wasPressedThisFrame){relation=(relation+1)%Investigation.Relations.Length;reviewed=false;}
            if(k.enterKey.wasPressedThisFrame&&first!=null&&second!=null){if(!reviewed)Review();else Commit();}
            if(k.backspaceKey.wasPressedThisFrame){first=second=null;reviewed=false;}
            if(k.deleteKey.wasPressedThisFrame&&first!=null&&second!=null)Remove();
        }
        void Review(){reviewed=true;detailScroll=Vector2.zero;message=Investigation.Match(first,second,Investigation.Relations[relation])==null?"现在还没有足够依据。可以留下这条待证联系，之后再回来检查。":"这两条记录支持下面的推论。读完依据，再决定是否写下。";}
        void Commit()
        {
            var rule=Investigation.Match(first,second,Investigation.Relations[relation]);
            bool changed=Investigation.Connect(first,second,Investigation.Relations[relation],rule!=null);
            message=changed?(rule!=null?"推论已写下。它也可以成为下一条推理的依据。":"已留下待证连线；它不会被当作事实。"):"这条联系已经保存在笔记里。";
        }
        void Remove(){message=Investigation.RemoveTentative(first,second,Investigation.Relations[relation])?"已擦去待证连线。":"只有待证连线可以擦去；已写下的推论保留其依据。";}
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        static void Line(Vector2 a,Vector2 b,Color color,bool dashed)
        {
            // Axis-aligned pixel strokes keep Unity's scroll clipping intact; rotating GUI.matrix rotates its clip too.
            if(Event.current.type!=EventType.Repaint)return;
            float length=Vector2.Distance(a,b);int count=Mathf.Max(1,Mathf.CeilToInt(length/3));
            for(int i=0;i<=count;i++){if(dashed&&(i/4)%2==1)continue;Vector2 p=Vector2.Lerp(a,b,(float)i/count);Fill(new Rect(p.x,p.y,3,3),color);}
        }

        public void Draw(float w,float h)
        {
            Styles();Fill(new Rect(0,82,w,h-82),new Color(.08f,.13f,.13f));
            float margin=28,side=Mathf.Clamp(w*.3f,320,430),graphWidth=w-side-margin*3;
            var graph=new Rect(margin,110,graphWidth,h-152);var detail=new Rect(graph.xMax+margin,110,side,h-152);
            Fill(graph,new Color(.79f,.77f,.66f));Fill(detail,Paper);
            GUI.Label(new Rect(graph.x+24,graph.y+16,graph.width-48,45),"线索连线",title);
            GUI.Label(new Rect(graph.x+24,graph.y+62,graph.width-48,46),"实线：已整理的依据  ·  虚线：待证\n只有主动收录的证据与推论会出现在这里。",small);
            var nodes=Investigation.Nodes();float cw=(graph.width-150)/2;
            var viewport=new Rect(graph.x+16,graph.y+128,graph.width-32,graph.height-195);
            var rects=new Dictionary<string,Rect>();
            for(int i=0;i<nodes.Count;i++)rects[nodes[i].id]=new Rect(12+(i%2)*(cw+86),12+(i/2)*190,cw,150);
            boardScroll=GUI.BeginScrollView(viewport,boardScroll,new Rect(0,0,viewport.width-20,Mathf.Max(viewport.height-20,((nodes.Count+1)/2)*190+24)));
            foreach(var link in Investigation.Links)
            {
                if(link==null||!rects.ContainsKey(link.left)||!rects.ContainsKey(link.right))continue;
                Line(rects[link.left].center,rects[link.right].center,string.IsNullOrEmpty(link.conclusion)?new Color(.45f,.38f,.28f):Green,string.IsNullOrEmpty(link.conclusion));
                if(!string.IsNullOrEmpty(link.conclusion)&&rects.ContainsKey(link.conclusion))
                {Line(rects[link.left].center,rects[link.conclusion].center,Green,false);Line(rects[link.right].center,rects[link.conclusion].center,Green,false);}
            }
            for(int i=0;i<nodes.Count;i++)
            {
                var entry=nodes[i];var rect=rects[entry.id];bool chosen=first==entry.id||second==entry.id;
                Fill(new Rect(rect.x+4,rect.y+5,rect.width,rect.height),new Color(.13f,.17f,.15f,.2f));Fill(rect,chosen?new Color(.98f,.94f,.79f):Paper);
                if(i==focus)Fill(new Rect(rect.x,rect.y,rect.width,3),Green);
                if(chosen)Fill(new Rect(rect.x,rect.y,5,rect.height),Green);
                bool deduction=Investigation.Evidence(entry.id)==null;
                GUI.Label(new Rect(rect.x+16,rect.y+12,rect.width-32,26),(chosen?(first==entry.id?"① ":"② "):"")+(deduction?"推论":"现场记录")+" · 第 "+entry.day+" 天",small);
                GUI.Label(new Rect(rect.x+16,rect.y+47,rect.width-32,72),entry.title,cardTitle);
                GUI.Label(new Rect(rect.x+16,rect.y+121,rect.width-32,24),deduction?"可继续作为依据":Investigation.Evidence(entry.id).source,small);
                if(GUI.Button(rect,GUIContent.none,GUIStyle.none)){focus=i;Select(entry.id);}
            }
            if(nodes.Count==0)GUI.Label(new Rect(24,35,viewport.width-65,180),"纸页还是空的。\n\n留意卧室桌上的手记、阳台望远镜和 401 门旁的传单。查看后，选择收录才会留下证据。",body);
            GUI.EndScrollView();
            GUI.Label(new Rect(graph.x+22,graph.yMax-56,graph.width-44,50),"鼠标选卡片  /  方向键移动 · 空格选择\nTab 换关系 · Enter 检查/写下 · Backspace 清空选择",small);
            GUILayout.BeginArea(new Rect(detail.x+22,detail.y+18,detail.width-44,detail.height-36));
            detailScroll=GUILayout.BeginScrollView(detailScroll);
            GUILayout.Label("整理思路",title);GUILayout.Space(16);
            GUILayout.Space(16);GUILayout.Label(message,small);
            if(reviewed){var rule=Investigation.Match(first,second,Investigation.Relations[relation]);if(rule!=null){GUILayout.Space(18);GUILayout.Label(rule.title,cardTitle);GUILayout.Label(rule.body,body);}}
            GUILayout.Space(18);
            GUILayout.Label("它们之间是什么关系？",body);
            for(int i=0;i<Investigation.Relations.Length;i++)if(GUILayout.Button((relation==i?"●  ":"○  ")+Investigation.Relations[i],button,GUILayout.MinHeight(42))){relation=i;reviewed=false;}
            GUILayout.Space(18);
            foreach(string id in new[]{first,second})
            {
                if(id==null){GUILayout.Label("还没有选择这张卡片",small);GUILayout.Space(15);continue;}
                var entry=nodes.Find(e=>e.id==id);if(entry==null)continue;
                GUILayout.Label(entry.title,cardTitle);GUILayout.Label(entry.body,small);
                if(!string.IsNullOrEmpty(entry.imageResource)){var texture=Resources.Load<Texture2D>(entry.imageResource);if(texture!=null){var r=GUILayoutUtility.GetRect(detail.width-70,145);GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit);}}
                GUILayout.Space(16);
            }

            GUILayout.EndScrollView();
            bool enabled=GUI.enabled;GUI.enabled=first!=null&&second!=null;
            if(GUILayout.Button(!reviewed?"检查这条联系  ·  Enter":Investigation.Match(first,second,Investigation.Relations[relation])!=null?"写下推论  ·  Enter":"保留待证连线  ·  Enter",button,GUILayout.MinHeight(56))){if(!reviewed)Review();else Commit();}
            if(GUILayout.Button("擦去这条待证线  ·  Delete",button,GUILayout.MinHeight(42)))Remove();GUI.enabled=enabled;
            GUILayout.EndArea();
        }
    }
}
