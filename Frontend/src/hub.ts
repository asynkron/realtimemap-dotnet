import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';

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

export interface HubConnection {
  setViewport(swLng: number, swLat: number, neLng: number, neLat: number);
  onPositions(callback: (positions: PositionsDto) => any);
  onNotification(callback: (notification: string) => any);
  disconnect(): Promise<void>;
}

export const connectToHub = async (): Promise<HubConnection> => {

  const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/events')
    .configureLogging(LogLevel.Debug)
    .build();

  console.log('connecting');
  await connection.start();
  console.log('connected');

  connection.onclose(() => {
    console.log('connection closed');
  });

  return {
    async setViewport(
      swLng: number,
      swLat: number,
      neLng: number,
      neLat: number
    ) {
      await connection.send("SetViewport", swLng, swLat, neLng, neLat);
    },

    onPositions(callback: (PositionsDto) => void){
      connection.on("positions", (positions: PositionsDto) => {
        console.log(`Got batch of positions ${positions.positions.length}`);
        callback(positions);
      });
    },

    onNotification(callback: (notification: string) => any) {
      connection.on("notification", (notification: string) => {
        console.log(`Got notification ${notification}`);
        callback(notification);
      });
    },

    async disconnect() {
      await connection.stop();
    }
  }

};
