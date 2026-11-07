using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.City;

public static class BuildCity
{
    public static void Validate(){ Run(); VerifyRevision.RunCity(); }
    [MenuItem("Prototype/Build city greybox scenes")]
    public static void Run()
    {
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CityGreybox.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")),"Assets/Resources/CityGreybox.mat");
        var previous=SceneManager.GetActiveScene().path;
        foreach(var name in new[]{"CityStreet","CityService","CityBroadcast","CityObservatory","CityEncounter3D"})
        {
            string path="Assets/Apartment/"+name+".unity";
            // Keep authored scenes and their object identifiers when this command is run again.
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("City world").AddComponent<CityWorld>();
                EditorSceneManager.SaveScene(scene,path);
            }
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes=scenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        Debug.Log("CITY_BUILD: four 2D regions and one 3D encounter registered");
    }

    public static void BuildPlayer()
    {
        string previousProduct=PlayerSettings.productName;
        try
        {
            // A standalone review build must not migrate saves used by an older open editor.
            PlayerSettings.productName="DeathbySevensDawn City Prototype";
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                "../../outputs/DeathbySevensDawn/SevenDay.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("City player build failed: "+report.summary.result);
        }
        finally {PlayerSettings.productName=previousProduct;AssetDatabase.SaveAssets();}
    }
}
