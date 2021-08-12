<template>
  <div>
    <div id="list">
      <ul>
        <li
          v-for="item in organizations"
          v-bind:key="item.id"
          v-bind:class="{ active: selectedItemId === item.id }"
          @click="getDetails(item.id)"
        >
          {{ item.name }}
        </li>
      </ul>
      <organization-details :details="details" />
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import {
  BrowseOrganizations,
  OrganizationDto,
  OrganizationDetailsDto,
  GetDetails,
} from './../services/api-organization';
import OrganizationDetails from './OrganizationDetails.vue';

export default defineComponent({
  data() {
    return {
      organizations: [] as OrganizationDto[],
      details: {} as OrganizationDetailsDto,
      selectedItemId: ''
    };
  },

  components: {
    OrganizationDetails,
  },

  created() {
    this.fetchData();
  },
  methods: {
    async fetchData() {
      this.organizations = await BrowseOrganizations();
    },
    async getDetails(id: string) {
      this.selectedItemId = id;
      const data = await GetDetails(id);
      this.details = data;
    },
  },
});
</script>
<style scoped>
#list {
  margin: 10px;
}

ul {
  text-align: left;
  list-style-type: none;
  padding: 0;
  margin: 0;
}

ul li {
  cursor: pointer;
}

ul li.active {
  font-weight: bold;
}
</style>