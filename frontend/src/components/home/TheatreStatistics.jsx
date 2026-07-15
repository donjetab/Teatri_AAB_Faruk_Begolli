import { useTranslation } from 'react-i18next'

function formatCompact(value) {
  if (value >= 100000) {
    return `${Math.round(value / 1000)}K+`
  }

  if (value >= 1000) {
    return `${Math.round(value / 100) / 10}K+`
  }

  return `${value}+`
}

export function TheatreStatistics({ statistics }) {
  const { t } = useTranslation()

  const items = [
    {
      key: 'founded',
      value: statistics.foundedYear,
      label: t('home.stats.founded'),
      icon: '▦',
    },
    {
      key: 'performances',
      value: formatCompact(statistics.performancesCount),
      label: t('home.stats.performances'),
      icon: '◭',
    },
    {
      key: 'spectators',
      value: formatCompact(statistics.spectatorsCount),
      label: t('home.stats.spectators'),
      icon: '≋',
    },
  ]

  return (
    <dl className="theatre-stats">
      {items.map((item) => (
        <div key={item.key} className="stat-item">
          <dt>
            <span aria-hidden="true">{item.icon}</span>
            {item.label}
          </dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  )
}
