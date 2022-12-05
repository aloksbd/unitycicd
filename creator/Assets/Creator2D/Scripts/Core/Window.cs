using UnityEngine;
using System;
using System.Collections.Generic;

namespace ObjectModel
{
    public class Window : Item
    {
        private Window() : base(() => NamingStrategy.GetName("Window")) { }
        private Window(IHasPosition position, IHasDimension dimension) : base(() => NamingStrategy.GetName("Window"))
        {
            AddComponent(position);
            AddComponent(new HasRotation());
            AddComponent(dimension);
            AddComponent(new Selectable());
            var mesh = new HasMesh(
                () =>
                {
                    var wallCreator = new WallCreator(dimension.Height, dimension.Length, dimension.Width, Children);
                    return wallCreator.CreateWallMesh();
                }
            );
            var gameObject3d = new GameObject3D(
                Name,
                () => mesh.CreateMesh(),
                () => GameObject3D.ChildrenToIGameObject3D(Children),
                () =>
                {
                    var weakPosition = GetComponent<IHasPosition>();
                    // var parentWeakDimension = GetComponent<IHasDimension>();
                    if (weakPosition.IsAlive)
                    {
                        // Vector3 Position = (weakPosition.Target as IHasPosition).Position;
                        // float parentLength = (parentWeakDimension.Target as IHasDimension).Length;
                        // return new Vector3(Position.x + parentLength / 2, Position.y, Position.z);
                        return (weakPosition.Target as IHasPosition).Position;
                    }
                    return new Vector3(0, 0, 0);
                },
                () =>
                {
                    var weakPosition = GetComponent<IHasRotation>();
                    if (weakPosition.IsAlive)
                    {
                        return (weakPosition.Target as IHasRotation).EulerAngles;
                    }
                    return new Vector3(0, 0, 0);
                },
                () =>
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = Color.white;
                    return material;
                }
            );
            AddComponent(gameObject3d);
        }

        public static Window Create(Vector3 position, Dimension dimension)
        {
            var _position = new HasPosition(position);
            // var dimension = DoorType.DOORDIMENSIONS[type];
            // var typedItem = new TypedItem(type);
            return new Window(_position, dimension);
        }

        override protected IItem GetClonedItem()
        {
            return new Window();
        }
    }
}
