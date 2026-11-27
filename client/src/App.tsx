import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import RequireAuth from './components/RequireAuth'
import AuthProvider from './context/AuthProvider'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import NotFoundPage from './pages/NotFoundPage'
import QuizzesPage from './pages/QuizzesPage'
import PlayQuizPage from './pages/PlayQuizPage'

/* All pages and their addresses. New pages are added inside the Layout route. */
export default function App() {
    return (
        <BrowserRouter>
            {/* Every page can see who is signed in */}
            <AuthProvider>
                <Routes>
                    <Route element={<Layout />}>
                        <Route path="/" element={<HomePage />} />
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/register" element={<RegisterPage />} />

                        {/* Pages that need a signed-in user. RequireAuth sends visitors to the login page. */}
                        <Route path="/quizzes" element={<RequireAuth><QuizzesPage /></RequireAuth>} />
                        <Route path="/quizzes/:id/play" element={<RequireAuth><PlayQuizPage /></RequireAuth>} />

                        <Route path="*" element={<NotFoundPage />} />
                    </Route>
                </Routes>
            </AuthProvider>
        </BrowserRouter>
    )
}