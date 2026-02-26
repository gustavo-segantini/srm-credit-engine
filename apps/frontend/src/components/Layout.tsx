import { NavLink, Outlet } from 'react-router-dom'
import { clsx } from 'clsx'

const nav = [
  { to: '/',             label: 'Simulator'     },
  { to: '/transactions', label: 'Transactions'  },
  { to: '/rates',        label: 'Exchange Rates' },
]

export default function Layout() {
  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      {/* Top bar */}
      <header className="bg-blue-700 text-white shadow">
        <div className="max-w-7xl mx-auto px-4 h-14 flex items-center gap-8">
          <span className="font-bold text-lg tracking-tight">SRM Credit Engine</span>
          <nav className="flex gap-4">
            {nav.map(({ to, label }) => (
              <NavLink
                key={to}
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  clsx(
                    'text-sm font-medium px-2 py-1 rounded transition-colors',
                    isActive
                      ? 'bg-white/20 text-white'
                      : 'text-blue-100 hover:text-white hover:bg-white/10',
                  )
                }
              >
                {label}
              </NavLink>
            ))}
          </nav>
        </div>
      </header>

      {/* Page content */}
      <main className="flex-1 max-w-7xl mx-auto w-full px-4 py-6">
        <Outlet />
      </main>

      <footer className="text-center text-xs text-gray-400 py-3">
        SRM Credit Engine © {new Date().getFullYear()}
      </footer>
    </div>
  )
}
