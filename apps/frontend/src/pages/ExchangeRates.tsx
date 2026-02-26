import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { currencyApi } from '../services/api'
import type { UpdateExchangeRateRequest, CurrencyCode } from '../types'
import { format } from 'date-fns'

const schema = z.object({
  fromCurrency: z.enum(['BRL', 'USD']),
  toCurrency:   z.enum(['BRL', 'USD']),
  rate:         z.coerce.number().positive('Rate must be positive'),
  source:       z.string().optional(),
})
type FormValues = z.infer<typeof schema>

export default function ExchangeRates() {
  const queryClient = useQueryClient()

  const { register, handleSubmit, watch, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { fromCurrency: 'USD', toCurrency: 'BRL' },
  })

  const from = watch('fromCurrency') as CurrencyCode
  const to   = watch('toCurrency')   as CurrencyCode

  const { data: currentRate, isLoading } = useQuery({
    queryKey: ['exchange-rate', from, to],
    queryFn: () => currencyApi.getLatestRate(from, to),
    enabled: from !== to,
  })

  const updateRate = useMutation<unknown, Error, UpdateExchangeRateRequest>({
    mutationFn: currencyApi.updateRate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exchange-rate'] })
      reset()
    },
  })

  function onSubmit(data: FormValues) {
    updateRate.mutate(data as UpdateExchangeRateRequest)
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-2xl">
      {/* Current rate */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
        <h2 className="text-base font-semibold text-gray-800 mb-4">Current Rate</h2>
        {isLoading && <p className="text-gray-400 text-sm">Loading…</p>}
        {currentRate && (
          <dl className="space-y-2 text-sm">
            <InfoRow label="Pair" value={`${currentRate.fromCurrency} → ${currentRate.toCurrency}`} />
            <InfoRow label="Rate" value={currentRate.rate.toFixed(6)} highlight />
            <InfoRow label="Source" value={currentRate.source ?? '—'} />
            <InfoRow
              label="Updated"
              value={format(new Date(currentRate.updatedAt), 'dd/MM/yyyy HH:mm')}
            />
          </dl>
        )}
        {!isLoading && !currentRate && from === to && (
          <p className="text-gray-400 text-sm">From and To currencies must differ</p>
        )}
      </div>

      {/* Update rate form */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
        <h2 className="text-base font-semibold text-gray-800 mb-4">Update Rate</h2>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <Field label="From" error={errors.fromCurrency?.message}>
            <select {...register('fromCurrency')} className={inputCls(errors.fromCurrency)}>
              <option value="USD">USD</option>
              <option value="BRL">BRL</option>
            </select>
          </Field>
          <Field label="To" error={errors.toCurrency?.message}>
            <select {...register('toCurrency')} className={inputCls(errors.toCurrency)}>
              <option value="BRL">BRL</option>
              <option value="USD">USD</option>
            </select>
          </Field>
          <Field label="Rate" error={errors.rate?.message}>
            <input
              {...register('rate')}
              type="number"
              step="0.000001"
              className={inputCls(errors.rate)}
              placeholder="5.750000"
            />
          </Field>
          <Field label="Source (optional)" error={undefined}>
            <input {...register('source')} className={inputCls()} placeholder="Bloomberg, manual, …" />
          </Field>
          <button
            type="submit"
            disabled={updateRate.isPending}
            className="w-full py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300
                       text-white text-sm font-medium rounded-lg transition-colors"
          >
            {updateRate.isPending ? 'Saving…' : 'Update Rate'}
          </button>
          {updateRate.isSuccess && (
            <p className="text-green-600 text-xs text-center">Rate updated ✓</p>
          )}
          {updateRate.isError && (
            <p className="text-red-600 text-xs text-center">{updateRate.error.message}</p>
          )}
        </form>
      </div>
    </div>
  )
}

function inputCls(error?: unknown) {
  return [
    'w-full rounded border px-3 py-1.5 text-sm focus:outline-none focus:ring-2',
    error ? 'border-red-400 focus:ring-red-400' : 'border-gray-300 focus:ring-blue-500',
  ].join(' ')
}
function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-500 mb-1">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-red-500">{error}</p>}
    </div>
  )
}
function InfoRow({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="flex justify-between">
      <dt className="text-gray-400">{label}</dt>
      <dd className={highlight ? 'font-bold text-gray-900' : 'text-gray-600'}>{value}</dd>
    </div>
  )
}
