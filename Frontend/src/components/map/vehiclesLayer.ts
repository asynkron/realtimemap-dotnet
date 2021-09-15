import { GeoJSONSource } from "mapbox-gl";
import { AssetStates, mapAssetsToGeoJson } from "./assetStates";
import { getBoundsWithMargin } from './boundsWithMargin';
import { trySetGeoJsonSource } from "./mapUtils";

export const showMarkerLevel = 12;

export const addVehiclesLayer = (map: mapboxgl.Map, assetStates: AssetStates) => {

  map.addSource('assets', {
    type: 'geojson',
    data: {
      type: 'FeatureCollection',
      features: [],
    },
  });

  map.addLayer({
    id: 'asset-layer',
    type: 'symbol',
    source: 'assets',
    minzoom: showMarkerLevel,
    layout: {
      'icon-image': ['get', 'icon'],
      'icon-size': ['interpolate', ['linear'], ['zoom'], 9, 0.05, 15, 0.4],
      'icon-allow-overlap': true,
      'icon-rotate': ['get', 'course'],
    },
  });

  map.loadImage('/bus.png', (error, image) => {
    if (error) throw error;
    map.addImage('moving', image);
  });

  map.loadImage('/doorsopen.png', (error, image) => {
    if (error) throw error;
    map.addImage('doorsopen', image);
  });

  setInterval(
    () => updateAssetLayers(map, assetStates),
    1000
  );

}

function updateAssetLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  if (map.getZoom() < showMarkerLevel) {
    return;
  }

  // expand viewport so we ingest things just outside the bounds also.
  const biggerBounds = getBoundsWithMargin(map);

  const data = mapAssetsToGeoJson(assetStates, (asset) =>
    biggerBounds.contains(asset.currentPosition)
  );

  trySetGeoJsonSource(map, "assets", data as any);
}
