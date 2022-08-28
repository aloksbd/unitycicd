using System; 
using System.IO;
// using System.NullReferenceException ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Autodesk.Fbx;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
#endif
using UnityEngine.SceneManagement;

public class FBXUIController : MonoBehaviour
{
    private UIDocument m_UIDocument;
    private Button exportButton;
    private Label messageLabel;
    // private Button importButton;

    private List<UnityEngine.Object> gameObjectList = new List<UnityEngine.Object> ();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello");
        m_UIDocument = GetComponent<UIDocument>();

        VisualElement root = m_UIDocument.rootVisualElement;

        // buttonsWrapper = root.Q<VisualElement>("Buttons");

        exportButton = root.Q<Button>("export-button");
        messageLabel = root.Q<Label>("message-label");
        // importButton = root.Q<Button>("import-button");

        exportButton.clickable.clicked += OnExportButtonClicked;
        // importButton.clicked += OnImportButtonClicked;

    }

    void OnExportButtonClicked ()
    {
        try {
            // bool exportAll = true;
            Debug.Log("OnExportButtonClicked::: I am clicked");
            // string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");
            // ModelExporter.ExportObjects(filePath, objects);
            string filePath = "Test1MyGame.fbx";
            // var filePath = EditorUtility.SaveFilePanel(
            //     "Export Gameobject as FBX",
            //     "",
            //     fbxFileName + ".fbx",
            //     "fbx");  

            if (filePath.Length != 0) {
                using(FbxManager fbxManager = FbxManager.Create ()) {
                    // configure IO settings.
                    fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));
                    // FbxScene newScene = FbxScene.Create (fbxManager, "myNewScene");
                    Scene newScene = SceneManager.CreateScene("myNewScene");
                    GameObject duplicate = new GameObject();
                    foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject))) {
                        if (obj.transform.parent == null && (obj.name == "Game Map")) {
                            // Traverse(obj);
                            Debug.Log("obj::" + obj);
                            duplicate = Instantiate(obj);
                            break;
                        }
                    }
                        
                    SceneManager.MoveGameObjectToScene(duplicate, SceneManager.GetSceneByName("myNewScene"));   

                    // Export the scene
                    using (FbxExporter exporter = FbxExporter.Create (fbxManager, "myNewScene")) {

                        // int fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX binary (*.fbx)");

                        // Initialize the exporter.
                        bool status = exporter.Initialize (filePath, 0, fbxManager.GetIOSettings ());

                        // Create a new scene to export
                        FbxScene scene = FbxScene.Create (fbxManager, "myScene");

                        // Export the scene to the file.
                        exporter.Export (scene);
                        messageLabel.text = "Successfuly exported Game Map to " + filePath;
                    }
                }
            }    
        // } else {
        //     foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
        //         {
        //             if (obj.transform.parent == null && (obj.name == "Game Map"))
        //             {
        //                 Traverse(obj);
        //             }
        //         }
        //         UnityEngine.Object[] objects = gameObjectList.ToArray();
                
        //         // FbxExporter exporter = FbxExporter.Create();
        //         #if UNITY_EDITOR
        //         ModelExporter.ExportObjects(filePath, objects);
        //         #endif
        //         // FbxExporter.ModelExporter.ExportObjects(filePath, objects);
        //         messageLabel.text = "Successfuly exported Game Map to " + filePath;
        //     }
        // }
        } catch (NullReferenceException e) {
            Debug.Log("EE::" + e);
            messageLabel.text = "Some error occurred. Please contact the customer support.";
        }
    }

    // void OnExportButtonClicked () {
    //     try {
    //         Debug.Log("OnExportButtonClicked::: I am clicked");
    //         // string filePath = Path.Combine(Application.dataPath, "MyGame.fbx");
    //         // ModelExporter.ExportObjects(filePath, objects);
    //         string fbxFileName = "MyGame";
    //         var filePath = EditorUtility.SaveFilePanel(
    //             "Export Gameobject as FBX",
    //             "",
    //             fbxFileName + ".fbx",
    //             "fbx");  

    //         if (filePath.Length != 0)
    //         {
    //             foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
    //             {
    //                 if (obj.transform.parent == null && (obj.name == "Game Map"))
    //                 {
    //                     Traverse(obj);
    //                 }
    //             }
    //             UnityEngine.Object[] objects = gameObjectList.ToArray();
    //             ModelExporter.ExportObjects(filePath, objects);
    //             messageLabel.text = "Successfuly exported Game Map to " + filePath.Substring(filePath.LastIndexOf("/") +1);
    //         }
    //     } catch (NullReferenceException e) {
    //         Debug.Log("EE::" + e);
    //         messageLabel.text = "Some error occurred. Please contact the customer support.";
    //     }
    // }

    void Traverse(GameObject obj)
    {
        Debug.Log (obj.name);
        UnityEngine.Object testObj = obj;
        gameObjectList.Add(testObj);
        foreach (Transform child in obj.transform) {
            Traverse (child.gameObject);
        }
    
    }

    // void OnImportButtonClicked () {
    //     Debug.Log("OnImportButtonClicked::: I am clicked");
    //     Debug.Log("OnImportButtonClicked::: I am Hello");
    //     string fileName = "D:\\WhiteHat\\workspace\\gameclients\\creator\\Assets\\Room.fbx";
    //     using(FbxManager fbxManager = FbxManager.Create()){
    //         Debug.Log("OnImportButtonClicked::: I am here");
    //         // configure IO settings.
    //         fbxManager.SetIOSettings (FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

    //         // Import the scene to make sure file is valid
    //         using (FbxImporter importer = FbxImporter.Create(fbxManager, "myImporter")) {

    //            // Initialize the importer.
    //            bool status = importer.Initialize(fileName, -1, fbxManager.GetIOSettings ());

    //            Debug.Log("status::: " + status);

    //            // Create a new scene so it can be populated by the imported file.
    //            FbxScene scene = FbxScene.Create(fbxManager, "myMain1");

    //            Debug.Log("scene::: " + scene);

    //            // Import the contents of the file into the scene.
    //            importer.Import(scene);

    //            SceneManager.LoadScene("myMain1");
    //         }
    //     }
    // }
}
