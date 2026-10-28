using System;
using System.Collections.Generic;
using UnityEngine;
using DeathBySeventhsDawn.Knowledge;
using DeathBySeventhsDawn.Progression;
namespace DeathBySeventhsDawn.Interaction
{
    // 保留已有日记数据与输入锁，仅负责目录、阅读区和翻阅布局。
    public sealed class JournalNotebookUI
    {
        int filter;
        Vector2 indexScroll, articleScroll;
        GUIStyle heading, eyebrow, body, small, rowTitle, rowMeta, tab, selectedTab, navigation;
        readonly List<int> visible=new List<int>();
        static readonly Color Ink=new Color(.18f,.22f,.20f), Muted=new Color(.39f,.41f,.36f), Accent=new Color(.20f,.36f,.32f);
        public void ResetNavigation(){filter=0;indexScroll=Vector2.zero;articleScroll=Vector2.zero;}
        void Prepare()
        {
            if(body!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC"},24);
            body=Style(font,22,Ink);body.wordWrap=true;
            heading=Style(font,34,Ink);heading.fontStyle=FontStyle.Bold;heading.wordWrap=true;
            eyebrow=Style(font,14,Accent);small=Style(font,15,Muted);
            rowTitle=Style(font,19,Ink);rowTitle.wordWrap=true;
            rowMeta=Style(font,13,Muted);
            tab=new GUIStyle(GUIStyle.none){font=font,fontSize=17,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(8,8,8,8)};
            foreach(var state in new[]{tab.normal,tab.hover,tab.active,tab.focused,tab.onNormal,tab.onHover,tab.onActive,tab.onFocused})state.background=null;
            tab.normal.textColor=Ink;tab.hover.textColor=Accent;
            selectedTab=new GUIStyle(tab);selectedTab.normal.textColor=new Color(.98f,.94f,.83f);selectedTab.fontStyle=FontStyle.Bold;selectedTab.hover.textColor=selectedTab.active.textColor=selectedTab.focused.textColor=selectedTab.normal.textColor;
            navigation=new GUIStyle(tab){fontSize=18};
        }
        static GUIStyle Style(Font font,int size,Color color)
        {
            var result=new GUIStyle(GUI.skin.label){font=font,fontSize=size,padding=new RectOffset(0,0,0,0)};
            result.normal.textColor=color;return result;
        }
        static void Fill(Rect rect,Color color)
        {
            var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
        }
        void Collect(IReadOnlyList<JournalEntry> entries)
        {
            visible.Clear();
            for(int i=entries.Count-1;i>=0;i--)
                if(entries[i]!=null&&(filter==0||(filter==1?entries[i].isKnowledge:!entries[i].isKnowledge)))visible.Add(i);
        }
        public bool MoveSelection(IReadOnlyList<JournalEntry> entries,ref int selected,int step)
        {
            Collect(entries);if(visible.Count==0)return false;
            int position=visible.IndexOf(selected);if(position<0)position=0;
            int next=visible[Mathf.Clamp(position+step,0,visible.Count-1)];
            if(next==selected)return false;selected=next;articleScroll=Vector2.zero;return true;
        }
        public void Draw(IReadOnlyList<JournalEntry> entries,ref int selected,Action readSelection,Action close,float width,float height)
        {
            Prepare();
            Fill(new Rect(0,0,width,height),new Color(.035f,.055f,.055f,.96f));
            float bookWidth=Mathf.Min(1380,width-48),bookHeight=height-56;
            var book=new Rect((width-bookWidth)/2,28,bookWidth,bookHeight);
            Fill(new Rect(book.x+9,book.y+10,book.width,book.height),new Color(0,0,0,.30f));
            Fill(book,new Color(.16f,.23f,.21f));
            float sidebar=Mathf.Clamp(bookWidth*.27f,255,340);
            var left=new Rect(book.x+12,book.y+12,sidebar,book.height-24);
            var right=new Rect(left.xMax+7,book.y+12,book.width-sidebar-31,book.height-24);
            Fill(left,new Color(.84f,.81f,.70f));Fill(right,new Color(.94f,.91f,.81f));
            Fill(new Rect(left.xMax,book.y+12,7,book.height-24),new Color(.43f,.43f,.34f));
            GUI.Label(new Rect(left.x+24,left.y+20,left.width-48,44),"随身日记",heading);
            GUI.Label(new Rect(left.x+25,left.y+70,left.width-50,24),"观察 · 梦境 · 无法遗忘的事",eyebrow);
            GUI.Label(new Rect(left.x+25,left.y+99,left.width-50,22),entries.Count+" 则记录  /  "+JournalUpdates.UnreadCount+" 则未读",small);
            var tabs=new[]{"全部","发现","梦境"};
            for(int i=0;i<3;i++)
            {
                var rect=new Rect(left.x+20+i*(left.width-40)/3,left.y+139,(left.width-46)/3,36);
                if(i==filter)Fill(rect,Accent);
                if(GUI.Button(rect,tabs[i],i==filter?selectedTab:tab)&&filter!=i)
                {
                    filter=i;Collect(entries);indexScroll=articleScroll=Vector2.zero;
                    if(visible.Count>0&&!visible.Contains(selected)){selected=visible[0];readSelection();}
                }
            }
            Collect(entries);
            var listRect=new Rect(left.x+18,left.y+190,left.width-36,left.height-240);
            float rowWidth=listRect.width-18,total=0;
            foreach(int index in visible)total+=Mathf.Max(86,rowTitle.CalcHeight(new GUIContent(entries[index].title),rowWidth-30)+40)+8;
            indexScroll=GUI.BeginScrollView(listRect,indexScroll,new Rect(0,0,rowWidth,total));
            float rowY=0;
            foreach(int index in visible)
            {
                var entry=entries[index];float rowHeight=Mathf.Max(86,rowTitle.CalcHeight(new GUIContent(entry.title),rowWidth-30)+40);
                var row=new Rect(0,rowY,rowWidth,rowHeight);
                Fill(row,index==selected?new Color(.94f,.92f,.83f):new Color(.80f,.78f,.67f));
                if(index==selected)Fill(new Rect(0,rowY,4,rowHeight),Accent);
                if(!entry.isRead)Fill(new Rect(rowWidth-13,rowY+12,5,5),new Color(.66f,.34f,.16f));
                GUI.Label(new Rect(14,rowY+10,rowWidth-30,rowHeight-35),entry.title,rowTitle);
                GUI.Label(new Rect(14,rowY+rowHeight-25,rowWidth-28,20),"第 "+entry.day+" 天 · "+(entry.isKnowledge?"发现":"梦境")+(!entry.isRead?" · 未读":""),rowMeta);
                if(GUI.Button(row,GUIContent.none,GUIStyle.none)&&selected!=index){selected=index;articleScroll=Vector2.zero;readSelection();}
                rowY+=rowHeight+8;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(left.x+24,left.yMax-36,left.width-48,20),"新记录在前 · 已读状态会保留",rowMeta);
            GUI.Label(new Rect(right.x+36,right.y+24,right.width-200,24),"DeathbySevensDawn   /   记忆手稿",eyebrow);
            if(GUI.Button(new Rect(right.xMax-147,right.y+16,121,38),"合上  ×",navigation))close();
            Fill(new Rect(right.x+30,right.y+75,right.width-60,1),new Color(.73f,.73f,.63f));
            bool selectedVisible=visible.Contains(selected);
            if(selectedVisible)DrawArticle(entries[selected],new Rect(right.x+36,right.y+98,right.width-72,right.height-185));
            else
            {
                GUI.Label(new Rect(right.x+40,right.y+130,right.width-80,55),"这一页还没有写下什么。",heading);
                GUI.Label(new Rect(right.x+40,right.y+204,right.width-80,90),filter==0?"去看看窗外，或留意身边的变化。发现会留在这里。":"这里暂时没有这一类记录。",body);
            }
            Fill(new Rect(right.x+30,right.yMax-70,right.width-60,1),new Color(.73f,.73f,.63f));
            int current=visible.IndexOf(selected);
            GUI.Label(new Rect(right.x+36,right.yMax-47,200,25),selectedVisible?"第 "+(current+1)+" / "+visible.Count+" 则  ·  ← →":"尚无记录",small);
            bool wasEnabled=GUI.enabled;
            GUI.enabled=wasEnabled&&current>0;
            if(GUI.Button(new Rect(right.xMax-280,right.yMax-55,112,35),"← 上一则",navigation)&&MoveSelection(entries,ref selected,-1))readSelection();
            GUI.enabled=wasEnabled&&current>=0&&current<visible.Count-1;
            if(GUI.Button(new Rect(right.xMax-151,right.yMax-55,112,35),"下一则 →",navigation)&&MoveSelection(entries,ref selected,1))readSelection();
            GUI.enabled=wasEnabled;
        }
        void DrawArticle(JournalEntry entry,Rect viewport)
        {
            float contentWidth=viewport.width-22;
            float titleHeight=heading.CalcHeight(new GUIContent(entry.title),contentWidth);
            string meta="第 "+(entry.loop+1)+" 次旅程   /   第 "+entry.day+" 天   /   "+(entry.isKnowledge?(entry.tier==KnowledgeTier.Cosmic?"宇宙知识":entry.tier==KnowledgeTier.Forbidden?"禁忌知识":"普通知识"):"梦境记录");
            string prose=string.IsNullOrWhiteSpace(entry.body)?"这一段记忆还没有整理成文字。":entry.body;
            float metaHeight=small.CalcHeight(new GUIContent(meta),contentWidth);
            float bodyHeight=body.CalcHeight(new GUIContent(prose),contentWidth);
            Texture2D picture=string.IsNullOrEmpty(entry.imageResource)?null:Resources.Load<Texture2D>(entry.imageResource);
            float imageHeight=picture!=null?Mathf.Min(420,contentWidth*picture.height/picture.width):0;
            float total=titleHeight+metaHeight+bodyHeight+imageHeight+140;
            articleScroll=GUI.BeginScrollView(viewport,articleScroll,new Rect(0,0,contentWidth,total));
            GUI.Label(new Rect(0,0,contentWidth,titleHeight),entry.title,heading);
            GUI.Label(new Rect(0,titleHeight+18,contentWidth,metaHeight+6),meta,small);
            float y=titleHeight+metaHeight+54;
            GUI.Label(new Rect(0,y,contentWidth,bodyHeight+6),prose,body);y+=bodyHeight+34;
            if(picture!=null)
            {
                Fill(new Rect(0,y,contentWidth,imageHeight+12),new Color(.78f,.77f,.67f));
                GUI.DrawTexture(new Rect(6,y+6,contentWidth-12,imageHeight),picture,ScaleMode.ScaleToFit);
            }
            GUI.EndScrollView();
        }
    }
}
