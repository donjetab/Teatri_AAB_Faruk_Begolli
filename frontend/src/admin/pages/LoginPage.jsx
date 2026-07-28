import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAdminAuth } from '../AuthContext'
import logo from '../../assets/theatre-icon.png'

export function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '', rememberMe: false })
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const { login } = useAdminAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const submit = async (event) => {
    event.preventDefault(); setError(''); setBusy(true)
    try { await login(form); navigate(location.state?.from?.pathname || '/admin', { replace: true }) }
    catch { setError('The email or password is incorrect.') }
    finally { setBusy(false) }
  }
  return <main className="admin-login"><section className="admin-login-card">
    <div className="admin-login-brand"><img src={logo} alt="" /><span>TEATRI AAB</span><small>ADMIN PANEL</small></div>
    <div className="admin-login-heading"><span>Secure administration</span><h1>Welcome back</h1><p>Sign in to manage the Teatri AAB public website.</p></div>
    <form onSubmit={submit}>
      <label>Email address<div className="admin-login-field"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 6h16v12H4zM4 7l8 6 8-6" /></svg><input autoComplete="username" type="email" required value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} placeholder="admin@teatriaab.com" /></div></label>
      <label>Password<div className="admin-login-field"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 11V8a5 5 0 0 1 10 0v3M5 11h14v10H5z" /></svg><input autoComplete="current-password" type="password" required value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} placeholder="Enter your password" /></div></label>
      {error && <div className="admin-form-error">{error}</div>}
      <button className="admin-primary-button admin-login-submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}<span>→</span></button>
    </form>
    <a href="#/sq">← Return to the public website</a>
  </section></main>
}
