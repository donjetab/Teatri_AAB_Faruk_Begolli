import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { apiClient } from '../../api/client'

export function NewsletterForm({ language }) {
  const { t } = useTranslation()
  const [email, setEmail] = useState('')
  const [status, setStatus] = useState('idle')
  const [message, setMessage] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    setStatus('loading')
    setMessage('')

    try {
      await apiClient.post('/api/newsletter/subscribe', {
        email,
        preferredLanguageCode: language,
      })
      setStatus('success')
      setMessage(t('newsletter.success'))
      setEmail('')
    } catch (error) {
      setStatus('error')
      const detail = error.response?.data?.detail
      setMessage(detail ?? t('newsletter.error'))
    }
  }

  return (
    <form className="newsletter-form" onSubmit={handleSubmit}>
      <p>{t('footer.newsletterText')}</p>
      <div className="newsletter-control">
        <label htmlFor="newsletter-email" className="sr-only">
          {t('newsletter.emailLabel')}
        </label>
        <input
          id="newsletter-email"
          type="email"
          value={email}
          placeholder={t('newsletter.placeholder')}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
        <button type="submit" disabled={status === 'loading'} aria-label={t('newsletter.submit')}>
          →
        </button>
      </div>
      {message && (
        <p className={status === 'success' ? 'newsletter-message success' : 'newsletter-message error'}>
          {message}
        </p>
      )}
    </form>
  )
}
