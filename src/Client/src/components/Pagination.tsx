interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, pageSize, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null

  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="pagination">
      <span className="pagination__info">{from}–{to} / {totalCount}</span>
      <div className="row" style={{ gap: 6 }}>
        <button className="btn btn--sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          ‹ Önceki
        </button>
        <span className="pagination__page">Sayfa {page} / {totalPages}</span>
        <button className="btn btn--sm" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Sonraki ›
        </button>
      </div>
    </div>
  )
}
