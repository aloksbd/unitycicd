using UnityEditor;
     namespace Editor {
          
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
}
