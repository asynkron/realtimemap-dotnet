using System.Collections.Generic;

namespace Backend.Actors
{
    public class VehiclePositionHistory
    {
        private const int Capacity = 100;
        
        // linked list is used for fast first item removing and inserting in chronological order
        private readonly LinkedList<Position> _positions = new();
        
        public IEnumerable<Position> Positions => _positions;
        
        public void Add(Position position)
        {
            var node = _positions.Last;

            // add and keep chronological order
            if (node == null || node.Value.Timestamp <= position.Timestamp)
            {
                _positions.AddLast(position);
            }
            else
            {
                while (node != null && node.Value.Timestamp > position.Timestamp) node = node.Previous;
                if (node == null)
                    _positions.AddFirst(position);
                else
                    _positions.AddBefore(node, position);
            }
            
            if (_positions.Count > Capacity)
                _positions.RemoveFirst();
        }
    }
}