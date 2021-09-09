# Real-time Map

_Real-time Map_ displays real-time positions of public transport in Helsinki. It's a showcase for Proto.Actor - an ultra-fast distributed actors solution for Go, C#, and Java/Kotlin.

**[Find more about Proto.Actor here.](https://proto.actor/)**

Showcase features:
* real-time positions of vehicles
* vehicle trails
* geofencing notifications (vehicle entering and exiting the area)
* vehicles in geofencing areas per public transport company


![image](https://user-images.githubusercontent.com/1219044/132653003-58733735-f49a-4615-adb5-36552b1415c1.png)


## Running

Configure Mapbox:
1. Create an account on [Mapbox](https://www.mapbox.com/).
1. Copy a token from: main dashbaord / access tokens / default public token.
1. Paste the token in `Frontend\src\mapboxConfig.ts`.

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

Start Proxy:
```
cd Proxy
dotnet run
```

The app is available on [localhost:8080](http://localhost:8080/).

## Data source

The positions are received via high-frequency vehicle positioning MQTT broker from Helsinki Region Transport (HRT). More info on data:
* [Helsinki Region Transport - open data](https://www.hsl.fi/en/hsl/open-data)
* [High-frequency positioning from HRT](https://digitransit.fi/en/developers/apis/4-realtime-api/vehicle-positions/)


## HRT data license

© Helsinki Region Transport 2021
Creative Commons BY 4.0 International
