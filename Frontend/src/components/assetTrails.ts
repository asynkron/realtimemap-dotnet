import { GetTrail } from "@/services/api-trail";
import mapboxgl, { GeoJSONSource } from "mapbox-gl";

export const addAssetTrails = async (map: mapboxgl.Map) => {

  let currentlySelectedAssetId: string | null = null;

  function getAssetTrailSource() {
    return map.getSource('asset-route') as GeoJSONSource;
  }

  async function drawTrail(assetId: string) {
    const trail = await GetTrail(assetId);
    const source = getAssetTrailSource();

    source.setData({
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

  setInterval(() => drawCurrentlySelectedAssetsTrail(), 2500);

};
