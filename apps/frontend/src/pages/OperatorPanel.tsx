import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { format } from 'date-fns'
import { usePricingSimulation } from '../hooks/usePricingSimulation'
import { useCreateSettlement } from '../hooks/useSettlements'
import type { SimulatePricingRequest, ReceivableType, CurrencyCode } from '../types'

// ── Validation schema ──────────────────────────────────────────────────────
const schema = z.object({
  cedentId:       z.string().uuid('Must be a valid UUID'),
  documentNumber: z.string().min(1, 'Required'),
  receivableType: z.enum(['DuplicataMercantil', 'ChequePredatado']),
  faceValue:      z.coerce.number().positive('Must be positive'),
  faceCurrency:   z.enum(['BRL', 'USD']),
  dueDate:        z.string().min(1, 'Required'),
  paymentCurrency:z.enum(['BRL', 'USD']),
})

type FormValues = z.infer<typeof schema>

// ── Currency formatter ─────────────────────────────────────────────────────
function fmt(value: number, currency: string) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value)
}

export default function OperatorPanel() {
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isValid },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: 'onChange',
    defaultValues: {
      faceCurrency: 'BRL',
      paymentCurrency: 'BRL',
      receivableType: 'DuplicataMercantil',
    },
  })

  const simulate   = usePricingSimulation()
  const createSettl = useCreateSettlement()

  const watchedValues = watch()

  // ── Real-time simulation (debounce via TanStack Query mutation) ──────────
  function onSimulate(data: FormValues) {
    const req: SimulatePricingRequest = {
      faceValue:       data.faceValue,
      faceCurrency:    data.faceCurrency as CurrencyCode,
      receivableType:  data.receivableType as ReceivableType,
      dueDate:         data.dueDate,
      paymentCurrency: data.paymentCurrency as CurrencyCode,
    }
    simulate.mutate(req)
  }

  // ── Confirm = create settlement ──────────────────────────────────────────
  function onConfirm(data: FormValues) {
    createSettl.mutate({
      cedentId:        data.cedentId,
      documentNumber:  data.documentNumber,
      receivableType:  data.receivableType as ReceivableType,
      faceValue:       data.faceValue,
      faceCurrency:    data.faceCurrency as CurrencyCode,
      dueDate:         data.dueDate,
      paymentCurrency: data.paymentCurrency as CurrencyCode,
    })
  }

  const result = simulate.data
  const isSaving = createSettl.isPending

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* ── Input form ────────────────────────────────────────────────── */}
      <section className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-5">Pricing Simulator</h2>

        <form className="space-y-4" onSubmit={handleSubmit(onSimulate)}>
          {/* Cedent ID */}
          <Field label="Cedent ID" error={errors.cedentId?.message}>
            <input
              {...register('cedentId')}
              className={inputCls(errors.cedentId)}
              placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            />
          </Field>

          {/* Document Number */}
          <Field label="Document Number" error={errors.documentNumber?.message}>
            <input {...register('documentNumber')} className={inputCls(errors.documentNumber)} />
          </Field>

          {/* Receivable Type */}
          <Field label="Receivable Type" error={errors.receivableType?.message}>
            <select {...register('receivableType')} className={inputCls(errors.receivableType)}>
              <option value="DuplicataMercantil">Duplicata Mercantil (1.5% a.m.)</option>
              <option value="ChequePredatado">Cheque Pré-datado (2.5% a.m.)</option>
            </select>
          </Field>

          {/* Face Value + Currency */}
          <div className="flex gap-3">
            <div className="flex-1">
              <Field label="Face Value" error={errors.faceValue?.message}>
                <input
                  {...register('faceValue')}
                  type="number"
                  step="0.01"
                  className={inputCls(errors.faceValue)}
                  placeholder="10000.00"
                />
              </Field>
            </div>
            <Field label="Face Currency" error={errors.faceCurrency?.message}>
              <select {...register('faceCurrency')} className={inputCls(errors.faceCurrency)}>
                <option value="BRL">BRL</option>
                <option value="USD">USD</option>
              </select>
            </Field>
          </div>

          {/* Due Date */}
          <Field label="Due Date" error={errors.dueDate?.message}>
            <input
              {...register('dueDate')}
              type="date"
              min={format(new Date(), 'yyyy-MM-dd')}
              className={inputCls(errors.dueDate)}
            />
          </Field>

          {/* Payment Currency */}
          <Field label="Payment Currency" error={errors.paymentCurrency?.message}>
            <select {...register('paymentCurrency')} className={inputCls(errors.paymentCurrency)}>
              <option value="BRL">BRL</option>
              <option value="USD">USD</option>
            </select>
          </Field>

          <button
            type="submit"
            disabled={!isValid || simulate.isPending}
            className="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300
                       text-white font-medium rounded-lg transition-colors"
          >
            {simulate.isPending ? 'Simulating…' : 'Simulate Pricing'}
          </button>

          {simulate.isError && (
            <p className="text-red-600 text-sm">{simulate.error.message}</p>
          )}
        </form>
      </section>

      {/* ── Simulation result ─────────────────────────────────────────── */}
      <section className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 flex flex-col">
        <h2 className="text-lg font-semibold text-gray-800 mb-5">Simulation Result</h2>

        {!result && !simulate.isPending && (
          <p className="text-gray-400 text-sm my-auto text-center">
            Fill the form and click "Simulate Pricing"
          </p>
        )}

        {simulate.isPending && (
          <div className="flex-1 flex items-center justify-center">
            <Spinner />
          </div>
        )}

        {result && (
          <>
            <dl className="space-y-3 flex-1">
              <Row label="Face Value"        value={fmt(result.faceValue, result.faceCurrency)} />
              <Row label="Present Value"     value={fmt(result.presentValue, result.paymentCurrency)} highlight />
              <Row label="Discount"          value={fmt(result.discount, result.paymentCurrency)} />
              <Row label="Net Disbursement"  value={fmt(result.netDisbursement, result.paymentCurrency)} highlight />
              <Row label="Spread"            value={`${(result.spread * 100).toFixed(2)}% a.m.`} />
              <Row label="Base Rate"         value={`${(result.baseRate * 100).toFixed(4)}% a.m.`} />
              <Row label="Term"              value={`${result.termInMonths} month(s)`} />
              {result.isCrossCurrency && (
                <Row
                  label="Exchange Rate"
                  value={`1 ${result.faceCurrency} = ${result.exchangeRateApplied} ${result.paymentCurrency}`}
                />
              )}
            </dl>

            <button
              onClick={handleSubmit(onConfirm)}
              disabled={isSaving}
              className="mt-6 w-full py-2 px-4 bg-green-600 hover:bg-green-700
                         disabled:bg-gray-300 text-white font-medium rounded-lg transition-colors"
            >
              {isSaving ? 'Confirming…' : 'Confirm Settlement'}
            </button>

            {createSettl.isSuccess && (
              <p className="text-green-600 text-sm text-center mt-2">
                ✓ Settlement created (ID: {createSettl.data.id.slice(0, 8)}…)
              </p>
            )}
            {createSettl.isError && (
              <p className="text-red-600 text-sm text-center mt-2">{createSettl.error.message}</p>
            )}
          </>
        )}
      </section>
    </div>
  )
}

// ── Shared micro-components ────────────────────────────────────────────────

function inputCls(error?: { message?: string }) {
  return [
    'w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2',
    error
      ? 'border-red-400 focus:ring-red-400'
      : 'border-gray-300 focus:ring-blue-500',
  ].join(' ')
}

function Field({ label, error, children }: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  )
}

function Row({ label, value, highlight = false }: {
  label: string
  value: string
  highlight?: boolean
}) {
  return (
    <div className="flex justify-between items-center text-sm">
      <dt className="text-gray-500">{label}</dt>
      <dd className={highlight ? 'font-semibold text-gray-900' : 'text-gray-700'}>{value}</dd>
    </div>
  )
}

function Spinner() {
  return (
    <svg
      className="animate-spin h-8 w-8 text-blue-500"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8v8H4z"
      />
    </svg>
  )
}
