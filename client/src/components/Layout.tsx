import { useLayoutEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

interface MenuLink {
    to: string
    label: string
    /* Only active on exactly this address (needed for Home, otherwise "/" matches every page) */
    end?: boolean
}

/*
 * The links for each role, same as _Layout.cshtml in Subapp 1:
 * students have a dashboard, administrators the user page, and everyone has Quizzes and Modules.
 */
function roleLinks(hasRole: (role: string) => boolean): MenuLink[] {
    if (hasRole('Admin')) {
        return [{ to: '/users', label: 'Users' }, { to: '/quizzes', label: 'Quizzes' }, { to: '/modules', label: 'Modules' }]
    }
    if (hasRole('Teacher')) {
        return [{ to: '/quizzes', label: 'Quizzes' }, { to: '/modules', label: 'Modules' }]
    }
    if (hasRole('Student')) {
        return [
            { to: '/dashboard', label: 'Dashboard' },
            { to: '/quizzes', label: 'Quizzes' },
            { to: '/modules', label: 'Modules' },
        ]
    }
    return []
}

/* The badge next to the email, the highest role first */
function roleLabel(hasRole: (role: string) => boolean): string | null {
    if (hasRole('Admin')) return 'Admin'
    if (hasRole('Teacher')) return 'Teacher'
    if (hasRole('Student')) return 'Student'
    return null
}

/* The navbar and the page area. Every page is shown where <Outlet /> is. */
export default function Layout() {
    const { user, hasRole, logout } = useAuth()
    const navigate = useNavigate()

    /* Whether the menu is open on small screens */
    const [menuOpen, setMenuOpen] = useState(false)

    const location = useLocation()
    const previousPath = useRef<string | null>(null)

    /*
     * Page transitions, the same as Subapp 1. A new page: <main> gets a new key below, so it mounts again
     * and the page-in animation in index.css runs. The same page with other filters (only the ?query changed):
     * the "same-page" class makes only the results (.filter-results) animate, like the script in _Layout.cshtml.
     * useLayoutEffect runs before the browser paints, so the class is right before anything is shown.
     */
    useLayoutEffect(() => {
        const samePage = previousPath.current === location.pathname
        document.documentElement.classList.toggle('same-page', samePage)

        /* A real page change starts at the top, like a normal page load in Subapp 1 */
        if (!samePage) {
            window.scrollTo(0, 0)
        }

        previousPath.current = location.pathname
    }, [location.pathname, location.search])

    const links: MenuLink[] = user
        ? [{ to: '/', label: 'Home', end: true }, ...roleLinks(hasRole), { to: '/leaderboard', label: 'Leaderboard' }]
        : []
    const label = roleLabel(hasRole)

    function closeMenu() {
        setMenuOpen(false)
    }

    async function handleLogout() {
        closeMenu()
        await logout()
        navigate('/', { replace: true })
    }

    return (
        <>
            <nav className="navbar navbar-expand-lg bg-body border-bottom mb-4">
                <div className="container">
                    <NavLink className="navbar-brand fw-bold text-primary" to="/" onClick={closeMenu}>
                        StudyQuest
                    </NavLink>

                    {/* The menu only has links when signed in, so visitors get no menu button */}
                    {user && (
                        <>
                            <button
                                className="navbar-toggler"
                                type="button"
                                aria-controls="mainNav"
                                aria-expanded={menuOpen}
                                aria-label="Toggle navigation"
                                onClick={() => setMenuOpen((open) => !open)}
                            >
                                <span className="navbar-toggler-icon"></span>
                            </button>

                            <div className={`collapse navbar-collapse${menuOpen ? ' show' : ''}`} id="mainNav">
                                <ul className="navbar-nav ms-auto align-items-lg-center gap-1">
                                    {links.map((link) => (
                                        <li className="nav-item" key={link.to}>
                                            <NavLink className="nav-link" to={link.to} end={link.end} onClick={closeMenu}>
                                                {link.label}
                                            </NavLink>
                                        </li>
                                    ))}

                                    <li className="nav-item text-secondary small px-2 d-flex align-items-center gap-2">
                                        <span>{user.email}</span>
                                        {label && <span className="badge bg-body-tertiary text-body border">{label}</span>}
                                    </li>
                                    <li className="nav-item">
                                        <NavLink className="nav-link" to="/settings" onClick={closeMenu}>
                                            <i className="bi bi-gear me-1" aria-hidden="true"></i>Settings
                                        </NavLink>
                                    </li>
                                    <li className="nav-item">
                                        <button type="button" className="nav-link btn btn-link" onClick={handleLogout}>
                                            Log out
                                        </button>
                                    </li>
                                </ul>
                            </div>
                        </>
                    )}
                </div>
            </nav>

            <main key={location.pathname} className="container pb-5">
                <Outlet />
            </main>
        </>
    )
}