import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { settlementsApi, reportsApi } from '../services/api'
import type {
  CreateSettlementRequest,
  SettlementResponse,
  CurrencyCode,
} from '../types'

// ── Settlement Statement (paginated report) ────────────────────────────────
export function useSettlementStatement(params: {
  from?: string
  to?: string
  cedentId?: string
  paymentCurrency?: CurrencyCode
  page: number
  pageSize: number
}) {
  return useQuery({
    queryKey: ['settlement-statement', params],
    queryFn: () => reportsApi.getStatement(params),
    placeholderData: (prev) => prev, // keep previous data while fetching new page
  })
}

// ── Create Settlement ──────────────────────────────────────────────────────
export function useCreateSettlement() {
  const queryClient = useQueryClient()

  return useMutation<SettlementResponse, Error, CreateSettlementRequest>({
    mutationFn: settlementsApi.create,
    onSuccess: () => {
      // Invalidate statement cache so grid refreshes automatically
      queryClient.invalidateQueries({ queryKey: ['settlement-statement'] })
    },
  })
}

// ── Get Settlement by ID ───────────────────────────────────────────────────
export function useSettlement(id: string) {
  return useQuery({
    queryKey: ['settlement', id],
    queryFn: () => settlementsApi.getById(id),
    enabled: Boolean(id),
  })
}
