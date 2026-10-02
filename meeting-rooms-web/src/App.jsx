import { useCallback, useEffect, useState } from 'react'
import { api } from './api.js'
import { useToast } from './toast.jsx'
import { useScheduleHub } from './useScheduleHub.js'
import LoginForm from './components/LoginForm.jsx'
import RoomList from './components/RoomList.jsx'
import RoomForm from './components/RoomForm.jsx'
import Schedule from './components/Schedule.jsx'
import BookingsTable from './components/BookingsTable.jsx'

export default function App() {
    const notify = useToast()
    const [me, setMe] = useState(null) // { email, isAdmin } or null
    const [checkingSession, setCheckingSession] = useState(true)
    const [rooms, setRooms] = useState([])
    const [roomId, setRoomId] = useState(null)
    const [editingRoom, setEditingRoom] = useState(null)
    const [bookingsVersion, setBookingsVersion] = useState(0) // bumping it reloads the bookings table

    const { connection, live, reconnects } = useScheduleHub(me !== null)

    // Already logged in (auth cookie still valid)?
    useEffect(() => {
        api('/api/auth/me')
            .then(setMe)
            .catch(() => setMe(null))
            .finally(() => setCheckingSession(false))
    }, [])

    // Any request answered with 401 → back to the login form.
    useEffect(() => {
        const onExpired = () => setMe(null)
        window.addEventListener('session-expired', onExpired)
        return () => window.removeEventListener('session-expired', onExpired)
    }, [])

    const loadRooms = useCallback(async () => {
        try {
            const list = await api('/api/rooms')
            setRooms(list)
            // keep the selected room if it still exists, otherwise select the first one
            setRoomId(current => (list.some(r => r.id === current) ? current : list[0]?.id ?? null))
        } catch (error) {
            notify(error.message, 'error')
        }
    }, [notify])

    useEffect(() => {
        if (me) loadRooms()
    }, [me, loadRooms])

    // An admin changed rooms somewhere: re-read the list through REST.
    useEffect(() => {
        if (!connection) return
        connection.on('RoomsChanged', loadRooms)
        return () => connection.off('RoomsChanged', loadRooms)
    }, [connection, loadRooms])

    const bookingsChanged = useCallback(() => setBookingsVersion(v => v + 1), [])

    async function logout() {
        try {
            await api('/api/auth/logout', { method: 'POST' })
        } catch {
            // already logged out
        }
        setMe(null)
        setRooms([])
        setRoomId(null)
        setEditingRoom(null)
        notify('You have been logged out.', 'info')
    }

    function roomDeleted(id) {
        if (editingRoom?.id === id) setEditingRoom(null)
        loadRooms()
        bookingsChanged()
    }

    if (checkingSession) return <p className="hint center">Loading…</p>

    const selectedRoom = rooms.find(r => r.id === roomId) ?? null

    return (
        <>
            <header className="topbar">
                <h1>Meeting Room Booking</h1>
                {me && (
                    <div className="user-bar">
                        <span className={live ? 'live on' : 'live'}>{live ? '● Live' : '○ Offline'}</span>
                        <span>{me.email}</span>
                        {me.isAdmin && <span className="badge">Admin</span>}
                        <button className="secondary" onClick={logout}>Log out</button>
                    </div>
                )}
            </header>

            <main>
                {!me ? (
                    <LoginForm onLoggedIn={setMe} />
                ) : (
                    <div className="layout">
                        <section className="card">
                            <h2>Rooms</h2>
                            <RoomList
                                rooms={rooms}
                                selectedId={roomId}
                                isAdmin={me.isAdmin}
                                onSelect={setRoomId}
                                onEdit={setEditingRoom}
                                onDeleted={roomDeleted}
                            />
                            {me.isAdmin && (
                                <RoomForm
                                    room={editingRoom}
                                    onDone={() => {
                                        setEditingRoom(null)
                                        loadRooms()
                                    }}
                                />
                            )}
                        </section>

                        <Schedule
                            room={selectedRoom}
                            connection={connection}
                            reconnects={reconnects}
                            onChanged={bookingsChanged}
                        />

                        {me.isAdmin && <BookingsTable version={bookingsVersion} />}
                    </div>
                )}
            </main>
        </>
    )
}