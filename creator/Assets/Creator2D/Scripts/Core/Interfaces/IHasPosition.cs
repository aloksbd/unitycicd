using UnityEngine;

namespace ObjectModel
{
    public interface IHasPosition : IItemComponent
    {
        Vector3 Position { get; set; }
        void MoveTo(Vector3 newPosition);
        void MoveBy(Vector3 vector3);
    }

    public class HasPosition : IHasPosition
    {
        private Vector3 _position = new Vector3(0, 0, 0);
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public HasPosition() { }

        public HasPosition(Vector3 position)
        {
            _position = position;
        }

        public void MoveTo(Vector3 newPosition)
        {
            _position = newPosition;
        }

        public void MoveBy(Vector3 vector3)
        {
            _position += vector3;
        }

        public IItemComponent Clone()
        {
            return new HasPosition(_position);
        }
    }
}