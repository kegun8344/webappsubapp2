import { Link } from 'react-router-dom'

/* Shown for any address that does not match a page */
export default function NotFoundPage() {
    return (
        <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
            <h1 className="h3 fw-bold mb-2">Page not found</h1>
            <p className="text-secondary mb-4">The page you are looking for does not exist.</p>
            <Link className="btn btn-primary" to="/">
                Back to home
            </Link>
        </div>
    )
}