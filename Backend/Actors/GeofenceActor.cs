using Backend.Models;
using Proto.Cluster.PubSub;

namespace Backend.Actors;

public class GeofenceActor : IActor
{
    private readonly string _organizationName;
    private readonly CircularGeofence _circularGeofence;
    private readonly HashSet<string> _vehiclesInZone;
    private readonly IPublisher _publisher;

    public GeofenceActor(string organizationName, CircularGeofence circularGeofence, Cluster cluster)
    {
        _organizationName = organizationName;
        _circularGeofence = circularGeofence;
        _vehiclesInZone = new HashSet<string>();
        _publisher = cluster.Publisher();
    }
        
    public async Task ReceiveAsync(IContext context)
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
                        await _publisher.Publish("notifications", new Notification
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
                        await _publisher.Publish("notifications", new Notification
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
    }
}