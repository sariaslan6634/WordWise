# WordWise project context

Last verified: 2026-10-28. This note is a compact navigation aid; source code is authoritative when behavior changes.

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
