using System;
namespace DeathBySeventhsDawn.Story
{
    [Serializable] public sealed class CityText
    {
        public string[] days, crisis;
        public CityLine[] entries;
        public CityLine Find(string id) => Array.Find(entries, e => e.id == id);
    }
    [Serializable] public sealed class CityLine
    {
        public string id, title, body, success, locked;
    }
}
