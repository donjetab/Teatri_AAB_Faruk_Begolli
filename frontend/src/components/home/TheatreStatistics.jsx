import { useTranslation } from 'react-i18next'
import theatreIcon from '../../assets/theatre-icon.png'
import actingIcon from '../../assets/acting-icon.png'
import spectatorsIcon from '../../assets/spectators-icon.png'

function formatCompact(value) {
  if (value >= 100000) {
    return `${Math.round(value / 1000)}K+`
  }

  if (value >= 1000) {
    return `${Math.round(value / 100) / 10}K+`
  }

  return `${value}+`
}

export function TheatreStatistics({ statistics, managedStatistics }) {
  const { t } = useTranslation()

  const fallbackItems = [
    {
      key: 'founded',
      value: statistics.foundedYear,
      label: t('home.stats.founded'),
      icon: theatreIcon,
    },
    {
      key: 'performances',
      value: formatCompact(statistics.performancesCount),
      label: t('home.stats.performances'),
      icon: actingIcon,
    },
    {
      key: 'spectators',
      value: formatCompact(statistics.spectatorsCount),
      label: t('home.stats.spectators'),
      icon: spectatorsIcon,
    },
  ]
  const icons = [theatreIcon, actingIcon, spectatorsIcon]
  const items = managedStatistics?.some((item) => item.value || item.label)
    ? managedStatistics.map((item, index) => ({
        key: `managed-${index}`,
        value: item.value,
        label: item.label,
        icon: icons[index],
      }))
    : fallbackItems

  return (
    <dl className="theatre-stats">
      {items.map((item) => (
        <div key={item.key} className="stat-item">
          <img src={item.icon} alt="" aria-hidden="true" />
          <dd>{item.value}</dd>
          <dt>{item.label}</dt>
        </div>
      ))}
    </dl>
  )
}
