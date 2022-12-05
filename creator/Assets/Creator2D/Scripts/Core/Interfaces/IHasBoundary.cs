using System.Collections.Generic;
using UnityEngine;

namespace ObjectModel
{
    public struct GeoLocation
    {
        public double latitude;
        public double longitude;

        public Vector2 GetVector2()
        {
            // TODO: implement conversion logic
            return new Vector2(0, 0);
        }

        public static GeoLocation FromVector2(Vector2 vector)
        {
            // TODO: implement conversion logic
            return new GeoLocation();
        }
    }

    public interface IHasBoundary
    {
        // path for texture in resource
        List<Vector2> Boundary { get; }
        List<GeoLocation> GeoBoundary { get; }
        void SetBoundary(List<Vector2> boundary);
        void SetBoundary(List<GeoLocation> boundary);
    }

    public class HasBoundary : IHasBoundary
    {
        private List<Vector2> _boundary;
        public List<Vector2> Boundary { get => _boundary; }
        private List<GeoLocation> _geoBoundary;
        public List<GeoLocation> GeoBoundary { get => _geoBoundary; }

        public void SetBoundary(List<Vector2> boundary)
        {
            _boundary = boundary;

            foreach (var item in boundary)
            {
                _geoBoundary.Add(GeoLocation.FromVector2(item));
            }
        }

        public void SetBoundary(List<GeoLocation> boundary)
        {
            _geoBoundary = boundary;

            foreach (var item in boundary)
            {
                _boundary.Add(item.GetVector2());
            }
        }
    }
}