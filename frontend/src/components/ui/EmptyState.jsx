import { useTranslation } from 'react-i18next'

export function EmptyState({ message }) {
  const { t } = useTranslation()
  return <p className="state-message">{message ?? t('states.empty')}</p>
}
