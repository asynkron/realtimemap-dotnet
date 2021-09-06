<template>
  <notifications position="top right"/>
  <div class="flex flex-row min-h-screen">
    <Map class="flex-1" />
    <GeofencingPanel class="flex-1" />
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

import Map from './components/Map.vue';
import GeofencingPanel from './components/GeofencingPanel.vue';

export default defineComponent({
  name: 'App',
  components: {
    Map,
    GeofencingPanel,
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

</style>
