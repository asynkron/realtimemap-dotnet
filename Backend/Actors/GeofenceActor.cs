using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Models;
using Proto;

namespace Backend.Actors
{
    public class GeofenceActor : IActor
    {
        private readonly string _name;
        private readonly CircularGeofence _circularGeofence;
        private readonly HashSet<string> _vehiclesInZone;

        public GeofenceActor(string name, CircularGeofence circularGeofence)
        {
            _name = name;
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
                            Console.WriteLine($"{position.VehicleId} entered the zone {_name}");
                        }
                    }
                    else
                    {
                        if (vehicleAlreadyInZone)
                        {
                            _vehiclesInZone.Remove(position.VehicleId);
                            Console.WriteLine($"{position.VehicleId} left the zone {_name}");
                        }   
                    }

                    break;
                }
            }

            return Task.CompletedTask;
        }
    }
}