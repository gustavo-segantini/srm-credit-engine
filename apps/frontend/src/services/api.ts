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
  CedentResponse,
  CreateCedentRequest,
  UpdateCedentRequest,
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

// ── Token management ───────────────────────────────────────────────────────
let _accessToken: string | null = null

async function fetchToken(): Promise<string> {
  const res = await axios.post<{ accessToken: string }>(
    `${BASE_URL}/api/v1/auth/token`,
    { username: 'operator' },
  )
  _accessToken = res.data.accessToken
  return _accessToken
}

async function getToken(): Promise<string> {
  if (!_accessToken) await fetchToken()
  return _accessToken!
}

// ── Request interceptor: attach Bearer token ───────────────────────────────
apiClient.interceptors.request.use(async (config) => {
  const token = await getToken()
  config.headers.Authorization = `Bearer ${token}`
  return config
})

// ── Response interceptor: rethrow with message from Problem Details ────────
apiClient.interceptors.response.use(
  (res) => res,
  async (error: AxiosError<{ title?: string; detail?: string }>) => {
    // On 401 refresh token once and retry
    if (error.response?.status === 401 && !((error.config as any)._retry)) {
      ;(error.config as any)._retry = true
      await fetchToken()
      error.config!.headers.Authorization = `Bearer ${_accessToken}`
      return apiClient(error.config!)
    }
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

// ── Cedents ────────────────────────────────────────────────────────────────
export const cedentsApi = {
  getAll: () =>
    apiClient.get<CedentResponse[]>('/cedents').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<CedentResponse>(`/cedents/${id}`).then((r) => r.data),

  create: (req: CreateCedentRequest) =>
    apiClient.post<CedentResponse>('/cedents', req).then((r) => r.data),

  update: (id: string, req: UpdateCedentRequest) =>
    apiClient.put<CedentResponse>(`/cedents/${id}`, req).then((r) => r.data),

  deactivate: (id: string) =>
    apiClient.delete(`/cedents/${id}`).then((r) => r.data),
}
