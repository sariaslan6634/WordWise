# WordWise project context

Last verified: 2026-10-28 (clip/dark-theme/Wiktionary sections below added 2026-11-08; these were written without compiling or running — see "Not yet verified"). This note is a compact navigation aid; source code is authoritative when behavior changes.

## Project map

- .NET 8 solution (`WordWise.slnx`), SQL Server persistence, ASP.NET Core Web API.
- `WordWise.Domain`: entities and enums; no application services.
- `WordWise.Application`: MediatR commands/queries, validators, DTOs, interfaces, mapping, exceptions.
- `WordWise.Infrastructure`: EF Core context/configurations/migrations, repositories, JWT/email/YouTube/dictionary/cache services.
- `WordWise.WebAPI`: controllers, JWT setup, exception middleware, endpoint registration.
- `WordWise.Tests`: xUnit tests; uses EF Core InMemory for application-handler tests.
- `frontend`: Next.js App Router, React, TypeScript and Tailwind CSS. `frontend/README.md` documents startup and the frontend structure.

## Main flows

- Registration/Login/Email confirmation: `Application/Features/Auth/Commands`; confirmation token is generated and hashed at registration, stored for 24 hours, and verified by `Infrastructure/Services/Email/EmailConfirmationService`. Login requires `IsEmailConfirmed`.
- Words: admin CRUD/import commands under `Features/Words/Commands`; public search is `GetWordByTextQuery` and requires a published word. Search results cache via `CacheKeys.WordByText`.
- Video review: fetch external YouTube candidates -> admin chooses a candidate -> sets a start/end second range -> previews the student-facing word card -> final approval creates a published `Video` and sets the associated word's `IsApproved` and `IsPublished`. Video `StartSec`/`EndSec` are persisted and used as YouTube embed `start`/`end` parameters. The separate `PublishWordCommand` still requires a published video.
- Saved words and profile use the user id from JWT claims in controllers, then MediatR query/command handlers.
- Quiz: published question lookup by video id; answer submission updates answer history and XP history (rules in `SubmitQuizAnswerCommand`).
- Frontend routes: `/` is the public vitrin, `/login` handles member login/registration, `/admin-login` is the admin login, `/learn`, `/learn/words`, and `/learn/word/[text]` are member pages, and `/admin`, `/admin/words`, `/admin/word-candidates`, `/admin/videos`, `/admin/videos/preview`, `/admin/users` are protected admin pages. Login verifies against the API and routes by role. The API JWT is stored in an HttpOnly cookie; private data calls go through Next server-side helpers. The admin header avatar menu signs out to `/`; the sidebar stays sticky while dashboard sections scroll and shows the WordWise logo. `/admin/word-candidates` has a one-click Datamuse candidate fetch; each candidate is enriched through FreeDictionary/Datamuse. The candidate action fetches/opens YouTube options without approving the word. `/admin/videos/preview` lets the admin set a clip range and preview word metadata, examples, synonyms, antonyms and the embedded segment. Only final video approval adds the word to the active library. `/learn` has a word search and `/learn/word/[text]` presents active word details and video clips. `/admin/words` defaults to approved terms, with search, auto-applied filters and 7-item pagination. Status meanings: all=approved; active=approved and published; passive=approved but unpublished; draft=unapproved candidate. Candidate fetch is user-triggered; no background daily scheduler runs.
- Dashboard activity is backed by `UserLoginEvents`: one row per successful authenticated login. `ActiveUsersLast30Days` is unique authenticated users in the last 30 UTC calendar days; the chart shows daily unique logins. Last-24-hour counters cover created words/videos, login events, and new accounts. Total users remains all-time. Anonymous page views are not tracked. `/admin/videos` lists unapproved/unrejected video candidates; word approval is a separate queue.
- `ExceptionMiddleware` maps domain/application exceptions to 4xx responses; unexpected 500s return a generic message and details are logged server-side.

## Local database (verified 2026-10-28)

