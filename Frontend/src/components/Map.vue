<template>
  <div>
    <div id="map"></div>
  </div>
</template>

<script lang="ts">
import {defineComponent, onMounted} from 'vue'
import assets, {PositionDto} from '../signalr-hub';
import mapboxgl, {GeoJSONSource} from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css'
import {throttle} from 'lodash';

const showMarkerLevel = 10;
const stepsInAnimation = 10;
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
    nextPosition: {lng: e.longitude, lat: e.latitude, heading: e.heading},
    currentPosition: {lng: e.longitude, lat: e.latitude, heading: e.heading},
    delta: {lat: 0, lng: 0, heading: 0},
    shouldAnimate: false,
    icon: "",
  };
}

function updateAssetFromEvent(assetStates: AssetStates, positionDto: PositionDto) {

  const assetState = assetStates[positionDto.vehicleId];
  // console.log(positionDto.vehicleId)
  // console.log(assetState.nextPosition);
  // console.log(positionDto);

  const lng = positionDto.longitude - assetState.nextPosition.lng;
  const lat = positionDto.latitude - assetState.nextPosition.lat;
  let heading = positionDto.heading - assetState.nextPosition.heading;

  if (lng != 0) {
    console.log(lng, lat)
  }


  //prevent full rotations when next and current course cross between 0 and 360
  if (heading > 180) {
    heading -= 360;
  }

  if (heading < -180) {
    heading += 360;
  }

  assetState.steps = stepsInAnimation;
  assetState.delta = {
    lng: lng / stepsInAnimation,
    lat: lat / stepsInAnimation,
    heading: heading / stepsInAnimation
  };
  //console.log(assetState.delta );
  assetState.currentPosition = assetState.nextPosition;
  assetState.nextPosition = {
    lng: positionDto.longitude,
    lat: positionDto.latitude,
    heading: (positionDto.heading)
  };
  assetState.shouldAnimate = assetState.delta.lat != 0 || assetState.delta.lng != 0 || assetState.delta.heading != 0;

    if (positionDto.doorsOpen) {
      //console.log("doors open...")
      assetState.icon = 'doorsopen';
    }
    else if ((positionDto.speed != undefined && positionDto.speed > 0) || assetState.shouldAnimate) {
      assetState.icon = 'moving';
    } else {
      assetState.icon = 'parked';
    }

}

function mapAssetsToGeoJson(assetStates: AssetStates, predicate: (assetState: VehicleState) => boolean) {
  return {
    type: 'FeatureCollection',
    features: Object
      .values(assetStates)
      .filter(predicate)
      .map(assetState => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [assetState.currentPosition.lng, assetState.currentPosition.lat],
        },
        properties: {
          'course': assetState.currentPosition.heading,
          'asset-id': assetState.vehicleId,
          'speed': assetState.speed,
          // 'asset-type': assetState.assetType,
          'icon': assetState.icon,
        }
      }))
  };
}

function updateClusterLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  const data = mapAssetsToGeoJson(assetStates, () => true);
  const b = map.getSource("assets-cluster") as GeoJSONSource;
  b.setData(data as any)
}

function updateAssetLayers(map: mapboxgl.Map, assetStates: AssetStates) {
  const bounds = map.getBounds();
  const sw = bounds.getSouthWest();
  const ne = bounds.getNorthEast();

  //expand viewport so we ingest things just outside the bounds also.
  const biggerBounds = new mapboxgl.LngLatBounds(
    {lat: sw.lat - 0.1, lng: sw.lng - 0.1},
    {lat: ne.lat + 0.1, lng: ne.lng + 0.1});

  const data = mapAssetsToGeoJson(assetStates, asset => biggerBounds.contains(asset.currentPosition));
  const b = map.getSource("assets") as GeoJSONSource;
  b.setData(data as any)
}

function createMapLayers(map: mapboxgl.Map) {
  map.on("load", () => {

    map.addSource('assets-cluster', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
      },
      cluster: true,
      clusterMaxZoom: showMarkerLevel, // Max zoom to cluster points on
      clusterRadius: 50 // Radius of each cluster when clustering points (defaults to 50)
    });

    map.addSource('assets', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
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
          '#73a824'
        ],
        'circle-radius': [
          'step',
          ['get', 'point_count'],
          20,
          100,
          30,
          750,
          40
        ]
      }
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
        "text-color": "#ffffff"
      }
    });

    map.addLayer({
      id: 'asset-layer',
      type: 'symbol',
      source: 'assets',
      minzoom: showMarkerLevel,
      layout: {
        'icon-image': ["get", "icon"],
        "icon-size": ['interpolate', ['linear'], ['zoom'], 9, 0.05, 15, 0.4],
        'icon-allow-overlap': true,
        'icon-rotate': ["get", "course"],
        // 'text-field': ['get', 'asset-id'],
        // 'text-variable-anchor': ['top'],
        // 'text-radial-offset': 1.2,
        // 'text-justify': 'center',
        // 'text-allow-overlap': true,
        // 'text-size': ['interpolate', ['linear'], ['zoom'], 9, 0, 15, 14],
      }
    });

    const popup = new mapboxgl.Popup({
      closeButton: false,
      closeOnClick: false
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

  })
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
  const throttledUpdateViewport = throttle(updateViewport, 100);

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
  name: "Map",
  setup: function (props: any) {

    onMounted(async () => {

      console.error("Add your mapbox token here...");
      mapboxgl.accessToken = 'pk.TOKEN';
      const map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v11',
        center: [10.844, 59.897],
        zoom: 8
      });

      map.loadImage("/bus.png", (error, image) => {
        if (error) throw error;
        map.addImage('moving', image);
      });

      map.loadImage("/doorsopen.png", (error, image) => {
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

        animateAssetPositions(map, assetStates);
        updateAssetLayers(map, assetStates);
      }, 100 / stepsInAnimation); //10 seconds per sensor reading, divided by steps

      setInterval(() => {
        //extrapolate asset positions
        updateClusterLayers(map, assetStates);
      }, layerUpdateInterval);

      const bounds = map.getBounds();
      const sw = bounds.getSouthWest();
      const ne = bounds.getNorthEast();
      await assets.connect(sw.lng, sw.lat, ne.lng, ne.lat, positionDto => {
        if (!assetStates[positionDto.assetId]) {
          assetStates[positionDto.vehicleId] = createAssetFromState(positionDto);
        }

        updateAssetFromEvent(assetStates, positionDto);
      });
    });
  }
});
</script>

<style>
body {
  margin: 0;
  padding: 0;
}

#map {
  position: absolute;
  top: 20px;
  height: 80%;
  width: 100%;
}

</style>
