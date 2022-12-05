using System;
using UnityEngine;
using System.Collections.Generic;

namespace ObjectModel
{
    public interface IGameObject3D : IItemComponent
    {
        GameObject GetGameObject();
    }
    public class GameObject3D : IGameObject3D
    {
        private Func<Mesh> _createMesh;
        private Func<List<IGameObject3D>> _getChildren;
        private Func<Vector3> _getPostion;
        private Func<Vector3> _getRotation;
        private Func<Material> _getMaterial;
        private string _name;
        private IItem _item;

        public GameObject3D(string name, Func<Mesh> createMesh, Func<List<IGameObject3D>> getChildren, Func<Vector3> getPostion, Func<Vector3> getRotation, Func<Material> getMaterial)
        {
            _name = name;
            _createMesh = createMesh;
            _getChildren = getChildren;
            _getPostion = getPostion;
            _getRotation = getRotation;
            _getMaterial = getMaterial;
        }
        public GameObject GetGameObject()
        {
            var gameObject = new GameObject();
            gameObject.name = _name;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = _getMaterial();

            meshFilter.mesh = _createMesh();
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.mesh;

            var children = _getChildren();
            foreach (var child in children)
            {
                var childGameObject = child.GetGameObject();
                childGameObject.transform.parent = gameObject.transform;
            }

            gameObject.transform.position = _getPostion();
            gameObject.transform.Rotate(_getRotation());
            return gameObject;
        }

        public static List<IGameObject3D> ChildrenToIGameObject3D(List<IItem> children)
        {
            List<IGameObject3D> childrenGameObject3D = new List<IGameObject3D>();
            foreach (var child in children)
            {
                var weakGameObject3D = child.GetComponent<IGameObject3D>();
                if (weakGameObject3D.IsAlive)
                {
                    childrenGameObject3D.Add(weakGameObject3D.Target as IGameObject3D);
                }
            }
            return childrenGameObject3D;
        }

        public IItemComponent Clone()
        {
            return new GameObject3D(_name, _createMesh, _getChildren, _getPostion, _getRotation, _getMaterial);
        }
    }
}