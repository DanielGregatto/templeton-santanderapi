[![Build and Test](https://github.com/DanielGregatto/templeton-santanderapi/actions/workflows/dotnet.yml/badge.svg)](https://github.com/DanielGregatto/templeton-santanderapi/actions/workflows/dotnet.yml)

# Hacker News Best Stories API

ASP.NET Core 8 Web API that returns the best *n* Hacker News stories, ranked by score, without
hammering the [Hacker News API](https://github.com/HackerNews/API).

## Running it

```
dotnet run --project src/UI.API
```

The API starts on `http://localhost:5116` (see `src/UI.API/Properties/launchSettings.json` for the
HTTPS/Docker profiles). Swagger UI opens automatically at `/swagger` in development.

```
GET /api/v1/stories/best?n=10
```

Returns `200 OK` with a JSON array of up to `n` stories, sorted by score descending:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

`n` must be a positive integer; anything else returns `400` with a structured error body. If `n`
exceeds the number of stories Hacker News currently considers "best" (it returns up to ~200 ids),
the response simply contains all of them.

Run the tests with:

```
dotnet test src/Tests/Unitary/Domain.Unit.Tests/Unit.Tests.csproj
```

## How it works

`StoriesController` → `IMediatorHandler` (MediatR) → `GetBestStoriesQueryHandler`
→ `IHackerNewsClient`. The handler validates `n`, asks the client for the current best-story ids,
fetches each story's details concurrently, filters out dead/deleted items, sorts by score, and takes
the top `n`.

**Not overloading Hacker News** is handled entirely inside `HackerNewsClient`
(`src/Services/Infrastructure/HackerNews`), not in the handler, so every caller benefits regardless of
how many concurrent requests the API itself is serving:

- **Caching** — the best-story id list and each individual item are cached (`ICacheService` /
  `MemoryCacheService`, TTLs configurable under `HackerNews` in `appsettings.json`, defaults 60s / 300s).
  Both change slowly enough that short-lived caching is safe.
- **Cache-stampede protection** — `MemoryCacheService` takes a per-key lock around cache misses, so if
  10 requests arrive at once against a cold cache, exactly one of them calls Hacker News for that key;
  the other 9 wait and get the cached result. Verified with a unit test
  (`MemoryCacheServiceTests.GetOrCreateAsync_WhenManyConcurrentCallsMissTheCache_OnlyInvokesFactoryOnce`)
  and manually: firing 10 concurrent cold-start requests for `n=5` produced ~201 upstream calls total
  (1 id-list fetch + 1 fetch per distinct story), not one full fan-out per inbound request.
- **Bounded concurrency** — a shared `SemaphoreSlim` (`HackerNewsRequestThrottle`) caps how many
  requests to Hacker News can be in flight at once across the whole app, configurable via
  `HackerNews:MaxConcurrentUpstreamRequests`.
- **Resilience** — the typed `HttpClient` for Hacker News has Polly retry (3 attempts, exponential
  backoff) and a circuit breaker (`ResiliencePolicies`, `src/UI.API/Configurations/`), both logged
  (retry attempts, circuit open/half-open/reset) so a flaky upstream is visible in the logs rather than
  silently retried. Registered as a singleton — Polly's circuit breaker needs shared state across calls
  to actually count consecutive failures; building it fresh per call would reset that count every time
  and it would never trip.
- **Isolated failures** — `GetBestStoriesQueryHandler` fetches each item independently; if one story's
  fetch fails (network blip, malformed item) after retries/circuit-breaker are exhausted, that one
  story is logged and excluded, not the whole request.

Determining the *actual* top `n` by score requires knowing every candidate's score, which means every
id from `beststories.json` has to be fetched at least once — there's no way to know a story's score
without asking for it. The caching above means that fan-out only happens once per TTL window, not once
per request.

## Reproducing problems from the logs

- Every request gets a correlation ID (`CorrelationIdMiddleware`) — read from an inbound
  `X-Correlation-ID` header if the caller sent one, otherwise generated. It's echoed back as a response
  header, embedded in every log line for that request (`Logging:Console:FormatterOptions:IncludeScopes`
  in `appsettings.json`), and used as the `traceId` in error response bodies (`CoreController`,
  `GlobalExceptionHandler`) — so a client-reported `traceId` is actually greppable in the logs, not a
  disconnected random GUID.
- Unhandled exceptions log the request path, **query string** (i.e. the `n` value), method, and
  correlation ID together.
- `MemoryCacheService` logs cache hits/misses at `Debug` level (quiet by default via
  `appsettings.json`, turned on locally via `appsettings.Development.json`) so a caching-related bug
  can be diagnosed by bumping one log level instead of adding print statements.
- `Microsoft.AspNetCore.Hosting.Diagnostics` is explicitly kept at `Information` (narrower than
  `Microsoft.AspNetCore`, which stays `Warning`) so each inbound request logs its status code and total
  duration without turning on framework-wide routing/MVC noise.

## Assumptions

- "Best n stories ... as determined by score" is computed from Hacker News' own best-stories list
  (`beststories.json`, currently ~200 ids), not the entire site — consistent with what the spec's URL
  points at.
- Stories without a `url` (e.g. "Ask HN" / text posts) link to their Hacker News discussion page
  (`https://news.ycombinator.com/item?id={id}`) instead of omitting `uri`.
- `dead` and `deleted` items are excluded from the results.
- `commentCount` maps to the item's `descendants` field (total comment count, not just top-level
  replies) — this is the field that matches the shape of the spec's example (story `21233041` had
  `descendants: 572` around when the spec was written; it's grown since, as comments keep being added).
- No authentication/authorization — the Hacker News API is public and the spec doesn't call for any.

## Architecture note

This uses the same CQRS/MediatR layering (`Domain` → `Services` → `IoC` → `UI.API`) and `Result<T>`
error-handling pattern as a larger personal boilerplate this was adapted from. For a single read
endpoint, a plain `Controller → Service` split would do the same job with less ceremony — MediatR here
is mostly demonstrating the pattern rather than earning its keep. The trade-off felt worth it to show
how the pattern scales, but it's a fair thing to push back on.

DI registration is split by concern rather than all living in one place: `IoC.DIBootstrapper` is the
composition root for business-layer wiring (MediatR, validators, caching) that has no ASP.NET Core
dependency. `HttpClient`/resilience wiring (`HackerNewsClientConfiguration`, `ResiliencePolicies`)
lives in `UI.API/Configurations/` instead, called explicitly from `Program.cs` — partly because it's
genuinely a host-level concern (matching where Swagger/CORS config already lives), and partly because
`IoC` can't reference `UI.API` back (it's already referenced *by* `UI.API`), so anything built in `IoC`
that needed a `UI.API` type would be a circular reference.

## Given more time

- Refresh the best-stories/id cache in the background (`IHostedService`) instead of on-demand, so
  requests never block on a cold cache, even for the very first request after startup.
- A distributed cache (e.g. Redis) instead of `IMemoryCache`, if this ran as more than one instance.
- Integration tests against a stubbed `HttpMessageHandler` (currently only unit tests, with
  `IHackerNewsClient` mocked).
- Inbound rate limiting to protect this API itself from abusive callers (the current caching/throttling
  protects Hacker News, not this API).
- A `Dockerfile` for containerized deployment.