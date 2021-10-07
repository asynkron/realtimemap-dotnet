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

export interface PositionsHubConnection {
  setViewport(swLng: number, swLat: number, neLng: number, neLat: number);
  disconnect(): Promise<void>;
}

export const connectToPositionsHub = async (callback: (value: PositionDto) => void): Promise<PositionsHubConnection> => {

  const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/positionhub')
    .configureLogging(LogLevel.Debug)
    .build();

  console.log('connecting');
  await connection.start();
  console.log('connected');

  connection.onclose(() => {
    console.log('connection closed');
  });

  connection.on("positions", (positions: PositionsDto) => {
    console.log(`Got batch of positions ${positions.positions.length}`);
    for (const position of positions.positions) {
      callback(position);
    }
  });

  return {
    async setViewport(
      swLng: number,
      swLat: number,
      neLng: number,
      neLat: number
    ) {
      await connection.send('SetViewport', swLng, swLat, neLng, neLat);
    },

    async disconnect() {
      await connection.stop();
    }
  }

};
