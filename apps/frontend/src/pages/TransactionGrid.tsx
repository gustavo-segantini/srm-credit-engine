import { useState } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  createColumnHelper,
} from '@tanstack/react-table'
import { format } from 'date-fns'
import { useQuery } from '@tanstack/react-query'
import { useSettlementStatement } from '../hooks/useSettlements'
import { cedentsApi } from '../services/api'
import type { SettlementStatementItemResponse, CurrencyCode } from '../types'

const colHelper = createColumnHelper<SettlementStatementItemResponse>()

const STATUS_BADGE: Record<string, string> = {
  Pending:   'bg-yellow-100 text-yellow-800',
  Settled:   'bg-green-100 text-green-800',
  Failed:    'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-600',
}

function fmt(v: number, curr: string) {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: curr,
    minimumFractionDigits: 2,
  }).format(v)
}

const columns = [
  colHelper.accessor('documentNumber', { header: 'Document #', size: 140 }),
  colHelper.accessor('receivableType', { header: 'Type', size: 160 }),
  colHelper.accessor('cedentName',     { header: 'Cedent',    size: 180 }),
  colHelper.accessor('faceValue', {
    header: 'Face Value',
    cell: (ctx) => fmt(ctx.getValue(), ctx.row.original.paymentCurrency),
  }),
  colHelper.accessor('netDisbursement', {
    header: 'Net Disbursement',
    cell: (ctx) => fmt(ctx.getValue(), ctx.row.original.paymentCurrency),
  }),
  colHelper.accessor('discount', {
    header: 'Discount',
    cell: (ctx) => fmt(ctx.getValue(), ctx.row.original.paymentCurrency),
  }),
  colHelper.accessor('paymentCurrency',{ header: 'Currency', size: 90 }),
  colHelper.accessor('status', {
    header: 'Status',
    cell: (ctx) => (
      <span
        className={
          'inline-block text-xs font-medium px-2 py-0.5 rounded-full ' +
          (STATUS_BADGE[ctx.getValue()] ?? 'bg-gray-100 text-gray-600')
        }
      >
        {ctx.getValue()}
      </span>
    ),
  }),
  colHelper.accessor('createdAt', {
    header: 'Created',
    cell: (ctx) => format(new Date(ctx.getValue()), 'dd/MM/yyyy HH:mm'),
  }),
]

export default function TransactionGrid() {
  const [page, setPage] = useState(1)
  const [from, setFrom] = useState('')
  const [to, setTo]     = useState('')
  const [currency, setCurrency] = useState<CurrencyCode | ''>('')
  const [cedentId, setCedentId] = useState('')
  const PAGE_SIZE = 15

  const { data: cedents = [] } = useQuery({
    queryKey: ['cedents'],
    queryFn: cedentsApi.getAll,
    staleTime: 60_000,
  })

  const { data, isFetching, isError, error } = useSettlementStatement({
    from:            from || undefined,
    to:              to   || undefined,
    cedentId:        cedentId || undefined,
    paymentCurrency: currency as CurrencyCode | undefined,
    page,
    pageSize: PAGE_SIZE,
  })

  const table = useReactTable({
    data:             data?.items ?? [],
    columns,
    getCoreRowModel:  getCoreRowModel(),
    manualPagination: true,
    pageCount:        data?.totalPages ?? 1,
  })

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3">
        <h1 className="text-lg font-semibold text-gray-800 mr-auto">
          Settlement Transactions
        </h1>

        {/* Filters */}
        <label className="flex flex-col gap-1 text-xs text-gray-500">
          From
          <input
            type="date"
            value={from}
            onChange={(e) => { setFrom(e.target.value); setPage(1) }}
            className="border rounded px-2 py-1 text-sm"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-gray-500">
          To
          <input
            type="date"
            value={to}
            onChange={(e) => { setTo(e.target.value); setPage(1) }}
            className="border rounded px-2 py-1 text-sm"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-gray-500">
          Currency
          <select
            value={currency}
            onChange={(e) => { setCurrency(e.target.value as CurrencyCode); setPage(1) }}
            className="border rounded px-2 py-1 text-sm"
          >
            <option value="">All</option>
            <option value="BRL">BRL</option>
            <option value="USD">USD</option>
          </select>
        </label>
        <label className="flex flex-col gap-1 text-xs text-gray-500">
          Cedent
          <select
            value={cedentId}
            onChange={(e) => { setCedentId(e.target.value); setPage(1) }}
            className="border rounded px-2 py-1 text-sm min-w-[160px]"
          >
            <option value="">All cedents</option>
            {cedents.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </label>
      </div>

      {/* Aggregates strip */}
      {data && (
        <div className="grid grid-cols-3 gap-4">
          <AggCard label="Total Face Value"       value={fmt(data.totalFaceValue, 'BRL')} />
          <AggCard label="Total Net Disbursement" value={fmt(data.totalNetDisbursement, 'BRL')} />
          <AggCard label="Total Discount"         value={fmt(data.totalDiscount, 'BRL')} />
        </div>
      )}

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-auto">
        {isError && (
          <p className="p-4 text-red-600 text-sm">{(error as Error).message}</p>
        )}
        <table className="w-full text-sm text-left">
          <thead className="bg-gray-50 text-gray-500 uppercase text-xs">
            {table.getHeaderGroups().map((hg) => (
              <tr key={hg.id}>
                {hg.headers.map((h) => (
                  <th key={h.id} className="px-4 py-3 font-medium whitespace-nowrap">
                    {flexRender(h.column.columnDef.header, h.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isFetching && !data && (
              <tr>
                <td colSpan={columns.length} className="py-10 text-center text-gray-400">
                  Loading…
                </td>
              </tr>
            )}
            {!isFetching && table.getRowModel().rows.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="py-10 text-center text-gray-400">
                  No records found
                </td>
              </tr>
            )}
            {table.getRowModel().rows.map((row) => (
              <tr key={row.id} className="hover:bg-gray-50 transition-colors">
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id} className="px-4 py-3 whitespace-nowrap">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100 text-sm text-gray-600">
            <span>
              Page {data.page} of {data.totalPages} — {data.totalItems} records
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 rounded border disabled:opacity-40 hover:bg-gray-50"
              >
                ← Prev
              </button>
              <button
                onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                disabled={page === data.totalPages}
                className="px-3 py-1 rounded border disabled:opacity-40 hover:bg-gray-50"
              >
                Next →
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

function AggCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 shadow-sm px-4 py-3">
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className="text-base font-semibold text-gray-800">{value}</p>
    </div>
  )
}
