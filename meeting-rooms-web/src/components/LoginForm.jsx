import { useState } from 'react'
import { api } from '../api.js'
import { useToast } from '../toast.jsx'

export default function LoginForm({ onLoggedIn }) {
    const notify = useToast()
    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [busy, setBusy] = useState(false)

    async function logIn() {
        await api('/api/auth/login?useCookies=true', { method: 'POST', body: { email, password } })
        const me = await api('/api/auth/me')
        notify(`Welcome, ${me.email}!`, 'success')
        onLoggedIn(me)
    }

    async function handleLogin(event) {
        event.preventDefault()
        setBusy(true)
        try {
            await logIn()
        } catch (error) {
            notify(error.status === 401 ? 'Wrong email or password.' : error.message, 'error')
        } finally {
            setBusy(false)
        }
    }

    async function handleRegister() {
        if (!email || !password) {
            notify('Enter an email and a password first.', 'error')
            return
        }
        setBusy(true)
        try {
            await api('/api/auth/register', { method: 'POST', body: { email, password } })
            notify('Account created.', 'success')
            await logIn()
        } catch (error) {
            notify(error.message, 'error')
        } finally {
            setBusy(false)
        }
    }

    return (
        <section className="card auth">
            <h2>Sign in</h2>
            <form onSubmit={handleLogin}>
                <label>
                    Email
                    <input type="email" value={email} onChange={e => setEmail(e.target.value)} required autoComplete="username" />
                </label>
                <label>
                    Password
                    <input type="password" value={password} onChange={e => setPassword(e.target.value)} required autoComplete="current-password" />
                </label>
                <p className="hint">At least 6 characters, with an uppercase letter, a digit and a symbol.</p>
                <div className="row">
                    <button type="submit" disabled={busy}>{busy ? 'Please wait…' : 'Log in'}</button>
                    <button type="button" className="secondary" disabled={busy} onClick={handleRegister}>Create account</button>
                </div>
            </form>
        </section>
    )
}