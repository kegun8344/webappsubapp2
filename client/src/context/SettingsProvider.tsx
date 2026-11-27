import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useAuth } from '../hooks/useAuth'
import * as settingsService from '../services/settingsService'
import { SettingsContext, type SettingsContextValue } from './settingsContext'
import { defaultSettings, type Settings } from '../types/settings'

/*
 * Loads the signed-in user's appearance settings and puts them on <html>,
 * the same attributes _Layout.cshtml sets in Subapp 1: data-bs-theme, data-motion and data-cvd.
 */
export default function SettingsProvider({ children }: { children: ReactNode }) {
    const { user } = useAuth()

    /* Remembers whose settings were loaded, so a new login never shows the previous user's settings */
    const [loaded, setLoaded] = useState<{ userId: string; settings: Settings } | null>(null)

    const settings = user && loaded?.userId === user.id ? loaded.settings : defaultSettings

    /* Load the settings when someone signs in */
    useEffect(() => {
        if (!user) return
        let active = true

        settingsService
            .getSettings()
            .then((data) => {
                if (active) setLoaded({ userId: user.id, settings: data })
            })
            .catch(() => {
                /* The defaults are used if the settings cannot be loaded */
            })

        return () => {
            active = false
        }
    }, [user])

    /* Bootstrap reads data-bs-theme, index.css reads data-motion and data-cvd */
    const { darkModeEnabled, reduceMotion, colorVisionMode } = settings
    useEffect(() => {
        const root = document.documentElement
        root.dataset.bsTheme = darkModeEnabled ? 'dark' : 'light'
        root.dataset.motion = reduceMotion ? 'reduce' : 'normal'
        root.dataset.cvd = colorVisionMode.toLowerCase()
    }, [darkModeEnabled, reduceMotion, colorVisionMode])

    const saveSettings = useCallback(async (next: Settings) => {
        if (!user) return
        const saved = await settingsService.updateSettings(next)
        setLoaded({ userId: user.id, settings: saved })
    }, [user])

    /* The same object until something changes, so pages do not render again for nothing */
    const value = useMemo<SettingsContextValue>(() => ({ settings, saveSettings }), [settings, saveSettings])

    return <SettingsContext.Provider value={value}>{children}</SettingsContext.Provider>
}