// ─────────────────────────────────────────────────────────────────────────────
// DTOs mirroring the .NET API contracts
// ─────────────────────────────────────────────────────────────────────────────

export type ReceivableType = 'DuplicataMercantil' | 'ChequePredatado';
export type CurrencyCode  = 'BRL' | 'USD';
export type SettlementStatus = 'Pending' | 'Settled' | 'Failed' | 'Cancelled';

// ── Requests ──────────────────────────────────────────────────────────────────
export interface SimulatePricingRequest {
  faceValue: number;
  faceCurrency: CurrencyCode;
  receivableType: ReceivableType;
  dueDate: string; // ISO 8601
  paymentCurrency: CurrencyCode;
}

export interface CreateSettlementRequest {
  cedentId: string;
  documentNumber: string;
  receivableType: ReceivableType;
  faceValue: number;
  faceCurrency: CurrencyCode;
  dueDate: string;
  paymentCurrency: CurrencyCode;
}

export interface UpdateExchangeRateRequest {
  fromCurrency: CurrencyCode;
  toCurrency: CurrencyCode;
  rate: number;
  source?: string;
}

// ── Responses ─────────────────────────────────────────────────────────────────
export interface PricingSimulationResponse {
  faceValue: number;
  faceCurrency: string;
  presentValue: number;
  discount: number;
  discountRatePercent: number;
  appliedSpreadPercent: number;
  baseRatePercent: number;
  termInMonths: number;
  netDisbursement: number;
  paymentCurrency: string;
  exchangeRateApplied: number;
  isCrossCurrency: boolean;
  simulatedAt: string;
}

export interface SettlementResponse {
  id: string;
  receivableId: string;
  cedentId: string;
  documentNumber: string;
  receivableType: ReceivableType;
  faceValue: number;
  presentValue: number;
  discount: number;
  netDisbursement: number;
  paymentCurrency: CurrencyCode;
  exchangeRateApplied: number;
  status: SettlementStatus;
  settledAt?: string;
  failureReason?: string;
  createdAt: string;
}

export interface ExchangeRateResponse {
  id: string;
  fromCurrency: CurrencyCode;
  toCurrency: CurrencyCode;
  rate: number;
  effectiveDate: string;
  source?: string;
  updatedAt: string;
}

export interface SettlementStatementItemResponse {
  settlementId: string;
  documentNumber: string;
  receivableType: ReceivableType;
  cedentName: string;
  faceValue: number;
  presentValue: number;
  discount: number;
  netDisbursement: number;
  paymentCurrency: CurrencyCode;
  status: SettlementStatus;
  createdAt: string;
  settledAt?: string;
}

export interface SettlementStatementResponse {
  items: SettlementStatementItemResponse[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  totalFaceValue: number;
  totalNetDisbursement: number;
  totalDiscount: number;
}

// ── Pagination ────────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ── Cedents ───────────────────────────────────────────────────────────────────
export interface CedentResponse {
  id: string;
  name: string;
  cnpj: string;
  contactEmail: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCedentRequest {
  name: string;
  cnpj: string;
  contactEmail: string;
}

export interface UpdateCedentRequest {
  name: string;
  contactEmail: string;
}
