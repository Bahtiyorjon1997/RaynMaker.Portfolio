import Vue from 'vue'
import Router from 'vue-router'
import App from './App'
import CoreuiVue from '@coreui/vue'
import VueCompositionAPI from '@vue/composition-api'

Vue.use(VueCompositionAPI)

Vue.use(CoreuiVue)
Vue.config.productionTip = false

Vue.use(Router)

const router = new Router({
  routes: [
    {
      path: '/Positions',
      alias: '/',
      component: () => import('@/views//Positions/Open/Positions.vue')
    },
    {
      path: '/Performance',
      component: () => import('@/views/Performance/Performance.vue')
    },
    {
      path: '/Cashflow',
      component: () => import('@/views/Cashflow/Cashflow.vue')
    },
    {
      path: '/ClosedPositions',
      component: () => import('@/views/Positions/Closed/ClosedPositions.vue')
    },
    {
      path: '/PositionDetails',
      name: 'PositionDetails',
      component: () => import('@/views/Positions/Details/PositionDetails.vue')
    }
  ],
  linkActiveClass: 'active',
  linkExactActiveClass: 'active'
})

new Vue({
  router,
  render: (h) => h(App)
}).$mount('#app')
