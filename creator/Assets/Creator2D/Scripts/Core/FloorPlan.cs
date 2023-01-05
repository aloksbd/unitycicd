using UnityEngine;

namespace ObjectModel
{
    public class FloorPlan : Item
    {
        private FloorPlan() : base(() => NamingStrategy.GetName(WHConstants.FLOOR_PLAN)) { }

        private FloorPlan(IHasPosition position, Item Parent) : base(() => NamingStrategy.GetName(WHConstants.FLOOR_PLAN, Parent.Children))
        {
            AddComponent(position);
            AddComponent(new Dimension());
            AddComponent(new Selectable());
            var gameObject3d = new GameObject3D(
                Name,
                () => new Mesh(),
                () => GameObject3D.ChildrenToIGameObject3D(Children),
                () =>
                {
                    var position = new Vector3(0, 0, 0);
                    var weakPosition = GetComponent<IHasPosition>();
                    if (weakPosition.IsAlive)
                    {
                        position = (weakPosition.Target as IHasPosition).Position;
                    }
                    return position;
                },
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

        public static FloorPlan Create(Vector3 position, Item Parent)
        {
            var hasPosition = new HasPosition(position);
            return new FloorPlan(hasPosition, Parent);
        }

        override protected IItem GetClonedItem()
        {
            return new FloorPlan();
        }

        override public void Destroy()
        {
            var weakFloorPlanDimension = this.GetComponent<IHasDimension>();
            Trace.Log($"Destroying FloorPlan {Name} parent :: {Parent}");
            var children = Parent.Children;
            base.Destroy();
            FloorPlanStrategy.AdjustUpperFloors(children, NamingStrategy.GetItemNameNumber(this.Name), -1, -((IHasDimension)weakFloorPlanDimension.Target).Height);
        }
    }
}