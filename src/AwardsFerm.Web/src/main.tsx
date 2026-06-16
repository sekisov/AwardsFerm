import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import './index.css'

// StrictMode отключён: двойной mount ломает SignalR negotiation в dev.
createRoot(document.getElementById('root')!).render(<App />)
