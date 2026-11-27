import type { CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

/* The landing page animations wait a little longer for each element (same as --delay in Subapp 1) */
function delay(seconds: number): CSSProperties {
    return { '--delay': `${seconds}s` } as CSSProperties
}

interface Shortcut {
    icon: string
    title: string
    text: string
    to: string
}

/* Text under the greeting and the shortcuts, different for each role (same as Home/Index in Subapp 1) */
function forRole(hasRole: (role: string) => boolean): { subtitle: string; shortcuts: Shortcut[] } {
    if (hasRole('Admin')) {
        return {
            subtitle: 'Keep an eye on the platform and everything in it.',
            shortcuts: [
                { icon: 'bi-shield-lock', title: 'Admin dashboard', text: 'Manage users and roles.', to: '/admin' },
                { icon: 'bi-collection', title: 'All quizzes', text: 'See and manage every quiz.', to: '/quizzes' },
                { icon: 'bi-trophy', title: 'Leaderboard', text: 'See how the students rank.', to: '/leaderboard' },
            ],
        }
    }
    if (hasRole('Teacher')) {
        return {
            subtitle: 'Make quizzes for your students and follow how they do.',
            shortcuts: [
                { icon: 'bi-plus-lg', title: 'Create a quiz', text: 'Start a new quiz that gives points.', to: '/quizzes/create' },
                { icon: 'bi-collection', title: 'Your quizzes', text: 'Edit questions or check your quizzes.', to: '/quizzes?view=mine' },
                { icon: 'bi-trophy', title: 'Leaderboard', text: 'See how the students rank.', to: '/leaderboard' },
            ],
        }
    }
    return {
        subtitle: "Ready for today's quiz?",
        shortcuts: [
            { icon: 'bi-play-fill', title: 'Take a quiz', text: 'Earn points and keep your streak going.', to: '/quizzes' },
            { icon: 'bi-speedometer2', title: 'Your dashboard', text: 'Level, badges and your latest results.', to: '/dashboard' },
            { icon: 'bi-trophy', title: 'Leaderboard', text: 'See where you rank in the class.', to: '/leaderboard' },
        ],
    }
}

/* Start page: a personal page when signed in, the landing page for visitors */
export default function HomePage() {
    const { user, isLoading, hasRole } = useAuth()

    /* Wait until we know if someone is signed in, so visitors never see the wrong page first */
    if (isLoading) return null

    if (user) {
        const displayName = user.email.split('@')[0]
        const { subtitle, shortcuts } = forRole(hasRole)

        return (
            <section aria-labelledby="welcome-heading">
                <h1 id="welcome-heading" className="landing-title mb-2">Hi, {displayName}</h1>
                <p className="lead text-secondary mb-5">{subtitle}</p>

                <ul className="row g-3 list-unstyled mb-0">
                    {shortcuts.map((shortcut) => (
                        <li className="col-lg-4" key={shortcut.title}>
                            <div className="card border-0 shadow-sm h-100 home-action">
                                <div className="card-body d-flex align-items-center gap-3 p-4">
                                    <span className="home-action-icon"><i className={`bi ${shortcut.icon}`} aria-hidden="true"></i></span>
                                    <span className="flex-grow-1">
                                        <Link className="home-action-title stretched-link" to={shortcut.to}>{shortcut.title}</Link>
                                        <span className="d-block small text-secondary">{shortcut.text}</span>
                                    </span>
                                    <i className="bi bi-arrow-right home-action-arrow" aria-hidden="true"></i>
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>

                {/* The streak, daily goal and quiz suggestions for students come here, from the points API */}
            </section>
        )
    }

    return (
        <>
            {/* Hero: the pitch on the left, an example result on the right */}
            <section className="row align-items-center g-4 g-lg-5 py-4 py-lg-5" aria-labelledby="hero-heading">
                <div className="col-lg-6">
                    <h1 id="hero-heading" className="landing-title mb-3 landing-drop" style={delay(0.1)}>
                        Turn practice into progress.
                    </h1>
                    <p className="lead text-secondary mb-4 landing-drop" style={delay(0.2)}>
                        Take quizzes from your teachers and watch your points, level and badges grow.
                    </p>

                    {/* New visitors: sign up is the main action, Log in is a quiet link for returning users */}
                    <div className="d-flex flex-wrap align-items-center gap-3">
                        <Link className="btn btn-primary btn-lg btn-hero px-5 landing-drop" style={delay(0.3)} to="/register">
                            Sign up
                        </Link>
                        <span className="text-secondary landing-drop" style={delay(0.35)}>
                            Already have an account?{' '}
                            <Link className="fw-semibold link-offset-2" to="/login">Log in</Link>
                        </span>
                    </div>
                </div>

                {/* Example of a result page, so visitors see what they get. Hidden from screen readers, it is only decoration. */}
                <div className="col-lg-6">
                    <div className="card border-0 text-center mx-auto landing-preview" aria-hidden="true">
                        <div className="card-body p-3 p-md-4">
                            {/* Score, same layout as the result page */}
                            <p className="small text-secondary mb-1">Quiz completed</p>
                            <p className="h5 fw-bold mb-3">Academic Writing</p>

                            <div className="display-5 fw-bold text-warning">80%</div>
                            <p className="fw-semibold mb-1">Good effort!</p>
                            <p className="small text-secondary mb-3">8 of 10 correct answers</p>

                            <div className="progress mb-3" style={{ height: '0.6rem' }}>
                                <div className="progress-bar bg-warning" style={{ width: '80%' }}></div>
                            </div>

                            {/* Points */}
                            <div className="text-start border-top pt-3 mb-3">
                                <p className="small text-secondary text-uppercase fw-semibold mb-2">Points</p>
                                <p className="fs-3 fw-bold text-primary mb-2">+140 points</p>
                                <ul className="list-unstyled small mb-0">
                                    <li className="d-flex justify-content-between gap-3 py-1"><span>Quiz score</span><span className="fw-semibold">+80</span></li>
                                    <li className="d-flex justify-content-between gap-3 py-1"><span>First time completing this quiz</span><span className="fw-semibold">+10</span></li>
                                    <li className="d-flex justify-content-between gap-3 py-1"><span>Streak day 5 (max bonus)</span><span className="fw-semibold">+50</span></li>
                                </ul>
                            </div>

                            {/* Level */}
                            <div className="text-start border-top pt-2 mb-3">
                                <p className="small text-secondary text-uppercase fw-semibold mb-2">Level 3</p>
                                <div className="progress mb-1" style={{ height: '0.6rem' }}>
                                    <div className="progress-bar" style={{ width: '55%' }}></div>
                                </div>
                                <p className="small text-secondary mb-0">220 / 400 points to level 4</p>
                            </div>

                            {/* New badge */}
                            <div className="text-start border-top pt-2">
                                <p className="small text-secondary text-uppercase fw-semibold mb-2">New badge</p>
                                <div className="d-flex align-items-center gap-3 achievement-gold">
                                    <span className="achievement-icon"><i className="bi bi-compass"></i></span>
                                    <div>
                                        <p className="fw-bold mb-0">Explorer</p>
                                        <p className="small text-secondary mb-0">Upgraded to Gold</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* What you get */}
            <section className="py-5" aria-labelledby="features-heading">
                <h2 id="features-heading" className="landing-section-title mb-4 landing-drop" style={delay(0.4)}>
                    Everything you do counts
                </h2>
                <ul className="row g-4 list-unstyled mb-0">
                    <li className="col-sm-6 col-lg-3 landing-drop" style={delay(0.4)}>
                        <span className="landing-feature-icon mb-3"><i className="bi bi-graph-up-arrow" aria-hidden="true"></i></span>
                        <h3 className="h6 fw-bold mb-1">Points for progress</h3>
                        <p className="small text-secondary mb-0">Your best score on each quiz counts. Beat it later and you earn the difference.</p>
                    </li>
                    <li className="col-sm-6 col-lg-3 landing-drop" style={delay(0.433)}>
                        <span className="landing-feature-icon mb-3"><i className="bi bi-fire" aria-hidden="true"></i></span>
                        <h3 className="h6 fw-bold mb-1">Daily streaks</h3>
                        <p className="small text-secondary mb-0">Practice a little every day. The bonus grows for five days in a row.</p>
                    </li>
                    <li className="col-sm-6 col-lg-3 landing-drop" style={delay(0.466)}>
                        <span className="landing-feature-icon mb-3"><i className="bi bi-compass" aria-hidden="true"></i></span>
                        <h3 className="h6 fw-bold mb-1">Badges and levels</h3>
                        <p className="small text-secondary mb-0">Unlock six badges from Bronze to Diamond, and level up as your points grow.</p>
                    </li>
                    <li className="col-sm-6 col-lg-3 landing-drop" style={delay(0.5)}>
                        <span className="landing-feature-icon mb-3"><i className="bi bi-trophy" aria-hidden="true"></i></span>
                        <h3 className="h6 fw-bold mb-1">Leaderboard</h3>
                        <p className="small text-secondary mb-0">See where you rank in the class, and who is on the podium.</p>
                    </li>
                </ul>
            </section>

            {/* The steps are a real sequence, so they are numbered */}
            <section className="py-5" aria-labelledby="steps-heading">
                <h2 id="steps-heading" className="landing-section-title mb-4 landing-drop" style={delay(0.4)}>How it works</h2>
                <ol className="row g-4 list-unstyled mb-0">
                    <li className="col-md-4 d-flex gap-3 landing-drop" style={delay(0.4)}>
                        <span className="landing-step-number" aria-hidden="true">1</span>
                        <div>
                            <h3 className="h6 fw-bold mb-1">Pick a quiz</h3>
                            <p className="small text-secondary mb-0">Choose a topic you want to practice.</p>
                        </div>
                    </li>
                    <li className="col-md-4 d-flex gap-3 landing-drop" style={delay(0.45)}>
                        <span className="landing-step-number" aria-hidden="true">2</span>
                        <div>
                            <h3 className="h6 fw-bold mb-1">Answer the questions</h3>
                            <p className="small text-secondary mb-0">Take your time. You can always try again to beat your score.</p>
                        </div>
                    </li>
                    <li className="col-md-4 d-flex gap-3 landing-drop" style={delay(0.5)}>
                        <span className="landing-step-number" aria-hidden="true">3</span>
                        <div>
                            <h3 className="h6 fw-bold mb-1">Collect your rewards</h3>
                            <p className="small text-secondary mb-0">Get points, keep your streak, unlock badges and climb the leaderboard.</p>
                        </div>
                    </li>
                </ol>
            </section>
        </>
    )
}