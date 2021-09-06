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
  .p-toast-message-icon.pi.pi-info-circle:before {
    content: "\e971" !important;
  }

  .p-toast-message-icon.pi.pi-check:before {
    content: "\e970" !important;
  }
</style>
