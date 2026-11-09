using System.Collections.Generic;
using UnityEngine;
namespace DeathBySeventhsDawn.Interaction
{
    // Only the apartment street receives this first art pass. Physical sites remain owned by CityWorld.
    public sealed class StreetDepth:MonoBehaviour
    {
        readonly List<Object> owned=new List<Object>();readonly List<Transform> distant=new List<Transform>();
        Sprite sprite;Camera view;Transform actor;float baseSize;
        void Rect(string name,Vector2 p,Vector2 size,Color color,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=p;go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;if(order<-5)distant.Add(go.transform);
        }
        void Start()
        {
            var tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();owned.Add(tex);sprite=Sprite.Create(tex,new Rect(0,0,1,1),Vector2.one*.5f,1);owned.Add(sprite);
            for(int i=0;i<13;i++)
            {
                float x=-10+i*1.7f,height=1.4f+(i*7%5)*.32f;
                Rect("Distant skyline",new Vector2(x,5.6f),new Vector2(1.5f,height),new Color(.12f,.17f,.21f),-8);
                for(int k=0;k<3;k++)Rect("Distant lit windows",new Vector2(x-.4f+k*.3f,5.8f),new Vector2(.08f,.13f),new Color(.35f,.39f,.36f),-7);
            }
            Rect("Shop rolling shutter",new Vector2(-6.4f,3.7f),new Vector2(2.4f,1.2f),new Color(.19f,.25f,.27f),0);
            for(int i=0;i<9;i++)Rect("Shutter seam",new Vector2(-6.4f,3.2f+i*.12f),new Vector2(2.4f,.025f),new Color(.09f,.14f,.17f),1);
            Rect("Canvas awning",new Vector2(-6.4f,4.5f),new Vector2(2.8f,.4f),new Color(.37f,.26f,.25f),1);
            for(int i=0;i<7;i++)Rect("Awning stripe",new Vector2(-7.55f+i*.4f,4.5f),new Vector2(.18f,.4f),new Color(.58f,.51f,.40f),2);
            for(int i=0;i<5;i++)Rect("Power cable",new Vector2(-4+i*2,4.95f-Mathf.Sin(i*.7f)*.22f),new Vector2(2.05f,.025f),new Color(.07f,.10f,.12f),2);
            for(int i=0;i<7;i++)Rect("Puddle glint",new Vector2(-5+(i*31%100)*.11f,-2.9f+(i%3)*.55f),new Vector2(.6f+(i%2)*.4f,.035f),new Color(.36f,.45f,.47f),-1);
            Rect("Near alley silhouette",new Vector2(-8,-4.2f),new Vector2(1.1f,2.2f),new Color(.07f,.10f,.12f),7);
            Rect("Near refuse bin",new Vector2(8,-4.7f),new Vector2(.8f,1.1f),new Color(.11f,.16f,.16f),7);
            view=Camera.main;actor=GameObject.FindWithTag("Player")?.transform;if(view){baseSize=view.orthographicSize;view.orthographicSize=5.4f;}
        }
        void LateUpdate()
        {
            if(!view||!actor)return;var target=new Vector3(Mathf.Clamp(actor.position.x*.15f,-1.1f,1.1f),Mathf.Clamp(actor.position.y*.1f,-.4f,.4f),-10);
            var old=view.transform.position;view.transform.position=Vector3.Lerp(old,target,Time.unscaledDeltaTime*3);
            var delta=view.transform.position-old;foreach(var layer in distant)if(layer)layer.position+=delta*.16f;
        }
        void OnDestroy(){foreach(var item in owned)if(item)Destroy(item);}
    }
}
