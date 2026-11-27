import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import MiniBadges from '../components/MiniBadges'
import { getLeaderboard } from '../services/statsService'
import type { Leaderboard, LeaderboardEntry } from '../types/stats'
import { formatDayMonth } from '../utils/dates'

/* The leaderboard loaded for one period, so an old answer is never shown for the other tab */
interface LoadedLeaderboard {
    period: string | null
    data: Leaderboard | null
    error: string | null
}

/* The streak flame, only from 2 days in a row */
function Streak({ days }: { days: number }) {
    if (days < 2) {
        return null
    }

    return (
        <span className="lb-streak" title={`${days} days in a row`}>
            <i className="bi bi-fire" aria-hidden="true"></i>
            {days}
            <span className="visually-hidden"> days in a row</span>
        </span>
    )
}

/* Students ranked by points, all time or this week. Same layout as Leaderboard/Index in Subapp 1. */
export default function LeaderboardPage() {
    const [searchParams, setSearchParams] = useSearchParams()
    const period = searchParams.get('period') === 'week' ? 'week' : 'all'

    const [loaded, setLoaded] = useState<LoadedLeaderboard>({ period: null, data: null, error: null })

    useEffect(() => {
        let active = true

        getLeaderboard(period)
            .then((data) => {
                if (active) setLoaded({ period, data, error: null })
            })
            .catch(() => {
                if (active) setLoaded({ period, data: null, error: 'The leaderboard could not be loaded. Please try again later.' })
            })

        return () => {
            active = false
        }
    }, [period])

    const isLoading = loaded.period !== period
    const data = loaded.data
    const weekly = period === 'week'

    const topThree = data?.entries.slice(0, 3) ?? []
    const others = data?.entries.slice(3) ?? []

    return (
        <>
            <p className="landing-eyebrow mb-2">Compete</p>
            <h1 className="landing-title mb-2">Leaderboard</h1>
            <p className="lead text-secondary mb-4">
                {weekly && data
                    ? `Points from quizzes made by teachers since Monday ${formatDayMonth(data.weekStart)}.`
                    : 'Students ranked by points from quizzes made by teachers.'}
            </p>

            <ul className="nav nav-pills mb-4" aria-label="Leaderboard period">
                <li className="nav-item">
                    <button
                        type="button"
                        className={`nav-link${weekly ? '' : ' active'}`}
                        aria-current={weekly ? undefined : 'page'}
                        onClick={() => setSearchParams({}, { replace: true })}
                    >
                        <i className="bi bi-trophy me-1" aria-hidden="true"></i>All time
                    </button>
                </li>
                <li className="nav-item">
                    <button
                        type="button"
                        className={`nav-link${weekly ? ' active' : ''}`}
                        aria-current={weekly ? 'page' : undefined}
                        onClick={() => setSearchParams({ period: 'week' }, { replace: true })}
                    >
                        <i className="bi bi-calendar-week me-1" aria-hidden="true"></i>This week
                    </button>
                </li>
            </ul>

            {isLoading ? (
                <LoadingSpinner message="Loading leaderboard..." />
            ) : !data ? (
                <ErrorAlert message={loaded.error} />
            ) : topThree.length === 0 ? (
                <p className="text-secondary fst-italic">No students on the leaderboard yet.</p>
            ) : (
                <>
                    {/* The signed-in student's own position. Teachers and administrators have none. */}
                    {data.standing && (
                        <section className="card border-0 shadow-sm lb-standing mb-5" aria-labelledby="standing-heading">
                            <div className="card-body p-4">
                                <div className="row g-4 align-items-center">
                                    <div className="col-md-auto text-center">
                                        <div className="lb-standing-rank">#{data.standing.rank}</div>
                                        <div className="small text-secondary">of {data.standing.studentCount} students</div>
                                    </div>
                                    <div className="col-md">
                                        <h2 id="standing-heading" className="h5 fw-bold mb-1">Your standing</h2>
                                        <p className="mb-3">
                                            You have <strong>{data.standing.points} points</strong> {weekly ? 'this week' : 'in total'}.{' '}
                                            {data.standing.nextPlaceName === null ? (
                                                <span className="fw-semibold text-success">
                                                    <i className="bi bi-trophy-fill" aria-hidden="true"></i> You are in the lead!
                                                </span>
                                            ) : (
                                                <>
                                                    That is <strong>{data.standing.pointsBehindNextPlace}</strong>{' '}
                                                    {data.standing.pointsBehindNextPlace === 1 ? 'point' : 'points'} behind{' '}
                                                    <strong>{data.standing.nextPlaceName}</strong>.
                                                </>
                                            )}
                                        </p>
                                        <div className="d-flex justify-content-between small text-secondary mb-1">
                                            <span>Level {data.standing.level}</span>
                                            <span>
                                                {data.standing.pointsToNextLevel} points to level {data.standing.level + 1}
                                            </span>
                                        </div>
                                        <div
                                            className="progress"
                                            role="progressbar"
                                            aria-label={`Progress to level ${data.standing.level + 1}`}
                                            aria-valuenow={data.standing.levelProgressPercent}
                                            aria-valuemin={0}
                                            aria-valuemax={100}
                                        >
                                            <div className="progress-bar" style={{ width: `${data.standing.levelProgressPercent}%` }}></div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </section>
                    )}

                    {/*
                        Top 3 as a podium. In the HTML they are in order 1, 2, 3 (so screen readers read them in order),
                        but on wider screens CSS order places them as 2, 1, 3 like a real podium.
                    */}
                    <section aria-labelledby="top-heading" className="mb-5">
                        <h2 id="top-heading" className="visually-hidden">Top 3</h2>
                        <ol className="podium list-unstyled d-flex flex-column flex-md-row align-items-md-end justify-content-center gap-3 mb-0">
                            {topThree.map((entry, index) => (
                                <PodiumStep key={entry.displayName + entry.rank} entry={entry} index={index} />
                            ))}
                        </ol>
                    </section>

                    {data.highlights.length > 0 && (
                        <section aria-labelledby="fame-heading" className="mb-5">
                            <h2 id="fame-heading" className="h5 fw-bold mb-3">Hall of fame</h2>
                            <div className="row g-3">
                                {data.highlights.map((highlight) => (
                                    <div className="col-md-4" key={highlight.title}>
                                        <div className="card border-0 shadow-sm h-100">
                                            <div className="card-body d-flex align-items-center gap-3">
                                                <span className="lb-fame-icon" aria-hidden="true">
                                                    <i className={`bi ${highlight.icon}`}></i>
                                                </span>
                                                <div className="min-w-0-lb">
                                                    <div className="small text-secondary">{highlight.title}</div>
                                                    <div className="fw-bold text-break">{highlight.name}</div>
                                                    <div className="small">
                                                        {highlight.value} {highlight.unit}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </section>
                    )}

                    {others.length > 0 && (
                        <section aria-labelledby="others-heading">
                            <h2 id="others-heading" className="h5 fw-bold mb-3">Everyone else</h2>
                            <ol className="list-unstyled card border-0 shadow-sm mb-0 leaderboard-list">
                                {others.map((entry) => (
                                    <li
                                        key={entry.displayName + entry.rank}
                                        className={`lb-row d-flex align-items-center gap-3 px-3 py-3 border-bottom${entry.isCurrentUser ? ' is-current-user' : ''}`}
                                    >
                                        <span className="leaderboard-rank fw-semibold text-secondary text-center">
                                            <span className="visually-hidden">Place </span>
                                            {entry.rank}
                                        </span>
                                        <span className={`lb-avatar lb-avatar-${entry.avatarColor}`} aria-hidden="true">{entry.initial}</span>
                                        <div className="flex-grow-1 min-w-0-lb">
                                            <div className="d-flex flex-wrap align-items-center gap-2">
                                                <span className="fw-semibold text-break">{entry.displayName}</span>
                                                {entry.isCurrentUser && <span className="badge text-bg-primary">You</span>}
                                                <span className="badge rounded-pill text-bg-light border">Level {entry.level}</span>
                                                <Streak days={entry.currentStreak} />
                                            </div>
                                            {/* The bar shows the points compared to the student in first place */}
                                            <div className="progress lb-bar mt-2" aria-hidden="true">
                                                <div className="progress-bar" style={{ width: `${entry.barPercent}%` }}></div>
                                            </div>
                                            {entry.topBadges.length > 0 && (
                                                <div className="d-flex flex-wrap gap-1 mt-2 lb-badge-row">
                                                    <MiniBadges badges={entry.topBadges} />
                                                </div>
                                            )}
                                        </div>
                                        <span className="fw-bold text-nowrap text-end">
                                            {entry.points} <span className="small text-secondary fw-normal">points</span>
                                        </span>
                                    </li>
                                ))}
                            </ol>
                        </section>
                    )}
                </>
            )}
        </>
    )
}

/* One of the top three on the podium */
function PodiumStep({ entry, index }: { entry: LeaderboardEntry; index: number }) {
    /* 1st in the middle, 2nd to the left, 3rd to the right */
    const visualOrder = index === 0 ? 2 : index === 1 ? 1 : 3

    return (
        <li className={`podium-step podium-${index + 1} order-md-${visualOrder}`}>
            <div className={`card border-0 shadow-sm text-center h-100${entry.isCurrentUser ? ' is-current-user' : ''}`}>
                <div className="card-body d-flex flex-column align-items-center justify-content-end">
                    {index === 0 && <i className="bi bi-trophy-fill lb-crown" aria-hidden="true"></i>}

                    <div className={`lb-avatar lb-avatar-lg lb-avatar-${entry.avatarColor} lb-ring mb-2`} aria-hidden="true">
                        {entry.initial}
                        <span className="lb-ring-rank">{entry.rank}</span>
                    </div>

                    <p className="fw-bold mb-1 text-break">
                        <span className="visually-hidden">Place {entry.rank}: </span>
                        {entry.displayName}
                        {entry.isCurrentUser && <span className="badge text-bg-primary ms-1">You</span>}
                    </p>

                    <p className="mb-2">
                        <span className="lb-points">{entry.points}</span> <span className="small text-secondary">points</span>
                    </p>

                    <p className="d-flex flex-wrap justify-content-center align-items-center gap-2 mb-2 small">
                        <span className="badge rounded-pill text-bg-light border">Level {entry.level}</span>
                        <Streak days={entry.currentStreak} />
                    </p>

                    <div className="d-flex flex-wrap justify-content-center gap-1 lb-badge-row">
                        {entry.topBadges.length > 0 ? (
                            <MiniBadges badges={entry.topBadges} />
                        ) : (
                            <span className="small text-secondary">No badges yet</span>
                        )}
                    </div>
                </div>
            </div>

            {/* The podium block under the card. Screen readers already get "Place 1:" from the card. */}
            <div className="podium-block" aria-hidden="true">{entry.rank}</div>
        </li>
    )
}