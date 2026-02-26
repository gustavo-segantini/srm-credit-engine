import axios, { AxiosError } from 'axios'
import type {
  SimulatePricingRequest,
  PricingSimulationResponse,
  CreateSettlementRequest,
  SettlementResponse,
  UpdateExchangeRateRequest,
  ExchangeRateResponse,
  SettlementStatementResponse,
  CurrencyCode,
} from '../types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export const apiClient = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
  timeout: 15_000,
})

// ── Response interceptor: rethrow with message from Problem Details ────────
apiClient.interceptors.response.use(
  (res) => res,
  (error: AxiosError<{ title?: string; detail?: string }>) => {
    const detail = error.response?.data?.detail ?? error.response?.data?.title
    return Promise.reject(new Error(detail ?? error.message))
  },
)

// ── Pricing ────────────────────────────────────────────────────────────────
export const pricingApi = {
  simulate: (req: SimulatePricingRequest) =>
    apiClient
      .post<PricingSimulationResponse>('/pricing/simulate', req)
      .then((r) => r.data),
}

// ── Settlements ────────────────────────────────────────────────────────────
export const settlementsApi = {
  create: (req: CreateSettlementRequest) =>
    apiClient
      .post<SettlementResponse>('/settlements', req)
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<SettlementResponse>(`/settlements/${id}`).then((r) => r.data),
}

// ── Reports ────────────────────────────────────────────────────────────────
export const reportsApi = {
  getStatement: (params: {
    from?: string
    to?: string
    cedentId?: string
    paymentCurrency?: CurrencyCode
    page?: number
    pageSize?: number
  }) =>
    apiClient
      .get<SettlementStatementResponse>('/reports/settlement-statement', { params })
      .then((r) => r.data),
}

// ── Currency ───────────────────────────────────────────────────────────────
export const currencyApi = {
  getLatestRate: (from: CurrencyCode, to: CurrencyCode) =>
    apiClient
      .get<ExchangeRateResponse>(`/currency/exchange-rates/${from}/${to}`)
      .then((r) => r.data),

  updateRate: (req: UpdateExchangeRateRequest) =>
    apiClient
      .put<ExchangeRateResponse>('/currency/exchange-rates', req)
      .then((r) => r.data),
}
