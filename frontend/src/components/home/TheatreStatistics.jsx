import { useEffect, useRef, useState } from 'react'
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

function animatedValue(value, progress) {
  const text = String(value ?? '')
  const match = text.trim().match(/^(\d+(?:[.,]\d+)?)(.*)$/)
  if (!match) return text
  const target = Number(match[1].replace(',', '.'))
  const decimals = match[1].includes('.') || match[1].includes(',') ? 1 : 0
  const current = target * progress
  return `${decimals ? current.toFixed(decimals) : Math.round(current)}${match[2]}`
}

export function TheatreStatistics({ statistics, managedStatistics }) {
  const { t } = useTranslation()
  const statsRef = useRef(null)
  const [progress, setProgress] = useState(0)

  useEffect(() => {
    const element = statsRef.current
    if (!element) return undefined
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setProgress(1)
      return undefined
    }
    let frame
    const observer = new IntersectionObserver(([entry]) => {
      if (!entry.isIntersecting) return
      observer.disconnect()
      const startedAt = performance.now()
      const duration = 1500
      const tick = now => {
        const elapsed = Math.min(1, (now - startedAt) / duration)
        setProgress(1 - Math.pow(1 - elapsed, 3))
        if (elapsed < 1) frame = requestAnimationFrame(tick)
      }
      frame = requestAnimationFrame(tick)
    }, { threshold: 0.35 })
    observer.observe(element)
    return () => { observer.disconnect(); if (frame) cancelAnimationFrame(frame) }
  }, [])

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
    <dl className="theatre-stats" ref={statsRef}>
      {items.map((item) => (
        <div key={item.key} className="stat-item">
          <img src={item.icon} alt="" aria-hidden="true" />
          <dd aria-label={String(item.value ?? '')}>{animatedValue(item.value, progress)}</dd>
          <dt>{item.label}</dt>
        </div>
      ))}
    </dl>
  )
}
