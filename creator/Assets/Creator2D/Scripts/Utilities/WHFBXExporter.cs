using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Fbx;
using UnityEngine;
using UnityEngine.UIElements;
class WHFbxExporter : System.IDisposable
{

    static WHFbxExporter Create()
    {
        return new WHFbxExporter();
    }

    string pathName;

    internal bool Verbose { set; get; }

    /// <summary>
    /// Number of nodes exported including siblings and decendents
    /// </summary>
    internal int NumNodes { set; get; }

    /// <summary>
    /// Number of meshes exported
    /// </summary>
    internal int NumMeshes { set; get; }

    /// <summary>
    /// Number of triangles exported
    /// </summary>
    internal int NumTriangles { set; get; }

    /// <summary>
    /// Number of vertices
    /// </summary>
    internal int NumVertices { private set; get; }

    /// <summary>
    /// Map Unity material name to FBX material object
    /// </summary>
    Dictionary<string, FbxSurfaceMaterial> MaterialMap = new Dictionary<string, FbxSurfaceMaterial>();

    /// <summary>
    /// Map texture filename name to FBX texture object
    /// </summary>
    Dictionary<string, FbxTexture> TextureMap = new Dictionary<string, FbxTexture>();

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
    }
    //
    // export mesh info from Unity
    //
    ///<summary>
    ///Information about the mesh that is important for exporting.
    ///</summary>
    public struct MeshInfo
    {
        /// <summary>
        /// The transform of the mesh.
        /// </summary>
        public Matrix4x4 xform;
        public Mesh mesh;

        /// <summary>
        /// The gameobject in the scene to which this mesh is attached.
        /// This can be null: don't rely on it existing!
        /// </summary>
        public GameObject unityObject;

        /// <summary>
        /// Return true if there's a valid mesh information
        /// </summary>
        /// <value>The vertex count.</value>
        public bool IsValid { get { return mesh != null; } }

        /// <summary>
        /// Gets the vertex count.
        /// </summary>
        /// <value>The vertex count.</value>
        public int VertexCount { get { return mesh.vertexCount; } }

        /// <summary>
        /// Gets the triangles. Each triangle is represented as 3 indices from the vertices array.
        /// Ex: if triangles = [3,4,2], then we have one triangle with vertices vertices[3], vertices[4], and vertices[2]
        /// </summary>
        /// <value>The triangles.</value>
        public int[] Triangles { get { return mesh.triangles; } }

        /// <summary>
        /// Gets the vertices, represented in local coordinates.
        /// </summary>
        /// <value>The vertices.</value>
        public Vector3[] Vertices { get { return mesh.vertices; } }

        /// <summary>
        /// Gets the normals for the vertices.
        /// </summary>
        /// <value>The normals.</value>
        public Vector3[] Normals { get { return mesh.normals; } }

        /// <summary>
        /// TODO: Gets the binormals for the vertices.
        /// </summary>
        /// <value>The normals.</value>
        private Vector3[] m_Binormals;
        public Vector3[] Binormals
        {
            get
            {
                /// NOTE: LINQ
                ///    return mesh.normals.Zip (mesh.tangents, (first, second)
                ///    => Math.cross (normal, tangent.xyz) * tangent.w
                if (m_Binormals.Length == 0)
                {
                    m_Binormals = new Vector3[mesh.normals.Length];

                    for (int i = 0; i < mesh.normals.Length; i++)
                        m_Binormals[i] = Vector3.Cross(mesh.normals[i],
                                                         mesh.tangents[i])
                                                 * mesh.tangents[i].w;

                }
                return m_Binormals;
            }
        }

        /// <summary>
        /// TODO: Gets the tangents for the vertices.
        /// </summary>
        /// <value>The tangents.</value>
        public Vector4[] Tangents { get { return mesh.tangents; } }

        /// <summary>
        /// TODO: Gets the tangents for the vertices.
        /// </summary>
        /// <value>The tangents.</value>
        public Color[] VertexColors { get { return mesh.colors; } }

        /// <summary>
        /// Gets the uvs.
        /// </summary>
        /// <value>The uv.</value>
        public Vector2[] UV { get { return mesh.uv; } }

        /// <summary>
        /// The material used, if any; otherwise null.
        /// We don't support multiple materials on one gameobject.
        /// </summary>
        public Material[] Materials
        {
            get
            {
                if (!unityObject) { return null; }
                var renderer = unityObject.GetComponent<Renderer>();
                if (!renderer) { return new Material[0]; }
                // .material instantiates a new material, which is bad
                // most of the time.
                return renderer.sharedMaterials;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshInfo"/> struct.
        /// </summary>
        /// <param name="mesh">A mesh we want to export</param>
        public MeshInfo(Mesh mesh)
        {
            this.mesh = mesh;
            this.xform = Matrix4x4.identity;
            this.unityObject = null;
            this.m_Binormals = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshInfo"/> struct.
        /// </summary>
        /// <param name="gameObject">The GameObject the mesh is attached to.</param>
        /// <param name="mesh">A mesh we want to export</param>
        public MeshInfo(GameObject gameObject, Mesh mesh)
        {
            this.mesh = mesh;
            this.xform = gameObject.transform.localToWorldMatrix;
            this.unityObject = gameObject;
            this.m_Binormals = null;
        }
    }

    /// <summary>
	/// Export child of Strucute object to fbx
	/// </summary>
	/// <param name="filePath">full file path</param>
	/// <param name="structureGO">Parent GameObject to Export</param>
	/// <param name="pathName">Directory</param>
    public static int ExportObjects(string filePath, string pathName, GameObject structureGO)
    {
        using (var fbxExporter = Create())
        {
            fbxExporter.pathName = pathName;
            Debug.Log("pathName:::" + pathName);
            return fbxExporter.ExportAll(filePath, structureGO);
        }
    }

    int ExportAll(string filePath, GameObject structureGO)
    {
        using (var fbxManager = FbxManager.Create())
        {

            // Configure fbx IO settings.
            var settings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);
            settings.SetBoolProp(Globals.EXP_FBX_EMBEDDED, true);
            fbxManager.SetIOSettings(settings);

            // Create the exporter
            var fbxExporter = FbxExporter.Create(fbxManager, "Exporter");

            // Initialize the exporter.
            // fileFormat must be binary if we are embedding textures
            int fileFormat = -1;
            fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX binary (*.fbx)");

            bool status = fbxExporter.Initialize(filePath, fileFormat, fbxManager.GetIOSettings());
            // Check that initialization of the fbxExporter was successful
            if (!status)
                return 0;

            // // Set compatibility to 2014
            // fbxExporter.SetFileExportVersion("FBX201400");

            // Create a scene
            var fbxScene = FbxScene.Create(fbxManager, "myScene");

            // set up the scene info
            FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "SceneInfo");

            fbxSceneInfo.mTitle = "fromRuntime";
            fbxSceneInfo.mSubject = "Exported from a Unity runtime";
            fbxSceneInfo.mAuthor = "Unity Technologies";
            fbxSceneInfo.mRevision = "1.0";
            fbxSceneInfo.mKeywords = "export runtime";
            fbxSceneInfo.mComment = "This is to demonstrate the capability of exporting from a Unity runtime, using the FBX SDK C# bindings";

            fbxScene.SetSceneInfo(fbxSceneInfo);

            // Set up the axes (Y up, Z forward, X to the right) and units (meters)
            var fbxSettings = fbxScene.GetGlobalSettings();
            fbxSettings.SetSystemUnit(FbxSystemUnit.m);

            // The Unity axis system has Y up, Z forward, X to the right (left handed system with odd parity).
            // The Maya axis system has Y up, Z forward, X to the left (right handed system with odd parity).
            // We need to export right-handed for Maya because ConvertScene can't switch handedness:
            // https://forums.autodesk.com/t5/fbx-forum/get-confused-with-fbxaxissystem-convertscene/td-p/4265472
            fbxSettings.SetAxisSystem(FbxAxisSystem.MayaYUp);

            // export set of object
            FbxNode fbxRootNode = fbxScene.GetRootNode();

            ExportComponents(structureGO, fbxScene, fbxRootNode);

            // Export the scene to the file.
            status = fbxExporter.Export(fbxScene);

            // cleanup
            fbxScene.Destroy();
            fbxExporter.Destroy();

            // AssetDatabase.Refresh();
            // StartCoroutine(UpdateMessageLabel());
            // return;    
            return status == true ? NumNodes : 0;
        }
    }

    /// <summary>
    /// Unconditionally export components on this game object
    /// </summary>
    private void ExportComponents(GameObject unityGo, FbxScene fbxScene, FbxNode fbxNodeParent)
    {
        if (!unityGo.activeSelf)
        {
            return;
        }
        if (unityGo.name.Contains("Clone")) return;
        // create an FbxNode and add it as a child of parent
        FbxNode fbxNode = FbxNode.Create(fbxScene, unityGo.name);
        NumNodes++;

        ExportTransform(unityGo.transform, fbxNode);
        // ExportMesh(GetMeshInfo(unityGo), fbxNode, fbxScene);
        // ExportCollider(unityGo, fbxNode);

        fbxNodeParent.AddChild(fbxNode);

        var name = unityGo.name;

        if (name.Contains(WHConstants.FLOOR_PLAN))
        {
            CreatorItem item = CreatorItemFinder.FindByName(name);
            if (item != null)
            {
                Label linkFloorName = item.uiItem.Foldout.Q<UnityEngine.UIElements.Label>("linkFloorName-" + name);
                if (linkFloorName != null)
                {
                    FbxProperty linkFloorProperty = FbxProperty.Create(fbxNode, new FbxDataType(EFbxType.eFbxString), "LinkFloorName");
                    linkFloorProperty.Set(linkFloorName.text);
                }

                var dimension = item.GetComponent<NewIHasDimension>().Dimension;
                FbxProperty heightProperty = FbxProperty.Create(fbxNode, new FbxDataType(EFbxType.eFbxFloat), "FLOORPLAN_HEIGHT");
                heightProperty.Set(dimension.Height);
            }
        }
        else if (name.Contains(WHConstants.WALL))
        {
            FbxProperty localBoundX = FbxProperty.Create(fbxNode, new FbxDataType(EFbxType.eFbxFloat), "LOCALBOUND_X");
            localBoundX.Set(unityGo.GetComponent<Renderer>().localBounds.size.x);
            CreatorItem item = CreatorItemFinder.FindByName(name);
            if (item != null)
            {
                FbxProperty exteriorWall = FbxProperty.Create(fbxNode, new FbxDataType(EFbxType.eFbxInt), "EXTERIOR");
                exteriorWall.Set(((NewWall)item).IsExterior ? 1 : 0);
            }
        }

        // now  unityGo  through our children and recurse
        foreach (Transform childT in unityGo.transform)
        {
            ExportComponents(childT.gameObject, fbxScene, fbxNode);
        }
        return;
    }

    // get a fbxNode's global default position.
    protected void ExportTransform(UnityEngine.Transform unityTransform, FbxNode fbxNode)
    {
        // get local position of fbxNode (from Unity)
        UnityEngine.Vector3 unityTranslate = unityTransform.localPosition;
        UnityEngine.Vector3 unityRotate = unityTransform.localRotation.eulerAngles;
        UnityEngine.Vector3 unityScale = unityTransform.localScale;

        // transfer transform data from Unity to Fbx
        // Negating the x value of the translation, and the y and z values of the rotation
        // to convert from Unity to Maya coordinates (left to righthanded)
        var fbxTranslate = new FbxDouble3(-unityTranslate.x, unityTranslate.y, unityTranslate.z);
        var fbxRotate = new FbxDouble3(unityRotate.x, -unityRotate.y, -unityRotate.z);
        var fbxScale = new FbxDouble3(unityScale.x, unityScale.y, unityScale.z);

        // set the local position of fbxNode
        fbxNode.LclTranslation.Set(fbxTranslate);
        fbxNode.LclRotation.Set(fbxRotate);
        fbxNode.LclScaling.Set(fbxScale);

        return;
    }

    /// <summary>
    /// Unconditionally export this mesh object to the file.
    /// We have decided; this mesh is definitely getting exported.
    /// </summary>
    void ExportMesh(MeshInfo meshInfo, FbxNode fbxNode, FbxScene fbxScene)
    {
        if (!meshInfo.IsValid)
            return;

        NumMeshes++;
        NumTriangles += meshInfo.Triangles.Length / 3;
        NumVertices += meshInfo.VertexCount;
        // NumVertices += meshInfo.VertexCount;

        Debug.Log("NumVertices:::" + NumVertices);

        // create the mesh structure.
        FbxMesh fbxMesh = FbxMesh.Create(fbxScene, "Scene");

        // Create control points.
        int NumControlPoints = meshInfo.VertexCount;

        Debug.Log("meshInfo.VertexCount:::" + meshInfo.VertexCount);

        fbxMesh.InitControlPoints(NumControlPoints);

        // copy control point data from Unity to FBX
        for (int v = 0; v < NumControlPoints; v++)
        {
            // convert from left to right-handed by negating x (Unity negates x again on import)
            fbxMesh.SetControlPointAt(new FbxVector4(-meshInfo.Vertices[v].x, meshInfo.Vertices[v].y, meshInfo.Vertices[v].z), v);
        }

        ExportUVs(meshInfo, fbxMesh);
        foreach (var material in meshInfo.Materials)
        {
            var fbxMaterial = ExportMaterial(material, fbxScene);
            fbxNode.AddMaterial(fbxMaterial);
        }

        // var fbxMaterial = ExportMaterial(meshInfo.Materials, fbxScene);
        // fbxNode.AddMaterial(fbxMaterial);

        /* Triangles have to be added in reverse order, 
         * or else they will be inverted on import 
         * (due to the conversion from left to right handed coords)
         */
        // Debug.Log("meshInfo")
        for (int f = 0; f < meshInfo.Triangles.Length / 3; f++)
        {
            fbxMesh.BeginPolygon();
            fbxMesh.AddPolygon(meshInfo.Triangles[3 * f]);
            fbxMesh.AddPolygon(meshInfo.Triangles[3 * f + 1]);
            fbxMesh.AddPolygon(meshInfo.Triangles[3 * f + 2]);
            fbxMesh.EndPolygon();
        }

        Debug.Log("fbxMesh.GetPolygonVertexCount ():::" + fbxMesh.GetPolygonVertexCount());

        // set the fbxNode containing the mesh
        fbxNode.SetNodeAttribute(fbxMesh);
        fbxNode.SetShadingMode(FbxNode.EShadingMode.eWireFrame);
    }



    /// <summary>
    /// Get a mesh renderer's mesh.
    /// </summary>
    private MeshInfo GetMeshInfo(GameObject gameObject, bool requireRenderer = true)
    {
        // Two possibilities: it's a skinned mesh, or we have a mesh filter.
        Mesh mesh;
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter)
        {
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            var renderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (!renderer)
            {
                mesh = null;
            }
            else
            {
                mesh = new Mesh();
                renderer.BakeMesh(mesh);
            }
        }
        if (!mesh)
        {
            return new MeshInfo();
        }
        return new MeshInfo(gameObject, mesh);
    }

    /// <summary>
    /// Export the mesh's UVs using layer 0.
    /// </summary>
    public void ExportUVs(MeshInfo mesh, FbxMesh fbxMesh)
    {
        // Set the normals on Layer 0.
        FbxLayer fbxLayer = fbxMesh.GetLayer(0 /* default layer */);
        if (fbxLayer == null)
        {
            fbxMesh.CreateLayer();
            fbxLayer = fbxMesh.GetLayer(0 /* default layer */);
        }

        using (var fbxLayerElement = FbxLayerElementUV.Create(fbxMesh, "UVSet"))
        {
            fbxLayerElement.SetMappingMode(FbxLayerElement.EMappingMode.eByPolygonVertex);
            fbxLayerElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eIndexToDirect);

            // set texture coordinates per vertex
            FbxLayerElementArray fbxElementArray = fbxLayerElement.GetDirectArray();

            for (int n = 0; n < mesh.UV.Length; n++)
            {
                fbxElementArray.Add(new FbxVector2(mesh.UV[n][0],
                                              mesh.UV[n][1]));
            }

            // For each face index, point to a texture uv
            var unityTriangles = mesh.Triangles;
            FbxLayerElementArray fbxIndexArray = fbxLayerElement.GetIndexArray();
            fbxIndexArray.SetCount(unityTriangles.Length);

            for (int i = 0, n = unityTriangles.Length; i < n; ++i)
            {
                fbxIndexArray.SetAt(i, unityTriangles[i]);
            }
            fbxLayer.SetUVs(fbxLayerElement, FbxLayerElement.EType.eTextureDiffuse);
        }
    }

    /// <summary>
    /// Export (and map) a Unity PBS material to FBX classic material
    /// </summary>
    FbxSurfaceMaterial ExportMaterial(Material unityMaterial, FbxScene fbxScene)
    {

        var materialName = unityMaterial ? unityMaterial.name : "DefaultMaterial";
        // if (MaterialMap.ContainsKey (materialName)) {
        //     return MaterialMap [materialName];
        // }

        // We'll export either Phong or Lambert. Phong if it calls
        // itself specular, Lambert otherwise.
        var shader = unityMaterial ? unityMaterial.shader : null;
        bool specular = shader && shader.name.ToLower().Contains("specular");

        var fbxMaterial = specular
            ? FbxSurfacePhong.Create(fbxScene, materialName)
            : FbxSurfaceLambert.Create(fbxScene, materialName);

        // Copy the flat colours over from Unity standard materials to FBX.
        fbxMaterial.Diffuse.Set(GetMaterialColor(unityMaterial, "_Color"));
        fbxMaterial.Emissive.Set(GetMaterialColor(unityMaterial, "_EmissionColor"));
        fbxMaterial.Ambient.Set(new FbxDouble3());
        fbxMaterial.BumpFactor.Set(unityMaterial && unityMaterial.HasProperty("_BumpScale") ? unityMaterial.GetFloat("_BumpScale") : 0);
        if (specular)
        {
            (fbxMaterial as FbxSurfacePhong).Specular.Set(GetMaterialColor(unityMaterial, "_SpecColor"));
        }

        // Export the textures from Unity standard materials to FBX.
        ExportTexture(unityMaterial, "_MainTex", fbxMaterial, FbxSurfaceMaterial.sDiffuse);
        ExportTexture(unityMaterial, "_EmissionMap", fbxMaterial, "emissive");
        ExportTexture(unityMaterial, "_BumpMap", fbxMaterial, FbxSurfaceMaterial.sNormalMap);
        if (specular)
        {
            ExportTexture(unityMaterial, "_SpecGlosMap", fbxMaterial, FbxSurfaceMaterial.sSpecular);
        }

        // MaterialMap.Add(materialName, fbxMaterial);
        return fbxMaterial;
    }

    /// <summary>
    /// Get the color of a material, or grey if we can't find it.
    /// </summary>
    public FbxDouble3 GetMaterialColor(Material unityMaterial, string unityPropName)
    {
        if (!unityMaterial) { return new FbxDouble3(0.5); }
        if (!unityMaterial.HasProperty(unityPropName)) { return new FbxDouble3(0.5); }
        var unityColor = unityMaterial.GetColor(unityPropName);
        return new FbxDouble3(unityColor.r, unityColor.g, unityColor.b);
    }

    /// <summary>
    /// Export an Unity Texture
    /// </summary>
    void ExportTexture(Material unityMaterial, string unityPropName,
        FbxSurfaceMaterial fbxMaterial, string fbxPropName)
    {
        if (!unityMaterial) { return; }

        // Get the texture on this property, if any.
        if (!unityMaterial.HasProperty(unityPropName)) { return; }
        var unityTexture = unityMaterial.GetTexture(unityPropName);
        if (!unityTexture) { return; }

        // Find its filename
        var textureSourceFullPath = unityTexture.name + ".png";
        bool isTexCopied = SaveTextureRunTime((Texture2D)unityTexture, pathName + "/Textures");
        if (textureSourceFullPath == "") { return; }

        // get absolute filepath to texture
        // textureSourceFullPath  = Path.GetFullPath(textureSourceFullPath);

        if (Verbose)
            Debug.Log(string.Format("{2}.{1} setting texture path {0}", textureSourceFullPath, fbxPropName, fbxMaterial.GetName()));

        // Find the corresponding property on the fbx material.
        var fbxMaterialProperty = fbxMaterial.FindProperty(fbxPropName);
        if (fbxMaterialProperty == null || !fbxMaterialProperty.IsValid()) { Debug.Log("property not found"); return; }

        // Find or create an fbx texture and link it up to the fbx material.
        var fbxTexture = FbxFileTexture.Create(fbxMaterial, fbxPropName + "_Texture");
        fbxTexture.SetFileName(textureSourceFullPath);
        fbxTexture.SetTextureUse(FbxTexture.ETextureUse.eStandard);
        fbxTexture.SetMappingType(FbxTexture.EMappingType.eUV);

        fbxTexture.ConnectDstProperty(fbxMaterialProperty);
    }




    /// <summary>
	/// Saves the texture in PNG format at runtime
	/// </summary>
	/// <param name="texture">Texture exported</param>
	/// <param name="pathName">The path to export to</param>
	bool SaveTextureRunTime(Texture2D texture, string path)
    {
        // if (!texture.isReadable) return false;
        Texture2D decopmpresseTex = DeCompress(texture);

        byte[] bytes = decopmpresseTex.EncodeToPNG();

        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        System.IO.File.WriteAllBytes(path + "/" + texture.name + ".png", bytes);
        return true;
    }

    /// <summary>
    /// Decompresses the texture at runtime
    /// </summary>
    Texture2D DeCompress(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    /// <summary>
    /// Get the GameObject
    /// </summary>
    // private GameObject GetGameObject (Object obj) {
    //     if (obj is UnityEngine.Transform) {
    //         var xform = obj as UnityEngine.Transform;
    //         return xform.gameObject;
    //     } else if (obj is UnityEngine.GameObject) {
    //         return obj as UnityEngine.GameObject;
    //     } else if (obj is MonoBehaviour) {
    //         var mono = obj as MonoBehaviour;
    //         return mono.gameObject;
    //     }
    //     return null;
    // }

    void ExportCollider(GameObject gameObject, FbxNode fbxNode)
    {
        if (gameObject.GetComponent<Collider>() != null)
        {
            FbxProperty colliderProperty = FbxProperty.Create(fbxNode, new FbxDataType(EFbxType.eFbxString), "ColliderObjectType");
            if (gameObject.GetComponent<SphereCollider>() != null)
            {
                colliderProperty.Set("SphereCollider");
            }
            else if (gameObject.GetComponent<BoxCollider>() != null)
            {
                colliderProperty.Set("BoxCollider");
            }
            else if (gameObject.GetComponent<MeshCollider>() != null)
            {
                colliderProperty.Set("MeshCollider");
            }
            else if (gameObject.GetComponent<CapsuleCollider>() != null)
            {
                colliderProperty.Set("CapsuleCollider");
            }
            else
            {
                colliderProperty.Set("MeshCollider");
            }
        }

    }
}