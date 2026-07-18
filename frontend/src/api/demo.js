let demoDataPromise

function loadDemoData(signal) {
  demoDataPromise ??= fetch(`${import.meta.env.BASE_URL}demo-data.json`, { signal })
    .then((response) => {
      if (!response.ok) {
        throw new Error('Demo data is unavailable.')
      }
      return response.json()
    })
    .catch((error) => {
      demoDataPromise = undefined
      throw error
    })

  return demoDataPromise
}

export async function getDemoHome(language, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.home ?? data.sq.home
}

export async function getDemoShows(language, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.shows ?? data.sq.shows
}

export async function getDemoShow(language, slug, signal) {
  const data = await loadDemoData(signal)
  return data[language]?.showDetails?.[slug] ?? data.sq.showDetails[slug]
}

export function isCanceledRequest(error) {
  return error?.name === 'CanceledError' ||
    error?.name === 'AbortError' ||
    error?.code === 'ERR_CANCELED'
}
