import { useEffect, useState } from 'react'
import { api } from '../api.js'
import { useToast } from '../toast.jsx'

// Admin form: adds a new room, or edits `room` when one is passed in.
export default function RoomForm({ room, onDone }) {
    const notify = useToast()
    const [name, setName] = useState('')
    const [capacity, setCapacity] = useState('')
    const [saving, setSaving] = useState(false)

    useEffect(() => {
        setName(room?.name ?? '')
        setCapacity(room?.capacity ?? '')
    }, [room])

    async function submit(event) {
        event.preventDefault()
        setSaving(true)
        try {
            const body = { name: name.trim(), capacity: Number(capacity) }
            await api(room ? `/api/rooms/${room.id}` : '/api/rooms', { method: room ? 'PUT' : 'POST', body })
            notify(room ? 'Room updated.' : `Room "${body.name}" created.`, 'success')
            setName('')
            setCapacity('')
            onDone()
        } catch (error) {
            notify(error.message, 'error')
        } finally {
            setSaving(false)
        }
    }

    return (
        <form className="room-form" onSubmit={submit}>
            <h3>{room ? `Edit "${room.name}"` : 'Add room'}</h3>
            <label>
                Name
                <input value={name} onChange={e => setName(e.target.value)} required maxLength={100} />
            </label>
            <label>
                Capacity
                <input type="number" min={1} max={100} value={capacity} onChange={e => setCapacity(e.target.value)} required />
            </label>
            <div className="row">
                <button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
                {room && <button type="button" className="secondary" onClick={onDone}>Cancel</button>}
            </div>
        </form>
    )
}