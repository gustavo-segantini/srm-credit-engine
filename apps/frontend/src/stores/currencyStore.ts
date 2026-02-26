import { create } from 'zustand'
import type { CurrencyCode, ExchangeRateResponse } from '../types'

interface CurrencyState {
  rates: Partial<Record<string, ExchangeRateResponse>>
  selectedPaymentCurrency: CurrencyCode
  setPaymentCurrency: (currency: CurrencyCode) => void
  cacheRate: (key: string, rate: ExchangeRateResponse) => void
}

/**
 * Lightweight Zustand store for currency state.
 * Payment currency selection propagates across both the simulator
 * and the settlement creation form.
 */
export const useCurrencyStore = create<CurrencyState>((set) => ({
  rates: {},
  selectedPaymentCurrency: 'BRL',

  setPaymentCurrency: (currency) =>
    set({ selectedPaymentCurrency: currency }),

  cacheRate: (key, rate) =>
    set((state) => ({
      rates: { ...state.rates, [key]: rate },
    })),
}))
