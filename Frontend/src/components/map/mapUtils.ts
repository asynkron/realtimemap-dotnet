import mapboxgl, { GeoJSONSource } from "mapbox-gl";

// const b = map.getSource('assets') as GeoJSONSource;
//   b.setData(data as any);

type GeoJSONSourceData = GeoJSON.Feature<GeoJSON.Geometry> | GeoJSON.FeatureCollection<GeoJSON.Geometry>;

export const trySetGeoJsonSource = (map: mapboxgl.Map, sourceId: string, data: GeoJSONSourceData) => {
  const source = map.getSource(sourceId) as GeoJSONSource;

  if (source) {
    source.setData(data);
  }
}
