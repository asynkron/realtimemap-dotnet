import { GetTrail } from "@/components/map/api-trail";
import mapboxgl from "mapbox-gl";
import { trySetGeoJsonSource } from "./mapUtils";

export const addVehicleTrailLayer = async (map: mapboxgl.Map) => {

  map.addSource('asset-route', {
    type: 'geojson',
    data: {
      type: 'Feature',
      properties: {},
      geometry: {
        type: 'LineString',
        coordinates: [],
      },
    },
  });

  map.addLayer({
    id: 'asset-route',
    type: 'line',
    source: 'asset-route',
    layout: {
      'line-join': 'round',
      'line-cap': 'round',
    },
    paint: {
      'line-color': '#ed4981',
      'line-width': 8,
    },
  });

  let currentlySelectedAssetId: string | null = null;

  async function drawTrail(assetId: string) {
    const trail = await GetTrail(assetId);

    trySetGeoJsonSource(map, 'asset-route', {
      type: 'Feature',
      properties: {},
      geometry: {
        type: 'LineString',
        coordinates: trail.positions.map((position) => {
          return [position.longitude, position.latitude];
        }),
      },
    });
  }

  async function drawCurrentlySelectedAssetsTrail() {
    if (currentlySelectedAssetId) {
      drawTrail(currentlySelectedAssetId);
    }
  }

  map.on('click', 'asset-layer', async (e: any) => {
    const features = map.queryRenderedFeatures(e.point);
    const feature = features[0];
    if (feature != null && feature.properties != null) {
      currentlySelectedAssetId = feature.properties['asset-id'];
      await drawCurrentlySelectedAssetsTrail();
    }
  });

  setInterval(
    () => drawCurrentlySelectedAssetsTrail(),
    2500
  );

}
