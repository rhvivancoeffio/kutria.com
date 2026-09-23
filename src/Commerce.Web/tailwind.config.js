/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        display: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'monospace'],
      },
      padding: {
        safe: 'env(safe-area-inset-bottom, 0px)',
      },
      minHeight: {
        dvh: '100dvh',
        svh: '100svh',
      },
      height: {
        dvh: '100dvh',
        svh: '100svh',
      },
      colors: {
        kutria: {
          bg: '#0A0A0A',
          cyan: '#00F5FF',
          purple: '#8B5CF6',
          gray: '#A1A1AA',
          border: '#27272A',
          success: '#22C55E',
          warning: '#EAB308',
        },
        ink: {
          950: '#0A0A0A',
          900: '#0F0F0F',
          850: '#111111',
          800: '#121212',
          750: '#141414',
          700: '#161616',
          600: '#1A1A1A',
          500: '#2A2A2A',
        },
        primary: {
          50: '#e8fffe',
          100: '#c4fffb',
          200: '#8dfff6',
          300: '#4dfff2',
          400: '#7dfff8',
          500: '#00F5FF',
          600: '#00F5FF',
          700: '#00d4dc',
          800: '#0b7c82',
          900: '#0c5c60',
        },
        accent: {
          50: '#f5f3ff',
          100: '#ede9fe',
          200: '#ddd6fe',
          300: '#c4b5fd',
          400: '#a78bfa',
          500: '#8B5CF6',
          600: '#7c3aed',
          700: '#6d28d9',
          800: '#5b21b6',
          900: '#4c1d95',
        },
      },
      backgroundImage: {
        kutria: 'linear-gradient(135deg, #00F5FF 0%, #8B5CF6 100%)',
      },
      boxShadow: {
        'glow-primary': '0 0 12px #00F5FF',
        'glow-primary-lg': '0 0 40px rgba(0,245,255,0.14)',
      },
      borderRadius: {
        '2.5xl': '1.25rem',
        '4xl': '1.75rem',
      },
    },
  },
  plugins: [],
}
