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
import CreateQuizPage from './pages/CreateQuizPage'
import EditQuizPage from './pages/EditQuizPage'
import QuestionFormPage from './pages/QuestionFormPage'
import LeaderboardPage from './pages/LeaderboardPage'
import DashboardPage from './pages/DashboardPage'
import ModulesPage from './pages/ModulesPage'
import ModuleFormPage from './pages/ModuleFormPage'
import SettingsProvider from './context/SettingsProvider'
import SettingsPage from './pages/SettingsPage'
import DeleteAccountPage from './pages/DeleteAccountPage'
import UsersPage from './pages/UsersPage'
import DeleteUserPage from './pages/DeleteUserPage'
import MoveQuizPage from './pages/MoveQuizPage'
import QuizAnalysisPage from './pages/QuizAnalysisPage'
import MyTagsPage from './pages/MyTagsPage'

/* All pages and their addresses. New pages are added inside the Layout route. */
export default function App() {
    return (
        <BrowserRouter>
            {/* Every page can see who is signed in */}
            <AuthProvider>
                <SettingsProvider>
                <Routes>
                    <Route element={<Layout />}>
                        <Route path="/" element={<HomePage />} />
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/register" element={<RegisterPage />} />

                        {/* Pages that need a signed-in user. RequireAuth sends visitors to the login page. */}
                        <Route path="/quizzes" element={<RequireAuth><QuizzesPage /></RequireAuth>} />
                        <Route path="/quizzes/:id/play" element={<RequireAuth><PlayQuizPage /></RequireAuth>} />
                        <Route path="/quizzes/new" element={<RequireAuth><CreateQuizPage /></RequireAuth>} />
                        <Route path="/quizzes/:id/edit" element={<RequireAuth><EditQuizPage /></RequireAuth>} />
                        <Route path="/quizzes/:quizId/questions/new" element={<RequireAuth><QuestionFormPage /></RequireAuth>} />
                        <Route path="/quizzes/:quizId/questions/:questionId/edit" element={<RequireAuth><QuestionFormPage /></RequireAuth>} />
                        <Route path="/leaderboard" element={<RequireAuth><LeaderboardPage /></RequireAuth>} />
                        <Route path="/modules" element={<RequireAuth><ModulesPage /></RequireAuth>} />
                        {/* Only teachers and administrators manage modules (the API checks the role too) */}
                        <Route path="/modules/new" element={<RequireAuth roles={['Teacher', 'Admin']}><ModuleFormPage /></RequireAuth>} />
                        <Route path="/modules/:id/edit" element={<RequireAuth roles={['Teacher', 'Admin']}><ModuleFormPage /></RequireAuth>} />
                        {/* Only students have a dashboard (the API also gives 403 to everyone else) */}
                        <Route path="/dashboard" element={<RequireAuth roles={['Student']}><DashboardPage /></RequireAuth>} />
                        {/* Only administrators manage users and move quizzes (the API checks the role too) */}
                        <Route path="/users" element={<RequireAuth roles={['Admin']}><UsersPage /></RequireAuth>} />
                        <Route path="/users/:id/delete" element={<RequireAuth roles={['Admin']}><DeleteUserPage /></RequireAuth>} />
                        <Route path="/quizzes/:id/move" element={<RequireAuth roles={['Admin']}><MoveQuizPage /></RequireAuth>} />
                        {/* The quiz owner or an administrator (the API checks the owner) */}
                        <Route path="/quizzes/:id/analysis" element={<RequireAuth roles={['Teacher', 'Admin']}><QuizAnalysisPage /></RequireAuth>} />
                        <Route path="/tags" element={<RequireAuth><MyTagsPage /></RequireAuth>} />
                        <Route path="/settings" element={<RequireAuth><SettingsPage /></RequireAuth>} />
                        {/* Administrator accounts are protected and cannot be deleted (the API refuses it too) */}
                        <Route path="/settings/delete" element={<RequireAuth roles={['Student', 'Teacher']}><DeleteAccountPage /></RequireAuth>} />
                        <Route path="*" element={<NotFoundPage />} />
                    </Route>
                </Routes>
                </SettingsProvider>
            </AuthProvider>
        </BrowserRouter>
    )
}