- Windows LocalDB instance: `(localdb)\\MSSQLLocalDB` (SQL Server 2025 Express LocalDB); connect in Visual Studio/SSMS with Windows Authentication.
- Database: `WordWiseDb`; local connection string is in ignored `WordWise.WebAPI/appsettings.Development.json` and must not be committed.
- Initial EF migration `20260629080427_InitialCreate` was applied successfully using EF Core 8.0.28.
- Additive EF migration `20261028121410_AddUserLoginEvents` was applied successfully. Activity history begins with this migration; older logins are not backfilled.
- Local seed contains `admin@wordwise.local` (Admin) and `member@wordwise.local` (User), both email-confirmed, plus 15 sample English words. Generated passwords were given to the user in the conversation; do not store them in this note. Reset the passwords if needed.
- Seeded words are drafts (`IsApproved=false`, `IsPublished=false`); they remain drafts until the admin approves a chosen video clip, which creates the video and activates the word together.
- On this machine, `frontend/.env.local` points to `http://localhost:5272`; that local API host was started with the Development settings and successfully connected to LocalDB. The YouTube API key is in ignored `WordWise.WebAPI/appsettings.Development.json`; it was recovered from the existing local Git history and live-tested successfully with an Apple pronunciation search. Never print or copy the key into tracked files. The Visual Studio host on 5271 redirects to a local HTTPS dev certificate and its own LocalDB startup failed during API login, so keep the 5272 host running while using the frontend. The local override is git-ignored.

## Verification

Admin word approval and status filtering were added 2026-10-28. Migration `20261028125127_AddWordApproval` was applied to `WordWiseDb` using its generated EF SQL script in the already-connected SSMS session; the migration history row and `Words.IsApproved` column were verified. Frontend typecheck and ESLint pass. API Release build passes with 8 existing nullable/unused-parameter warnings, using an isolated output directory because another process holds the normal WebAPI Release binaries. The current API process status was not verified.

Run all tests: `dotnet test WordWise.slnx` (or target `WordWise.Tests/WordWise.Tests.csproj`). The test project restores xUnit and EF InMemory packages from NuGet on a fresh machine.

Frontend integration checked 2026-10-28: `pnpm typecheck`, `pnpm lint`, and `pnpm build` pass. Manual local requests confirmed API-backed admin/member login, live admin stats/users/words, and member profile/word pages. The default VS API on 5271 redirects HTTP to its HTTPS dev certificate and failed LocalDB startup; the working local API host is 5272.

Admin dashboard update checked 2026-10-28: API Release build succeeds (8 pre-existing nullable/unused-parameter warnings); frontend typecheck, lint, and production build pass. Manual requests returned 200 for admin login/dashboard/pending queue/logout. Current local data showed 2 total users, 1 active user after a login, 15 draft words awaiting publication, and no pending video candidates. Login/session graph data starts at `AddUserLoginEvents`; there is no historical backfill or anonymous page-view tracking.

Admin UI polish checked 2026-10-28: logo now has a subtle divider beneath it. Word candidate actions read response bodies as text before parsing JSON, so empty/invalid responses no longer hide the HTTP status behind a JSON parse exception; approval accepts an empty successful response while import still requires result counts. Frontend typecheck and ESLint pass after this update.

Word candidate follow-up checked 2026-10-28: the stale API process on port 5272 was missing `POST /api/admin/AdminWord/daily-candidates`; replacing it with the latest Release build restored that route. The previous direct word-approval endpoint was removed so words cannot become active before a video preview is accepted. Current Datamuse query (`sp` with URL-encoded `?` wildcards, `max=100`, `md=p`) matches official docs and returned 100 live results for the 6-letter daily query. Dictionary lookups can independently return 404 for words absent from Free Dictionary; those are counted as skipped failures. Candidate import explicitly starts `IsApproved=false`, `IsPublished=false`. The local database currently has 15 words, all `IsApproved=false` and `IsPublished=false`. User-table header and rows share matching six-column grids. Frontend typecheck and ESLint pass.

