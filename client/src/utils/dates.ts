/*
 * Dates are shown in Norwegian time, the same as Subapp 1 (PointRules.NorwegianDate),
 * but in English words like the rest of the app, e.g. "27 Nov 2026".
 */

/* The API stores times in UTC. If the text has no time zone, it is read as UTC. */
function parseUtc(value: string): Date {
    const hasZone = /([zZ]|[+-]\d{2}:\d{2})$/.test(value)
    return new Date(hasZone ? value : `${value}Z`)
}

export function formatDate(utcValue: string): string {
    return parseUtc(utcValue).toLocaleDateString('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        timeZone: 'Europe/Oslo',
    })
}

/* A date without time, e.g. "2026-11-23" becomes "23 November" */
export function formatDayMonth(dateOnly: string): string {
    const [year, month, day] = dateOnly.split('-').map(Number)
    return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', timeZone: 'UTC' })
}