#!/usr/bin/env node
/**
 * Verifica que el build de producción contenga las URLs esperadas de .env.production.
 * Uso: node scripts/verify-build-env.js
 * Ejecutar después de "npm run build" (requiere que exista dist/).
 */
import { readFileSync, readdirSync, existsSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const root = join(__dirname, '..')
const distDir = join(root, 'dist')

const envPath = join(root, '.env.production')
if (!existsSync(envPath)) {
  console.warn('No existe .env.production, no se puede verificar.')
  process.exit(0)
}

const envContent = readFileSync(envPath, 'utf8')
const authBase = (envContent.match(/VITE_AUTH_BASE_URL=(.+)/)?.[1] || '').trim()
const apiUrl = (envContent.match(/VITE_API_URL=(.+)/)?.[1] || '').trim()

const expected = []
if (authBase && !authBase.startsWith('#')) expected.push({ name: 'VITE_AUTH_BASE_URL', value: authBase })
if (apiUrl && !apiUrl.startsWith('#')) expected.push({ name: 'VITE_API_URL', value: apiUrl })

if (expected.length === 0) {
  console.log('No hay URLs de producción que verificar en .env.production.')
  process.exit(0)
}

if (!existsSync(distDir)) {
  console.error('No existe dist/. Ejecuta "npm run build" antes.')
  process.exit(1)
}

const assetsDir = join(distDir, 'assets')
if (!existsSync(assetsDir)) {
  console.error('No existe dist/assets/. Build incompleto.')
  process.exit(1)
}

const files = readdirSync(assetsDir).filter((f) => f.endsWith('.js'))
let allContent = ''
for (const f of files) {
  allContent += readFileSync(join(assetsDir, f), 'utf8')
}

let failed = false
for (const { name, value } of expected) {
  if (allContent.includes(value)) {
    console.log('✓', name, '→', value)
  } else {
    console.error('✗', name, 'no encontrado en el build:', value)
    failed = true
  }
}

if (failed) {
  console.error('\nEl build no contiene las URLs de .env.production. ¿Se usó "vite build" (mode production)?')
  process.exit(1)
}
console.log('\nBuild verificado: las URLs de producción están presentes.')
