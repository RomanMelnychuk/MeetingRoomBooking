import { useState } from 'react'
import { api } from '../api.js'
import { useToast } from '../toast.jsx'

export default function RoomList({ rooms, selectedId, isAdmin, onSelect, onEdit, onDeleted }) {
    const notify = useToast()
    const [deletingId, setDeletingId] = useState(null)

    async function remove(room) {
        if (!confirm(`Delete "${room.name}"? All its bookings will be deleted too.`)) return

        setDeletingId(room.id)
        try {
            await api(`/api/rooms/${room.id}`, { method: 'DELETE' })
            notify(`Room "${room.name}" deleted.`, 'success')
            onDeleted(room.id)
        } catch (error) {
            notify(error.message, 'error')
        } finally {
            setDeletingId(null)
        }
    }

    if (rooms.length === 0) return <p className="hint">No rooms yet.</p>

    return (
        <ul className="rooms">
            {rooms.map(room => (
                <li key={room.id}>
                    <button
                        className={room.id === selectedId ? 'room-btn active' : 'room-btn'}
                        onClick={() => onSelect(room.id)}
                    >
                        {room.name} · {room.capacity} people
                    </button>
                    {isAdmin && (
                        <>
                            <button className="secondary icon-btn" onClick={() => onEdit(room)}>Edit</button>
                            <button className="danger icon-btn" disabled={deletingId === room.id} onClick={() => remove(room)}>
                                {deletingId === room.id ? '…' : 'Delete'}
                            </button>
                        </>
                    )}
                </li>
            ))}
        </ul>
    )
}