import { useTranslation } from 'react-i18next'

export function LoadingState() {
  const { t } = useTranslation()
  return <p className="state-message">{t('states.loading')}</p>
}
