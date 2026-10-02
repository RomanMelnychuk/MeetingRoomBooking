import { useEffect, useState } from 'react'
import { api, slotLabel } from '../api.js'
import { useToast } from '../toast.jsx'

// Admin only: every booking across all users. Reloads whenever `version` changes.
export default function BookingsTable({ version }) {
    const notify = useToast()
    const [bookings, setBookings] = useState([])

    useEffect(() => {
        api('/api/bookings')
            .then(setBookings)
            .catch(error => notify(error.message, 'error'))
    }, [version, notify])

    return (
        <section className="card wide">
            <h2>All bookings</h2>
            <table>
                <thead>
                    <tr><th>Room</th><th>Date</th><th>Time</th><th>User</th><th>Booked at (UTC)</th></tr>
                </thead>
                <tbody>
                    {bookings.length === 0 ? (
                        <tr><td colSpan={5} className="hint">No bookings yet.</td></tr>
                    ) : (
                        bookings.map(b => (
                            <tr key={b.id}>
                                <td>{b.roomName}</td>
                                <td>{b.date}</td>
                                <td>{slotLabel(b.startHour)}</td>
                                <td>{b.userEmail}</td>
                                <td>{b.createdAtUtc.slice(0, 16).replace('T', ' ')}</td>
                            </tr>
                        ))
                    )}
                </tbody>
            </table>
        </section>
    )
}