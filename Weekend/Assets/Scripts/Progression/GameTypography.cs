using UnityEngine;
using DeathBySeventhsDawn.Story;
namespace DeathBySeventhsDawn.Progression
{
    public static class GameTypography
    {
        static Font chinese,english;
        public static Font Chinese=>chinese!=null?chinese:(chinese=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimSun","Noto Serif CJK SC"},28));
        public static Font English=>english!=null?english:(english=Font.CreateDynamicFontFromOSFont(new[]{"Times New Roman","Georgia","Liberation Serif"},28));
        public static Font Current=>StoryText.English?English:Chinese;
        public static void Use(params GUIStyle[] styles){foreach(var style in styles)if(style!=null)style.font=Current;}
    }
}
