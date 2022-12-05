using System;
using UnityEngine;

namespace ObjectModel
{
    public interface IHasMesh : IItemComponent
    {
        Mesh CreateMesh();
    }

    public class HasMesh : IHasMesh
    {
        private Func<Mesh> _createMesh;
        public Mesh CreateMesh()
        {
            return _createMesh();
        }

        public HasMesh(Func<Mesh> createMesh)
        {
            _createMesh = createMesh;
        }

        public void SetCreateMesh(Func<Mesh> createMesh)
        {
            _createMesh = createMesh;
        }

        public IItemComponent Clone()
        {
            return new HasMesh(_createMesh);
        }
    }
}