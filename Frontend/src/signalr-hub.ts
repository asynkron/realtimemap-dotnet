import {HubConnectionBuilder, LogLevel} from '@aspnet/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:5001/assethub')
  .configureLogging(LogLevel.Debug)
  .build();

export enum AssetType {
  Equipment = 0,
  Mini = 1,
  Vehicle= 2
}

export interface AssetStateEvent {
  assetId: string;
  assetType: AssetType;
  position: GpsData;
}

export interface GpsData{
  longitude: number;
  latitude: number;
  timestamp: number;
  course?: number;
  speed?: number;
}

export default {
  async connect(lng1: number, lat1: number, lng2: number, lat2: number, callback: (value: any) => void) {

    await connection.start()

    connection
      .stream('Connect', lng1, lat1, lng2, lat2)
      .subscribe({
        next: (batch: AssetStateEvent[]) => {
          console.log(`Got batch of events ${batch.length}`);
          for (const e of batch) {
            if (e.assetType != AssetType.Vehicle) {
              console.log(e);
            }
            callback(e);
          }
        },
        complete: () => {
          //pass
        },
        error: () => {
          //pass
        }
      });
  },
  async setViewport(lng1: number, lat1: number, lng2: number, lat2: number) {
    await connection.send('SetViewport', lng1, lat1, lng2, lat2);
  }
};


