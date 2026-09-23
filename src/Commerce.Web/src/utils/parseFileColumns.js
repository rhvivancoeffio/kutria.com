import * as XLSX from 'xlsx'

/**
 * Parsea un archivo Excel o CSV y devuelve las filas crudas (array de arrays).
 * Útil para que el usuario seleccione en qué fila están las columnas.
 *
 * @param {File} file - Archivo Excel (.xlsx, .xls) o CSV
 * @param {Object} options - Commerceiones
 * @param {number} [options.maxPreviewRows=20] - Máximo de filas a devolver para preview
 * @returns {Promise<{ rawRows: string[][], totalRows: number }>}
 */
export async function parseFileRawRows(file, options = {}) {
  const { maxPreviewRows = 20 } = options
  if (!file) return { rawRows: [], totalRows: 0 }

  const data = await file.arrayBuffer()
  const workbook = XLSX.read(data, { type: 'array' })
  const sheetNames = workbook.SheetNames || []
  if (sheetNames.length === 0) return { rawRows: [], totalRows: 0 }

  const firstSheetName = sheetNames[0]
  const sheet = workbook.Sheets[firstSheetName]
  if (!sheet) return { rawRows: [], totalRows: 0 }

  const raw = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '', raw: false })
  const totalRows = Array.isArray(raw) ? raw.length : 0
  const rawRows = (Array.isArray(raw) ? raw : [])
    .slice(0, maxPreviewRows)
    .map((row) =>
      Array.isArray(row) ? row.map((c) => (c != null ? String(c).trim() : '')) : []
    )

  return { rawRows, totalRows }
}

/**
 * Parsea un archivo Excel o CSV en el navegador (solo en memoria, sin enviar al backend).
 * Extrae las columnas (cabecera) y una muestra de filas.
 *
 * @param {File} file - Archivo Excel (.xlsx, .xls) o CSV
 * @param {Object} options - Commerceiones
 * @param {number} [options.headerRowIndex=0] - Índice de la fila que contiene las columnas (0-based)
 * @param {number} [options.maxRows=50] - Máximo de filas de muestra a devolver
 * @returns {Promise<{ columns: string[], rows: object[], totalRows: number }>}
 */
export async function parseFileColumns(file, options = {}) {
  const { headerRowIndex = 0, maxRows = 50 } = options
  if (!file) return { columns: [], rows: [], totalRows: 0 }

  const data = await file.arrayBuffer()
  const workbook = XLSX.read(data, { type: 'array' })
  const firstSheetName = workbook.SheetNames[0]
  const sheet = workbook.Sheets[firstSheetName]
  if (!sheet) return { columns: [], rows: [], totalRows: 0 }

  const raw = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '', raw: false })
  const totalRows = raw.length
  if (totalRows <= headerRowIndex) return { columns: [], rows: [], totalRows }

  const headerRow = raw[headerRowIndex]
  const columns = (Array.isArray(headerRow) ? headerRow : [])
    .map((c, i) => {
      const val = c != null ? String(c).trim() : ''
      return val || `Columna_${i + 1}`
    })

  const dataRows = raw.slice(headerRowIndex + 1)
  const rows = dataRows.slice(0, maxRows).map((row) => {
    const arr = Array.isArray(row) ? row : []
    const obj = {}
    columns.forEach((col, i) => {
      obj[col] = arr[i] != null ? arr[i] : ''
    })
    return obj
  })

  return { columns, rows, totalRows: dataRows.length }
}
