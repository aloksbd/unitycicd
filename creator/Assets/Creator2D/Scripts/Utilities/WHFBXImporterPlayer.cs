using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Fbx;
using UnityEngine;
class WHFbxImporterPlayer : System.IDisposable
{

    string parentGameObjectName = "Structure";
    public string pathName;

    private CreatorItem buildingItem;

    private List<Vector3> _floorBoundary = new List<Vector3>();

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

    private string AxisSystemToString(FbxAxisSystem fbxAxisSystem)
    {
        return string.Format("[{0}, {1}, {2}]",
                              fbxAxisSystem.GetUpVector().ToString(),
                              fbxAxisSystem.GetFrontVector().ToString(),
                              fbxAxisSystem.GetCoorSystem().ToString());
    }

    // static WHFbxImporterPlayer Create()
    // {
    //     return new WHFbxImporterPlayer();
    // }

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
    }

    public CreatorItem ImportObjects(string filePath, List<Vector3> floorBoundary)
    {
        pathName = filePath.Substring(0, filePath.LastIndexOf(WHConstants.PATH_DIVIDER));
        _floorBoundary = floorBoundary;
        // return fbxImporter.ImportAll(filePath);
        if (ImportAll(filePath) > 0)
        {
            return buildingItem;
        }
        else
        {
            return null;
        }
    }

    // public static CreatorItem ImportObjects(string filePath, List<Vector3> floorBoundary)
    // {
    //     using (var fbxImporter = Create())
    //     {
    //         fbxImporter.pathName = filePath.Substring(0, filePath.LastIndexOf("\\"));
    //         _floorBoundary = floorBoundary;
    //         // return fbxImporter.ImportAll(filePath);
    //         if (fbxImporter.ImportAll(filePath) > 0)
    //         {
    //             return buildingItem;
    //         }
    //         else
    //         {
    //             return null;
    //         }
    //     }
    // }

    int ImportAll(string filePath)
    {
        // CreatorItem building = NewBuildingController.GetBuilding();
        // if (building != null)
        // {
        //     building.Destroy();
        // }
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
                    return 0;
                }
                else
                {
                    // import data into scene 
                    ProcessScene(fbxScene);
                }

                // cleanup
                fbxScene.Destroy();
                // fbxImporter.Destroy();
                return status == true ? 1 : 0;
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

        // GameObject Structure = SceneObject.Find(SceneObject.Mode.Creator, parentGameObjectName);
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
    /// 
    Vector3 goPos = new Vector3();
    Quaternion goRot = new Quaternion(0, 0, 0, 1);
    CreatorItem roofItem = null;
    public void ProcessNode(FbxNode fbxNode, CreatorItem parentItem)
    {
        string name = fbxNode.GetName();

        if (name.Contains("Clone")) return;

        ProcessTransform(fbxNode);
        CreatorItem currentItem = null;

        if (name.Contains(WHConstants.ROOF))
        {
            currentItem = new CreatorRoofFactory().Create(WHConstants.ROOF, false);
            parentItem.AddChild(currentItem);
            roofItem = currentItem;
            roofItem.SetPosition(new Vector3(0, 0, 0));
        }
        else if (name.Contains(WHConstants.FLOOR_PLAN))
        {
            if (roofItem == null)
            {
                roofItem = new CreatorRoofFactory().Create(WHConstants.ROOF, false);
                parentItem.AddChild(roofItem);
                roofItem.SetPosition(new Vector3(0, 0, 0));

                var floorItem = new CreatorFloorFactory().Create(WHConstants.FLOOR + "001", false);
                roofItem.AddChild(floorItem);
                floorItem.GetComponent<NewIHasBoundary>().SetBoundary(_floorBoundary);
            }
            currentItem = new CreatorFloorPlanFactory().Create(name, false);
            var childrenCount = parentItem.children.Count;
            CreatorItem floorPlan = null;
            if (childrenCount > 0)
            {
                floorPlan = parentItem.children[childrenCount - 1];
            }
            parentItem.AddChild(currentItem);

            float posZ = 0;
            if (floorPlan != null)
            {
                var z = floorPlan.GetComponent<NewIHasPosition>().Position.z;
                var floorHeight = floorPlan.GetComponent<NewIHasDimension>().Dimension.Height;
                posZ = z + floorHeight;
            }

            currentItem.GetComponent<NewIHasPosition>().SetPosition(new Vector3(0, 0, posZ));

            var floorPlanHeight = WHConstants.DefaultWallHeight;
            var heightProperty = fbxNode.FindProperty("FLOORPLAN_HEIGHT");
            if (heightProperty != null && heightProperty.IsValid())
            {
                floorPlanHeight = heightProperty.GetFloat();
            }

            var _dimension = new Dimension(0, floorPlanHeight, 0);
            currentItem.GetComponent<NewIHasDimension>().SetDimension(_dimension.Length, _dimension.Height, _dimension.Width);

            var roofPosition = roofItem.Position;
            roofItem.SetPosition(new Vector3(0, 0, roofPosition.z + floorPlanHeight));
        }
        else if (name.Contains(WHConstants.FLOOR))
        {
            currentItem = new CreatorFloorFactory().Create(name, false);
            parentItem.AddChild(currentItem);
            currentItem.GetComponent<NewIHasBoundary>().SetBoundary(_floorBoundary);
        }
        else if (name.Contains(WHConstants.CEILING))
        {
            float height = WHConstants.DefaultFloorHeight;
            if (parentItem.GetComponent<NewIHasDimension>() != null)
            {
                height = parentItem.GetComponent<NewIHasDimension>().Dimension.Height;
            }
            currentItem = new CreatorCeilingFactory().Create(name, false);
            parentItem.AddChild(currentItem);
            currentItem.GetComponent<NewIHasPosition>().SetPosition(new Vector3(0, 0, height));
            currentItem.GetComponent<NewIHasBoundary>().SetBoundary(_floorBoundary);
        }
        else if (name.Contains(WHConstants.WALL))
        {
            var pos = goPos;
            var endX = pos.x;
            var endY = pos.z;
            var LOCALBOUND_X_Property = fbxNode.FindProperty("LOCALBOUND_X");
            if (LOCALBOUND_X_Property != null)
            {
                endX = pos.x + ((float)LOCALBOUND_X_Property.GetFloat()) * MathF.Cos((-goRot.eulerAngles.y * (MathF.PI)) / 180.0F);
                endY = pos.z + ((float)LOCALBOUND_X_Property.GetFloat()) * MathF.Sin((-goRot.eulerAngles.y * (MathF.PI)) / 180.0F);
            }
            var isExterior = false;
            var EXTERIOR_Property = fbxNode.FindProperty("EXTERIOR");
            if (EXTERIOR_Property != null)
            {
                isExterior = (int)EXTERIOR_Property.GetInt() == 1 ? true : false;
            }
            currentItem = new CreatorWallFactory(new Vector3(pos.x, pos.z, WHConstants.DefaultZ), new Vector3(endX, endY, WHConstants.DefaultZ), isExterior).Create(name, false);
            parentItem.AddChild(currentItem);
        }
        else if (name.Contains(WHConstants.DOOR) || name.Contains(WHConstants.WINDOW))
        {
            var pos = goPos;
            var parentPos = parentItem.GetComponent<NewIHasPosition>().Position;
            Sprite sprite = name.Contains(WHConstants.DOOR) ? Resources.Load<Sprite>("Sprites/Door") : Resources.Load<Sprite>("Sprites/Window");
            currentItem = name.Contains(WHConstants.DOOR) ? new CreatorDoorFactory(parentItem, new Vector3(pos.x + parentPos.x, parentPos.y, WHConstants.DefaultZ), sprite).Create(name, false) : new CreatorWindowFactory(parentItem, new Vector3(pos.x + parentPos.x, parentPos.y, WHConstants.DefaultZ), sprite).Create(name, false);
            parentItem.AddChild(currentItem);
            // currentItem.gameObject.transform.localPosition = new Vector3(goPos.x, 0.0f, -0.2f);
        }
        else if (name.Contains(WHConstants.ELEVATOR))
        {
            var pos = goPos;
            Sprite sprite = Resources.Load<Sprite>("Sprites/Elevator");
            var height = WHConstants.DefaultElevatorLength;
            if (parentItem.GetComponent<NewIHasDimension>() != null)
            {
                height = parentItem.GetComponent<NewIHasDimension>().Dimension.Height;
            }
            currentItem = new CreatorElevatorFactory(new Vector3(pos.x, pos.z, -0.2f), sprite, height).Create(name, false);
            parentItem.AddChild(currentItem);
            currentItem.GetComponent<NewIHasRotation>().SetRotation(0f, goRot.eulerAngles.y, 0f);
        }
        else if (name.Contains(ObjectName.CREATOR_BUILDING))
        {
            currentItem = new CreatorBuildingFactory().Create("Building", false);
            buildingItem = currentItem;
            // NewBuildingController.SetBuilding(currentItem);
        }

        for (int i = 0; i < fbxNode.GetChildCount(); ++i)
        {
            ProcessNode(fbxNode.GetChild(i), currentItem);
        }
        // UnityEngine.Object.Destroy(unityGo);
    }

    /// <summary>
    /// Process transformation data and setup Transform component
    /// </summary>
    private void ProcessTransform(FbxNode fbxNode)
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

        goPos = new Vector3(-(float)lclTrs[0], (float)lclTrs[1], (float)lclTrs[2]);
        goRot = new Quaternion((float)lclRot[0], -(float)lclRot[1], -(float)lclRot[2], (float)lclRot[3]);

        // unityGo.transform.localPosition = new Vector3(-(float)lclTrs[0], (float)lclTrs[1], (float)lclTrs[2]);
        // unityGo.transform.localRotation = new Quaternion((float)lclRot[0], -(float)lclRot[1], -(float)lclRot[2], (float)lclRot[3]);
        // unityGo.transform.localScale = new Vector3((float)lclScl[0], (float)lclScl[1], (float)lclScl[2]);

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

            unityVertices.Add(new Vector3(-(float)fbxVector4.X, (float)fbxVector4.Z, (float)fbxVector4.Y));
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
