import { AssetConnection } from "@/signalr-hub";
import { throttle } from "lodash";

export const handleViewportUpdates = (map: mapboxgl.Map, assetConnection: AssetConnection) => {

  const throttledUpdateViewport = throttle(setViewport, 1000);

  map.on('zoomend', () => {
    throttledUpdateViewport(map, assetConnection);
  });

  map.on('move', () => {
    throttledUpdateViewport(map, assetConnection);
  });

  setTimeout(
    () => setViewport(map, assetConnection),
    500
  );

}

function setViewport(map: mapboxgl.Map, assetConnection: AssetConnection) {
  const bounds = map.getBounds();
  const sw = bounds.getSouthWest();
  const ne = bounds.getNorthEast();
  assetConnection.setViewport(sw.lng, sw.lat, ne.lng, ne.lat);
}
