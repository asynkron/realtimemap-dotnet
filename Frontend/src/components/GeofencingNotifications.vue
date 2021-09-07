<template>
  <Toast />
</template>

<script lang="ts">
import { defineComponent } from 'vue';

export default defineComponent({
  name: 'GeofencingNotifications ',

  mounted() {
    // todo: this is not the cleanest solution
    window.toast = this.$toast;
  }
});
</script>

<style scoped>
  /* replace info icon with exit icon */
  .p-toast-message-icon.pi.pi-info-circle:before {
    content: "\e971" !important;
  }

  /* replace check icon with entry icon */
  .p-toast-message-icon.pi.pi-check:before {
    content: "\e970" !important;
  }
</style>

<style>
  /* for some reason, toast container needs to be styled globally */
  .p-toast-top-right {
    top: 5.5rem !important;
  }
</style>
