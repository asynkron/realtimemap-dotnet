<template>
  <div>
    <div id="map"></div>
  </div>
</template>

<script lang="ts">
import { defineComponent, onMounted } from 'vue';
import assets, { PositionDto } from '../signalr-hub';
import mapboxgl, { GeoJSONSource } from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import { throttle } from 'lodash';
import { GetTrail } from './../services/api-trail';
import mapboxConfig from './../mapboxConfig';
import { addAssetTrails } from './assetTrails';

const showMarkerLevel = 12;
// const stepsInAnimation = 10;
const layerUpdateInterval = 5000; //every 5 sec, update the layers

//this must have this shape to be compatible with mapbox
interface VehiclePosition {
  lat: number;
  lng: number;
  heading: number;
}

interface VehicleState {
  vehicleId: string;
  steps: number;
  delta: VehiclePosition;
  currentPosition: VehiclePosition;
  speed: number;
  nextPosition: VehiclePosition;
  shouldAnimate: boolean;
  icon: string;
}

type AssetStates = { [vehicleId: string]: VehicleState };

function createAssetFromState(e: PositionDto): VehicleState {
  return {
    vehicleId: e.vehicleId,
    speed: 0,
    steps: 0,
    nextPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    currentPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    delta: { lat: 0, lng: 0, heading: 0 },
    shouldAnimate: false,
    icon: '',
  };
}

function updateAssetFromEvent(
  assetStates: AssetStates,
  positionDto: PositionDto
) {
  const assetState = assetStates[positionDto.vehicleId];

  assetState.currentPosition = {
    lat: positionDto.latitude,
    lng: positionDto.longitude,
    heading: positionDto.heading
  };

  // console.log(positionDto.vehicleId)
  // console.log(assetState.nextPosition);
  // console.log(positionDto);

  // const lng = positionDto.longitude - assetState.nextPosition.lng;
  // const lat = positionDto.latitude - assetState.nextPosition.lat;
  // let heading = positionDto.heading - assetState.nextPosition.heading;

  // if (lng != 0) {
  //   console.log(lng, lat);
  // }

  // //prevent full rotations when next and current course cross between 0 and 360
  // if (heading > 180) {
  //   heading -= 360;
  // }

  // if (heading < -180) {
  //   heading += 360;
  // }

  // assetState.steps = stepsInAnimation;
  // assetState.delta = {
  //   lng: lng / stepsInAnimation,
  //   lat: lat / stepsInAnimation,
  //   heading: heading / stepsInAnimation,
  // };
  // //console.log(assetState.delta );
  // assetState.currentPosition = assetState.nextPosition;
  // assetState.nextPosition = {
  //   lng: positionDto.longitude,
  //   lat: positionDto.latitude,
  //   heading: positionDto.heading,
  // };
  // assetState.shouldAnimate =
  //   assetState.delta.lat != 0 ||
  //   assetState.delta.lng != 0 ||
  //   assetState.delta.heading != 0;

  if (positionDto.doorsOpen) {
    //console.log("doors open...")
    assetState.icon = 'doorsopen';
  } else if (
    (positionDto.speed != undefined && positionDto.speed > 0) ||
    assetState.shouldAnimate
  ) {
    assetState.icon = 'moving';
  } else {
    assetState.icon = 'parked';
  }
}

function mapAssetsToGeoJson(
  assetStates: AssetStates,
  predicate: (assetState: VehicleState) => boolean
) {
  return {
    type: 'FeatureCollection',
    features: Object.values(assetStates)
      .filter(predicate)
      .map((assetState) => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [
            assetState.currentPosition.lng,
            assetState.currentPosition.lat,
          ],
        },
        properties: {
          course: assetState.currentPosition.heading,
          'asset-id': assetState.vehicleId,
          speed: assetState.speed,
          // 'asset-type': assetState.assetType,
          icon: assetState.icon,
        },
      })),
  };
}

function updateClusterLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  const data = mapAssetsToGeoJson(assetStates, () => true);
  const b = map.getSource('assets-cluster') as GeoJSONSource;
  if (b != null) {
    b.setData(data as any);
  }
}

function getBoundsWithMargin(bounds: mapboxgl.LngLatBounds) {
  // get bounds with 10% margin on each side (proportional to a viewbox)
  const marginPercent = 0.1;

  const sw = bounds.getSouthWest();
  const ne = bounds.getNorthEast();

  const lngMargin = Math.abs(sw.lng - ne.lng) * marginPercent;
  const latMargin = Math.abs(sw.lat - ne.lat) * marginPercent;

  return new mapboxgl.LngLatBounds(
    { lat: sw.lat - latMargin, lng: sw.lng - lngMargin },
    { lat: ne.lat + latMargin, lng: ne.lng + lngMargin }
  );
}

function updateAssetLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  const bounds = map.getBounds();

  // expand viewport so we ingest things just outside the bounds also.
  const biggerBounds = getBoundsWithMargin(bounds);

  const data = mapAssetsToGeoJson(assetStates, (asset) =>
    biggerBounds.contains(asset.currentPosition)
  );

  const b = map.getSource('assets') as GeoJSONSource;
  b.setData(data as any);
}

