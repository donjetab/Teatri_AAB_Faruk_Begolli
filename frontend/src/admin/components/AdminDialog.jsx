import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react'
import { useAdminLanguage } from '../AdminLanguageContext'

const AdminDialogContext = createContext(null)

export function AdminDialogProvider({ children }) {
  const { t } = useAdminLanguage()
  const [dialog, setDialog] = useState(null)
  const [value, setValue] = useState('')
  const resolver = useRef(null)

  const open = useCallback(options => new Promise(resolve => {
    resolver.current = resolve
    setValue(options.defaultValue ?? '')
    setDialog(options)
  }), [])

  const confirm = useCallback(options => open({ type: 'confirm', ...options }), [open])
  const prompt = useCallback(options => open({ type: 'prompt', ...options }), [open])
  const close = useCallback(result => {
    resolver.current?.(result)
    resolver.current = null
    setDialog(null)
  }, [])

  useEffect(() => {
    if (!dialog) return
    const onKeyDown = event => {
      if (event.key === 'Escape') close(dialog.type === 'confirm' ? false : null)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [dialog, close])

  return <AdminDialogContext.Provider value={{ confirm, prompt }}>
    {children}
    {dialog && <div className="admin-modal-backdrop admin-dialog-backdrop" onMouseDown={event => event.target === event.currentTarget && close(dialog.type === 'confirm' ? false : null)}>
      <section className="admin-dialog" role="dialog" aria-modal="true" aria-labelledby="admin-dialog-title">
        <div className={`admin-dialog-icon ${dialog.danger ? 'danger' : ''}`}>{dialog.danger ? '!' : '◇'}</div>
        <div>
          <span className="admin-dialog-eyebrow">{t(dialog.eyebrow ?? (dialog.danger ? 'Please confirm' : 'Admin action'))}</span>
          <h2 id="admin-dialog-title">{t(dialog.title)}</h2>
          {dialog.message && <p>{t(dialog.message)}</p>}
        </div>
        {dialog.type === 'prompt' && <label>{t(dialog.label)}<input autoFocus type={dialog.inputType ?? 'text'} value={value} onChange={event => setValue(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); close(value) } }} /></label>}
        <div className="admin-dialog-actions">
          <button type="button" className="admin-text-button" onClick={() => close(dialog.type === 'confirm' ? false : null)}>{t('Cancel')}</button>
          <button type="button" className={dialog.danger ? 'admin-danger-button' : 'admin-primary-button'} onClick={() => close(dialog.type === 'confirm' ? true : value)}>{t(dialog.confirmLabel ?? (dialog.type === 'confirm' ? 'Confirm' : 'Save'))}</button>
        </div>
      </section>
    </div>}
  </AdminDialogContext.Provider>
}

export function useAdminDialog() {
  const context = useContext(AdminDialogContext)
  if (!context) throw new Error('useAdminDialog must be used inside AdminDialogProvider.')
  return context
}
