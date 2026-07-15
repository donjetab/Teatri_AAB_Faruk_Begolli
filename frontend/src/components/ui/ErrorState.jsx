import { useTranslation } from 'react-i18next'

export function ErrorState({ message }) {
  const { t } = useTranslation()
  return <p className="state-message state-message-error">{message ?? t('states.error')}</p>
}
