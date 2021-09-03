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
        private readonly string _zoneName;
        private readonly string _organizationName;
        private readonly CircularGeofence _circularGeofence;
        private readonly HashSet<string> _vehiclesInZone;

        public GeofenceActor(string zoneName, string organizationName, CircularGeofence circularGeofence)
        {
            _zoneName = zoneName;
            _organizationName = organizationName;
            _circularGeofence = circularGeofence;
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
                            context.System.EventStream.Publish(new Notification
                            {
                                Message = $"{position.VehicleId} from {_organizationName} entered the zone {_zoneName}"
                            });
                        }
                    }
                    else
                    {
                        if (vehicleAlreadyInZone)
                        {
                            _vehiclesInZone.Remove(position.VehicleId);
                            context.System.EventStream.Publish(new Notification
                            {
                                Message = $"{position.VehicleId} from {_organizationName} left the zone {_zoneName}"
                            });
                        }   
                    }

                    break;
                }
                case GetGeofencesRequest detailsRequest:
                {
                    var geofenceDetails = new GeofenceDetails
                    {
                        Name = _zoneName,
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