YouTube review flow checked 2026-10-28: the prior YouTube key was present in previous local Development settings in Git history but missing from the current ignored settings; restored the same key locally without exposing it. One live request to YouTube Data API v3 `search.list` for Apple returned a pronunciation video. The word review button calls `POST /api/AdminVideo/fetch-candidates/{wordId}` without approving the word and opens the word-filtered list. `/admin/videos/preview?candidateId=...` loads a candidate plus its word details, allows second-level start/end editing, embeds a segment preview, and on final confirmation posts those values to the existing video approval endpoint. Its admin player uses the YouTube IFrame Player API with a visible seek bar, current/total time, ±5s controls, and buttons to copy current time into clip start/end. That command creates a video with the selected range and marks the candidate and word approved/published; it clears the word detail cache. `/learn/word/[text]` searches the authorized API and embeds approved clips with `controls=1` and YouTube `start`/`end` parameters. The existing database already has `Videos.StartSec`/`EndSec`, so no migration was needed. Two words (`apple`, `achieve`) had been activated by the earlier flow without any published videos; both were returned to the candidate queue. Apple has 10 pending video candidates ready for preview. Latest API Release build succeeds with 8 existing warnings; frontend typecheck and ESLint pass. API routes and frontend dev server response were checked locally.

## Verified fixes in current tree

- Registration previously generated and emailed an email-confirmation token but never stored its hash/expiry, making confirmation impossible. It now persists the hash and 24-hour expiry.
- Login previously allowed accounts with unconfirmed email addresses. It now rejects them.
- Email confirmation verification previously hashed the stored hash instead of hashing the presented token. It now compares the correct SHA-256 values in constant time and rejects malformed stored hashes.
- Resend request JSON now emits lowercase `subject`, as required by its API payload shape.
- Unexpected server error responses no longer expose exception messages, stack traces, or inner exception details to API clients.
- Quiz answer submission now rejects unpublished questions and unknown user ids before persisting an answer.

## Known review findings to track

- Existing build warnings remain: nullable generic/cache mismatch in `GetWordByTextQuery`; unused cache parameter in `CreateWordCommandHandler`; repository `GetByIdAsync` nullable contract mismatch; possible null JWT settings dereference in infrastructure registration; possible null claim dereference in `ProfileController`.
- Several write handlers schedule YouTube candidate fetching with fire-and-forget `Task.Run`; failures are not tracked/retried, and task lifetime is detached from the request.
- Quiz answer submission's 24-hour XP guard still permits multiple answer rows; concurrent duplicate submissions can race without a database uniqueness/transaction rule.
- The bulk import loop commits each word individually and detaches candidate fetching, so partial import is expected if later items fail.
- `ResendEmailSender` logs and swallows provider failures; registration can succeed while its confirmation email was not delivered, and no resend-confirmation endpoint is currently present.
- SQL Server integration/migration behavior and external provider calls are not exercised by the in-memory unit tests.

## Video clips pipeline (branch `feature/clip-agents`, added 2026-11-06..08)

