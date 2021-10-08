import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

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
  onPositions(callback: (positions: PositionsDto) => void);
  onNotification(callback: (notification: string) => void);
  disconnect(): Promise<void>;
}

export const connectToHub = async (): Promise<HubConnection> => {

  const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/events')
    .configureLogging(LogLevel.Debug)
    .withAutomaticReconnect()
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

    onPositions(callback: (PositionsDto) => void) {
      connection.on("positions", (positions: PositionsDto) => {
        callback(positions);
      });
    },

    onNotification(callback: (notification: string) => void) {
      connection.on("notification", (notification: string) => {
        callback(notification);
      });
    },

    async disconnect() {
      await connection.stop();
    }
  }

};
