using UnityEngine;
using System.Collections.Generic;

namespace ObjectModel
{
    public class Building : Item
    {
        private Building() : base(() => NamingStrategy.GetName("Building")) { }
        private Building(Vector3 position) : base(() => NamingStrategy.GetName("Building"))
        {
            AddComponent(new HasPosition(position));
            AddComponent(new HasRotation());
            AddComponent(new Selectable());
            var gameObject3d = new GameObject3D(
                Name,
                () => new Mesh(),
                () => GameObject3D.ChildrenToIGameObject3D(Children),
                () => { return new Vector3(0, 0, 0); },
                () => { return new Vector3(0, 0, 0); },
                () =>
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = Color.white;
                    return material;
                }
            );

            AddComponent(gameObject3d);
        }

        public static Building Create(Vector3 position)
        {
            return new Building(position);
        }

        public static Building Create()
        {
            return new Building(new Vector3(0, 0, 0));
        }

        override protected IItem GetClonedItem()
        {
            return new Building();
        }
    }
}
