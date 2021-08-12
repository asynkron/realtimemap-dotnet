<template>
  <div id="content" v-if="anythingToDisplay">
    <div>
      <p>{{ details.name }} - {{ details.id }}</p>
    </div>
    <div>
      <div v-for="geofence in details.geofences" v-bind:key="geofence.name">
        <p>{{parseGeofence(geofence)}}</p>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue';
import {
  OrganizationDetailsDto,
  GeofenceDto,
} from './../services/api-organization';

export default defineComponent({
  name: 'OrganizationDetails',
  props: {
    details: {
      type: Object as PropType<OrganizationDetailsDto>,
      required: true,
    },
  },
  computed: {
    anythingToDisplay(): boolean {
      return Object.keys(this.details).length > 0;
    },
  },

  methods: {
    parseVehicles(vehicles: string[]) {
      if (vehicles.length == 0) return '';

      return vehicles.join();
    },
    parseGeofence(geofence: GeofenceDto) {
      const count = geofence.vehiclesInZone.length;
      const parsedVehicles = count == 0 ? '' : geofence.vehiclesInZone.join();

      return `${geofence.name} (${count}): ${parsedVehicles}`;
    }
  },
});
</script>
<style scoped>
#content {
  text-align: left;
}
</style>