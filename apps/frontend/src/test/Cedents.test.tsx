import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { vi, describe, it, expect, beforeEach } from 'vitest'
import Cedents from '../pages/Cedents'
import * as api from '../services/api'

// ── Mock the API module ───────────────────────────────────────────────────────
vi.mock('../services/api', () => ({
  cedentsApi: {
    getAll:     vi.fn().mockResolvedValue([]),
    create:     vi.fn(),
    update:     vi.fn(),
    deactivate: vi.fn(),
    getById:    vi.fn(),
  },
}))

// ── Test helper ───────────────────────────────────────────────────────────────

function renderCedents() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <Cedents />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('Cedents page', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    ;(api.cedentsApi.getAll as ReturnType<typeof vi.fn>).mockResolvedValue([])
  })

  it('renders the registration form', () => {
    renderCedents()
    expect(screen.getByText('Register New Cedent')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('Empresa LTDA')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('12345678000195')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('financeiro@empresa.com.br')).toBeInTheDocument()
  })

  it('shows validation error for invalid CNPJ', async () => {
    const user = userEvent.setup()
    renderCedents()

    await user.type(screen.getByPlaceholderText('Empresa LTDA'), 'Test Empresa')
    await user.type(screen.getByPlaceholderText('12345678000195'), '123') // too short
    await user.type(screen.getByPlaceholderText('financeiro@empresa.com.br'), 'email@test.com')
    await user.click(screen.getByText('Register Cedent'))

    await waitFor(() =>
      expect(screen.getByText('CNPJ must be 14 digits (no punctuation)')).toBeInTheDocument(),
    )
  })

  it('shows validation error for missing email', async () => {
    const user = userEvent.setup()
    renderCedents()

    // Valid name and CNPJ, but no email → should fail email validation
    await user.type(screen.getByPlaceholderText('Empresa LTDA'), 'Test Corp')
    await user.type(screen.getByPlaceholderText('12345678000195'), '12345678000195')
    // Leave email blank and submit
    await user.click(screen.getByText('Register Cedent'))

    await waitFor(() =>
      expect(screen.getByText('Invalid email address')).toBeInTheDocument(),
    )
  })

  it('calls cedentsApi.create on valid submission', async () => {
    const mockCreate = vi.fn().mockResolvedValue({
      id: 'abc-123',
      name: 'Empresa Valid',
      cnpj: '12345678000195',
      contactEmail: 'ok@empresa.com',
      isActive: true,
      createdAt: new Date().toISOString(),
    })
    ;(api.cedentsApi.create as ReturnType<typeof vi.fn>).mockImplementation(mockCreate)

    const user = userEvent.setup()
    renderCedents()

    await user.type(screen.getByPlaceholderText('Empresa LTDA'), 'Empresa Valid')
    await user.type(screen.getByPlaceholderText('12345678000195'), '12345678000195')
    await user.type(screen.getByPlaceholderText('financeiro@empresa.com.br'), 'ok@empresa.com')
    await user.click(screen.getByText('Register Cedent'))

    await waitFor(() => expect(mockCreate).toHaveBeenCalledOnce())
    // React Query passes a second metadata argument; check only the first arg
    expect(mockCreate).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Empresa Valid',
        cnpj: '12345678000195',
        contactEmail: 'ok@empresa.com',
      }),
      expect.anything(),
    )
  })

  it('renders "No active cedents found" when list is empty', async () => {
    renderCedents()
    await waitFor(() =>
      expect(screen.getByText('No active cedents found.')).toBeInTheDocument(),
    )
  })

  it('renders cedent rows when list has data', async () => {
    ;(api.cedentsApi.getAll as ReturnType<typeof vi.fn>).mockResolvedValue([
      {
        id: 'c1',
        name: 'Acme Corp',
        cnpj: '12345678000195',
        contactEmail: 'acme@corp.com',
        isActive: true,
        createdAt: '2025-01-01T00:00:00Z',
      },
    ])

    renderCedents()
    await waitFor(() => expect(screen.getByText('Acme Corp')).toBeInTheDocument())
    expect(screen.getByText('12345678000195')).toBeInTheDocument()
  })
})
