const paths = {
  calendar: <><rect x="4" y="5" width="16" height="15" rx="2" /><path d="M8 3v4M16 3v4M4 10h16M8 14h.01M12 14h.01M16 14h.01M8 17h.01M12 17h.01" /></>,
  person: <><circle cx="12" cy="8" r="3" /><path d="M6 20c.5-4 2.5-6 6-6s5.5 2 6 6" /></>,
  people: <><circle cx="9" cy="8" r="3" /><circle cx="17" cy="9" r="2" /><path d="M3 20c.5-4 2.5-6 6-6s5.5 2 6 6M15 15c3 0 4.5 1.5 5 4" /></>,
  book: <><path d="M4 5c3-1 5-.5 8 1v14c-3-1.5-5-2-8-1V5ZM20 5c-3-1-5-.5-8 1v14c3-1.5 5-2 8-1V5Z" /></>,
  stage: <><path d="M4 6h16v12H4zM8 6v12M16 6v12M4 10h16" /></>,
  music: <><path d="M9 18V6l10-2v12M9 10l10-2" /><circle cx="6.5" cy="18" r="2.5" /><circle cx="16.5" cy="16" r="2.5" /></>,
  light: <><circle cx="12" cy="10" r="4" /><path d="M12 2v2M4 10H2M22 10h-2M5.5 3.5 7 5M17 5l1.5-1.5M9 16h6M10 20h4" /></>,
  network: <><circle cx="12" cy="5" r="2" /><circle cx="5" cy="18" r="2" /><circle cx="19" cy="18" r="2" /><path d="m11 7-5 9M13 7l5 9M7 18h10" /></>,
  masks: <><path d="M3 6c3 0 5-1 8-2v8c0 4-2 6-4 7-2-1-4-3-4-7V6ZM13 6c3 0 5-1 8-2v8c0 4-2 6-4 7-1-.5-2-1.5-3-3" /><path d="M5.5 9h1M8.5 8h1M5.5 14c1 .8 2 .8 3 0M15 9h1M18 8h1M15 14c1-.8 2-.8 3 0" /></>,
}

const iconForType = {
  cast: 'masks',
  author: 'book',
  dramaturge: 'book',
  scenography: 'stage',
  costumes: 'stage',
  music: 'music',
  orchestration: 'music',
  lighting: 'light',
  organizer: 'network',
  coordinator: 'network',
  producer: 'network',
}

export function CreditIcon({ type = 'person' }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      {paths[iconForType[type] ?? 'person']}
    </svg>
  )
}

export function CalendarIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true">{paths.calendar}</svg>
}
