import { ApiError } from '../api/http'

/**
 * Backend'in standart ErrorResponse gövdesini gösterir:
 * kısa mesaj + alan hataları + makine okunur errorCode.
 */
export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null

  if (error instanceof ApiError) {
    return (
      <div className="alert alert--error">
        <strong>{error.message}</strong>
        {error.fieldErrors && (
          <ul>
            {Object.entries(error.fieldErrors).map(([field, messages]) => (
              <li key={field}>
                <strong>{field}:</strong> {messages.join(' ')}
              </li>
            ))}
          </ul>
        )}
        <span className="alert__code">
          HTTP {error.status} · {error.errorCode}
        </span>
      </div>
    )
  }

  return <div className="alert alert--error">{error instanceof Error ? error.message : String(error)}</div>
}
