using UnityEditor;
class MyEditorScript
{
     static void PerformBuild ()
     {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Main.unity" };
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "CREATOR");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
     }
}