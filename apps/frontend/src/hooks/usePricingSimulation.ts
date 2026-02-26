import { useMutation } from '@tanstack/react-query'
import { pricingApi } from '../services/api'
import type { SimulatePricingRequest, PricingSimulationResponse } from '../types'

/**
 * Hook that wraps the POST /api/v1/pricing/simulate endpoint.
 * Uses mutation (not query) because it's a command-like operation
 * with side-effect semantics (rate look-up, computation trigger).
 */
export function usePricingSimulation() {
  return useMutation<PricingSimulationResponse, Error, SimulatePricingRequest>({
    mutationFn: pricingApi.simulate,
  })
}
