import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { adminApi } from './api'

const AuthContext = createContext(null)

export function AdminAuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)
  useEffect(() => {
    adminApi.session().then(setUser).catch(() => setUser(null)).finally(() => setLoading(false))
  }, [])
  const value = useMemo(() => ({
    user, loading,
    login: async (data) => { const session = await adminApi.login(data); setUser(session); return session },
    logout: async () => { await adminApi.logout(); setUser(null) },
  }), [user, loading])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAdminAuth = () => useContext(AuthContext)

export function ProtectedAdminRoute() {
  const { user, loading } = useAdminAuth()
  const location = useLocation()
  if (loading) return <div className="admin-full-state">Loading secure session…</div>
  return user ? <Outlet /> : <Navigate to="/admin/login" replace state={{ from: location }} />
}
