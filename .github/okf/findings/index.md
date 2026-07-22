---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-07-22
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 7 finding(s) across 5 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### articles (3)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [DELETE /articles/{slug} returns 204 No Content; the spec documents 200. The NSwag-generated client throws ApiException on the typed call because 204 is not in its expected set.](articles/delete-articles-slug-returns-204-no-content-the-spec-documen.md) | NEW | 1 | Delete via a raw HTTP call and assert 204, not the typed client. Do not 'fix' this by widening the assertion - the spec is the side that needs updating. |
| [GET /articles omits the required 'body' field from each item, while GET /articles/{slug} includes it. Schema validation against components.schemas.Article fails with ArrayItemNotValid on every list item.](articles/get-articles-omits-the-required-body-field-from-each-item-wh.md) | NEW | 1 | Validate GET /articles with 'body' waived (SchemaValidator waiveRequired) and keep the single-article endpoint on the full schema. Raised as CONDUIT-302. |
| [POST /articles rejects a title of 100 characters or more with 422 {"errors":{"body":["title is a string of less than 100 chars"]}}. The spec declares no maxLength on Article.title.](articles/post-articles-rejects-a-title-of-100-characters-or-more-with.md) | NEW | 1 | Keep generated titles under 100 chars (TestDataFactory.MaxTitleLength), truncating the phrase and never the uniqueness suffix. Spec gap raised as CONDUIT-303. |

### auth-and-test-fixtures (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [Every ApiClientFactory call used to mint its own token, so a test holding both a typed and a raw client acted as two different registered users - creating a resource as one and fetching it as the other, which 404s because this backend scopes resources to their owner.](auth-and-test-fixtures/every-apiclientfactory-call-used-to-mint-its-own-token-so-a-.md) | NEW | 1 | Acquire the session token once (ApiClientFactory.SessionToken) and share it across every client. Use CreateRawClientForToken only when a test deliberately needs a second identity. |

### http-client-setup (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [BASE_URL carries a path segment (https://api.realworld.show/api). HttpClient resolved 'users' and '/users' alike against a base without a trailing slash, dropping /api and 404ing every raw call - which is why AuthTokenGenerator could never register a user.](http-client-setup/base-url-carries-a-path-segment-https-api-realworld-show-api.md) | NEW | 1 | Build raw clients from ApiClientFactory.ResolveBaseAddress() (guaranteed trailing slash) and use relative request URIs with no leading slash. |

### profiles (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [GET /profiles/{username} returns 404 for any caller other than the profile's owner, and 404 (not the documented 401) when unauthenticated. Verified against api.realworld.show: registering user A and fetching A's profile with A's token returns 200; fetching it with B's token, or anonymously, returns 404.](profiles/get-profiles-username-returns-404-for-any-caller-other-than-.md) | NEW | 1 | Fetch a profile as its own owner. Do not write cross-user profile or follow tests against this backend - POST /profiles/{username}/follow 404s for every other user, so the follow flow is untestable here (quarantined as CONDUIT-301). |

### spec-corrections (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [specs/openapi.json amended from verified evidence: GET /profiles/{username} now documents 404; DELETE /articles/{slug} corrected 200 -> 204; NewArticle.title given maxLength 99; MultipleArticlesResponse now references a new ArticleSummary schema without the required 'body'.](spec-corrections/specs-openapi-json-amended-from-verified-evidence-get-profil.md) | NEW | 1 | The spec now matches observed behaviour for these four; re-run 'appqe contract verify' after any backend deploy rather than assuming it still does. Do not reverse these to make a test pass - the evidence is recorded here. |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
