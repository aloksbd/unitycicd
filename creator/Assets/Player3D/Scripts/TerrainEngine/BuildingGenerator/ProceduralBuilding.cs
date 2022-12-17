using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainEngine
{
    /// <summary>
    /// This class contains basic information about the building.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

    public class ProceduralBuilding : MonoBehaviour
    {
        const float PYLON_PRIMITIVE_HEIGHT = 2.0f; // meters

        public enum RoofType
        {
            flat = 0,
            dome,s
        };

        public string id;                       // building id
        public OsmBuildingData buildingData;    // OSM building data
        public float baseHeight;                // wall height in meters
        public Vector3[] worldFootprint;        // base vertices in world coordinates
        public Vector3[] localFootprint;        // base vertices in coordinates relative to transform.position (arithmetic centerpoint of worldFootprint).
        public bool invertRoof;                 // roof normals are inverted
        public bool invertWall;                 // wall normals are inverted
        public float roofHeight;                // height of roof (for non-flat types)
        public RoofType roofType;               // root type
        public bool generateWall;           
        public Material roofMaterial;                 
        public Material wallMaterial;
        public Material pylonMaterial;
        public Transform pylonsTransformParent; // parent transform for pylons
        public Vector2 tileSize                 // material tile size
            = new Vector2(30, 30); 
        public Vector2 uvOffset = Vector2.zero;
        public string statusDescription;        

        private float _startHeight = 0;
        private Vector3 _scale = new Vector3(1.0f, 1.0f, 1.0f);
        private Dictionary<string, List<GameObject>> _pylonGameObjects;
        private MeshFilter _meshFilter;    


        public MeshFilter meshFilter
        {
            get
            {
                if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
                return _meshFilter;
            }
        }

        [RuntimeAsync("CreateRoofDome")]
        private void CreateRoofDome(List<Vector3> vertices, List<int> triangles)
        {
            Vector3 roofTopPoint = Vector3.zero;
            roofTopPoint = vertices.Aggregate(roofTopPoint, (current, point) => current + point) / vertices.Count;
            roofTopPoint.y = (baseHeight + roofHeight) * this._scale.y;
            int vIndex = vertices.Count;

            for (int i = 0; i < vertices.Count; i++)
            {
                int p1 = i;
                int p2 = i + 1;
                if (p2 >= vertices.Count) p2 -= vertices.Count;

                triangles.AddRange(new[] { p1, p2, vIndex });
            }

            vertices.Add(roofTopPoint);
        }

        [RuntimeAsync("CreateRoofMesh")]
        private void CreateRoofMesh(List<Vector3> vertices, out List<Vector2> uv, out List<int> triangles)
        {
            List<Vector2> roofPoints = CreateRoofVertices(vertices);
            triangles = CreateRoofTriangles(vertices, roofPoints);

            if (invertRoof) triangles.Reverse();

            float minX = vertices.Min(p => p.x);
            float minZ = vertices.Min(p => p.z);
            float maxX = vertices.Max(p => p.x);
            float maxZ = vertices.Max(p => p.z);
            float offX = maxX - minX;
            float offZ = maxZ - minZ;

            uv = vertices.Select(v => new Vector2((v.x - minX) / offX, (v.z - minZ) / offZ)).ToList();
        }

        [RuntimeAsync("CreateRoofTriangles")]
        private List<int> CreateRoofTriangles(List<Vector3> vertices, List<Vector2> roofPoints)
        {
            List<int> triangles = new List<int>();
            if (roofType == RoofType.flat)
            {
                int[] trs = Triangulator2.Triangulate(roofPoints);
                if (trs != null) triangles.AddRange(trs);
            }
            else if (roofType == RoofType.dome)
            {
                CreateRoofDome(vertices, triangles);
            }
            return triangles;
        }

        [RuntimeAsync("CreateRoofVertices")]
        private List<Vector2> CreateRoofVertices(List<Vector3> vertices)
        {
            Vector3[] targetVertices = new Vector3[localFootprint.Length];
            Array.Copy(localFootprint, targetVertices, localFootprint.Length);

#if false
            if (container.prefs.buildingBottomMode == RealWorldTerrainBuildingBottomMode.followTerrain)
            {
                Vector3 tp = transform.position;
                RealWorldTerrainItem terrainItem = container.GetItemByWorldPosition(localFootprint[0] + tp);
                if (terrainItem != null)
                {
                    TerrainData t = terrainItem.terrainData;

                    Vector3 offset = tp - terrainItem.transform.position;

                    for (int i = 0; i < targetVertices.Length; i++)
                    {
                        Vector3 v = targetVertices[i];
                        Vector3 localPos = offset + v;
                        float y = t.GetInterpolatedHeight(localPos.x / t.size.x, localPos.z / t.size.z);
                        v.y = terrainItem.transform.position.y + y - tp.y;
                        targetVertices[i] = v;
                    }
                }
            }
#endif 
            List<Vector2> roofPoints = new List<Vector2>();
            float topPoint = targetVertices.Max(v => v.y) + baseHeight * this._scale.y;
            foreach (Vector3 p in targetVertices)
            {
                Vector3 tv = new Vector3(p.x, topPoint, p.z);
                Vector2 rp = new Vector2(p.x, p.z);

                vertices.Add(tv);
                roofPoints.Add(rp);
            }

            return roofPoints;
        }

        [RuntimeAsync("CreateWallMesh")]
        private void CreateWallMesh(List<Vector3> vertices, List<Vector2> uv, out List<int> triangles)
        {
            List<Vector3> wv = new List<Vector3>();
            List<Vector2> wuv = new List<Vector2>();
            bool reversed = CreateWallVertices(wv, wuv);
            if (invertWall) reversed = !reversed;
            triangles = CreateWallTriangles(wv, vertices.Count, reversed);
            vertices.AddRange(wv);
            uv.AddRange(wuv);
        }

        [RuntimeAsync("CreateWallTriangles")]
        private List<int> CreateWallTriangles(List<Vector3> vertices, int offset, bool reversed)
        {
            List<int> triangles = new List<int>();
            for (int i = 0; i < vertices.Count / 4; i++)
            {
                int p1 = i * 4;
                int p2 = i * 4 + 2;
                int p3 = i * 4 + 3;
                int p4 = i * 4 + 1;

                if (p2 >= vertices.Count) p2 -= vertices.Count;
                if (p3 >= vertices.Count) p3 -= vertices.Count;

                p1 += offset;
                p2 += offset;
                p3 += offset;
                p4 += offset;

                if (reversed)
                {
                    triangles.AddRange(new[] { p1, p4, p3, p1, p3, p2 });
                }
                else
                {
                    triangles.AddRange(new[] { p2, p3, p1, p3, p4, p1 });
                }
            }
            return triangles;
        }

        [RuntimeAsync("CreateWallVertices")]
        private bool CreateWallVertices(List<Vector3> vertices, List<Vector2> uv)
        {
            Vector3[] targetVertices = new Vector3[localFootprint.Length];
            Array.Copy(localFootprint, targetVertices, localFootprint.Length);

#if false
            if (container.prefs.buildingBottomMode == RealWorldTerrainBuildingBottomMode.followTerrain)
            {
                Vector3 tp = transform.position;
                RealWorldTerrainItem terrainItem = container.GetItemByWorldPosition(localFootprint[0] + tp);
                if (terrainItem != null)
                {
                    TerrainData t = terrainItem.terrainData;

                    Vector3 offset = tp - terrainItem.transform.position;

                    for (int i = 0; i < targetVertices.Length; i++)
                    {
                        Vector3 v = targetVertices[i];
                        Vector3 localPos = offset + v;
                        float y = t.GetInterpolatedHeight(localPos.x / t.size.x, localPos.z / t.size.z);
                        v.y = terrainItem.transform.position.y + y - tp.y;
                        targetVertices[i] = v;
                    }
                }
            }
#endif

            float topPoint = targetVertices.Max(v => v.y) + baseHeight * this._scale.y;

            float startY = _startHeight * this._scale.y;
            float offsetY = startY < 0 ? startY : 0;

            for (int i = 0; i < targetVertices.Length; i++)
            {
                Vector3 p1 = targetVertices[i];
                Vector3 p2 = i < targetVertices.Length - 1 ? targetVertices[i + 1] : targetVertices[0];
                if (p1.y < startY) p1.y = startY;
                if (p2.y < startY) p2.y = startY;
                p1.y += offsetY;
                p2.y += offsetY;
                vertices.Add(p1);
                vertices.Add(new Vector3(p1.x, topPoint, p1.z));
                vertices.Add(p2);
                vertices.Add(new Vector3(p2.x, topPoint, p2.z));
            }

            float totalDistance = 0;
            float bottomPoint = float.MaxValue;

            for (int i = 0; i < vertices.Count / 4; i++)
            {
                int i1 = Mathf.RoundToInt(Mathf.Repeat(i * 4, vertices.Count));
                int i2 = Mathf.RoundToInt(Mathf.Repeat((i + 1) * 4, vertices.Count));
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                v1.y = v2.y = 0;
                totalDistance += (v1 - v2).magnitude;
                if (bottomPoint > targetVertices[i].y) bottomPoint = targetVertices[i].y;
            }

            Vector3 lv1 = vertices[vertices.Count - 4];
            Vector3 lv2 = vertices[0];
            lv1.y = lv2.y = 0;
            totalDistance += (lv1 - lv2).magnitude;

            float currentDistance = 0;
            float nextU = 0;
            float uMul = totalDistance / tileSize.x;
            float vMax = topPoint / tileSize.y;
            float vMinMul = this._scale.y * tileSize.y;

            for (int i = 0; i < vertices.Count / 4; i++)
            {
                int i1 = Mathf.RoundToInt(Mathf.Repeat(i * 4, vertices.Count));
                int i2 = Mathf.RoundToInt(Mathf.Repeat((i + 1) * 4, vertices.Count));
                float curU = nextU;
                uv.Add(new Vector2(curU * uMul + uvOffset.x, (vertices[i * 4].y - bottomPoint) / vMinMul + uvOffset.y));
                uv.Add(new Vector2(curU * uMul + uvOffset.x, vMax + uvOffset.y));

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                v1.y = v2.y = 0;
                currentDistance += (v1 - v2).magnitude;
                nextU = currentDistance / totalDistance;

                uv.Add(new Vector2(nextU * uMul + uvOffset.x, (vertices[i * 4 + 2].y - bottomPoint) / vMinMul + uvOffset.y));
                uv.Add(new Vector2(nextU * uMul + uvOffset.x, vMax + uvOffset.y));
            }

            int southIndex = -1;
            float southZ = float.MaxValue;

            for (int i = 0; i < targetVertices.Length; i++)
            {
                if (targetVertices[i].z < southZ)
                {
                    southZ = targetVertices[i].z;
                    southIndex = i;
                }
            }

            int prevIndex = southIndex - 1;
            if (prevIndex < 0) prevIndex = targetVertices.Length - 1;

            int nextIndex = southIndex + 1;
            if (nextIndex >= targetVertices.Length) nextIndex = 0;

            float angle1 = Angle2D(targetVertices[southIndex], targetVertices[nextIndex]);
            float angle2 = Angle2D(targetVertices[southIndex], targetVertices[prevIndex]);

            return angle1 < angle2;
        }

        private void CreatePylons()
        {
            Abortable abortable = new Abortable("BuildingGenerator.CreatePylons");

            List<GameObject> pylons = new List<GameObject>();

            foreach (Vector3 vertex in worldFootprint)
            {
                if (abortable.shouldAbort)
                {
                    return;
                }

                GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pylon.name = id;
                pylon.transform.parent = pylonsTransformParent;
                pylon.transform.position = new Vector3(vertex.x, baseHeight / 2.0f + transform.position.y, vertex.z);
                pylon.transform.localScale = new Vector3(.25f, baseHeight / PYLON_PRIMITIVE_HEIGHT, .25f);

                pylon.GetComponent<MeshRenderer>().materials = new[]
                {
                    pylonMaterial,
                };

                //  Remove collider added by CreatePrimitive()
                Destroy(pylon.GetComponent<CapsuleCollider>());

                pylons.Add(pylon);
            }

            if (_pylonGameObjects == null)
            {
                _pylonGameObjects = new Dictionary<string, List<GameObject>>();
            }
            _pylonGameObjects.Add(buildingData.id, pylons);
        }

        private bool GetStartHeight(out float height)   
        {
            height = 0;
            float lowestTerrainHeight = float.MaxValue;
           
            foreach (Vector3 vertex in worldFootprint)
            {
                float y;
                if (TerrainRuntime.SampleHeightAtPosition(vertex, out y))
                {
                    if (y < lowestTerrainHeight)
                    {
                        lowestTerrainHeight = y;
                    }
                }
            }

            if (lowestTerrainHeight < float.MaxValue)
            {
                height = lowestTerrainHeight;
                return true;
            }

            return false;
        }

        public void UpdatePositionOnTerrain()
        {
            float height;
            if (GetStartHeight(out height))
            {
                transform.position = new Vector3(transform.position.x, height, transform.position.z);

                if (_pylonGameObjects != null)
                {
                    foreach (List<GameObject> pylons in _pylonGameObjects.Values)
                    {
                        foreach (GameObject pylon in pylons)
                        {
                            pylon.transform.position = 
                                new Vector3(pylon.transform.position.x, (baseHeight / 2.0f) + transform.position.y, pylon.transform.position.z);
                        }
                    }
                    _pylonGameObjects = null;
                }
            }
        }

        public void Generate(bool addCollider)
        {
            Abortable abortable = new Abortable("ProceduralBuilding.Generate");

            if (abortable.shouldAbort)
            {
                return;
            }

            Mesh mesh;
            if (meshFilter.sharedMesh != null)
            {
                mesh = meshFilter.sharedMesh;
            }
            else
            {
                mesh = new Mesh();
                mesh.name = id;
                mesh.subMeshCount = 2;
                meshFilter.sharedMesh = mesh;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv;
            List<int> roofTriangles;
            List<int> wallTriangles = null;

            if (abortable.shouldAbort)
            {
                return;
            }

            CreateRoofMesh(vertices, out uv, out roofTriangles);
            if (generateWall)
            {
                CreateWallMesh(vertices, uv, out wallTriangles);
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);

            mesh.SetTriangles(roofTriangles, 0);
            if (generateWall)
            {
                mesh.SetTriangles(wallTriangles, 1);
            }

            if (abortable.shouldAbort)
            {
                return;
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GetComponent<MeshRenderer>().materials = new[]
            {
                roofMaterial,
                wallMaterial,
            };

            if (addCollider)
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning |
                                              MeshColliderCookingOptions.WeldColocatedVertices;
            }

            if (abortable.shouldAbort)
            {
                return;
            }
            UpdatePositionOnTerrain();

            if (abortable.shouldAbort)
            {
                return;
            }
            CreatePylons();
        }

        public void Dispose()
        {
            if (_pylonGameObjects != null)
            {
                foreach (List<GameObject> pylons in _pylonGameObjects.Values)
                {
                    foreach (GameObject gameObject in pylons)
                    {
                        Destroy(gameObject);
                    }
                }
                _pylonGameObjects = null;
            }

            worldFootprint = null;
            localFootprint = null;
        }

        public static float Angle2D(Vector3 point1, Vector3 point2)
        {
            return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }

        public static float Angle2D(Vector2 point1, Vector2 point2)
        {
            return Mathf.Atan2((point2.y - point1.y), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }
    }
}