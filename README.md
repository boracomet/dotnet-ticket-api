# Ticket API: .NET 8

Katmanlı ticket yönetim API'si: JWT kimlik doğrulama, **Admin/User** rolleri, sayfalama, filtreleme ve sıkı durum iş akışı; yanında Türkçe React arayüzü (Ticket Board). Docker Compose ile çalışır.

## Mimari

```
TicketApi.Api            → Controllers, JWT, Swagger
TicketApi.Application    → Services, DTO'lar, arayüzler
TicketApi.Domain         → Entity'ler ve enum'lar
TicketApi.Infrastructure → EF Core, PostgreSQL/SQLite, JWT ve şifre hashleme
web/                     → Vite + React + TypeScript arayüzü (Nginx)
```

## Özellikler

- JWT ile kayıt / giriş
- Ticket: oluşturma (User), listeleme/filtreleme, detay; silme ve durum değiştirme (Admin)
- Sayfalama ve `status`, `priority`, `search` ile filtreleme
- Durum iş akışı: `Open → InProgress → Resolved → Closed` (geçersiz geçişler → `422`)
- Rol kuralları: kullanıcılar kendi / atanmış ticket'larını görür; adminler tümünü görür
- Markdown cevaplar; ekler PDF/JPG/PNG (en fazla 3 dosya, her biri 5 MB)
- Cevapta e-posta bildirimi (SMTP: `.env` veya admin paneli)

### Rol kuralları

| İşlem | User | Admin |
|-------|------|-------|
| Ticket oluştur | ✓ | ✗ |
| Ticket sil | ✗ | ✓ |
| Durum değiştir | ✗ | ✓ |
| Cevap yaz (görebilen) | ✓ | ✓ |

Ticket'ı görebilen herkes (oluşturan, atanan veya admin) cevap yazabilir.

**Seed kullanıcılar (ilk çalıştırma)**

| E-posta | Şifre | Rol |
|---------|-------|-----|
| admin@ticket.local | Admin123! | Admin |
| user@ticket.local | User1234! | User |

## Hızlı başlangıç: Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

- **Web arayüzü:** http://localhost:3000  
- **API:** http://localhost:8080  
- **Swagger:** http://localhost:8080/swagger  

Portlar: `WEB_PORT` (varsayılan **3000**), `APP_PORT` (varsayılan **8080**), `POSTGRES_PORT`. Port doluysa `.env` içinde `WEB_PORT` (veya diğerlerini) değiştirin.

> Aynı anda varsayılan portları (8080/3000/5432) kullanan başka bir uygulama ile birlikte çalıştırmayın.

## Yerel (SQLite)

```bash
cd src/TicketApi.Api
dotnet run
# Development profili SQLite kullanır (appsettings.Development.json)
```

## Yerel (PostgreSQL)

```bash
docker compose up -d postgres
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=ticket_api;Username=ticket;Password=ticket_secret_change_me'
export Database__Provider=Postgres
dotnet run --project src/TicketApi.Api
```

## Web arayüzü (React)

Yumuşak, profesyonel Türkçe arayüz: giriş/kayıt, filtreli ticket panosu, ticket oluşturma (User), Markdown cevaplar ve ekler, admin durum/silme, admin SMTP ayarları.

### Kimlik doğrulama: giriş ve kayıt

Seed demo hesaplarla giriş yapın veya yeni kullanıcı kaydedin.

![Giriş](docs/screenshots/ui-login.png)

*Giriş: demo hesaplar ekranda listelenir*

![Kayıt](docs/screenshots/ui-register.png)

*Kayıt: yeni User hesabı*

### Pano

User panosu: kendi / atanmış ticket'lar, durum/öncelik filtreleri, arama, yatay liste, sayfalama. Adminler tüm ticket'ları görür; yalnızca User'larda "+ Yeni ticket" vardır.

![Ticket panosu](docs/screenshots/ui-board.png)

*Pano: filtreler, öncelik etiketleri, sayfalama*

### Ticket oluşturma

Markdown araç çubuğu ve dosya ekleme (PDF/JPG/PNG, en fazla 3 × 5 MB). Adminler ticket oluşturamaz.

![Ticket oluştur](docs/screenshots/ui-ticket-create.png)

*Yeni ticket: Markdown + Dosya ekle*

### Ticket detayı ve cevaplar

Markdown gövde, cevap alanı, ekler. Durum açılır menüsü yalnızca Admin'e açıktır. SMTP açıksa cevaplar e-posta ile bildirilir.

![Ticket detayı](docs/screenshots/ui-ticket-detail.png)

*Detay: Markdown gövde, cevap alanı + dosya*

### Yerel geliştirme

```bash
# Terminal 1
cd src/TicketApi.Api && dotnet run

# Terminal 2
cd web && npm install && npm run dev
# → http://localhost:5173
```

### Docker

`docker compose up --build` arayüzü **:3000** üzerinde sunar (Nginx → API).

CORS, `http://localhost:3000` ve `:5173` için açıktır. Enum'lar `JsonStringEnumConverter` ile string olarak serileştirilir (`Open`, `High`, …).

## E-posta / SMTP

Birisi cevap verdiğinde:

- **User** cevap verirse → adminlere `"{Ad} cevap verdi"` gider
- **Admin** cevap verirse → ticket sahibine (ve varsa atanan kişiye) `"Admin cevap verdi"` gider

**Yapılandırma sırası**

1. Önce `.env` / Compose ortam değişkenleri (`SMTP_*` → `Smtp__*`).
2. `SMTP_HOST` boşsa API, admin Ayarlar formunda kaydedilen değerlere düşer (yerel deneme için).
3. Admin formundaki "Test e-postası" form/DB ayarlarını kullanır (üretim yapılandırması için değildir).

| Ortam anahtarı | Amaç |
|----------------|------|
| `SMTP_HOST` | SMTP sunucusu (boş → admin formu / DB) |
| `SMTP_PORT` | Port (varsayılan `587`) |
| `SMTP_USERNAME` | Kullanıcı adı |
| `SMTP_PASSWORD` | Şifre |
| `SMTP_FROM_EMAIL` | Gönderen adresi |
| `SMTP_FROM_NAME` | Gönderen görünen adı (varsayılan `Ticket Board`) |
| `SMTP_ENABLE_SSL` | SSL / STARTTLS (varsayılan `true`) |
| `SMTP_ENABLED` | Bildirim gönder (varsayılan `false`) |

![Admin SMTP ayarları](docs/screenshots/ui-settings.png)

*Admin: E-posta bildirimleri / SMTP (deneme formu)*

## Testler

```bash
dotnet build
dotnet test
```

## Örnek

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"user@ticket.local","password":"User1234!"}' | jq -r .accessToken)

curl -s -X POST http://localhost:8080/api/v1/tickets \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Login bug","description":"Cannot login on Safari","priority":"High"}'
```

## Lisans

MIT: Bora Ata Türkoğlu tarafından portfolio demosu.
