import { Routes, Route } from 'react-router-dom'
import { Suspense, lazy } from 'react'
import Layout from './components/Layout'

// Lazy-load pages for code-splitting
const OperatorPanel  = lazy(() => import('./pages/OperatorPanel'))
const TransactionGrid = lazy(() => import('./pages/TransactionGrid'))
const ExchangeRates   = lazy(() => import('./pages/ExchangeRates'))

function PageLoader() {
  return (
    <div className="flex items-center justify-center h-40 text-gray-400 text-sm">
      Loading…
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route
          index
          element={
            <Suspense fallback={<PageLoader />}>
              <OperatorPanel />
            </Suspense>
          }
        />
        <Route
          path="transactions"
          element={
            <Suspense fallback={<PageLoader />}>
              <TransactionGrid />
            </Suspense>
          }
        />
        <Route
          path="rates"
          element={
            <Suspense fallback={<PageLoader />}>
              <ExchangeRates />
            </Suspense>
          }
        />
        <Route path="*" element={<p className="text-gray-500 mt-10 text-center">404 — Page not found</p>} />
      </Route>
    </Routes>
  )
}

