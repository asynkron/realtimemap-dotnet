import mapboxgl, { GeoJSONSource } from "mapbox-gl"
import turfCircle from "@turf/circle"

export interface Geofence {
  long: number;
  lat: number;
  radiusInMeters: number;
}

const geofencesSourceId = 'geofences';

export const addGeofencesSource = (map: mapboxgl.Map) => {
  map.addSource(geofencesSourceId, {
    type: "geojson",
    data: {
      type: "FeatureCollection",
      features: []
    }
  });
}

export const addGeofencesLayer = (map: mapboxgl.Map) => {
  map.addLayer({
    id: "geofences",
    type: "line",
    source: geofencesSourceId,
    layout: {},
    paint: {
      "line-color": "#9c27b0",
      "line-width": 5
    }
  });
}

export const onGeofencingSourceLoaded = (map: mapboxgl.Map, callback: () => void) => {
  map.on('sourcedata', () => {
    if (map.getSource(geofencesSourceId) && map.isSourceLoaded(geofencesSourceId)) {
      callback();
    }
  });
}

export const setGeofences = (map: mapboxgl.Map, geofences: Geofence[]) => {
  const source = map.getSource(geofencesSourceId) as GeoJSONSource;

  if (source) {
    source.setData({
      type: "FeatureCollection",
      features: geofences.map(mapGeofenceToPolygon)
    });
  }
}

const mapGeofenceToPolygon = (geofence: Geofence): GeoJSON.Feature<GeoJSON.Polygon, GeoJSON.GeoJsonProperties> => {
  const radiusInKilometers = geofence.radiusInMeters / 1000;
  return turfCircle([geofence.long, geofence.lat], radiusInKilometers, {
    steps: 25,
    units: 'kilometers'
  });
}
