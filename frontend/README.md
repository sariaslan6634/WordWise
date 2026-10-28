# WordWise ön yüzü

WordWise .NET API'si için Next.js App Router, React ve TypeScript arayüzü.

## Yerelde çalıştırma

1. `.env.example` dosyasını `.env.local` olarak kopyala; `WORDWISE_API_URL` değerini API adresine ayarla. Bu makinedeki LocalDB çalışma adresi `http://localhost:5272` olarak `.env.local` içinde kayıtlıdır.
2. API'yi ayrı bir PowerShell penceresinde çalışır tut. Kök dizinden: `$env:ASPNETCORE_ENVIRONMENT = "Development"`, `$env:ASPNETCORE_URLS = "http://localhost:5272"`, ardından `dotnet run --configuration Release --no-build --project WordWise.WebAPI --no-launch-profile`.
3. Bu klasörde `pnpm dev` çalıştır ve `http://localhost:3000` adresini aç.

Vitrin `/`, üye giriş/kayıt `/login`, admin girişi `/admin-login`, üye alanı `/learn`, üye kelime defteri `/learn/words`, admin paneli `/admin`, admin kelimeleri `/admin/words`, onay kuyruğu `/admin/videos`, admin kullanıcı listesi `/admin/users` adresindedir. Giriş API üzerinden doğrulanır; role göre farklı alana yönlendirilir. JWT HttpOnly çerezde tutulur ve tarayıcı `localStorage`'ına yazılmaz. Üye ve admin rotaları sunucuda rol kontrolünden geçer.

Doğrulama komutları: `pnpm lint`, `pnpm typecheck`, `pnpm build`.

## Mimari

- `app/`: sayfa rotaları ve giriş/kayıt/çıkış ile kimlik doğrulamalı API proxy route'ları.
- `lib/contracts.ts`: API yanıt ve DTO şekillerinin TypeScript karşılıkları, ayrıca yalnız önizleme için örnek veriler.
- `lib/server-api.ts`: sunucu tarafı API çağrı yardımcıları. API adresi tarayıcıya açılmaz.
- `lib/session.ts`: HttpOnly oturum çerezini ve ekranda gösterilecek JWT claim'lerini okur. Kimlik doğrulama ve yetkilendirme için asıl otorite .NET API'dir.
- `app/globals.css`: WordWise tema ve duyarlı ekran stilleri.

`WORDWISE_API_URL` Next.js sunucusunun erişebildiği API adresi olmalıdır. Admin uçları .NET API'de Admin rolünü denetlemeye devam eder.
