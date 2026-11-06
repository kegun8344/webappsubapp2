import { NavLink, Outlet } from 'react-router-dom'

/* The navbar and the page area. Every page is shown where <Outlet /> is. */
export default function Layout() {
    return (
        <>
            <nav className="navbar navbar-expand bg-white border-bottom mb-4">
                <div className="container">
                    <NavLink className="navbar-brand fw-bold text-primary" to="/">
                        StudyQuest
                    </NavLink>
                    <ul className="navbar-nav ms-auto gap-1">
                        <li className="nav-item">
                            <NavLink className="nav-link" to="/" end>
                                Home
                            </NavLink>
                        </li>
                    </ul>
                </div>
            </nav>

            <main className="container pb-5">
                <Outlet />
            </main>
        </>
    )
}