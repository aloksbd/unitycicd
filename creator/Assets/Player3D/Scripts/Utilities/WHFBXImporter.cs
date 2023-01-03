using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Fbx;
using UnityEngine;
using Newtonsoft.Json;

class WHFbxImporter : System.IDisposable
{

    string pathName;

    /// <summary>
    /// Number of nodes imported including siblings and decendents
    /// </summary>
    public int NumNodes { private set; get; }

    private FbxSystemUnit UnitySystemUnit { get { return FbxSystemUnit.m; } }

    public static string structureName;

    public static string buildingName;

    /// <summary>
    /// Number of fbx imported 
    /// </summary>
    public static int NumFbx { private set; get; }

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

    static WHFbxImporter Create()
    {
        return new WHFbxImporter();
    }

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
    }

    public static int ImportObjects(string filePath)
    {
        using (var fbxImporter = Create())
        {
            buildingName = ObjectName.BUILDING + NumFbx.ToString();
            structureName = ObjectName.CREATOR_STRUCTURE + NumFbx.ToString();
            SceneObject.Create(SceneObject.Mode.Player, buildingName);
            SceneObject.Create(SceneObject.Mode.Player, structureName);
            NumFbx++;
            fbxImporter.pathName = filePath.Substring(0, filePath.LastIndexOf(WHConstants.PATH_DIVIDER));
            return fbxImporter.ImportAll(filePath);
        }
    }

    int ImportAll(string filePath)
    {

        using (var fbxManager = FbxManager.Create())
        {
            // configure IO settings.
            FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);

            // Configure the IO settings.
            fbxManager.SetIOSettings(fbxIOSettings);
            // fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));

            if (SceneObject.Find(SceneObject.Mode.Player, buildingName) != null)
            {
                UnityEngine.Object.Destroy(SceneObject.Find(SceneObject.Mode.Player, buildingName));
            }
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

        GameObject Structure = SceneObject.Find(SceneObject.Mode.Player, structureName);
        FbxNode StructureNode = fbxScene.GetRootNode().FindChild("Structure");

        ProcessNode(StructureNode != null ? StructureNode : fbxScene.GetRootNode(), Structure);

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
    public void ProcessNode(FbxNode fbxNode, GameObject unityParentObj = null)
    {
        string name = fbxNode.GetName();

        if (name.Contains("Clone")) return;

        GameObject unityGo;

        if (name == structureName)
        {
            unityGo = unityParentObj;
        }
        else
        {
            var tempGOName = name + unityParentObj != null ? ("_" + unityParentObj.name) : "";
            unityGo = SceneObject.Create(SceneObject.Mode.Player, tempGOName);
            unityGo.name = name;
        }

        NumNodes++;

        if (name != structureName && unityParentObj != null)
        {
            unityGo.transform.parent = unityParentObj.transform;
        }

        ProcessTransform(fbxNode, unityGo);

        if (name.Contains("Door") || name.Contains("Window") || name.Contains("Elevator"))
        {
            GameObject item;
            if (name.Contains("Door"))
            {
                item = PrefabFinder.Find("Door");
            }
            else if (name.Contains("Window"))
            {
                item = PrefabFinder.Find("Window");
            }
            else
            {
                item = PrefabFinder.Find("Elevator");
            }
            item.transform.parent = unityGo.transform;
            item.transform.localPosition = new Vector3(0, 0, 0);
            item.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            ProcessMesh(fbxNode, unityGo);
        }

        for (int i = 0; i < fbxNode.GetChildCount(); ++i)
        {
            ProcessNode(fbxNode.GetChild(i), unityGo);
        }
    }

    /// <summary>
    /// Process transformation data and setup Transform component
    /// </summary>
    private void ProcessTransform(FbxNode fbxNode, GameObject unityGo)
    {
        // Construct rotation matrices
        FbxVector4 fbxRotation = new FbxVector4(fbxNode.LclRotation.Get());
        FbxAMatrix fbxRotationM = new FbxAMatrix();
        fbxRotationM.SetR(fbxRotation);

        FbxVector4 fbxPreRotation = new FbxVector4(fbxNode.GetPreRotation(FbxNode.EPivotSet.eSourcePivot));
        FbxAMatrix fbxPreRotationM = new FbxAMatrix();
        fbxPreRotationM.SetR(fbxPreRotation);

        FbxVector4 fbxPostRotation = new FbxVector4(fbxNode.GetPostRotation(FbxNode.EPivotSet.eSourcePivot));
        FbxAMatrix fbxPostRotationM = new FbxAMatrix();
        fbxPostRotationM.SetR(fbxPostRotation);

        // Construct translation matrix
        FbxAMatrix fbxTranslationM = new FbxAMatrix();
        FbxVector4 fbxTranslation = new FbxVector4(fbxNode.LclTranslation.Get());
        fbxTranslationM.SetT(fbxTranslation);

        // Construct scaling matrix
        FbxAMatrix fbxScalingM = new FbxAMatrix();
        FbxVector4 fbxScaling = new FbxVector4(fbxNode.LclScaling.Get());
        fbxScalingM.SetS(fbxScaling);

        // Construct offset and pivot matrices
        FbxAMatrix fbxRotationOffsetM = new FbxAMatrix();
        FbxVector4 fbxRotationOffset = fbxNode.GetRotationOffset(FbxNode.EPivotSet.eSourcePivot);
        fbxRotationOffsetM.SetT(fbxRotationOffset);

        FbxAMatrix fbxRotationPivotM = new FbxAMatrix();
        FbxVector4 fbxRotationPivot = fbxNode.GetRotationPivot(FbxNode.EPivotSet.eSourcePivot);
        fbxRotationPivotM.SetT(fbxRotationPivot);

        FbxAMatrix fbxScalingOffsetM = new FbxAMatrix();
        FbxVector4 fbxScalingOffset = fbxNode.GetScalingOffset(FbxNode.EPivotSet.eSourcePivot);
        fbxScalingOffsetM.SetT(fbxScalingOffset);

        FbxAMatrix fbxScalingPivotM = new FbxAMatrix();
        FbxVector4 fbxScalingPivot = fbxNode.GetScalingPivot(FbxNode.EPivotSet.eSourcePivot);
        fbxScalingPivotM.SetT(fbxScalingPivot);

        FbxAMatrix fbxTransform =
            fbxTranslationM *
            fbxRotationOffsetM *
            fbxRotationPivotM *
            fbxPreRotationM *
            fbxRotationM *
            fbxPostRotationM *
            fbxRotationPivotM.Inverse() *
            fbxScalingOffsetM *
            fbxScalingPivotM *
            fbxScalingM *
            fbxScalingPivotM.Inverse();

        FbxVector4 lclTrs = fbxTransform.GetT();
        FbxQuaternion lclRot = fbxTransform.GetQ();
        FbxVector4 lclScl = fbxTransform.GetS();

        Debug.Log(string.Format("processing {3} Lcl : T({0}) R({1}) S({2})",
                                 lclTrs.ToString(),
                                 lclRot.ToString(),
                                 lclScl.ToString(),
                                 fbxNode.GetName()));

        unityGo.transform.localPosition = new Vector3(-(float)lclTrs[0], (float)lclTrs[1], (float)lclTrs[2]);
        unityGo.transform.localRotation = new Quaternion((float)lclRot[0], -(float)lclRot[1], -(float)lclRot[2], (float)lclRot[3]);
        unityGo.transform.localScale = new Vector3((float)lclScl[0], (float)lclScl[1], (float)lclScl[2]);

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

            unityVertices.Add(new Vector3(-(float)fbxVector4.X, (float)fbxVector4.Y, (float)fbxVector4.Z));
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

        ProcessUVs(fbxMesh, unityMesh);

        var unityMeshFilter = unityGo.GetComponent<MeshFilter>();
        if (unityMeshFilter == null)
        {
            unityMeshFilter = unityGo.AddComponent<MeshFilter>();
        }
        unityMeshFilter.sharedMesh = unityMesh;

        var unityRenderer = unityGo.GetComponent<MeshRenderer>();

        if (unityRenderer == null)
        {
            unityRenderer = unityGo.AddComponent<MeshRenderer>();
        }

        int j = 0;
        bool isMaterialExist = true;
        List<Material> multipleMaterials = new List<Material>();
        do
        {
            FbxSurfaceMaterial materialTemp = fbxNode.GetMaterial(j);
            if (materialTemp != null)
            {
                var unityMaterial = new Material(Shader.Find("Standard"));
                ProcessMaterial(materialTemp, unityMaterial);
                multipleMaterials.Add(unityMaterial);
                j++;
            }
            else
            {
                isMaterialExist = false;
            }

        } while (isMaterialExist);
        unityRenderer.materials = multipleMaterials.ToArray();
        unityRenderer.sharedMaterials = multipleMaterials.ToArray();

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
            var filePath = pathName + WHConstants.PATH_DIVIDER + "Textures" + WHConstants.PATH_DIVIDER + filePathProperty.GetString();
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
                // unityGo.AddComponent<MeshCollider>();
                break;
        }
    }
}
