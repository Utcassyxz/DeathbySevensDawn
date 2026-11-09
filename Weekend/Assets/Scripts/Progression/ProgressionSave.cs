// 职责：本机 JSON 存档的加载和原子替换；失败时保留原始存档。
using System;
using System.IO;
using UnityEngine;

namespace DeathBySeventhsDawn.Progression
{
    public static class ProgressionSave
    {
        public static bool DebugStorage;
        public static string StorageDirectory
        {
            get
            {
                string root=Application.persistentDataPath;
#if UNITY_EDITOR
                if(UnityEditor.SessionState.GetBool("NeighborSliceValidationStorage",false))root=Path.Combine(root,"neighbor-slice-acceptance");
#endif
                return DebugStorage?Path.Combine(root,"neighbor-debug-session"):root;
            }
        }
        public static string PathName => Path.Combine(StorageDirectory, "seven-day-progress-v1.json");

        public static ProgressionState Load()
        {
            try
            {
                if (!File.Exists(PathName))
                    return new ProgressionState();
                var state = JsonUtility.FromJson<ProgressionState>(File.ReadAllText(PathName));
                if (state == null || (state.version < 1 || state.version > 7) || state.loopCount < 0 || !Valid(state.knowledge) || !Valid(state.mutation) || !Valid(state.sanityBurden))
                    throw new InvalidDataException("Unsupported or invalid progression save.");
                SaveValidation.NormalizeStage(state);
                state.stageEvents=state.stageEvents??new System.Collections.Generic.List<string>();
                if(float.IsNaN(state.savedMinute)||float.IsInfinity(state.savedMinute))state.savedMinute=-1;
                state.bubblePurchases = state.bubblePurchases ?? new System.Collections.Generic.List<string>();
                state.knowledgeIds = state.knowledgeIds ?? new System.Collections.Generic.List<string>();
                Knowledge.Investigation.Normalize(state);
                if (state.version < 3)
                {
                    state.version = 3;
                    if (state.yogDreamSeen)
                        state.bubbleIntroduced = state.bubblePortraitUnlocked = true;
                }

                if (state.version < 4)
                {
                    state.bubbleDreamUnlocked = state.ancientGodSeen || state.yogDreamSeen;
                    state.bubbleVisitLoop = state.yogDreamSeen ? state.loopCount : -1;
                    state.fishCompanionUnlocked = state.fishAlly;
                    state.fishCompanionUnlockLoop = state.loopCount;
                    state.version = 4;
                }
                state.fishStoryDays = state.fishStoryDays ?? new System.Collections.Generic.List<int>();
                if (state.version < 5)
                {
                    state.fishCompanionUnlocked = false;
                    state.fishStoolBorrowed = false;
                    state.fishMorningLoop = -1;
                    state.version = 5;
                }
                state.cityActions = state.cityActions ?? new System.Collections.Generic.List<string>();
                state.neighborTakenItems=state.neighborTakenItems??new System.Collections.Generic.List<string>();
                state.version = 7;
                if(string.IsNullOrEmpty(state.encounterDestination))state.encounterDestination="CityEncounter3D";
                return state;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Progression save could not be read; original file retained: " + ex.Message);
                return new ProgressionState();
            }
        }

        static bool Valid(float v) => v >= 0 && !float.IsInfinity(v) && !float.IsNaN(v);
        public static void Write(ProgressionState state, bool captureCheckpoint = true)
        {
            try
            {
                // Capture only the live state, never test fixtures or migration inputs.
                if (captureCheckpoint && Core.GameManager.Instance != null && object.ReferenceEquals(state, Core.GameManager.Instance.State) && TimeSystem.TimeManager.Instance != null)
                {
                    state.hasCheckpoint = true;
                    state.savedDay = TimeSystem.TimeManager.Instance.Days;
                    state.savedPeriod = (int)TimeSystem.TimeManager.Instance.Period;
                    state.savedVitality = Core.GameManager.Instance.Vitality;
                    state.savedFloor = DeathBySeventhsDawn.Interaction.RooftopAccess.Instance != null ? DeathBySeventhsDawn.Interaction.RooftopAccess.Instance.CurrentFloor : 4;
                    var actor = GameObject.FindWithTag("Player");
                    if(actor != null) { var p = actor.transform.position; state.savedX=p.x;state.savedY=p.y;state.savedZ=p.z;state.savedScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; }
                }
                if(captureCheckpoint&&Core.GameManager.Instance!=null&&object.ReferenceEquals(state,Core.GameManager.Instance.State)&&TimeSystem.TimeManager.Instance!=null)state.savedMinute=TimeSystem.TimeManager.Instance.GameMinute;
                string path = PathName, temp = path + ".tmp";
                Directory.CreateDirectory(StorageDirectory);
                File.WriteAllText(temp, JsonUtility.ToJson(state, true));
                if (File.Exists(path))
                    {
                    for(int attempt=0;;attempt++)
                    {
                        try{File.Replace(temp,path,path+".bak");break;}
                        catch(IOException) when(attempt<2){System.Threading.Thread.Sleep(5);}
                    }
                }
                else
                    File.Move(temp, path);
            }
            catch (Exception ex)
            {
                Debug.LogError("Progression save failed: " + ex.Message);
            }
        }
    }
}
