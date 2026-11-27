import { Link } from 'react-router-dom'

/* Shown for any address that does not match a page. Same layout as the 404 page in Subapp 1. */
export default function NotFoundPage() {
    return (
        <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
            <div className="display-1 fw-bold text-primary">404</div>

            <h1 className="h3 fw-bold mb-2">Page not found</h1>
            <p className="text-secondary mb-4">The page you are looking for does not exist or has been moved.</p>

            <div className="d-grid gap-2">
                <Link className="btn btn-primary btn-hero" to="/">Back to home</Link>
                <Link className="btn btn-outline-secondary" to="/quizzes">Browse quizzes</Link>
            </div>
        </div>
    )
}