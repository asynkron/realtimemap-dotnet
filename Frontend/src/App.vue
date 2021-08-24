<template>
  <notifications position="top right"/>
  <div class="wrapper">
    <div class="flex-grid">
      <Map class="col" />
      <List class="col" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

import Map from './components/Map.vue';
import List from './components/List.vue';

export default defineComponent({
  name: 'App',
  components: {
    Map,
    List,
  },
});
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}

.flex-grid {
  display: flex;
  min-height: 100vh;
}

.col {
  flex: 1;
  word-break: break-all;
}
</style>
