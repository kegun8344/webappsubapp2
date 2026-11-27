import { useContext } from 'react'
import { SettingsContext } from '../context/settingsContext'

/* The appearance settings and saveSettings, from anywhere in the app */
export function useSettings() {
    const context = useContext(SettingsContext)

    if (!context) {
        throw new Error('useSettings must be used inside <SettingsProvider>.')
    }

    return context
}