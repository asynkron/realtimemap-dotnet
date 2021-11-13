<template>
  <Panel :header="getGeofenceHeader(geofence)" class="mb-4">
    <transition name="fade" mode="out-in">
      <transition-group name="fade" tag="span" v-if="geofence.vehiclesInZone.length !== 0">
      <Tag
        v-for="vehicle in geofence.vehiclesInZone"
        v-bind:key="vehicle"
        :value="vehicle"
        class="m-1">
      </Tag>
      </transition-group>
      <div v-else class="font-italic" >
        no vehicles in this zone
      </div>
    </transition>
  </Panel>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';
import { GeofenceDto } from './api-organization';

export default defineComponent({

  name: 'GeofenceDetails',

  props: {
    geofence: {
      type: Object as PropType<GeofenceDto>,
      required: true,
    },
  },

  methods: {
    getGeofenceHeader() {
      const vehicleWord = this.geofence.vehiclesInZone.length === 1
        ? "vehicle"
        : "vehicles";

      return `${this.geofence.name} (${this.geofence.vehiclesInZone.length} ${vehicleWord})`;
    },
  },

});
</script>

<style scoped>
  .fade-enter-active, .fade-leave-active {
    transition: opacity .5s ease-in-out;
  }
  .fade-enter-from, .fade-leave-to {
    opacity: 0;
  }
</style>
