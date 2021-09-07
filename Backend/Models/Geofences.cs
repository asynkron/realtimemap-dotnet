namespace Backend.Models
{
    public static class Geofences
    {
        public static readonly CircularGeofence Airport = new(
            name: "Airport",
            centralPoint: new GeoPoint(24.96907, 60.31146),
            radiusInMetres: 2000);
        
        public static readonly CircularGeofence Downtown = new(
            name: "Downtown",
            centralPoint: new GeoPoint(24.941068845053014, 60.16422983026082),
            radiusInMetres: 1700);
        
        public static readonly CircularGeofence RailwaySquare = new(
            name: "Railway Square",
            centralPoint: new GeoPoint(24.943936, 60.171285),
            radiusInMetres: 150);
        
        public static readonly CircularGeofence LauttasaariIsland = new(
            name: "Lauttasaari island",
            centralPoint: new GeoPoint(24.873788, 60.158536),
            radiusInMetres: 1400);
        
        public static readonly CircularGeofence LaajasaloIsland = new(
            name: "Laajasalo island",
            centralPoint: new GeoPoint(25.052851825093114, 60.16956184470527),
            radiusInMetres: 2200);
        
        public static readonly CircularGeofence KallioDistrict = new(
            name: "Kallio district",
            centralPoint: new GeoPoint(24.953588638997264, 60.18260263288996),
            radiusInMetres: 600);
    }
}