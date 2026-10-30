using System;
using System.IO;
using UnityEngine;
namespace DeathBySeventhsDawn.Progression
{
    public static class ProgressionSave
    {
        public static string PathName => Path.Combine(Application.persistentDataPath,"seven-day-progress-v1.json");
        public static ProgressionState Load()
        {
            try {
                if(!File.Exists(PathName)) return new ProgressionState();
                var state=JsonUtility.FromJson<ProgressionState>(File.ReadAllText(PathName));
                if(state==null || (state.version!=1&&state.version!=2) || state.loopCount<0 || !Valid(state.knowledge) || !Valid(state.mutation) || !Valid(state.sanityBurden))
                    throw new InvalidDataException("Unsupported or invalid progression save.");
                state.knowledgeIds=state.knowledgeIds??new System.Collections.Generic.List<string>();
                Knowledge.Investigation.Normalize(state);
                return state;
            } catch(Exception ex) { Debug.LogWarning("Progression save could not be read; original file retained: "+ex.Message); return new ProgressionState(); }
        }
        static bool Valid(float v) => v>=0 && !float.IsInfinity(v) && !float.IsNaN(v);
        public static void Write(ProgressionState state)
        {
            try {
                string path=PathName, temp=path+".tmp";
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temp,JsonUtility.ToJson(state,true));
                if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
            } catch(Exception ex) { Debug.LogError("Progression save failed: "+ex.Message); }
        }
    }
}