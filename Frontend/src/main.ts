import { createApp } from 'vue'
import Notifications from '@kyvg/vue3-notification'
import App from './App.vue'

import PrimeVue from 'primevue/config'
import Dropdown from 'primevue/dropdown'
import 'primevue/resources/themes/saga-blue/theme.css'
import 'primevue/resources/primevue.min.css'
import 'primeicons/primeicons.css'

import 'primeflex/primeflex.css'

const app = createApp(App);

app.use(Notifications);
app.use(PrimeVue);

app.component('Dropdown', Dropdown);

app.mount('#app');

