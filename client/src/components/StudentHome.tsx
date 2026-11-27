import { Link } from 'react-router-dom'
import InfoPopover from './InfoPopover'
import type { StudentHome as StudentHomeData } from '../types/home'

/* The streak bonus per day (PointRules.StreakPoints): +10 on day 1 up to +50 from day 5 */
const STREAK_STEPS = [1, 2, 3, 4, 5]

/* Today's streak and daily goal, and quiz suggestions. Same sections as the student part of Home/Index in Subapp 1. */
export default function StudentHome({ home }: { home: StudentHomeData }) {
    const streakDays = home.streakDays

    /* "1st day" reads better than "1 day in a row" */
    const daysInARow = streakDays === 1 ? '1st day' : `${streakDays} days in a row`
    const filledSteps = Math.min(streakDays, 5)

    const streak = home.streakKeptToday
        ? { icon: 'bi-check-circle-fill', color: 'bg-success-subtle text-success', title: daysInARow }
        : streakDays === 0
          ? { icon: 'bi-fire', color: 'bg-body-secondary text-secondary', title: 'No streak yet' }
          : { icon: 'bi-fire', color: 'bg-warning-subtle text-warning-emphasis', title: daysInARow }

    const starsNeeded = home.dailyGoalStarsNeeded
    const starsDone = Math.min(home.dailyGoalStars, starsNeeded)
    const starsLeft = starsNeeded - starsDone
    const dailyPercent = Math.floor((starsDone * 100) / starsNeeded)

    return (
        <>
            <section className="row g-3 mt-0 mb-5" aria-labelledby="today-heading">
                <h2 id="today-heading" className="visually-hidden">Today</h2>

                <div className="col-md-6">
                    <div className="card border-0 shadow-sm h-100">
                        <div className="card-body p-4">
                            <div className="d-flex align-items-center gap-3">
                                <span className={`home-action-icon ${streak.color}`}>
                                    <i className={`bi ${streak.icon}`} aria-hidden="true"></i>
                                </span>
                                <div>
                                    <p className="small text-secondary text-uppercase fw-semibold mb-0">Streak</p>
                                    <p className="h5 fw-bold mb-0">{streak.title}</p>
                                </div>
                                <InfoPopover label="How the streak works" title="Streak">
                                    <p className="mb-2">Take at least one quiz every day.</p>
                                    <div className="row row-cols-5 g-1 mb-2">
                                        {STREAK_STEPS.map((day) => (
                                            <div className="col" key={day}>
                                                <div className="text-center bg-body-tertiary rounded py-1">
                                                    <span className="d-block small text-secondary">Day {day}</span>
                                                    <strong>+{day * 10}</strong>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                    <p className="small text-secondary mb-0">Day 5 and every day after gives +50. Miss a day and it starts over.</p>
                                </InfoPopover>
                            </div>

                            <div className="d-flex gap-1 mt-3" aria-hidden="true">
                                {STREAK_STEPS.map((step) => {
                                    const color =
                                        step <= filledSteps
                                            ? 'bg-warning'
                                            : !home.streakKeptToday && step === filledSteps + 1
                                              ? 'bg-warning-subtle border border-warning'
                                              : 'bg-body-secondary'
                                    return <span key={step} className={`streak-step rounded-pill flex-fill ${color}`}></span>
                                })}
                            </div>

                            <p className="small text-secondary mt-2 mb-0">
                                {home.streakKeptToday ? (
                                    <>
                                        <i className="bi bi-check-circle-fill text-success me-1" aria-hidden="true"></i>
                                        <strong className="text-body">+{home.streakPointsToday} points</strong> earned today
                                    </>
                                ) : (
                                    <>
                                        {streakDays === 0 ? 'Take a quiz today to start:' : 'Take a quiz today:'}{' '}
                                        <strong className="text-body">+{home.streakPointsToday} points</strong>
                                    </>
                                )}
                            </p>
                        </div>
                    </div>
                </div>

                <div className="col-md-6">
                    <div className="card border-0 shadow-sm h-100">
                        <div className="card-body p-4">
                            <div className="d-flex align-items-center gap-3">
                                <span className={`home-action-icon ${home.dailyGoalReached ? 'bg-success-subtle text-success' : 'bg-warning-subtle text-warning-emphasis'}`}>
                                    <i className={`bi ${home.dailyGoalReached ? 'bi-check-circle-fill' : 'bi-star-fill'}`} aria-hidden="true"></i>
                                </span>
                                <div>
                                    <p className="small text-secondary text-uppercase fw-semibold mb-0">Daily goal</p>
                                    <p className="h5 fw-bold mb-0">
                                        {starsDone} of {starsNeeded} today
                                    </p>
                                </div>
                                <InfoPopover label="How the daily goal works" title="Daily goal">
                                    <p className="mb-2">Each quiz gives up to 2 stars a day.</p>
                                    <div className="d-flex justify-content-between align-items-center border-top py-2">
                                        <span>Below 100 %</span>
                                        <span className="text-nowrap"><i className="bi bi-star-fill text-warning"></i></span>
                                    </div>
                                    <div className="d-flex justify-content-between align-items-center border-top py-2">
                                        <span>100 %</span>
                                        <span className="text-nowrap">
                                            <i className="bi bi-star-fill text-warning"></i> <i className="bi bi-star-fill text-warning"></i>
                                        </span>
                                    </div>
                                    <p className="small text-secondary border-top pt-2 mb-0">
                                        Collect {starsNeeded} stars today for <strong className="text-body">+{home.dailyGoalBonus} points</strong>.
                                    </p>
                                </InfoPopover>
                            </div>

                            <div className="progress mt-3" style={{ height: '0.45rem' }} aria-hidden="true">
                                <div className={`progress-bar ${home.dailyGoalReached ? 'bg-success' : 'bg-warning'}`} style={{ width: `${dailyPercent}%` }}></div>
                            </div>

                            <p className="small text-secondary mt-2 mb-0">
                                {home.dailyGoalReached ? (
                                    <>
                                        <i className="bi bi-check-circle-fill text-success me-1" aria-hidden="true"></i>
                                        <strong className="text-body">+{home.dailyGoalBonus} points</strong> earned today
                                    </>
                                ) : (
                                    <>
                                        {starsLeft} <i className="bi bi-star-fill text-warning" aria-label="stars"></i> left today for{' '}
                                        <strong className="text-body">+{home.dailyGoalBonus} points</strong>
                                    </>
                                )}
                            </p>
                        </div>
                    </div>
                </div>
            </section>

            {home.improveQuizzes.length > 0 && (
                <section className="mb-5" aria-labelledby="improve-heading">
                    <div className="d-flex flex-wrap align-items-baseline justify-content-between gap-2 mb-3">
                        <h2 id="improve-heading" className="h4 fw-bold mb-0">Beat your best</h2>
                        <span className="small text-secondary d-none d-md-inline">Improve your score and earn the difference in points</span>
                    </div>
                    <ul className="list-group shadow-sm">
                        {home.improveQuizzes.map((quiz) => (
                            <li key={quiz.quizId} className="list-group-item d-flex align-items-center justify-content-between gap-3 py-3">
                                <div className="d-flex align-items-center gap-3">
                                    <span className="home-action-icon"><i className="bi bi-arrow-repeat" aria-hidden="true"></i></span>
                                    <div>
                                        <p className="fw-semibold mb-0">{quiz.title}</p>
                                        <p className="small text-secondary mb-0">
                                            {quiz.moduleName} · Your best: {quiz.bestScore}% · up to <strong className="text-body">+{quiz.pointsLeft} points</strong>
                                        </p>
                                    </div>
                                </div>
                                <Link className="btn btn-outline-primary btn-sm text-nowrap" to={`/quizzes/${quiz.quizId}/play`} aria-label={`Try ${quiz.title} again`}>
                                    Try again
                                </Link>
                            </li>
                        ))}
                    </ul>
                </section>
            )}

            {home.improveQuizzes.length === 0 && home.quizzesToTry.length === 0 && (
                <section className="card border-0 shadow-sm mb-5" aria-labelledby="all-done-heading">
                    <div className="card-body d-flex flex-column flex-sm-row align-items-sm-center gap-3 p-4">
                        <span className="home-action-icon bg-success-subtle text-success">
                            <i className="bi bi-stars" aria-hidden="true"></i>
                        </span>
                        <div className="flex-grow-1">
                            <h2 id="all-done-heading" className="h5 fw-bold mb-1">No quizzes waiting for you</h2>
                            <p className="small text-secondary mb-0">
                                You have taken every quiz and there are no points left to win. New quizzes from your teachers will show up here.
                            </p>
                        </div>
                        <Link className="btn btn-outline-primary btn-sm text-nowrap align-self-start align-self-sm-center" to="/leaderboard">
                            See the leaderboard
                        </Link>
                    </div>
                </section>
            )}

            {home.quizzesToTry.length > 0 && (
                <section className="mb-5" aria-labelledby="try-heading">
                    <div className="d-flex flex-wrap align-items-baseline justify-content-between gap-2 mb-3">
                        <h2 id="try-heading" className="h4 fw-bold mb-0">Quizzes to try</h2>
                        <Link className="small fw-semibold link-offset-2" to="/quizzes">See all quizzes</Link>
                    </div>
                    <ul className="row g-3 list-unstyled mb-0">
                        {home.quizzesToTry.map((quiz) => (
                            <li key={quiz.quizId} className="col-md-6 col-lg-4">
                                <div className="card border-0 shadow-sm h-100">
                                    <div className="card-body d-flex flex-column p-4">
                                        <p className="small fw-semibold text-secondary mb-1">{quiz.moduleName}</p>
                                        <h3 className="h6 fw-bold mb-1">{quiz.title}</h3>
                                        <p className="small text-secondary mb-3">
                                            {quiz.difficulty} · {quiz.questionCount} {quiz.questionCount === 1 ? 'question' : 'questions'}
                                        </p>
                                        <Link
                                            className="btn btn-primary btn-sm align-self-start mt-auto d-inline-flex align-items-center gap-1"
                                            to={`/quizzes/${quiz.quizId}/play`}
                                            aria-label={`Start quiz ${quiz.title}`}
                                        >
                                            <i className="bi bi-play-fill" aria-hidden="true"></i>
                                            Start quiz
                                        </Link>
                                    </div>
                                </div>
                            </li>
                        ))}
                    </ul>
                </section>
            )}
        </>
    )
}