const dateFormatter = new Intl.DateTimeFormat('tr-TR', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

const dateTimeFormatter = new Intl.DateTimeFormat('tr-TR', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

export const formatDate = (iso: string) => dateFormatter.format(new Date(iso))
export const formatDateTime = (iso: string) => dateTimeFormatter.format(new Date(iso))

/** Görsel olmadığı için avatar baş harflerden oluşuyor. */
export function initials(person: { firstName: string; lastName: string; email: string }) {
  const letters = [person.firstName, person.lastName]
    .map((part) => part.trim()[0])
    .filter(Boolean)
    .join('')

  return letters || person.email[0]
}

