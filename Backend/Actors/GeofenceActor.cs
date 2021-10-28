using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Models;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
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
                                Message = $"{position.VehicleId} from {_organizationName} entered the zone {_circularGeofence.Name}"
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
                                Message = $"{position.VehicleId} from {_organizationName} left the zone {_circularGeofence.Name}"
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
}