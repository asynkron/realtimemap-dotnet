# Real-time Map

_Real-time Map_ displays real-time positions of public transport vehicles in Helsinki. It's a showcase for Proto.Actor - an ultra-fast distributed actors solution for Go, C#, and Java/Kotlin.

The app features:
* Real-time positions of vehicles.
* Vehicle trails.
* Geofencing notifications (vehicle entering and exiting the area).
* Vehicles in geofencing areas per public transport company.
* Horizontal scaling.

The goals of this app are:
1. Showing what Proto.Actor can do.
1. Presenting a semi-real-world use case of the distributed actor model.
1. Aiding people in learning how to use Proto.Actor.

**[Find more about Proto.Actor here.](https://proto.actor/)**

![image](https://user-images.githubusercontent.com/1219044/132653003-58733735-f49a-4615-adb5-36552b1415c1.png)


## Running the app

Configure Mapbox:
1. Create an account on [Mapbox](https://www.mapbox.com/).
1. Copy a token from: main dashboard / access tokens / default public token.
1. Paste the token in `Frontend\src\config.ts`.

Start frontend:
```
cd Frontend
npm install
npm run serve
```

Start Backend:
```
cd Backend
dotnet run
```

The app is available on [localhost:8080](http://localhost:8080/).

### Kubernetes

In order to deploy to Kubernetes and use the Kubernetes cluster provider, see [Deploying to Kubernetes](deploying-to-kubernetes)

## How does it work?


### Prerequisites

To understand how this app works, it's highly recommended to understand the basics of the following technologies:
1. [.NET / C#](https://dotnet.microsoft.com/)
1. [ASP.NET](https://dotnet.microsoft.com/apps/aspnet)
1. [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)

It would also be great, if you knew the basics the of actor model, Proto.Actor, and _virtual actors_ (also called _grains_). If you don't you can try reading this document anyway: we'll try to explain the essential parts as we go.

[Learn more about Proto.Actor here.](https://proto.actor/docs/)

[Learn more about virtual actors here.](https://proto.actor/docs/cluster/)

Also, since this app aims to provide horizontal scalability, this  document will assume, that we're running a cluster with two nodes.

One last note: this document is not a tutorial, but rather a documentation of this app. If you're learning Proto.Actor, you'll benefit the most by jumping between code and this document.


### Data source

Since this app is all about tracking vehicles, we need to get their positions from somewhere. In this app, the positions are received from high-frequency vehicle positioning MQTT broker from Helsinki Region Transport. More info on data:
* [Helsinki Region Transport - open data.](https://www.hsl.fi/en/hsl/open-data)
* [High-frequency positioning from Helsinki Region Transport.](https://digitransit.fi/en/developers/apis/4-realtime-api/vehicle-positions/)

This data is licensed under © Helsinki Region Transport 2021, Creative Commons BY 4.0 International

In our app, `Ingress` is a [hosted service](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio) responsible for subscribing to Helsinki Region Transport MQTT server and handling vehicle position updates.

### Vehicles

When creating a system with actors, is common to model real-world physical objects as actors. We'll start by modelling a vehicle since all the features depend on it. It will be implemented as a virtual actor (`VehicleActor`). It will be responsible for handling events related to that vehicle and remembering its state, e.g. its current position and position history.

Quick info on virtual actors:
1. Each virtual actor is of a specific kind, in this case, it's `Vehicle`.
2. Each virtual actor has an ID, in this case, it's an actual vehicle's number.
3. Virtual actors can have state, in this case, vehicle's current position and position history.
4. Virtual actors handle messages sent to them one by one, meaning we don't have to worry about synchronization.
5. Virtual actors are distributed in the cluster. In normal circumstances, a single virtual actor (in this case a specific vehicle) will be hosted on a single node.
6. We don't need to spawn (create) virtual actors explicitly. They will be spawned automatically on **one of the nodes** the first time a message is sent to them.
7. To communicate with a virtual actor we only need to know its kind and ID. We don't have to care about which node it's hosted on.

_Note: virtual actors are sometimes referred to as "grains" - terminology originating in the MS Orleans project._

The workflow looks like this:
1. `Ingress` receives an event from Helsinki Region Transport MQTT server.
1. `Ingress` reads vehicle's ID and its new position from the event and sends it to a vehicle in question.
1. `VehicleActor` processes the message.

![vehicles only in a cluster drawio](docs/hrt%20ingress%20vehicles.drawio.png)

Notice, that in the above diagram, vehicles (virtual actors) are distributed between two nodes, however, `Ingress` is present in both of the nodes.

### Organizations

Let's consider the following feature: in this app, each vehicle belongs to an organization. Each organization has a specified list of geofences. In our case, a geofence is simply a circular area somewhere in Helsinki, e.g. airport, railway square, or downtown. Users should receive notifications when a vehicle enters or leaves a geofence (from that vehicle's organization).

For that purpose, we'll implement a virtual actor to model an organization (`OrganizationActor`). When activated, `OrganizationActor` will spawn (create) a child actor for each configured geofence (`GeofenceActor`).

Quick info on child actors:
1. In contrast to virtual actors, we need to manually spawn a child actor.
1. Child actor is hosted on the same node as the virtual actor that spawned it. Communication in the same node is quite fast, as neither serialization nor remote calls are needed.
1. After spawning a child actor, its PID is stored in the parent actor. Think of it as a reference to an actor: you can use it to communicate with it.
1. Since parent actor has PIDs of their child actors, it can easily communicate with them.
1. Technically, we can communicate with a child actor using their PID from anywhere within the cluster. This functionality is not utilized in this app, though.
1. Child actor lifecycle is bound to the parent that spawned it. It will be stopped when parent gets stopped.

The workflow looks like this:
1. `VehicleActor` receives a position update.
1. `VehicleActor` forwards position update to its organization.
1. `OrganizationActor` forwards position update to all its geofences.
1. Each `GeofenceActor` keeps track of which vehicles are already inside the zone. Thanks to this, `GeofenceActor` can detect if a vehicle entered or left the geofence.

![organization geofences only drawio](docs/vehicles%20organizations%20geofences.drawio.png)

### Viewport, positions and notifications

So far we've only sent messages to and between actors. However, if we want to send notifications to actual users, we need to find a way to communicate between the actor system and the outside world. In this case, we'll use:
1. SignalR to push notifications to users.
1. Proto.Actor's ability to broadcast messages cluster-wide by means of the `EventStream`.

[Learn more about EventStream here.](https://proto.actor/docs/eventstream/)

First we'll introduce the `UserActor`. It models the user's ability to view positions of vehicles and geofencing notifications. `UserActor` will be implemented as an actor (i.e. non-virtual actor).

Quick info on actors:
1. An actor's lifecycle is not managed, meaning we have to manually spawn and stop them.
1. An actor will be hosted on the same node that spawned it. Like with child actors, this gives us performance benefits when communicating with an actor.
1. Since an actor lives in the same node (i.e. the same process), we can pass .NET references to it. It will come in handy in this feature.
1. After spawning an actor, we receive its PID. Think of it as a reference to an actor: you can use it to communicate with it. Don't lose it!
1. Technically, we can communicate with an actor using their PID from anywhere within the cluster. This functionality is not utilized in this app, though.

The workflow looks like this:
1. When user connects to SignalR hub, user actor is spawned to represent the connection. Delegates to send the positions and notifications back to the user are provided to the actor upon creation.
1. When starting, the user actor subscribes to `Position` and `Notification` events being broadcasted through the `EventStream`. It also makes sure to unsubscribe when stopping.
1. When user pans the map, updates of the viewport (south-west and north-east coords of the visible area on the map) are sent to the user actor through SignalR connection. The actor keeps track of the viewport to filter out positions.
1. Geofence actor detects vehicle entering or leaving the geofencing zone. These events are broadcasted as `Notification` message to all cluster members and available through `EventStream` on each member. Same goes for `Position` events broadcasted from vehicle actor.
1. When receiving a `Notification` message, user actor will push it to the user via SignalR connection. It will do the same with `Position` message provided it is within currently visible area on the map. The positions are sent in batches to improve performance.

![viewport and event stream drawio](docs/viewport%20and%20event%20stream.drawio.png)

### Getting vehicles currently in an organization's geofences

Let's consider the following feature: when a user requests it, we want to list all vehicles currently present in a selected organization's geofences.

This one is quite easy, as actors support request/response pattern.

The workflow looks like that:
1. A user calls API to get organization's details (including geofences and vehicles in them).
1. `OrganizationController` asks `OrganizationActor` for the details.
1. `OrganizationActor` asks each of its geofences (`GeofenceActor`) for a list of vehicles in these geofences.
1. Each `GeofenceActor` returns that information.
1. `OrganizationActor` combines that information into a response and returns it to `OrganizationController`.
1. `OrganizationController` maps and returns the results to the user.

![getting vehicles in the org drawio](docs/getting%20vehicles%20in%20geofence.drawio.png)

## Deploying to Kubernetes

Prerequisites:
* [kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl) with configured connection to your cluster, e.g. Docker desktop
* [Helm](https://helm.sh/docs/intro/install/)
* [nginx ingress controller](https://kubernetes.github.io/ingress-nginx/deploy/) configured on your cluster

The Realtime Map sample can be configured to use Kubernetes as cluster provider. The attached [chart](chart) contains definition for the deployment. You will need to supply some additional configuration, so create a new values file, e.g. `my-values.yaml` in the root directory of the sample. Example contents:

```yaml
frontend:
  config:
    mapboxToken: SOME_TOKEN # provide your MapBox token here

backend:
  config:
    # since all backend pods need to share MQTT subscription, 
    # generate a new guid and provide it here (no dashes)
    sharedSubscriptionGroupName: SOME_GUID 

ingress:
  # specify localhost if on Docker Desktop, 
  # otherwise add a domain for the sample to your DNS and specify it here
  host: localhost 
```

Create the namespace and deploy the sample:

```bash
kubectl create namespace realtimemap

helm upgrade --install -f my-values.yml --namespace realtimemap realtimemap ./chart
```

*NOTE:* the chart creates a new role in the cluster with permissions to access Kubernetes API required for the cluster provider.

The above config will deploy the sample without TLS on the ingress, additional configuration is required to secure the connection.