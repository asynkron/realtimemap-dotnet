<template>

  <div class="flex flex-row p-3">

    <div class="flex-1 scrollable-wrapper w-12rem sm:w-18rem md:w-24rem">
      <div class="scrollable">
        <h2 class="mt-0 mb-0">
          Geofencing
        </h2>
        <div class="mt-6">
          <div class="p-field">
          <span class="p-float-label">
            <Dropdown id="organization-select" v-model="selectedOrganization" :options="organizations" optionLabel="name" />
            <label for="organization-select">Organization</label>
          </span>
          </div>
        </div>
        <div class="mt-5" v-if="details">
          <OrganizationDetails :details="details" />
        </div>
      </div>
    </div>

    <div class="notifications-placeholder hidden xl:flex xl:w-26rem" />
  </div>

</template>

<script lang="ts">

import { defineComponent, PropType } from 'vue';
import OrganizationDetails from "@/components/geofencing/OrganizationDetails.vue";
import {
  browseOrganizations, getDetails,
  OrganizationDetailsDto,
  OrganizationDto
} from "@/components/geofencing/api-organization";
import {HubConnection, NotificationDto} from "@/hub";

export default defineComponent({
  name: "GeofencingPanel",

  components: {
    OrganizationDetails
  },

  props: {
    hubConnection: {
      type: Object as PropType<HubConnection>,
      require: true
    }
  },

  data() {
    return {
      organizations: [] as OrganizationDto[],
      details: null as OrganizationDetailsDto | null,
      selectedOrganization: null as OrganizationDto | null,
    };
  },

  watch: {
    selectedOrganization(org) {
      if (org) {
        this.getDetails(org.id);
      }
    }
  },

  mounted() {
    this.fetchData();

    this.hubConnection?.onNotification(notification => {
      this.showNotification(notification);
      this.updateVehiclesInZone(notification);
    });
  },

  unmounted() {
    this.hubConnection?.clearNotificationCallback();
  },

  methods: {

    async fetchData() {
      this.organizations = await browseOrganizations();
    },

    async getDetails(id: string) {
      this.details = await getDetails(id);

      const geofences = this.details
        .geofences
        .map(geofence => ({
          long: geofence.longitude,
          lat: geofence.latitude,
          radiusInMeters: geofence.radiusInMeters
        }));

      this.$emit("geofences-updated", geofences);
    },

    showNotification(notification: NotificationDto){
      if (notification.event === "Enter") {
        this.$toast.add({
          severity:'success',
          detail: `${notification.vehicleId} from ${notification.orgName} entered the zone ${notification.zoneName}`,
          life: 6000
        });
      } else {
        this.$toast.add({
          severity:'info',
          detail: `${notification.vehicleId} from ${notification.orgName} exited the zone ${notification.zoneName}`,
          life: 6000
        });
      }
    },

    updateVehiclesInZone(notification: NotificationDto) {
      if (notification.orgId === this.selectedOrganization?.id) {

        const zone = this.details?.geofences.find(g => g.name === notification.zoneName);

        if(zone) {

          if(notification.event === "Enter") {
            if(zone.vehiclesInZone.indexOf(notification.vehicleId) === -1) {
              zone.vehiclesInZone.push(notification.vehicleId);
            }
          } else {
            const index = zone.vehiclesInZone.indexOf(notification.vehicleId);
            if(index !== -1) {
              zone.vehiclesInZone.splice(index, 1);
            }
          }

        }

      }
    }
  },
});

</script>

<style scoped>

  .p-dropdown {
    width: 100%;
  }

  .scrollable-wrapper {
    position: relative;
  }

  .scrollable {
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    padding-right: 1rem;
    overflow-y: auto;
  }

</style>
