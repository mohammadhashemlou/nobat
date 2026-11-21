// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-11-01',
  devtools: { enabled: true },

  modules: ['@nuxt/ui', '@pinia/nuxt'],

  css: ['~/assets/css/main.css'],

  // Full Persian RTL
  app: {
    head: {
      htmlAttrs: {
        lang: 'fa',
        dir: 'rtl'
      },
      title: 'نوبت | رزرو آنلاین آرایشگاه',
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        {
          name: 'description',
          content: 'رزرو آنلاین نوبت آرایشگاه و سالن زیبایی — سریع، ساده و بدون تماس تلفنی'
        },
        { name: 'theme-color', content: '#0f172a' }
      ],
      link: [
        { rel: 'preconnect', href: 'https://cdn.jsdelivr.net' },
        {
          rel: 'stylesheet',
          href: 'https://cdn.jsdelivr.net/gh/rastikerdar/vazirmatn@v33.003/Vazirmatn-font-face.css'
        }
      ]
    }
  },

  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5080',
      salonSlug: process.env.NUXT_PUBLIC_SALON_SLUG || 'default'
    }
  },

  colorMode: {
    preference: 'system',
    fallback: 'light',
    classSuffix: ''
  },

  ui: {
    fonts: false
  },

  typescript: {
    strict: true,
    typeCheck: false
  },

  vite: {
    optimizeDeps: {
      include: ['dayjs', 'dayjs/plugin/utc', 'dayjs/plugin/timezone', 'dayjs/locale/fa', 'jalaliday']
    }
  },

  devServer: {
    port: 3000
  }
})
