# Ticket Board — Web UI

Vite + React + TypeScript frontend for the .NET 8 Ticket API.

## Features

- Türkçe arayüz: giriş / kayıt / ticket panosu
- JWT + rol rozeti (Admin / User)
- Filtre (status, priority, search) + sayfalama
- Ticket oluşturma, durum workflow geçişleri, silme
- Seed hesaplar login ekranında tek tıkla doldurulur

## Scripts

```bash
npm install
npm run dev      # :5173, /api → localhost:8080
npm run build
npm run preview
```

## Demo users

| Email | Password | Role |
|-------|----------|------|
| admin@ticket.local | Admin123! | Admin |
| user@ticket.local | User1234! | User |

## Docker

Root `docker compose` service `web` on port **3000** (Nginx proxies `/api` → `app:8080`).
