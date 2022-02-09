<template>

  <div class="w-full bg-primary text-0 p-3 flex align-items-center">

    <i class="pi pi-map text-3xl"></i>

    <div class="ml-3 flex flex-column">
      <div class="font-semibold">
        Real-time Map
      </div>
      <div class="text-sm">
        Proto.Actor showcase
      </div>
    </div>

    <div class = "ml-3">
      <a v-bind:href="dashboardUrl" target="_blank">
        <i class="pi pi-external-link mr-2"></i>Proto.Actor Dashboard (alpha)
      </a>
    </div>

    <div class="flex-1">
    </div>

    <a href="https://www.skyrise.tech" target="_blank" rel="noopener noreferrer" class="h-full px-3 block flex align-items-center">
      <img src="../assets/skyrise.svg" class="logo" />
    </a>

    <a href="https://proto.actor" target="_blank" rel="noopener noreferrer" class="h-full px-3 block flex align-items-center">
      <img src="../assets/protoactor.svg" class="logo" />
    </a>

    <a href="https://github.com/AsynkronIT/realtimemap" target="_blank" rel="noopener noreferrer" class="h-full px-3 block">
      <i class="pi pi-github text-3xl"></i>
    </a>

  </div>

</template>

<script lang="ts">
import { defineComponent } from 'vue';
import config from "@/config";

export default defineComponent({
  data() {
    return {
      dashboardUrl: "",
    };
  },

  mounted() {
    this.dashboardUrl = `${config.backendUrl}`;
    if(!this.dashboardUrl.endsWith("/")) {
      this.dashboardUrl += "/";
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

  a:link {
    color: var(--surface-0);
  }

  a:visited {
    color: var(--surface-0);
  }

  a:hover {
    color: var(--cyan-100);
  }

  a:active {
    color: var(--cyan-100);
  }

</style>
