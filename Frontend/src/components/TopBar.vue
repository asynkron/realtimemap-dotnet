<template>

  <div id="topbar" class="w-full bg-primary text-0 p-3 flex align-items-center">

    <i class="pi pi-map text-3xl"></i>

    <div class="ml-3 flex flex-column">
      <div class="font-semibold">
        Real-time Map
      </div>
      <div class="text-sm">
        Proto.Actor showcase
      </div>
    </div>

    <div class="ml-3 hidden md:block">
      <a v-bind:href="dashboardUrl" target="_blank">
        <i class="pi pi-external-link mr-2"></i>Proto.Actor Dashboard
      </a>
    </div>

    <div class="flex-1">
    </div>

    <a href="https://www.etteplan.com/" target="_blank" rel="noopener noreferrer" class="h-full px-3 hidden md:flex align-items-center">
      <img src="../assets/etteplan.svg" class="logo" />
    </a>

    <a href="https://proto.actor" target="_blank" rel="noopener noreferrer" class="h-full px-3 hidden md:flex align-items-center">
      <img src="../assets/protoactor.svg" class="logo" />
    </a>

    <a href="https://github.com/AsynkronIT/realtimemap" target="_blank" rel="noopener noreferrer" class="h-full px-3 hidden md:block">
      <i class="pi pi-github text-3xl"></i>
    </a>

    <i class="pi pi-info-circle text-3xl block" @click="showTutorial"></i>

  </div>

  <Dialog :modal="true" :showHeader="true" header="Welcome!" v-model:visible="tutorialVisible" :style="{width: '75vw'}" :breakpoints="{'1000px': '90vw', '700px': '100vw'}" @hide="onTutorialHidden">
    <p>Welcome to the Proto.Actor realtime map showcase. This technology demo shows how Proto.Actor actor framework can be used to track the location of assets in real time, and how to notify users when certain conditions are observed.</p>
    <p class="mobile-warning" v-if="mobileNoteVisible">NOTE: For best user experience, open this website in a desktop browser!</p>
    <p>We use data from <a target="_blank" href="https://digitransit.fi/en/developers/apis/4-realtime-api/vehicle-positions/">Helsinki Region Transport</a> to get real-time information about position, heading and status of the public transport vehicles in the city of Helsinki. The data is published under <a target="_blank" href="https://creativecommons.org/licenses/by/4.0/deed.en">Creative Commons BY 4.0 International license</a> © Helsinki Region Transport {{ year }}</p>
    <p>The showcase was built in cooperation with <a href="https://www.etteplan.com" target="_blank">Etteplan</a>.</p>
    <h3>Navigation</h3>
    <p>Zoom out to see an overview of the whole city or zoom in to see the details.</p>
    <video autoplay muted loop>
      <source src="../assets/navigation.mp4" />
    </video>
    <h3>Vehicle trail</h3>
    <p>You can see the latest 100 positions recorded for a selected vehicle to track its journey. The actor based approach makes it easy to keep recent history of positions for each vehicle in memory, available for quick querying.</p>
    <video autoplay muted loop>
      <source src="../assets/vehicle-trail.mp4" />
    </video>
    <h3>Organizations and geofencing</h3>
    <p>Public transport vehicles are owned by organizations. You can see a list of organizations in the dropdown in the geofencing section. When you select an organization from the dropdown, a list of its geofencing areas will be presented. For each geofencing area, you can observe the list of organization's vehicles currently in the area. The areas will also be presented on the map as purple circles. The purpose of this functionality is to showcase how easy it is to keep track of assets entering and exiting a geofencing area using the actor based approach.</p>
    <video autoplay muted loop>
      <source src="../assets/geofencing.mp4" />
    </video>
    <h3>Notifications</h3>
    <p>We can also push notifications about the geofencing events real-time to user's browser via websockets.</p>
    <video autoplay muted loop>
      <source src="../assets/notifications.mp4" />
    </video>
    <h3>More information</h3>
    <ul>
      <li><a href="https://github.com/AsynkronIT/realtimemap" target="_blank">Github page</a></li>
      <li><a href="https://proto.actor" target="_blank">Proto.Actor framework</a></li>
      <li><a href="https://www.etteplan.com" target="_blank">Etteplan</a></li>
    </ul>
  </Dialog>

</template>

<script lang="ts">
import { defineComponent } from 'vue';
import config from "@/config";
import { UAParser } from 'ua-parser-js';

export default defineComponent({
  data() {
    return {
      dashboardUrl: "",
      tutorialVisible: false,
      mobileNoteVisible: false,
      year: new Date().getFullYear()
    };
  },

  mounted() {
    this.dashboardUrl = `${config.backendUrl}`;
    if(!this.dashboardUrl.endsWith("/")) {
      this.dashboardUrl += "/";
    }

    if(!localStorage.tutorialClosed)
      this.tutorialVisible = true;

    this.mobileNoteVisible = new UAParser().getDevice().type === "mobile";
  },

  methods: {
    onTutorialHidden() {
      localStorage.tutorialClosed = true;
    },

    showTutorial() {
      this.tutorialVisible = true;
    }
  }

});
</script>

<style scoped>

  .logo {
    height: 2rem;
  }

  .logo:hover {
    opacity: .75;
  }

  #topbar a:link {
    color: var(--surface-0);
  }

  #topbar a:visited {
    color: var(--surface-0);
  }

  #topbar a:hover {
    color: var(--cyan-100);
  }

  #topbar a:active {
    color: var(--cyan-100);
  }

  a:link {
    color: var(--cyan-700)
  }

  a:hover {
    color: var(--cyan-500)
  }

  a:active {
    color: var(--cyan-500)
  }

  a:visited {
    color: var(--cyan-700)
  }

  video {
    margin-left: auto;
    margin-right: auto;
    display: block;
    border: 1px solid lightgrey;
    border-radius: 5px;
    max-width: 660px;
    width: 100%;
  }

  .pi-info-circle {
    padding-left: 16px;
    padding-right: 16px;
    cursor: pointer;
  }

  .mobile-warning {
    font-weight: bold;
  }

</style>