Goal: turn YouTube videos into single-sentence "clips" for a karaoke-style learner player (`/learn/clips`).
- Domain: `VideoClip` (YoutubeId, StartSec/EndSec, Text, `WordTimingsJson`, IsAutoCaption, IsPublished, IsRejected, `HasBurnedInCaptions`, QcStatus/QcReasons/QcWarnings, nullable `VideoCandidateId` kept only to avoid a migration), `DiscoveredVideo`, `ClipReport` (learner reports: Quality/CaptionSync/Inappropriate/Other).
- Application `Features/Clips`: `ClipGenerator` (fetch captions -> `ClipSelector` rule-based sentence picker -> `ClipQualityChecker` -> save; auto-publish only when QC passed), `WordAligner` (aligns caption words to ASR word times), `ContentFilters` (profanity/adult lists), `ClipDiscoveryRunner`. Commands: `GenerateClipsFromVideoCommand`, Publish/Reject/SetBurnedInCaptions, ReportClip, RunDiscovery. Queries: `GetRecentClipsQuery` (admin), `GetLearnerClipsQuery`.
- QC checks: embeddable/public, age restriction, resolution (HD always ok; otherwise yt-dlp-measured max height must be >= 480p, unmeasurable = warning only), caption-sync heuristics (speech rate 1-5 words/s, foreign overlapping caption lines, clip inside video duration), profanity, adult terms.
- Infrastructure: `YouTubeCaptionService` (watch page -> InnerTube ANDROID -> yt-dlp; manual + ASR tracks), `YouTubeVideoDetailsService` (+ yt-dlp height probe), `YouTubeDiscoveryService`, `ClipDiscoveryWorker` (BackgroundService, `AutoDiscovery` config section, ~100 quota units per search, auto-publishes passed clips). `YouTubeSettings:YtDlpPath` configures yt-dlp.
- API: admin (`AdminVideoController`): `POST clips/from-video`, `POST auto-discovery/run`, `GET clips`, `POST clips/{id}/publish|reject`, `POST clips/by-video/{youtubeId}/burned-in`. Learner (`ClipsController`): `GET api/Clips`, `POST api/Clips/{id}/report`. Unused candidate-based clip endpoints were removed 2026-11-07.
- Frontend: `components/synced-caption-player.tsx` (YouTube IFrame API; rAF word highlight with 0.15s lead; `cinema` variant overlays captions inside the video, caption appears only after playback starts; auto-advance + 3x loop switches; centered replay button after finish; replay/fullscreen/volume tools in the bottom-right corner, volume saved in localStorage), `app/learn/clips/*` (report modal), `app/admin/clips/*` (generate/publish/reject/burned-in toggle). Clips with `HasBurnedInCaptions` hide our caption.
- Migrations: the `AddBurnedInCaptionsFlag` migration had a model-snapshot problem ("DiscoveredVideos already exists"); fix = remove the pending migration, re-add, delete the CreateTable DiscoveredVideos/ClipReports parts, `dotnet ef database update`. User later reported things working.

## Dark theme (2026-11-07)
Whole site is dark: `frontend/app/globals.css` `:root` has `color-scheme:dark`, `--paper:#0b100d`, `--ink:#e8efe9`, `--muted:#93a097`, `--line:#26302a`, `--green:#c9f06b`. The original light rules were converted programmatically (surfaces dark, text light, accents kept); the old clips-only `:has(.cinema-page)` dark override was removed. globals.css is minified one-rule-per-block; later additions are appended at the end.

## Wiktionary word import (2026-11-08)
`WordWise.WiktionaryImporter` (console app, in the solution; tests in `WordWise.Tests/WiktionaryParserTests.cs`) streams `raw-wiktextract-data.jsonl` (24 GB, never uploaded; lives in git-ignored `Data/`) and bulk-inserts English single-word entries into `Words` (definition, IPA, POS, <=3 examples, <=5 synonyms/antonyms; skips proper nouns, inflections, obsolete/offensive senses, existing words). Flags: `--file`, `--connection` (or env `WORDWISE_CONNECTION`), `--limit`, `--wordlist`, `--batch`, `--dry-run`, `--publish` (insert as approved+published; otherwise unapproved). The user ran it and has Wiktionary words in the DB. CEFR level is not available from Wiktionary (left null).

## Word pronunciation audio (2026-11-08)
- Source: user's Wiktionary audio files in git-ignored `Data/wiktionary-audios` (~30k files, ~592 MB: `.ogg`, `.ogg.mp3` transcodes, a few `.oga`). Names look like `En-GB-augment-noun.ogg.mp3`, `En-us-beautiful.ogg`, `En-gb-Jam_%28fruit_spread%29.ogg` (percent-encoded, `_` = space).
- Backend: `Application/Features/WordAudio/WordAudioIndex` parses names (only `en-us` -> "us", `en-gb`/`en-uk` -> "gb"; strips POS suffix and `(qualifier)`; per (word, accent) prefers plain name over POS/qualified and mp3 over ogg over oga). `Infrastructure/Services/WordAudioService` (singleton, lazy scan once; folder = `WordAudio:Folder` setting or `Data/wiktionary-audios` found by walking up from the app directory). `GET api/WordAudio/{accent}/{word}` (authorized, streams the file with range support). `WordDetailDto.AudioAccents` is filled in `GetWordByTextQueryHandler` after the cache (not cached).
- Frontend: `components/pronunciation-button.tsx` shows one button per available accent (US / UK), a single untagged button if only one exists, a disabled one if none. Plays via the Next proxy `/api/backend/api/WordAudio/{accent}/{word}`. Tests: `WordWise.Tests/WordAudioIndexTests.cs`.
- Not yet tested end to end with the real folder; pending user run (`dotnet test`, restart API, open `/learn/word/<word>`).

