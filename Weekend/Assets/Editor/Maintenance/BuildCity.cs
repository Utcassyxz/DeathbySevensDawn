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
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("City world").AddComponent<CityWorld>();
            string path="Assets/Apartment/"+name+".unity";
            EditorSceneManager.SaveScene(scene,path);
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes=scenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        Debug.Log("CITY_BUILD: four city scenes registered");
    }

    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            "../../outputs/DeathbySevensDawn/SevenDay.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("City player build failed: "+report.summary.result);
    }
}
