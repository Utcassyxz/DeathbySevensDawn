using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using DeathBySeventhsDawn.Interaction;
using DeathBySeventhsDawn.Player;
using DeathBySeventhsDawn.TimeSystem;

[InitializeOnLoad]
public static class VerifyBedroomWindowInteraction
{
    const string Active="VerifyBedroomWindowInteraction.Active";
    static int phase; static double next,deadline; static Keyboard keyboard;
    static VerifyBedroomWindowInteraction(){EditorApplication.update+=Tick;}
    [MenuItem("Prototype/Test bedroom window and Days")]
    public static void Run()
    {
        phase=0;next=0;deadline=EditorApplication.timeSinceStartup+60;
        SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene("Assets/Scenes/Apartment/Bedroom.unity");
        EditorApplication.isPlaying=true;
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false))return;
        if(EditorApplication.timeSinceStartup>deadline&&deadline>0){Finish("FAIL: timeout",false);return;}
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
        try
        {
            var window=UnityEngine.Object.FindFirstObjectByType<WindowLookout2D>();
            var time=TimeManager.Instance;
            if(window==null||time==null)return;
            var player=GameObject.FindWithTag("Player");var movement=player.GetComponent<PlayerController2D>();
            if(phase==0)
            {
                Check(!window.TryOpen(),"Window opens outside interaction range");
                player.GetComponent<Rigidbody2D>().position=new Vector2(-.2f,.95f);Physics2D.SyncTransforms();
                time.ResetToDayOne();Check(time.Days==1,"Days reset");
                keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
            }
            else if(phase==1)
            {
                Check(window.IsOpen&&!movement.enabled,"E opens window and pauses movement");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            }
            else if(phase>=2&&phase<=8)
            {
                int day=phase-1;
                Check(time.Days==day&&window.DisplayedDay==day,"Days display mapping "+day);
                Check(window.DisplayedTexture!=null&&window.DisplayedTexture==window.Sequence.GetImage(day),"Texture mapping "+day);

                if(day<7)for(int i=0;i<4;i++)time.AdvanceTime();
            }
            else if(phase==9)
            {
                Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot("../window-day7.png");
            }
            else if(phase==10)
            {
                Check(window.Sequence.GetImage(0)==window.Sequence.GetImage(1)&&window.Sequence.GetImage(8)==window.Sequence.GetImage(7),"Texture bounds");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
            }
            else if(phase==11)
            {
                Check(!window.IsOpen&&movement.enabled,"Esc closes and restores movement");
                Check(time.Days==7,"Viewing does not advance Days");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                time.ResetToDayOne();
                Finish("PASS: distant interaction rejected; E opens; Days 1-7 selects all seven textures; Esc restores movement; viewing preserves Days; bounds clamp; reset returns Days to 1.",true);return;
            }
            phase++;next=EditorApplication.timeSinceStartup+.25;
        }
        catch(Exception e){Finish("FAIL: "+e,false);}
    }
    static void Finish(string result,bool success)
    {
        File.WriteAllText("../window-runtime-validation.txt",result);SessionState.SetBool(Active,false);
        if(success)Debug.Log("WINDOW_RUNTIME_OK "+result);else Debug.LogError(result);
        if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
