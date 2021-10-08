using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Proto;
using Proto.Cluster;

namespace Backend.Actors
{
    public delegate Task SendPositionBatch(PositionBatch batch);

    public delegate Task SendNotification(Notification notification);

    public class ViewportActor : IActor
    {
        const int PositionBatchSize = 10;

        private readonly SendPositionBatch _sendPositionBatch;
        private readonly SendNotification _sendNotification;
        private readonly Viewport _viewport;
        private readonly List<Position> _positions = new(PositionBatchSize);
        private EventStreamSubscription<object> _notificationSubscription;
        private EventStreamSubscription<object> _positionSubscription;

        public ViewportActor(SendPositionBatch sendPositionBatch, SendNotification sendNotification)
        {
            _sendPositionBatch = sendPositionBatch;
            _sendNotification = sendNotification;
            _viewport = new Viewport();
        }

        public async Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Started:
                    SubscribeToEvents(context.System, context.Self);
                    break;

                case Position position:
                    await OnPosition(position);
                    break;

                case Notification notification:
                    await _sendNotification(notification);
                    break;

                case UpdateViewport updateViewport:
                    await OnUpdateViewport(context, updateViewport);
                    break;

                case Stopping:
                    UnsubscribeEvents(context);
                    break;
            }
        }


        private void SubscribeToEvents(ActorSystem system, PID self)
        {
            // do not try to process the events in the handler, send to self instead
            // to avoid concurrency issues

            _notificationSubscription =
                system.EventStream.Subscribe<Notification>(
                    notification => system.Root.Send(self, notification));
            _positionSubscription =
                system.EventStream.Subscribe<Position>(
                    position => system.Root.Send(self, position));
        }

        private void UnsubscribeEvents(IContext context)
        {
            context.System.EventStream.Unsubscribe(_positionSubscription);
            _positionSubscription = null;
            context.System.EventStream.Unsubscribe(_notificationSubscription);
            _notificationSubscription = null;
        }


        private async Task OnPosition(Position position)
        {
            if (position.IsWithinViewport(_viewport))
            {
                _positions.Add(position);
                if (_positions.Count >= PositionBatchSize)
                {
                    await _sendPositionBatch(new PositionBatch { Positions = { _positions } });
                    _positions.Clear();
                }
            }
        }

        private async Task OnUpdateViewport(IContext context, UpdateViewport updateViewport)
        {
            _viewport.MergeFrom(updateViewport.Viewport);

            var positionsInViewport = await context
                .Cluster()
                .GetGlobalViewportActor()
                .GetPositionsInViewport(_viewport, CancellationToken.None);

            await _sendPositionBatch(positionsInViewport);
        }
    }

    public class UpdateViewport
    {
        public Viewport Viewport { get; set; }
    }
}