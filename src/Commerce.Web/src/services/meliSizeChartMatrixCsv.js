/**
 * CSV mínimo (RFC4180-ish) para export/import de la grilla de guías de tallas ML.
 */

export function stripBom(text) {
  return String(text ?? '').replace(/^\uFEFF/, '')
}

export function sniffCsvDelimiter(firstLine) {
  const line = String(firstLine ?? '')
  const commas = (line.match(/,/g) || []).length
  const semis = (line.match(/;/g) || []).length
  return semis > commas ? ';' : ','
}

export function escapeCsvField(value, delimiter = ',') {
  const s = value == null ? '' : String(value)
  const d = delimiter === ';' ? ';' : ','
  if (/[\r\n"]/.test(s) || s.includes(d)) {
    return `"${s.replace(/"/g, '""')}"`
  }
  return s
}

/**
 * @param {string} text
 * @returns {{ delimiter: string, headers: string[], rows: string[][] }}
 */
export function parseCsv(text) {
  const s = stripBom(text)
  if (!s.trim()) {
    return { delimiter: ',', headers: [], rows: [] }
  }

  let delim = ','
  const nl = s.indexOf('\n')
  const firstLineEnd = nl >= 0 ? nl : s.length
  delim = sniffCsvDelimiter(s.slice(0, firstLineEnd).replace(/\r$/, ''))

  const rows = []
  let row = []
  let field = ''
  let inQuotes = false
  let i = 0

  while (i < s.length) {
    const c = s[i]
    if (inQuotes) {
      if (c === '"') {
        if (s[i + 1] === '"') {
          field += '"'
          i += 2
          continue
        }
        inQuotes = false
        i += 1
        continue
      }
      if (c === '\r') {
        i += 1
        continue
      }
      field += c
      i += 1
      continue
    }

    if (c === '"') {
      inQuotes = true
      i += 1
      continue
    }
    if (c === delim) {
      row.push(field)
      field = ''
      i += 1
      continue
    }
    if (c === '\r') {
      i += 1
      continue
    }
    if (c === '\n') {
      row.push(field)
      rows.push(row)
      row = []
      field = ''
      i += 1
      continue
    }
    field += c
    i += 1
  }

  row.push(field)
  if (row.length > 1 || row[0] !== '') {
    rows.push(row)
  }

  if (!rows.length) {
    return { delimiter: delim, headers: [], rows: [] }
  }

  const headerRow = rows.shift() || []
  const headers = headerRow.map((h) => String(h ?? '').trim())

  const dataRows = rows.filter((r) =>
    r.some((cell) => String(cell ?? '').trim() !== '')
  )

  return { delimiter: delim, headers, rows: dataRows }
}

/**
 * @param {string} filename
 * @param {string} csvBody sin BOM (se antepone UTF-8 BOM para Excel)
 */
export function downloadUtf8Csv(filename, csvBody) {
  const body = `\uFEFF${csvBody}`
  const blob = new Blob([body], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename.replace(/[^\w.\-]+/g, '_').slice(0, 120) || 'size-chart.csv'
  a.rel = 'noopener'
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}
