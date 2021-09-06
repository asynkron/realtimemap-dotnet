import { createApp } from 'vue'
import Notifications from '@kyvg/vue3-notification'
import App from './App.vue'

import PrimeVue from 'primevue/config'
import 'primevue/resources/themes/saga-blue/theme.css'
import 'primevue/resources/primevue.min.css'
import 'primeicons/primeicons.css'

import Dropdown from 'primevue/dropdown'
import Tag from 'primevue/tag'
import Panel from 'primevue/panel';

import 'primeflex/primeflex.css'

const app = createApp(App);

app.use(Notifications);
app.use(PrimeVue);

app.component('Dropdown', Dropdown);
app.component('Tag', Tag);
app.component('Panel', Panel);

app.mount('#app');