function createMapLayers(map: mapboxgl.Map) {
  map.on('load', () => {
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

    map.addSource('assets', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: [],
      },
    });

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
        // 'text-field': ['get', 'asset-id'],
        // 'text-variable-anchor': ['top'],
        // 'text-radial-offset': 1.2,
        // 'text-justify': 'center',
        // 'text-allow-overlap': true,
        // 'text-size': ['interpolate', ['linear'], ['zoom'], 9, 0, 15, 14],
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
        'line-color': '#2196f3',
        'line-width': 8,
      },
    });

    const popup = new mapboxgl.Popup({
      closeButton: false,
      closeOnClick: false,
    });

    map.on('mouseenter', 'asset-layer', (e: any) => {
      // Change the cursor style as a UI indicator.
      map.getCanvas().style.cursor = 'pointer';

      const coordinates = e.features[0].geometry.coordinates.slice();
      const description = e.features[0].properties['asset-id'];

      // Ensure that if the map is zoomed out such that multiple
      // copies of the feature are visible, the popup appears
      // over the copy being pointed to.
      while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
        coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
      }

      // Populate the popup and set its coordinates
      // based on the feature found.
      popup.setLngLat(coordinates).setHTML(description).addTo(map);
    });

    map.on('mouseleave', 'asset-layer', function () {
      map.getCanvas().style.cursor = '';
      popup.remove();
    });

    addAssetTrails(map);

  });
}

function updateViewport(map: mapboxgl.Map, assetStates: AssetStates) {
  const zoom = map.getZoom();
  if (zoom > showMarkerLevel) {
    updateAssetLayers(map, assetStates);
  }
  const bounds = map.getBounds();
  const sw = bounds.getSouthWest();
  const ne = bounds.getNorthEast();
  assets.setViewport(sw.lng, sw.lat, ne.lng, ne.lat);
}

function subscribeToMapEvents(map: mapboxgl.Map, assetStates: AssetStates) {
  const throttledUpdateViewport = throttle(updateViewport, 1000);

  map.on('zoomend', () => {
    throttledUpdateViewport(map, assetStates);
  });

  map.on('move', () => {
    throttledUpdateViewport(map, assetStates);
  });
}

function animateAssetPositions(map: mapboxgl.Map, assetStates: AssetStates) {
  const bounds = map.getBounds();
  for (const assetState of Object.values(assetStates)) {
    if (assetState.steps <= 0 || !assetState.shouldAnimate) {
      continue;
    }

    if (!bounds.contains(assetState.nextPosition)) {
      continue;
    }

    // console.log("Animating ", assetState);

    assetState.currentPosition.lng += assetState.delta.lng;
    assetState.currentPosition.lat += assetState.delta.lat;
    assetState.currentPosition.heading += assetState.delta.heading;
    assetState.steps--;
  }
}

export default defineComponent({
  name: 'Map',
  setup: function (props: any) {
    onMounted(async () => {
      mapboxgl.accessToken = mapboxConfig.getAccessToken();

      const map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v11',
        center: [24.938, 60.169],
        zoom: 8,
      });

      map.loadImage('/bus.png', (error, image) => {
        if (error) throw error;
        map.addImage('moving', image);
      });

      map.loadImage('/doorsopen.png', (error, image) => {
        if (error) throw error;
        map.addImage('doorsopen', image);
      });
      //
      // map.loadImage("/parked.png", (error, image) => {
      //   if (error) throw error;
      //   map.addImage('parked', image);
      // });
      //
      // map.loadImage("/mini.png", (error, image) => {
      //   if (error) throw error;
      //   map.addImage('mini', image);
      // });
      //
      // map.loadImage("/eq.png", (error, image) => {
      //   if (error) throw error;
      //   map.addImage('eq', image);
      // });

      const assetStates: AssetStates = {};
      (window as any).assetStates = assetStates;

      subscribeToMapEvents(map, assetStates);

      createMapLayers(map);

      setInterval(() => {
        const zoom = map.getZoom();
        if (zoom < showMarkerLevel) {
          return;
        }

        // animateAssetPositions(map, assetStates);
        updateAssetLayers(map, assetStates);
      }, 1000); //10 seconds per sensor reading, divided by steps

      // clear assets outside of a viewbox
      setInterval(() => {

        const biggerBounds = getBoundsWithMargin(map.getBounds());

        Object.values(assetStates)
          .filter(assetState => !biggerBounds.contains(assetState.currentPosition))
          .map(assetState => assetState.vehicleId)
          .forEach(id => delete assetStates[id]);

      }, 5000);

      setInterval(() => {
        //extrapolate asset positions
        updateClusterLayers(map, assetStates);
      }, layerUpdateInterval);


      const bounds = map.getBounds();
      const sw = bounds.getSouthWest();
      const ne = bounds.getNorthEast();
      await assets.connect((positionDto) => {
        if (!assetStates[positionDto.assetId]) {
          assetStates[positionDto.vehicleId] = createAssetFromState(positionDto);
        }

        updateAssetFromEvent(assetStates, positionDto);
      });
      setTimeout(
        async () => await assets.setViewport(sw.lng, sw.lat, ne.lng, ne.lat),
        500
      );
    });
  },
});
</script>

<style>
body {
  margin: 0;
  padding: 0;
}

#map {
  position: relative;
  /* top: 20px; */
  height: 100%;
  width: 100%;
}
</style>
