using UnityEngine;

namespace ObjectModel
{
    public interface IHasRotation : IItemComponent
    {
        Vector3 EulerAngles { get; set; }
        void RotateBy(Vector3 vector3);
    }

    public class HasRotation : IHasRotation
    {
        private Vector3 _eulerAngles = new Vector3(0, 0, 0);
        public Vector3 EulerAngles
        {
            get => _eulerAngles;
            set => _eulerAngles = value;
        }

        public HasRotation() { }

        public HasRotation(Vector3 eulerAngles)
        {
            _eulerAngles = eulerAngles;
        }

        public void RotateBy(Vector3 vector3)
        {
            _eulerAngles += vector3;
        }

        public IItemComponent Clone()
        {
            return new HasRotation(_eulerAngles);
        }
    }
}