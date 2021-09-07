using System.Collections.Generic;

namespace Backend.Actors
{
    public class VehiclePositionHistory
    {
        private const int Capacity = 100;
        
        // linked list is used for fast first item removing
        private readonly LinkedList<Position> _positions = new();
        
        public IEnumerable<Position> Positions => _positions;
        
        public void Add(Position position)
        {
            _positions.AddLast(position);

            if (_positions.Count > Capacity)
            {
                _positions.RemoveFirst();
            }
        }
    }
}