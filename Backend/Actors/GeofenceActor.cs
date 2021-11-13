using Backend.Models;

namespace Backend.Actors;

public class GeofenceActor : IActor
{
    private readonly string _organizationName;
    private readonly CircularGeofence _circularGeofence;
    private readonly Cluster _cluster;
    private readonly HashSet<string> _vehiclesInZone;

    public GeofenceActor(string organizationName, CircularGeofence circularGeofence, Cluster cluster)
    {
        _organizationName = organizationName;
        _circularGeofence = circularGeofence;
        _cluster = cluster;
        _vehiclesInZone = new HashSet<string>();
    }
        
    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Position position:
            {
                var vehicleAlreadyInZone = _vehiclesInZone.Contains(position.VehicleId);
                    
                if (_circularGeofence.IncludesLocation(position.Latitude, position.Longitude))
                {
                    if (!vehicleAlreadyInZone)
                    {
                        _vehiclesInZone.Add(position.VehicleId);
                        _cluster.MemberList.BroadcastEvent(new Notification
                        {
                            VehicleId = position.VehicleId,
                            OrgId = position.OrgId,
                            OrgName = _organizationName,
                            ZoneName = _circularGeofence.Name,
                            Event = GeofenceEvent.Enter
                        });
                    }
                }
                else
                {
                    if (vehicleAlreadyInZone)
                    {
                        _vehiclesInZone.Remove(position.VehicleId);
                        _cluster.MemberList.BroadcastEvent(new Notification
                        {
                            VehicleId = position.VehicleId,
                            OrgId = position.OrgId,
                            OrgName = _organizationName,
                            ZoneName = _circularGeofence.Name,
                            Event = GeofenceEvent.Exit
                        });
                    }   
                }

                break;
            }
            case GetGeofencesRequest detailsRequest:
            {
                var geofenceDetails = new GeofenceDetails
                {
                    Name = _circularGeofence.Name,
                    RadiusInMeters = _circularGeofence.RadiusInMetres,
                    Longitude = _circularGeofence.Coordinate.Longitude,
                    Latitude = _circularGeofence.Coordinate.Latitude,
                    OrgId = detailsRequest.OrgId,
                    VehiclesInZone = {_vehiclesInZone}
                };
                    
                context.Respond(geofenceDetails);

                break;
            }
        }

        return Task.CompletedTask;
    }
}