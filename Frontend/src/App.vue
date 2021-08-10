<template>
  <div class="wrapper">
    <div class="row">
      <Map class="column" />
      <List class="column" />
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
  margin-top: 60px;
}

.row {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  width: 100%;
}

.column {
  display: flex;
  flex-direction: column;
  flex-basis: 100%;
  flex: 1;
}
</style>
