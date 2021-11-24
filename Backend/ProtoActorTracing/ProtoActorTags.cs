﻿namespace Backend.ProtoActorTracing;

public static class ProtoActorTags
{
    /// <summary>
    /// Dns.GetHostName()
    /// </summary>
    public const string Hostname = "host.name";
    
    /// <summary>
    ///     GetType().Name on the message
    /// </summary>
    public const string MessageType = "proto.messagetype";

    /// <summary>
    ///     Message destination
    /// </summary>
    public const string TargetPID = "proto.targetpid";

    /// <summary>
    ///     Message origin
    /// </summary>
    public const string SenderPID = "proto.senderpid";

    /// <summary>
    ///     Current actor PID, when applicable (equals TargetPID when this is a receive span, or SenderId when this is a
    ///     sending span)
    /// </summary>
    public const string ActorPID = "proto.actorpid";

    /// <summary>
    ///     Type of the current actor, when applicable
    /// </summary>
    public const string ActorType = "proto.actortype";
}