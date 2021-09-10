using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
    public class ViewportActor : IActor
    {
        private readonly ChannelWriter<Position> _positionChannel;
        private readonly Viewport _viewport;

        public ViewportActor(ChannelWriter<Position> positionChannel)
        {
            _positionChannel = positionChannel;
            _viewport = new Viewport();
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Position position:
                    await OnPosition(position);
                    break;
                
                case UpdateViewport updateViewport:
                    await OnUpdateViewport(context, updateViewport);
                    break;
            }
        }

        private async Task OnPosition(Position position)
        {
            if (position.IsWithinViewport(_viewport))
            {
                await _positionChannel.WriteAsync(position);
            }
        }
        
        private async Task OnUpdateViewport(IContext context, UpdateViewport updateViewport)
        {
            _viewport.MergeFrom(updateViewport.Viewport);

            var positionsInViewport = await context
                .Cluster()
                .GetGlobalViewportActor()
                .GetPositionsInViewport(_viewport, CancellationToken.None);
            
            foreach (var position in positionsInViewport.Positions)
            {
                await _positionChannel.WriteAsync(position);
            }
        }
    }

    public class UpdateViewport
    {
        public Viewport Viewport { get; set; }
    }
}