## Save word / "Kelime defterim" (2026-11-08)
- `/learn/word/[text]` has a top-right bookmark button (`components/save-word-button.tsx`) -> `POST/DELETE /api/UserWord/{wordId}` via the Next proxy. `WordDetailDto.IsSaved` is filled per user in `GetWordByTextQueryHandler` on a clone of the cached DTO (the query now takes an optional `UserId`; `WordsController.Search` passes it from the JWT).
- `SaveWordCommand` now restores a soft-deleted `UserWord` row instead of inserting (the unique index `(UserId, WordId)` ignores `IsDeleted`, so re-saving after unsave used to fail).
- `/learn/words` (kelime defterim) lists text (link to the word page), definition, first example sentence (`SavedWordDto.ExampleSentence`), level and review info. No unsave button there yet.
- Tests: `WordWise.Tests/SavedWordTests.cs`. Not compiled/run by the author.

## Player options, notebook, review, search, CEFR (2026-11-09)
- Clip types (song/tongue twister/set phrase filter) were tried and REMOVED on 2026-11-09 because the rule-based classification was unreliable (no `ClipClassifier`, no `categories` param). Songs are therefore no longer blocked from auto-publish; if songs are a concern, add a manual rule again.
- `/learn/clips` toolbar switches (localStorage `wordwise-clip-settings`): auto-advance, loop x3, caption on/off (default on), sync speech on/off (off = whole sentence shown, no word highlight). Caption is shown only while the clip plays (hidden before start and after the clip ends).
- Search suggestions: `GET api/Words/suggest?prefix=&limit=8` (`SuggestWordsQuery`, published words starting with the prefix, shortest first, prefix sanitized to letters/'/-). `/learn` search box (`app/learn/word-search.tsx`) shows them from the first letter (120 ms debounce, keyboard navigation).
- Notebook `/learn/words` (`saved-words-view.tsx`): loads up to 100 saved words, part-of-speech dropdown (built from the user's own words), "Tekrar yap" button, per-word bookmark button to remove (compact `SaveWordButton`), example sentence shown. >100 saved words are not paged yet.
- Review: `/learn/words/review?pos=&all=1` (`review-session.tsx`): flashcards (word, IPA, US/UK audio, reveal meaning + example, "Bilemedim/Biliyordum"). Backend: `GetReviewWordsQuery(UserId, IncludeNotDue, PartOfSpeech)` (due words first by KnownLevel/NextReviewAt, max 50), `POST api/UserWord/{wordId}/review {known}` -> `ReviewWordCommand` Leitner schedule: known = level+1 (max 5) and next review in 2/4/8/16/32 days; unknown = level-1 (min 0) and next review in 10 minutes. Before this there was only the due-words query, no update step.
- CEFR: Wiktionary has no levels, so `Words.CefrLevel` is null for imported words. `WordWise.WiktionaryImporter --cefr-file list.csv [--freq-file freq.txt] --connection ... [--overwrite] [--dry-run]` fills it (CSV needs headword/word/lemma + CEFR/level columns; easiest level per word wins; frequency list gives only approximate bands: rank <=1000 A1, <=2500 A2, <=5000 B1, <=10000 B2, <=20000 C1, <=40000 C2). Suggested source: CEFR-J Vocabulary Profile (github openlanguageprofiles/olp-en-cefrj; free incl. commercial use with citation per its README — verify licence) + Octanove C1/C2 (CC BY-SA 4.0). Not run yet.
- Tests: `ReviewAndSuggestTests.cs`, `CefrLevelsTests.cs`. Nothing here was compiled/run by the author (TS syntax-checked only).

## Not yet verified (written without dotnet/npm in the authoring sandbox)
The 480p QC change, dark theme, volume/replay UI, Wiktionary importer and pronunciation button were committed without being compiled; the user runs them locally. If something fails to build, fix from the compiler error. Still open: admin screen for learner clip reports, Mod 2 (fill-in-the-blank), CEFR levels for imported words, Claude-API-based checks (no API key yet).

## Silinenler (24 saat geri alma) ve bildirimler
- Kelime defterinden çıkarılan kelime soft-delete olur; silme anı `UserWord.UpdatedAt` (migration yok). `GET api/UserWord/removed` son 24 saatte çıkarılanları döner (`GetRemovedWordsQuery`, `RemovedAt/ExpiresAt`). Süresi dolan kelime listeden kendiliğinden düşer (satır DB'de kalır; yeniden kaydedilirse eski satır geri gelir).
- Geri al = mevcut `POST api/UserWord/{wordId}` (SaveWordCommand silinmiş satırı geri getirir).
- Frontend: `app/learn/words/removed-words.tsx` ("Silinen kelimeler" butonu + modal, kalan süre, Geri al), `components/notification-bell.tsx` (üst çubukta zil; silinmesine ≤3 saat kalanlar için bildirim, 60 sn'de bir yoklar, okundu bilgisi localStorage `wordwise-dismissed-notifications`), `lib/removal-time.ts`.
- Bildirimler sunucuda saklanmaz; "removed" listesinden hesaplanır (e-posta/push yok).
- Test: `SavedWordTests.Removed_words_are_listed_for_24_hours_and_can_be_restored` (derlenmedi/çalıştırılmadı).
- Navbar (`app/learn/layout.tsx`): Öğrenme Alanı · Kelime Defteri · Listening · Writing (yer tutucu sayfa `/learn/writing`) · Tekrar Yap (eski sıra değişti) (eski "Klipler") · Kelime defterim · Tekrar yap; "Vitrin" linki kaldırıldı, "Tekrar yap" butonu defter sayfasından navbar'a taşındı (tür filtresi artık tekrar linkine geçmiyor).

## Listening: kaynak bilgisi, kayıtlı klipler, zamanlama (Kasım 2026)
- **Migration gerekir** (yeni): `VideoClips.SourceChannel` kolonu ve `UserClips` tablosu (unique `(UserId, ClipId)`, soft-delete; kaydı kaldırıp yeniden kaydedince satır geri gelir).
- Klip altında (Önceki/Sonraki butonlarının altında) kaynak kutusu: başlık, kanal, "YouTube'da izle" (`&t=` ile); Kaydet ikonu videonun sağ üstünde. Açıklama kaldırıldı (kolon da yok). Yeni klipler bilgiyi üretimde `VideoDetails`'ten alır; eski klipler için `ClipSourceBackfiller` isteklerde en çok 4 videoyu YouTube snippet'iyle tamamlar (`IVideoDetailsService.GetSnippetAsync`, yt-dlp probe yok).
- Kaydetme: `POST/DELETE api/Clips/{id}/save` (idempotent), `GET api/Clips/saved`, `GET api/Clips?savedOnly=true` (kayıtlılardan rastgele). UI: klip altında Kaydet butonu, "Sadece kaydettiklerim" anahtarı, `/learn/clips/saved` (thumbnail `i.ytimg.com/vi/{id}/hqdefault.jpg` + cümle + Kaydet/Kaydedildi), `/learn/clips?saved=1`.
- Zamanlama: `AutoDiscovery:AutoPublishRequiresRealTimings` (varsayılan true) — kelime zamanları tahmini olan klipler otomatik yayınlanmaz (admin onayı bekler). Mevcut yayınlanmış tahmini klipleri kaldırmak için (isteğe bağlı): `UPDATE VideoClips SET IsPublished = 0 WHERE IsPublished = 1 AND QcWarnings LIKE N'%Kelime zamanları tahmin edildi%';`
- Oynatıcı: Shorts/videonun sonundaki klipler "ended" vermeden başa sarabiliyor → `synced-caption-player.tsx` süre geri sarmasını (`wrapped`) ve video sonunu (`atVideoEnd`) yakalayıp klibi bitirir; oynatma klip başlangıcından önce başlarsa bir kez `seekTo(startSec)`.
- Derlenmedi/çalıştırılmadı: yeni testler `LearnerClipTests` (kaydetme), `AutoDiscoveryTests` (gerçek zaman şartı).
