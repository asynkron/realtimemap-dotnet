using Backend.Models;
using Proto.Cluster.PubSub;

namespace Backend.Actors;

public delegate Task SendPositionBatch(PositionBatch batch);

public delegate Task SendNotification(Notification notification);

public class UserActor : IActor
{
    private static readonly ILogger Logger = Log.CreateLogger<UserActor>();
    
    const int PositionBatchSize = 10;

    private readonly string _id;
    private readonly SendPositionBatch _sendPositionBatch;
    private readonly SendNotification _sendNotification;
    private readonly MapGrid _mapGrid;
    private readonly Viewport _viewport;
    private readonly List<Position> _positions = new(PositionBatchSize);
    private string[] _subscribedTopics = {};

    public UserActor(
        string id,
        SendPositionBatch sendPositionBatch, 
        SendNotification sendNotification, 
        MapGrid mapGrid)
    {
        _id = id;
        _sendPositionBatch = sendPositionBatch;
        _sendNotification = sendNotification;
        _mapGrid = mapGrid;
        _viewport = new Viewport();
    }

    public async Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Started:
                await SubscribeToNotifications(context);
                break;

            case Position position:
                await OnPosition(position);
                break;

            case Notification notification:
                await _sendNotification(notification);
                break;

            case UpdateViewport updateViewport:
                _viewport.MergeFrom(updateViewport.Viewport);
                await SubscribeViewportPositions(context);
                break;
            
            case Stopping:
                await Task.WhenAll(UnsubscribeViewportPositions(context), UnsubscribeFromNotifications(context));
                break;
        }
    }

    private async Task SubscribeViewportPositions(IContext context)
    {
        var viewportTopics = _mapGrid.TopicsFromViewport(_viewport);
        
        var newTopics = viewportTopics.Except(_subscribedTopics).ToArray();
        var subscribeTasks = newTopics.Select(t => (Task)context.Cluster().Subscribe(t, context.Self)).ToArray();

        var topicsToUnsubscribe = _subscribedTopics.Except(viewportTopics).ToArray();
        var unsubscribeTasks = topicsToUnsubscribe.Select(t => (Task)context.Cluster().Unsubscribe(t, context.Self)).ToArray();
        
        Logger.LogDebug("User {UserId} subscribed to {NewTopics} and unsubscribed from {UnsubscribedTopics}",
            _id, newTopics, topicsToUnsubscribe);
        
        await Task.WhenAll(subscribeTasks.Concat(unsubscribeTasks));
        
        _subscribedTopics = viewportTopics;
    }
    
    private async Task UnsubscribeViewportPositions(IContext context)
    {
        var unsubscribeTasks = _subscribedTopics.Select(t => (Task)context.Cluster().Unsubscribe(t, context.Self)).ToArray();

        Logger.LogDebug("User {UserId} unsubscribed from {UnsubscribedTopics}", _id, _subscribedTopics);
        
        await Task.WhenAll(unsubscribeTasks);
        _subscribedTopics = Array.Empty<string>();
    }

    private async Task SubscribeToNotifications(IContext context)
    {
        await context.Cluster().Subscribe("notifications", context.Self);
    }

    private async Task UnsubscribeFromNotifications(IContext context)
    {
        await context.Cluster().Unsubscribe("notifications", context.Self);
    }


    private async Task OnPosition(Position position)
    {
        if (position.IsWithinViewport(_viewport))
        {
            _positions.Add(position);
            if (_positions.Count >= PositionBatchSize)
            {
                await _sendPositionBatch(new PositionBatch {Positions = {_positions}});
                _positions.Clear();
            }
        }
    }
}

public record UpdateViewport(Viewport Viewport);