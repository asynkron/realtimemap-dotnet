<template>

  <div class="flex flex-row">
    <List class="flex-1"/>
    <div class="notifications-placeholder">
    </div>
  </div>

</template>

<script lang="ts">

import { defineComponent } from 'vue';
import List from './List.vue';

export default defineComponent({
  name: "GeofencingPanel",

  components: {
    List
  }
});

</script>

<style scoped>

  .notifications-placeholder {
    width: 27rem;
  }

</style>
