import {HubConnectionBuilder, LogLevel} from '@aspnet/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/positionhub')
  .configureLogging(LogLevel.Debug)
  .build();

export interface PositionsDto {
  positions: PositionDto[];
}
export interface PositionDto {
  vehicleId: string;
  longitude: number;
  latitude: number;
  heading: number;
  speed: number;
  doorsOpen: boolean;
}

export default {
  async connect(lng1: number, lat1: number, lng2: number, lat2: number, callback: (value: any) => void) {

    console.log("connecting");
    await connection.start()
    console.log("connected");

    connection.onclose(() => {
      console.log('connection closed');
    })

    connection
      .stream('Connect')
      .subscribe({
        next: (batch: PositionsDto) => {
          console.log(`Got batch of events ${batch.positions.length}`);
          for (const e of batch.positions) {
            callback(e);
          }
        },
        complete: () => {
          //pass
          console.log("signalr completed");
        },
        error: x => {
          //pass
          console.error("signalr error", x)
        }
      });
  },
  async setViewport(lng1: number, lat1: number, lng2: number, lat2: number) {
    console.log('setting viewport', lng1, lat1, lng2, lat2);
    await connection.send('SetViewport', lng1, lat1, lng2, lat2);
  },

  async getTrail(assetId: string, callback: (value: PositionsDto) => void) {
    const trail = await connection.invoke('GetTrail', assetId);
    callback(trail);
  }
};


