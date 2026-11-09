using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.Interaction;
public static class BuildNeighborSlice
{
    public static void ValidateAll(){Run();VerifyRevision.RunNeighborAll();}
    public static void Validate(){Run();VerifyRevision.RunNeighbor();}
    public static void BuildPlayer()
    {BuildReview("DeathbySevensDawn Day2-3 Review","../../outputs/DeathbySevensDawn-Day2-3/SevenDay.exe");}
    public static void BuildStageOne()
    {
        int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;var mode=PlayerSettings.fullScreenMode;string version=PlayerSettings.bundleVersion;
        try{PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.bundleVersion="0.3.2";BuildReview("Death by Sevens Dawn First Stage","../../outputs/DeathbySevensDawn-StageOne-0.3.2/SevenDay.exe",BuildOptions.None);}
        finally{PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.bundleVersion=version;AssetDatabase.SaveAssets();}
    }
    public static void BuildRework()
    {BuildReview("DeathbySevensDawn Interaction Rework","../../outputs/DeathbySevensDawn-Rework/SevenDay.exe");}
    public static void BuildNov08()
    {BuildReview("DeathbySevensDawn Nov08 Review","../../outputs/DeathbySevensDawn-Nov08/SevenDay.exe");}
    static void BuildReview(string product,string destination,BuildOptions options=BuildOptions.Development)
    {
        string original=PlayerSettings.productName;
        try
        {
            PlayerSettings.productName=product;
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                destination,BuildTarget.StandaloneWindows64,options);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Day 2-3 build failed: "+report.summary.result);
        }
        finally{PlayerSettings.productName=original;AssetDatabase.SaveAssets();}
    }
    [MenuItem("Prototype/Build Day 2-3 slice scenes")]
    public static void Run()
    {
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/VenueSurface.mat")==null)
        {var surface=new Material(Shader.Find("Universal Render Pipeline/Lit"));surface.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(surface,"Assets/Resources/VenueSurface.mat");}
        if(AssetDatabase.LoadAssetAtPath<PerceptualFilterSettings>("Assets/Resources/PerceptualFilter.asset")==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PerceptualFilterSettings>(),"Assets/Resources/PerceptualFilter.asset");
        string previous=SceneManager.GetActiveScene().path;
        foreach(string name in new[]{"NeighborRoom2D","NeighborArena3D","BuildingService2D","SushiInterior3D","CircusInterior3D"})
        {
            string path="Assets/Apartment/"+name+".unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)
            {var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=new GameObject("Location runtime");if(name.EndsWith("Interior3D"))root.AddComponent<VenueRoom>();else root.AddComponent<NeighborRoom>();EditorSceneManager.SaveScene(scene,path);}
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        Debug.Log("NEIGHBOR_SLICE_BUILD_SUCCESS");
    }
}
