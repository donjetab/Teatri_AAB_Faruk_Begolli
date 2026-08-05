import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// import { BrowserRouter } from 'react-router-dom'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import './i18n'
import App from './App.jsx'

if (window.location.hash.startsWith('#/')) {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '')
  window.location.replace(`${base}${window.location.hash.slice(1)}`)
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    {/* <BrowserRouter> */}
    <BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '')}>
      <App />
    </BrowserRouter>
    {/* </BrowserRouter> */}
  </StrictMode>,
)
