import { useEffect, useState } from 'react'
import { getPing } from '../services/pingService'
import { ApiError } from '../services/apiClient'
import type { Ping } from '../types/ping'

/* Start page. For now it only checks that React can talk to the API. */
export default function HomePage() {
    const [ping, setPing] = useState<Ping | null>(null)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        /* Stops a late answer from updating the page after the user has left it */
        let active = true

        getPing()
            .then((data) => {
                if (active) setPing(data)
            })
            .catch((err: unknown) => {
                if (active) setError(err instanceof ApiError ? err.message : 'Something went wrong.')
            })

        return () => {
            active = false
        }
    }, [])

    return (
        <>
            <h1 className="landing-title mb-2">StudyQuest</h1>
            <p className="lead text-secondary mb-4">Practise ITPE3200 with quizzes.</p>

            {/* Three states: waiting, failed, or the answer from the API */}
            {!ping && !error && <p className="text-secondary">Checking the API…</p>}

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            {ping && (
                <div className="alert alert-success" role="status">
                    <i className="bi bi-check-circle-fill me-2" aria-hidden="true"></i>
                    {ping.message}
                </div>
            )}
        </>
    )
}