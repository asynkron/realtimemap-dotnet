import { HubConnectionBuilder, LogLevel } from "@aspnet/signalr";

export const connectToGeofencingNotificationsHub = async (notificationReceived: (notification: string) => void) => {

  const connection = new HubConnectionBuilder()
    .withUrl('http://localhost:5000/geofencingNotificationsHub')
    .configureLogging(LogLevel.Debug)
    .build();

  await connection.start();

  console.log("started geofencing notifications hub");

  connection.on("notification", (notification: string) => {
    notificationReceived(notification);
  });

};
