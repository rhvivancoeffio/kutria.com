import * as XLSX from 'xlsx'
import { MAX_CATALOG_CSV_DATA_ROWS } from '@/utils/catalogImportTypes'

/** Máximo de filas que guardamos en memoria para la vista previa del asistente. */
const MAX_ROWS_IN_MEMORY = 50_000

/**
 * Lee un CSV (UTF-8) y devuelve cabeceras + filas como objetos { [header]: string }.
 * @param {File} file
 */
export async function parseCatalogCsvFile(file) {
  const name = (file.name || '').toLowerCase()
  const mime = (file.type || '').toLowerCase()
  if (!name.endsWith('.csv') && mime !== 'text/csv' && mime !== 'application/csv') {
    throw new Error('Solo se admite archivo CSV.')
  }

  const text = await file.text()
  const wb = XLSX.read(text.trimStart('\uFEFF'), { type: 'string', raw: false })
  const sheetName = wb.SheetNames[0]
  if (!sheetName) {
    return { headers: [], rows: [], truncated: false, totalDataRows: 0 }
  }
  const sheet = wb.Sheets[sheetName]
  const matrix = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '', raw: false })
  if (!matrix.length) {
    return { headers: [], rows: [], truncated: false, totalDataRows: 0 }
  }

  const headerRow = matrix[0] || []
  const headers = headerRow.map((c) => (c == null ? '' : String(c).trim()))

  const rows = []
  let totalDataRows = 0
  for (let r = 1; r < matrix.length; r++) {
    const line = matrix[r]
    if (!line || !line.some((c) => c !== '' && c != null)) continue
    totalDataRows++
    if (totalDataRows > MAX_CATALOG_CSV_DATA_ROWS) {
      throw new Error(`El CSV supera el máximo de ${MAX_CATALOG_CSV_DATA_ROWS.toLocaleString()} filas de datos.`)
    }
    if (rows.length >= MAX_ROWS_IN_MEMORY) continue
    const obj = {}
    headers.forEach((h, i) => {
      const v = line[i]
      obj[h] = v == null ? '' : String(v)
    })
    rows.push(obj)
  }

  const truncated = totalDataRows > rows.length
  return { headers, rows, truncated, totalDataRows }
}

/** @deprecated Usar parseCatalogCsvFile */
export async function parseCatalogImportFile(file) {
  return parseCatalogCsvFile(file)
}
