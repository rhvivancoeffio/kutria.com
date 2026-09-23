import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

// Classic script bundle so the embed snippet can omit type="module"
// and still read data-tenant / data-api from document.currentScript.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  build: {
    emptyOutDir: false,
    outDir: 'dist',
    cssCodeSplit: false,
    rollupOptions: {
      input: path.resolve(__dirname, 'src/embed.js'),
      output: {
        format: 'iife',
        inlineDynamicImports: true,
        entryFileNames: 'embed.js',
        name: 'KutriaChatEmbed'
      }
    }
  }
})
