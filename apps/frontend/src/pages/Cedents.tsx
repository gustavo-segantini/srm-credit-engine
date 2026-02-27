import { useState, Fragment } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { cedentsApi } from '../services/api'
import type { CedentResponse, UpdateCedentRequest } from '../types'
import { format } from 'date-fns'

// ── Validation schemas ────────────────────────────────────────────────────────

const createSchema = z.object({
  name:         z.string().min(2, 'Name must be at least 2 characters'),
  cnpj:         z.string().regex(/^\d{14}$/, 'CNPJ must be 14 digits (no punctuation)'),
  contactEmail: z.string().email('Invalid email address'),
})
type CreateForm = z.infer<typeof createSchema>

const updateSchema = z.object({
  name:         z.string().min(2, 'Name must be at least 2 characters'),
  contactEmail: z.string().email('Invalid email address'),
})
type UpdateForm = z.infer<typeof updateSchema>

// ── Page component ────────────────────────────────────────────────────────────

export default function Cedents() {
  const queryClient = useQueryClient()
  const [editingId, setEditingId] = useState<string | null>(null)

  const { data: cedents = [], isLoading, isError } = useQuery({
    queryKey: ['cedents'],
    queryFn:  cedentsApi.getAll,
    staleTime: 30_000,
  })

  // ── Create mutation ──────────────────────────────────────────────────────
  const {
    register: regCreate,
    handleSubmit: handleCreate,
    reset: resetCreate,
    formState: { errors: createErrors },
  } = useForm<CreateForm>({ resolver: zodResolver(createSchema) })

  const createMutation = useMutation<CedentResponse, Error, CreateForm>({
    mutationFn: cedentsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cedents'] })
      resetCreate()
    },
  })

  // ── Update mutation ──────────────────────────────────────────────────────
  const {
    register: regUpdate,
    handleSubmit: handleUpdate,
    reset: resetUpdate,
    setValue: setUpdateValue,
    formState: { errors: updateErrors },
  } = useForm<UpdateForm>({ resolver: zodResolver(updateSchema) })

  const updateMutation = useMutation<CedentResponse, Error, { id: string; data: UpdateCedentRequest }>({
    mutationFn: ({ id, data }) => cedentsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cedents'] })
      setEditingId(null)
      resetUpdate()
    },
  })

  // ── Deactivate mutation ──────────────────────────────────────────────────
  const deactivateMutation = useMutation<unknown, Error, string>({
    mutationFn: (id) => cedentsApi.deactivate(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['cedents'] }),
  })

  function startEdit(cedent: CedentResponse) {
    setEditingId(cedent.id)
    setUpdateValue('name', cedent.name)
    setUpdateValue('contactEmail', cedent.contactEmail)
  }

  function cancelEdit() {
    setEditingId(null)
    resetUpdate()
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold text-gray-800">Cedent Management</h1>

      {/* ── Create form ── */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 max-w-lg">
        <h2 className="text-base font-semibold text-gray-800 mb-4">Register New Cedent</h2>
        <form onSubmit={handleCreate((d) => createMutation.mutate(d))} className="space-y-3">
          <Field label="Company Name" error={createErrors.name?.message}>
            <input
              {...regCreate('name')}
              className={inputCls(createErrors.name)}
              placeholder="Empresa LTDA"
            />
          </Field>
          <Field label="CNPJ (14 digits, no punctuation)" error={createErrors.cnpj?.message}>
            <input
              {...regCreate('cnpj')}
              className={inputCls(createErrors.cnpj)}
              placeholder="12345678000195"
              maxLength={14}
            />
          </Field>
          <Field label="Contact Email" error={createErrors.contactEmail?.message}>
            <input
              {...regCreate('contactEmail')}
              type="email"
              className={inputCls(createErrors.contactEmail)}
              placeholder="financeiro@empresa.com.br"
            />
          </Field>

          <button
            type="submit"
            disabled={createMutation.isPending}
            className="w-full py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {createMutation.isPending ? 'Registering…' : 'Register Cedent'}
          </button>

          {createMutation.isSuccess && (
            <p className="text-green-600 text-xs text-center">Cedent registered ✓</p>
          )}
          {createMutation.isError && (
            <p className="text-red-600 text-xs text-center">{createMutation.error.message}</p>
          )}
        </form>
      </div>

      {/* ── Cedent list ── */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-4 border-b border-gray-100">
          <h2 className="text-base font-semibold text-gray-800">Active Cedents</h2>
        </div>

        {isLoading && (
          <p className="text-gray-400 text-sm p-5">Loading cedents…</p>
        )}
        {isError && (
          <p className="text-red-500 text-sm p-5">Failed to load cedents.</p>
        )}
        {!isLoading && cedents.length === 0 && (
          <p className="text-gray-400 text-sm p-5">No active cedents found.</p>
        )}

        {cedents.length > 0 && (
          <table className="min-w-full divide-y divide-gray-100 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Name', 'CNPJ', 'Email', 'Since', 'Actions'].map((h) => (
                  <th key={h} className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {cedents.map((c) => (
                <Fragment key={c.id}>
                  <tr className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3 font-medium text-gray-900">{c.name}</td>
                    <td className="px-4 py-3 font-mono text-gray-600">{c.cnpj}</td>
                    <td className="px-4 py-3 text-gray-600">{c.contactEmail}</td>
                    <td className="px-4 py-3 text-gray-400">
                      {format(new Date(c.createdAt), 'dd/MM/yyyy')}
                    </td>
                    <td className="px-4 py-3 flex gap-2">
                      <button
                        onClick={() => startEdit(c)}
                        className="text-xs px-2 py-1 rounded bg-amber-50 text-amber-700 border border-amber-200 hover:bg-amber-100 transition-colors"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => {
                          if (confirm(`Deactivate "${c.name}"?`)) deactivateMutation.mutate(c.id)
                        }}
                        disabled={deactivateMutation.isPending}
                        className="text-xs px-2 py-1 rounded bg-red-50 text-red-700 border border-red-200 hover:bg-red-100 transition-colors disabled:opacity-50"
                      >
                        Deactivate
                      </button>
                    </td>
                  </tr>

                  {/* Inline edit row */}
                  {editingId === c.id && (
                    <tr key={`${c.id}-edit`} className="bg-blue-50">
                      <td colSpan={5} className="px-4 py-3">
                        <form
                          onSubmit={handleUpdate((d) => updateMutation.mutate({ id: c.id, data: d }))}
                          className="flex flex-wrap gap-3 items-end"
                        >
                          <Field label="Name" error={updateErrors.name?.message} inline>
                            <input
                              {...regUpdate('name')}
                              className={inputCls(updateErrors.name, true)}
                            />
                          </Field>
                          <Field label="Email" error={updateErrors.contactEmail?.message} inline>
                            <input
                              {...regUpdate('contactEmail')}
                              type="email"
                              className={inputCls(updateErrors.contactEmail, true)}
                            />
                          </Field>
                          <div className="flex gap-2 pb-0.5">
                            <button
                              type="submit"
                              disabled={updateMutation.isPending}
                              className="text-xs px-3 py-1.5 rounded bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
                            >
                              {updateMutation.isPending ? 'Saving…' : 'Save'}
                            </button>
                            <button
                              type="button"
                              onClick={cancelEdit}
                              className="text-xs px-3 py-1.5 rounded bg-white text-gray-600 border border-gray-300 hover:bg-gray-50 transition-colors"
                            >
                              Cancel
                            </button>
                          </div>
                          {updateMutation.isError && (
                            <p className="text-red-500 text-xs w-full">{updateMutation.error.message}</p>
                          )}
                        </form>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function inputCls(error?: unknown, compact = false) {
  return [
    'rounded border px-3 text-sm focus:outline-none focus:ring-2',
    compact ? 'py-1 w-48' : 'py-1.5 w-full',
    error
      ? 'border-red-400 focus:ring-red-400'
      : 'border-gray-300 focus:ring-blue-500',
  ].join(' ')
}

function Field({
  label,
  error,
  children,
  inline = false,
}: {
  label: string
  error?: string
  children: React.ReactNode
  inline?: boolean
}) {
  return (
    <div className={inline ? 'flex flex-col' : undefined}>
      <label className="block text-xs font-medium text-gray-500 mb-1">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-red-500">{error}</p>}
    </div>
  )
}
