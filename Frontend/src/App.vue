<template>
  <div class="min-h-screen flex flex-column">
    <GeofencingNotifications  />
    <TopBar />
    <div class="flex-1 flex flex-row">
      <Map class="flex-1" />
      <GeofencingPanel class="flex-1" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

import TopBar from './components/TopBar.vue';
import Map from './components/Map.vue';
import GeofencingPanel from './components/GeofencingPanel.vue';
import GeofencingNotifications from './components/GeofencingNotifications.vue';

export default defineComponent({
  name: 'App',
  components: {
    TopBar,
    Map,
    GeofencingPanel,
    GeofencingNotifications,
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

.p-toast-message-icon.pi.pi-info-circle:before {
  content: "\e971" !important;
}

.p-toast-message-icon.pi.pi-check:before {
  content: "\e970" !important;
}

</style>
