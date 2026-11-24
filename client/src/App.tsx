import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import AuthProvider from './context/AuthProvider'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'

/* All pages and their addresses. New pages are added inside the Layout route. */
export default function App() {
    return (
        <BrowserRouter>
            {/* Every page can see who is signed in */}
            <AuthProvider>
                <Routes>
                    <Route element={<Layout />}>
                        <Route path="/" element={<HomePage />} />
                        <Route path="*" element={<NotFoundPage />} />
                    </Route>
                </Routes>
            </AuthProvider>
        </BrowserRouter>
    )
}