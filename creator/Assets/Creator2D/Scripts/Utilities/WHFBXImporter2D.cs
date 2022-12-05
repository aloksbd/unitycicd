using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Fbx;
using UnityEngine;
class WHFbxImporter2D : System.IDisposable
{

    string parentGameObjectName = "Structure";
    public string pathName;

    /// <summary>
    /// Number of nodes imported including siblings and decendents
    /// </summary>
    public int NumNodes { private set; get; }

    private FbxSystemUnit UnitySystemUnit { get { return FbxSystemUnit.m; } }

    private FbxAxisSystem UnityAxisSystem
    {
        get
        {
            return new FbxAxisSystem(FbxAxisSystem.EUpVector.eYAxis,
                                        FbxAxisSystem.EFrontVector.eParityOdd,
                                        FbxAxisSystem.ECoordSystem.eLeftHanded);
        }
    }

    private static string AxisSystemToString(FbxAxisSystem fbxAxisSystem)
    {
        return string.Format("[{0}, {1}, {2}]",
                              fbxAxisSystem.GetUpVector().ToString(),
                              fbxAxisSystem.GetFrontVector().ToString(),
                              fbxAxisSystem.GetCoorSystem().ToString());
    }

    static WHFbxImporter2D Create()
    {
        return new WHFbxImporter2D();
    }

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
    }

    public static int ImportObjects(string filePath)
    {
        using (var fbxImporter = Create())
        {
            fbxImporter.pathName = filePath.Substring(0, filePath.LastIndexOf("\\"));
            return fbxImporter.ImportAll(filePath);
        }
    }

    int ImportAll(string filePath)
    {
        CreatorItem building = NewBuildingController.GetBuilding();
        if (building != null)
        {
            building.Destroy();
        }
        using (var fbxManager = FbxManager.Create())
        {
            // configure IO settings.
            FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);

            // Configure the IO settings.
            fbxManager.SetIOSettings(fbxIOSettings);
            // fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));

            // Import the scene to make sure file is valid
            using (FbxImporter fbxImporter = FbxImporter.Create(fbxManager, "myImporter"))
            {

                // Initialize the importer.
                bool status = fbxImporter.Initialize(filePath, 0, fbxManager.GetIOSettings());

                if (!status)
                    return 0;

                // Import options. Determine what kind of data is to be imported.
                // The default is true, but here we set the options explictly.
                fbxIOSettings.SetBoolProp(Globals.IMP_FBX_MATERIAL, false);
                fbxIOSettings.SetBoolProp(Globals.IMP_FBX_TEXTURE, false);
                fbxIOSettings.SetBoolProp(Globals.IMP_FBX_ANIMATION, false);
                fbxIOSettings.SetBoolProp(Globals.IMP_FBX_EXTRACT_EMBEDDED_DATA, false);
                fbxIOSettings.SetBoolProp(Globals.IMP_FBX_GLOBAL_SETTINGS, true);

                // Create a new scene so it can be populated by the imported file.
                FbxScene fbxScene = FbxScene.Create(fbxManager, "myScene");

                // Import the contents of the file into the scene.
                status = fbxImporter.Import(fbxScene);

                if (!status)
                {
                    Debug.LogError(string.Format("failed to import file ({0})",
                    fbxImporter.GetStatus().GetErrorString()));
                }
                else
                {
                    // import data into scene 
                    ProcessScene(fbxScene);
                }

                // cleanup
                fbxScene.Destroy();
                // fbxImporter.Destroy();
                return status == true ? NumNodes : 0;
            }
        }
    }

    /// <summary>
    /// Process fbxScene. If system units don't match then bake the convertion into the position 
    /// and vertices of objects.
    /// </summary>
    public void ProcessScene(FbxScene fbxScene)
    {
        Debug.Log(string.Format("Scene name: {0}", fbxScene.GetName()));

        var fbxSettings = fbxScene.GetGlobalSettings();
        FbxSystemUnit fbxSystemUnit = fbxSettings.GetSystemUnit();

        if (fbxSystemUnit != UnitySystemUnit)
        {
            Debug.Log(string.Format("converting system unit to match Unity. Expected {0}, found {1}",
                                     UnitySystemUnit, fbxSystemUnit));

            ConvertScene(fbxScene, UnitySystemUnit);
        }
        else
        {
            Debug.Log(string.Format("file system units {0}", fbxSystemUnit));
        }

        // The Unity axis system has Y up, Z forward, X to the right (left-handed).
        FbxAxisSystem fbxAxisSystem = fbxSettings.GetAxisSystem();

        if (fbxAxisSystem != UnityAxisSystem)
        {
            Debug.LogWarning(string.Format("file axis system do not match Unity, Expected {0} found {1}",
                                             AxisSystemToString(UnityAxisSystem),
                                             AxisSystemToString(fbxAxisSystem)));
        }

        GameObject Structure = SceneObject.Find(SceneObject.Mode.Creator, parentGameObjectName);
        FbxNode StructureNode = fbxScene.GetRootNode().FindChild(parentGameObjectName);

        ProcessNode(StructureNode != null ? StructureNode : fbxScene.GetRootNode(), null);

        return;
    }

    /// <summary>
    /// Convert scene's system units but leave scaling unchanged
    /// </summary>
    public void ConvertScene(FbxScene fbxScene, FbxSystemUnit toUnits)
    {
        // Get scale factor.
        double scaleFactor = (float)fbxScene.GetGlobalSettings().GetSystemUnit().GetConversionFactorTo(toUnits);

        if (scaleFactor.Equals(1.0f))
            return;

        // Get root node.
        FbxNode fbxRootNode = fbxScene.GetRootNode();

        // For all the nodes to convert the translations
        Queue<FbxNode> fbxNodes = new Queue<FbxNode>();

        fbxNodes.Enqueue(fbxRootNode);

        while (fbxNodes.Count > 0)
        {
            FbxNode fbxNode = fbxNodes.Dequeue();

            // Convert node's translation.
            FbxDouble3 lclTrs = fbxNode.LclTranslation.Get();

            lclTrs.X *= scaleFactor;
            lclTrs.Y *= scaleFactor;
            lclTrs.Z *= scaleFactor;

            fbxNode.LclTranslation.Set(lclTrs);

            FbxMesh fbxMesh = fbxNode.GetMesh();

            if (fbxMesh != null)
            {
                for (int i = 0; i < fbxMesh.GetControlPointsCount(); ++i)
                {
                    FbxVector4 fbxVector4 = fbxMesh.GetControlPointAt(i);

                    fbxVector4 *= scaleFactor;

                    fbxMesh.SetControlPointAt(fbxVector4, i);
                }
            }

            for (int i = 0; i < fbxNode.GetChildCount(); ++i)
            {
                fbxNodes.Enqueue(fbxNode.GetChild(i));
            }
        }
    }

    /// <summary>
    /// Process fbxNode, configure the transform and construct mesh
    /// </summary>
    public void ProcessNode(FbxNode fbxNode, CreatorItem parentItem)
    {
        string name = fbxNode.GetName();
        Debug.Log("Node Name::: " + name);

        GameObject unityGo = new GameObject(name);

        ProcessMesh(fbxNode, unityGo);
        // NumNodes++;
        CreatorItem currentItem = null;

        if (name.Contains("FloorPlan"))
        {
            currentItem = new CreatorFloorPlanFactory().Create(name);
            // NewBuildingController.CreateFloorPlan();
        }
        else if (name.Contains("Floor"))
        {
            currentItem = new CreatorFloorFactory().Create(name);
            // NewBuildingController.CreateFloor();
        }
        else if (name.Contains("Wall"))
        {
            currentItem = new CreatorWallFactory(new Vector3(0, 0, -0.2f), new Vector3(unityGo.GetComponent<Renderer>().bounds.size.x
            , unityGo.GetComponent<Renderer>().bounds.size.y, -0.2f)).Create(name);
        }
        else if (name.Contains(ObjectName.CREATOR_BUILDING))
        {
            currentItem = new CreatorBuildingFactory().Create();
            NewBuildingController.SetBuilding(currentItem);
        }
        if (parentItem != null && currentItem != null)
        {
            currentItem.gameObject.transform.position = unityGo.transform.position;
            parentItem.AddChild(currentItem);
        }


        for (int i = 0; i < fbxNode.GetChildCount(); ++i)
        {
            ProcessNode(fbxNode.GetChild(i), currentItem);
        }
        UnityEngine.Object.Destroy(unityGo);
    }

    /// <summary>
    /// Process mesh data and setup MeshFilter component
    /// </summary>
    private void ProcessMesh(FbxNode fbxNode, GameObject unityGo)
    {
        FbxMesh fbxMesh = fbxNode.GetMesh();


        if (fbxMesh == null) return;

        var unityMesh = new Mesh();

        // create mesh
        var unityVertices = new List<Vector3>();
        var unityTriangleIndices = new List<int>();

        // transfer vertices
        for (int i = 0; i < fbxMesh.GetControlPointsCount(); ++i)
        {
            FbxVector4 fbxVector4 = fbxMesh.GetControlPointAt(i);

            Debug.Assert(fbxVector4.X <= float.MaxValue && fbxVector4.X >= float.MinValue);
            Debug.Assert(fbxVector4.Y <= float.MaxValue && fbxVector4.Y >= float.MinValue);
            Debug.Assert(fbxVector4.Z <= float.MaxValue && fbxVector4.Z >= float.MinValue);

            unityVertices.Add(new Vector3((float)fbxVector4.X, (float)fbxVector4.Z, (float)fbxVector4.Y));
        }

        // transfer triangles
        for (int polyIndex = 0; polyIndex < fbxMesh.GetPolygonCount(); ++polyIndex)
        {
            int polySize = fbxMesh.GetPolygonSize(polyIndex);
            // only support triangles
            Debug.Assert(polySize == 3);

            for (int polyVertexIndex = 0; polyVertexIndex < polySize; ++polyVertexIndex)
            {
                int vertexIndex = fbxMesh.GetPolygonVertex(polyIndex, polyVertexIndex);

                unityTriangleIndices.Add(vertexIndex);
            }
        }

        unityMesh.vertices = unityVertices.ToArray();

        // TODO: 
        // - support Mesh.SetTriangles - multiple materials per mesh
        // - support Mesh.SetIndices - other topologies e.g. quads
        unityMesh.triangles = unityTriangleIndices.ToArray();
        unityMesh.RecalculateNormals();

        // ProcessUVs(fbxMesh, unityMesh);

        var unityMeshFilter = unityGo.AddComponent<MeshFilter>();
        unityMeshFilter.sharedMesh = unityMesh;

        var unityRenderer = unityGo.AddComponent<MeshRenderer>();

        int j = 0;
        bool isMaterialExist = true;
        List<Material> multipleMaterials = new List<Material>();
        //assign it
        // go.GetComponent<Renderer>().materials = yourMaterials ;
        do
        {
            FbxSurfaceMaterial materialTemp = fbxNode.GetMaterial(j);
            if (materialTemp != null)
            {
                var unityMaterial = ProcessMaterial(materialTemp, unityRenderer.material);
                multipleMaterials.Add(unityMaterial);
                // unityRenderer.sharedMaterial = unityMaterial;
                j++;
            }
            else
            {
                isMaterialExist = false;
            }

        } while (isMaterialExist);
        unityRenderer.materials = multipleMaterials.ToArray();

        ProcessCollider(fbxNode, unityGo);
    }

    /// <summary>
    /// Process UV data and configure the Mesh's UV attributes
    /// </summary>
    private void ProcessUVs(FbxMesh fbxMesh, Mesh unityMesh)
    {

        // First just try importing diffuse UVs from separate layers
        // (Maya exports that way)
        FbxLayerElementUV fbxFirstUVSet = null;
        FbxLayer fbxFirstUVLayer = null;

        // NOTE: assuming triangles
        int polygonIndexCount = fbxMesh.GetPolygonVertexCount();
        int vertexCount = fbxMesh.GetControlPointsCount();

        int[] polygonVertexIndices = new int[polygonIndexCount];

        int j = 0;

        for (int polyIndex = 0; polyIndex < fbxMesh.GetPolygonCount(); ++polyIndex)
        {
            for (int positionInPolygon = 0; positionInPolygon < fbxMesh.GetPolygonSize(polyIndex); ++positionInPolygon)
            {
                polygonVertexIndices[j++] = fbxMesh.GetPolygonVertex(polyIndex, positionInPolygon);
            }
        }

        for (int i = 0; i < fbxMesh.GetLayerCount(); i++)
        {
            FbxLayer fbxLayer = fbxMesh.GetLayer(i);
            if (fbxLayer == null)
                continue;

            FbxLayerElementUV fbxUVSet = fbxLayer.GetUVs();

            if (fbxUVSet == null)
                continue;

            if (fbxFirstUVSet != null)
            {
                fbxFirstUVSet = fbxUVSet;
                fbxFirstUVLayer = fbxLayer;
            }

            unityMesh.uv = ProcessUVSet(fbxUVSet, polygonVertexIndices, vertexCount);
        }
    }

    /// <summary>
    /// Process a single UV dataset and return data for configuring a Mesh UV attribute
    /// </summary>
    private Vector2[] ProcessUVSet(FbxLayerElementUV element,
                                    int[] polygonVertexIndices,
                                    int vertexCount)
    {
        Vector2[] result = new Vector2[vertexCount];

        FbxLayerElement.EReferenceMode referenceMode = element.GetReferenceMode();
        FbxLayerElement.EMappingMode mappingMode = element.GetMappingMode();

        // direct or via-index
        bool isDirect = referenceMode == FbxLayerElement.EReferenceMode.eIndexToDirect;

        var fbxElementArray = element.GetDirectArray();
        var fbxIndexArray = element.GetIndexArray();
        // var fbxIndexArray = isDirect ? null : element.GetIndexArray ();
        if (mappingMode == FbxLayerElement.EMappingMode.eByPolygonVertex)
        {
            Debug.Log("vertexCount::: " + vertexCount);
            if (fbxElementArray.GetCount() != vertexCount)
            {
                Debug.LogError(string.Format("UVSet size ({0}) does not match vertex count {1}",
                                               fbxElementArray.GetCount(), vertexCount));
                return null;
            }

            for (int i = 0; i < fbxElementArray.GetCount(); i++)
            {
                int index = i;
                if (!isDirect)
                {
                    index = fbxIndexArray.GetAt(i);
                }

                FbxVector2 fbxVector2 = fbxElementArray.GetAt(index);
                Debug.Assert(fbxVector2.X >= float.MinValue && fbxVector2.X <= float.MaxValue);
                Debug.Assert(fbxVector2.Y >= float.MinValue && fbxVector2.Y <= float.MaxValue);

                result[i] = new Vector2((float)fbxVector2.X, (float)fbxVector2.Y);

                // UVs in FBX can contain NaNs, so we set these vertices to (0,0)
                if (float.IsNaN(result[i][0]) || float.IsNaN(result[i][1]))
                {
                    Debug.LogWarning(string.Format("invalid UV detected at {0}", i));
                    result[i] = Vector2.zero;
                }
            }

        }
        else
        {
            Debug.LogError("unsupported UV-to-Component mapping mode");
        }
        return result;
    }

    private Material ProcessMaterial(FbxSurfaceMaterial fbxMaterial, Material unityMaterial)
    {
        unityMaterial.name = fbxMaterial.GetName();
        unityMaterial.SetColor("_Color", getFBXColor(fbxMaterial, FbxSurfaceMaterial.sDiffuse));
        unityMaterial.SetColor("_EmissionColor", getFBXColor(fbxMaterial, FbxSurfaceMaterial.sEmissive));

        if (fbxMaterial.FindProperty(FbxSurfaceMaterial.sBumpFactor) != null)
        {
            unityMaterial.SetFloat("_BumpScale", fbxMaterial.FindProperty(FbxSurfaceMaterial.sBumpFactor).GetFloat());
        }

        if (fbxMaterial.FindProperty(FbxSurfaceMaterial.sSpecular) != null)
        {
            unityMaterial.SetColor("_SpecColor", getFBXColor(fbxMaterial, FbxSurfaceMaterial.sSpecular));
        }

        // Import the textures from FBX materials to Unity.
        ProcessTexture(fbxMaterial, FbxSurfaceMaterial.sDiffuse, unityMaterial, "_MainTex");
        // ProcessTexture(fbxMaterial, "emissive", unityMaterial, "_EmissionMap");
        // ProcessTexture(fbxMaterial, FbxSurfaceMaterial.sNormalMap, unityMaterial, "_BumpMap");

        if (fbxMaterial.FindProperty(FbxSurfaceMaterial.sSpecular) != null)
        {
            ProcessTexture(fbxMaterial, FbxSurfaceMaterial.sSpecular, unityMaterial, "_SpecGlosMap");
        }

        return unityMaterial;

    }

    private Color getFBXColor(FbxSurfaceMaterial fbxMaterial, string fbxPropName)
    {
        if (fbxMaterial.FindProperty(fbxPropName) == null) { return new Color(0.5f, 0.5f, 0.5f); }
        var fbxColor = fbxMaterial.FindProperty(fbxPropName).GetFbxColor();
        return new Color((float)fbxColor.mRed, (float)fbxColor.mGreen, (float)fbxColor.mBlue, (float)fbxColor.mAlpha);
    }

    private void ProcessTexture(FbxSurfaceMaterial fbxMaterial, string fbxPropName, Material unityMaterial, string unityPropName)
    {
        var fbxMaterialProperty = fbxMaterial.FindProperty(fbxPropName);
        if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid()) { Debug.Log("property not found"); return; }

        // eFbxReference

        int numTextures = fbxMaterialProperty.GetSrcObjectCount();

        for (int j = 0; j < numTextures; ++j)
        {
            var tempTexture = fbxMaterialProperty.GetSrcObject(j);
            if (tempTexture != null)
            {
                Texture2D texture = ReadTexture(tempTexture.GetSrcObject(j));
                if (texture != null)
                {
                    unityMaterial.SetTexture(unityPropName, texture);
                }
            }
        }

    }

    Texture2D ReadTexture(FbxObject textureObject)
    {
        Texture2D tex = null;
        byte[] fileData;

        var filePathProperty = textureObject.FindProperty("Path");

        if (filePathProperty != null && filePathProperty.IsValid())
        {
            var filePath = pathName + "\\Textures\\" + filePathProperty.GetString();
            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
        }

        return tex;
    }

    void ProcessCollider(FbxNode fbxNode, GameObject unityGo)
    {
        var fbxColliderProperty = fbxNode.FindProperty("ColliderObjectType");
        // if (fbxColliderProperty == null || !fbxColliderProperty.IsValid()) { Debug.Log("property not found"); return; }
        // if (fbxColliderProperty && fbxColliderProperty.IsValid()) {

        // }
        switch (fbxColliderProperty.GetString())
        {
            case "SphereCollider":
                unityGo.AddComponent<SphereCollider>();
                break;
            case "BoxCollider":
                unityGo.AddComponent<BoxCollider>();
                break;
            case "MeshCollider":
                unityGo.AddComponent<MeshCollider>();
                break;
            case "CapsuleCollider":
                unityGo.AddComponent<CapsuleCollider>();
                break;
            default:
                unityGo.AddComponent<MeshCollider>();
                break;
        }
    }
}
