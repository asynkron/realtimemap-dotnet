import { AssetStates, mapAssetsToGeoJson } from "./assetStates";
import { trySetGeoJsonSource } from "./mapUtils";
import { showMarkerLevel } from "./vehiclesLayer";

export const addVehicleClustersLayer = (map: mapboxgl.Map, assetStates: AssetStates) => {

  map.addSource('assets-cluster', {
    type: 'geojson',
    data: {
      type: 'FeatureCollection',
      features: [],
    },
    cluster: true,
    clusterMaxZoom: showMarkerLevel, // Max zoom to cluster points on
    clusterRadius: 50, // Radius of each cluster when clustering points (defaults to 50)
  });

  map.addLayer({
    id: 'clusters',
    type: 'circle',
    source: 'assets-cluster',
    maxzoom: showMarkerLevel,
    filter: ['has', 'point_count'],
    paint: {
      'circle-color': [
        'step',
        ['get', 'point_count'],
        '#9c313a',
        100,
        '#0c7186',
        750,
        '#73a824',
      ],
      'circle-radius': ['step', ['get', 'point_count'], 20, 100, 30, 750, 40],
    },
  });

  map.addLayer({
    id: 'cluster-count',
    type: 'symbol',
    source: 'assets-cluster',
    maxzoom: showMarkerLevel,
    filter: ['has', 'point_count'],
    layout: {
      'text-field': '{point_count_abbreviated}',
      'text-font': ['DIN Offc Pro Medium', 'Arial Unicode MS Bold'],
      'text-size': 12,
    },
    paint: {
      'text-color': '#ffffff',
    },
  });

  setInterval(
    () => updateClusterLayers(map, assetStates),
    5000
  );

}

function updateClusterLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  const data = mapAssetsToGeoJson(assetStates, () => true);
  trySetGeoJsonSource(map, 'assets-cluster', data as any);
}
