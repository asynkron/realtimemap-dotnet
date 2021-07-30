using GeoCoordinatePortable;

namespace Backend.Models
{
    public class CircularGeofence
    {
        private readonly double _radiusInMetres;
        private readonly GeoCoordinate _coordinate;

        public CircularGeofence(GeoPoint centralPoint, double radiusInMetres)
        {
            _radiusInMetres = radiusInMetres;

            _coordinate = new GeoCoordinate(centralPoint.Latitude, centralPoint.Longitude);
        }

        public bool IncludesLocation(double latitude, double longitude)
        {
            return _coordinate.GetDistanceTo(new GeoCoordinate(latitude, longitude)) <= _radiusInMetres;
        }
    }
}