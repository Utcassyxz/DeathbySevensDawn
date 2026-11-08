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
    {
        string original=PlayerSettings.productName;
        try
        {
            PlayerSettings.productName="DeathbySevensDawn Day2-3 Review";
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                "../../outputs/DeathbySevensDawn-Day2-3/SevenDay.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Day 2-3 build failed: "+report.summary.result);
        }
        finally{PlayerSettings.productName=original;AssetDatabase.SaveAssets();}
    }
    [MenuItem("Prototype/Build Day 2-3 slice scenes")]
    public static void Run()
    {
        if(AssetDatabase.LoadAssetAtPath<PerceptualFilterSettings>("Assets/Resources/PerceptualFilter.asset")==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PerceptualFilterSettings>(),"Assets/Resources/PerceptualFilter.asset");
        string previous=SceneManager.GetActiveScene().path;
        foreach(string name in new[]{"NeighborRoom2D","NeighborArena3D","BuildingService2D"})
        {
            string path="Assets/Apartment/"+name+".unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)
            {var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Neighbor slice room").AddComponent<NeighborRoom>();EditorSceneManager.SaveScene(scene,path);}
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        Debug.Log("NEIGHBOR_SLICE_BUILD_SUCCESS");
    }
}
