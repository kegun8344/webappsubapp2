import { createContext } from 'react'
import type { Settings } from '../types/settings'

export interface SettingsContextValue {
    /* The signed-in user's settings, or the defaults */
    settings: Settings
    /* Saves to the API and applies the new look straight away */
    saveSettings: (settings: Settings) => Promise<void>
}

export const SettingsContext = createContext<SettingsContextValue | null>(null)