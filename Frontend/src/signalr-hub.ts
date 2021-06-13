import {HubConnectionBuilder, LogLevel} from '@aspnet/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/positionhub')
  .configureLogging(LogLevel.Debug)
  .build();

export interface PositionDto {
  vehicleId: string;
  longitude: number;
  latitude: number;
  heading: number;
  speed: number;
}

export default {
  async connect(lng1: number, lat1: number, lng2: number, lat2: number, callback: (value: any) => void) {

    console.log("connecting");
    await connection.start()
    console.log("connected");

    connection
      .stream('Connect', lng1, lat1, lng2, lat2)
      .subscribe({
        next: (batch: PositionDto[]) => {
          console.log(`Got batch of events ${batch.length}`);
          for (const e of batch) {
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
    await connection.send('SetViewport', lng1, lat1, lng2, lat2);
  }
};


