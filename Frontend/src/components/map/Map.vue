<template>
  <div>
    <div id="map"></div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';
import assets, { AssetConnection } from '@/signalr-hub';
import mapboxgl from 'mapbox-gl';
import 'mapbox-gl/dist/mapbox-gl.css';
import mapboxConfig from '@/mapboxConfig';
import { addVehicleTrailLayer } from './vehicleTrailsLayer';
import { addGeofencesLayer, Geofence, setGeofences } from './geofencesLayer';
import { AssetStates, handlePositionEvent, clearAssetsOutsideOfViewbox } from "./assetStates";
import { addVehicleDetailsPopup } from './vehicleDetailsPopup';
import { addVehiclesLayer } from './vehiclesLayer';
import { addVehicleClustersLayer } from './vehicleClustersLayer';
import { handleViewportUpdates } from './viewportUpdates';

export default defineComponent({
  name: 'Map',

  props: {
    geofences: {
      type: Array as PropType<Geofence[]>,
      require: true
    }
  },

  data() {
    return {
      // it will be set in mounted and available later on
      map: undefined as unknown as mapboxgl.Map,
      assetConnection: undefined as unknown as AssetConnection
    }
  },

  async mounted() {
    mapboxgl.accessToken = mapboxConfig.getAccessToken();

    this.map = new mapboxgl.Map({
      container: 'map',
      style: 'mapbox://styles/mapbox/streets-v11',
      center: [24.938, 60.169],
      zoom: 8,
    });

    const assetStates: AssetStates = {};

    this.assetConnection = await assets.connect((positionDto) => {
      handlePositionEvent(assetStates, positionDto);
    });

    this.map.on('load', () => {

      addVehicleClustersLayer(this.map, assetStates);
      addVehiclesLayer(this.map, assetStates);
      addVehicleTrailLayer(this.map);
      addGeofencesLayer(this.map);

      addVehicleDetailsPopup(this.map);

      handleViewportUpdates(this.map, this.assetConnection);

      setInterval(
        () => clearAssetsOutsideOfViewbox(this.map, assetStates),
        5000
      );

    });

  },

  async unmounted() {
    await this.assetConnection.disconnect();
  },

  watch: {
    geofences(newGeofences: Geofence[] | undefined) {
      setGeofences(this.map, newGeofences);
    }
  }

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
