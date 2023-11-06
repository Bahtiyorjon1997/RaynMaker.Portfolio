<template>
  <div>
    <CCard>
      <CCardHeader>
        <CCardTitle>Cashflow</CCardTitle>
      </CCardHeader>
      <CCardBody>
        <label>Limit: </label>
        <input
          v-model="limit"
          @keyup.enter="onLimitChanged"
        />

        <table class="table-hover">
          <thead>
            <tr>
              <th class="date">Date</th>
              <th class="comment">Type/Comment</th>
              <th class="value">Value</th>
              <th class="value">Balance</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="t in transactions"
              :key="t.date + Math.random()"
            >
              <td class="date">{{ t.date }}</td>
              <td
                class="comment"
                style="padding-left: 100px; padding-right: 100px"
              >
                <b>{{ t.type }}</b
                ><br />
                {{ t.comment }}
              </td>
              <td class="value">{{ t.value }}</td>
              <td class="value">{{ t.balance }}</td>
            </tr>
          </tbody>
        </table>
      </CCardBody>
    </CCard>
  </div>
</template>

<script>
import { ref, onMounted, watch } from 'vue'
import API from '@/api'

export default {
  name: 'CashflowComposition',
  setup() {
    const transactions = ref(null)
    const limit = ref(25)

    const onLimitChanged = async () => {
      const response = await API.get(`/cashflow?limit=${limit.value}`)
      transactions.value = response.data
    }

    onMounted(() => {
      onLimitChanged()
    })

    watch(limit, () => {
      onLimitChanged()
    })

    return { transactions, limit, onLimitChanged }
  }
}
</script>

<style scoped>
.date {
  text-align: left;
}

.value {
  text-align: right;
}

.comment {
  text-align: center;
}

th,
td {
  padding: 15px;
}

td {
  vertical-align: top;
}
</style>
