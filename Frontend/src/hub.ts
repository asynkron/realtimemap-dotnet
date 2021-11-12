import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import config from "@/config";

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

export interface NotificationDto {
  vehicleId: string;
  orgId: string;
  orgName: string;
  zoneName: string;
  event: "Enter" | "Exit";
}

export interface HubConnection {
  setViewport(swLng: number, swLat: number, neLng: number, neLat: number);
  onPositions(callback: (positions: PositionsDto) => void);
  onNotification(callback: (notification: NotificationDto) => void);
  disconnect(): Promise<void>;
}

export const connectToHub = async (): Promise<HubConnection> => {

  const connection = new HubConnectionBuilder()
    .withUrl(config.backendUrl + "/events")
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

    onNotification(callback: (notification: NotificationDto) => void) {
      connection.on("notification", (notification: NotificationDto) => {
        callback(notification);
      });
    },

    async disconnect() {
      await connection.stop();
    }
  }

};
