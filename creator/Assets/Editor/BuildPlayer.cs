using UnityEditor;
namespace Editor {          
     class MyEditorScript
     {
          static void PerformBuild ()
          {
               PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "CREATOR");
               BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
               buildPlayerOptions.scenes = new[] { "Assets/Main.unity" };
               buildPlayerOptions.locationPathName = "build";
               buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
               buildPlayerOptions.options = BuildOptions.None;
               BuildPipeline.BuildPlayer(buildPlayerOptions);
          }
     }
}
