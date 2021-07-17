using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Proto;

namespace Backend.Actors
{
    public class StreamActor : IActor
    {
        private readonly Channel<Position> _positions;

        public StreamActor(Channel<Position> positions)
        {
            _positions = positions;
        }

        public async Task ReceiveAsync(IContext context)
        {
            if (context.Message is Position position)
            {
                Console.WriteLine("Got position " + position);
                //Apply bounds checks / filtering here
                await _positions.Writer.WriteAsync(position);
            }
        }
    }
}