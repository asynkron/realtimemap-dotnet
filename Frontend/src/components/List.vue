<template>
  <div>
    <div id="list">
      <ul>
        <li
          v-for="item in organizations"
          v-bind:key="item.id"
          @click="getDetails(item.id)"
        >
          {{ item.name }}
        </li>
      </ul>
      <div id="content">
        {{ details }}
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import {
  BrowseOrganizations,
  OrganizationDto,
  GetDetails,
} from './../services/api-organization';

export default defineComponent({
  data() {
    return {
      organizations: [] as OrganizationDto[],
      details: '',
    };
  },
  created() {
    this.fetchData();
  },
  methods: {
    async fetchData() {
      this.organizations = await BrowseOrganizations();
    },
    async getDetails(id: string) {
        const data = await GetDetails(id);
        this.details = JSON.stringify(data);
    },
  },
});
</script>
<style scoped>
ul {
  list-style-type: square;
  list-style-position: inside;
  list-style-image: none;
  padding: 10px;
  text-align: left;
}

ul li {
    cursor: pointer;
}
</style>