<template>
  <div class="min-h-screen flex flex-column">
    <GeofencingNotifications />
    <TopBar />
    <div class="flex-1 flex flex-row">
      <Map class="flex-1" :geofences="geofences" />
      <GeofencingPanel class="flex-1" @geofences-updated="geofences = $event" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

import TopBar from './components/TopBar.vue';
import Map from './components/map/Map.vue';
import GeofencingPanel from './components/geofencing/GeofencingPanel.vue';
import GeofencingNotifications from './components/geofencing/GeofencingNotifications.vue';
import { Geofence } from "./components/map/geofencesLayer"

export default defineComponent({

  name: 'App',

  components: {
    TopBar,
    Map,
    GeofencingPanel,
    GeofencingNotifications,
  },

  data() {
    return {
      geofences: [] as Geofence[],
    };
  },

});
</script>

<style>

#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #2c3e50;
}

/* toast messages need to be styled globally */

.p-toast-message-icon.pi.pi-info-circle:before {
  content: "\e971" !important;
}

.p-toast-message-icon.pi.pi-check:before {
  content: "\e970" !important;
}

</style>
