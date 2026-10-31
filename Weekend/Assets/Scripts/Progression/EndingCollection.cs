// 职责：跨旅程保存结局收录；开新游戏不会清空收藏。
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using DeathBySeventhsDawn.Story;

namespace DeathBySeventhsDawn.Progression
{
    public static class EndingCollection
    {
        [Serializable]
        sealed class Album
        {
            public List<string> ids = new List<string>();
        }

        static Album album;
        public static readonly string[] Ids =
        {
            "world-end",
            "bad-ending-1"
        };
        public static string PathName => Path.Combine(Application.persistentDataPath, "seven-day-endings.json");
        public static string FreshlyUnlocked { get; private set; }

        static Album Data
        {
            get
            {
                if (album != null)
                    return album;
                try
                {
                    album = File.Exists(PathName) ? JsonUtility.FromJson<Album>(File.ReadAllText(PathName)) : new Album();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Ending collection could not be read: " + e.Message);
                }

                if (album == null)
                    album = new Album();
                if (album.ids == null)
                    album.ids = new List<string>();
                return album;
            }
        }

        public static bool Has(string id) => Data.ids.Contains(id);
        public static int Count
        {
            get
            {
                int n = 0;
                foreach (var id in Ids)
                    if (Has(id))
                        n++;
                return n;
            }
        }

        public static void Reload()
        {
            album = null;
            FreshlyUnlocked = null;
        }

        public static bool Unlock(string id, bool announce = true)
        {
            if (Array.IndexOf(Ids, id) < 0 || Has(id))
                return false;
            Data.ids.Add(id);
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(PathName))
                    File.Replace(temp, PathName, PathName + ".bak");
                else
                    File.Move(temp, PathName);
            }
            catch (Exception e)
            {
                Data.ids.Remove(id);
                Debug.LogError("Ending collection could not be saved: " + e.Message);
                return false;
            }

            if (announce)
                FreshlyUnlocked = id;
            return true;
        }

        public static string Title(string id) => id == "world-end" ? StoryText.Current.worldEnd.title : StoryText.Current.ending.title;
    }
}
