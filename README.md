# giftlist-identity

Identity service: sign up, log in, JWT issuance. Owns the `identity` database and the `identity`
Rebus queue (both singular — CONVENTIONS.md "Persistence").

```
src/Identity.Domain          entities and value objects; references nothing
src/Identity.Application     use cases and ports; Domain + BuildingBlocks only
src/Identity.Infrastructure  Mongo, Rebus handlers, JWT issuance, Platform/ wiring
src/Identity.Host            composition root; wiring only
src/Identity.Contracts       THE PUBLISHED PACKAGE (see below)
```

## `Identity.Contracts`

Identity's entire public wire surface, and nothing else: **the commands it accepts and the events
it publishes** (ARCHITECTURE.md "Contracts: each service owns and publishes its own"). If a type
is not in here, no other service can depend on it — which is the property this shape exists for.

| Kind | Types |
|---|---|
| Commands it accepts | `SignUp`, `Login` |
| Reply shapes for those commands | `SignUpReply`, `LoginReply` |
| Events it publishes | `UserRegisteredV1` |

`SignUpReply`/`LoginReply` are in the package because sign-up and log-in go over the
request/reply bridge rather than fire-and-forget (ARCHITECTURE.md "Command → event flow" — the
caller is actively waiting), so the reply shape is as much a part of the accepted-command
contract as the command is. Nothing else qualifies: the aggregate, its domain events, the
password hash and the repository port are all implementation, and a consumer that could name them
would be coupled to how Identity works rather than to what it promises.

The package references nothing — never Domain, never BuildingBlocks (CONVENTIONS.md "Project
reference graph"), asserted two ways: no `ProjectReference` and no `PackageReference`.

Type names and namespaces are part of the wire format, because Rebus routes and deserializes on
the .NET type name. Renaming one is a **major** version bump even if the shape is byte-identical
(ARCHITECTURE.md "What 'breaking' means for a message contract").

## Directory.Build.props is a copy, and it is checked

`net10.0`, `LangVersion latest`, nullable on, warnings as errors, implicit usings — set once in
`Directory.Build.props` at this repo's root, inherited by every project. No `.csproj` sets
`TargetFramework` itself (CONVENTIONS.md "Target framework").

Before the split there was one such file, at the monorepo root, and MSBuild's directory walk gave
every service the same values. MSBuild does not walk out of a repo, so each .NET repo now has its
own copy — and copies drift. **Do not hand-edit this one.** Edit the canonical copy in
`giftlist-buildingblocks`, then run `giftlist-devenv/scripts/sync-repo-roots.sh`, which rewrites
every copy and regenerates the `repo-root-files.sha256` manifest beside each.

Two tests fail if you edit it in place, and they check different things:

- `RepoRootFileSyncTests` — this copy is byte-identical to the canonical one.
- `TargetFrameworkTests.DirectoryBuildProps_ShouldMatchConventionsVerbatim` — the content is the
  block CONVENTIONS.md documents. Every repo can agree on a wrong file; this is what catches it.

The `Architecture/` suite under `tests/*.UnitTests/` is governed the same way: canonical copy in
`giftlist-giftlists`, propagated by `giftlist-devenv/scripts/sync-arch-tests.sh`, pinned by
`architecture-tests.sha256` and `ArchitectureTestSyncTests`.

## Where this repo sits

Seven repos under `goodsell-engineering`, cloned as siblings (ARCHITECTURE.md "Repository
layout"):

```
giftlist/
  local-feed/              <- .nupkg and .tgz files land here; not a git repo
  giftlist-devenv/         <- docker compose, make up, the sync scripts
  giftlist-buildingblocks/
  giftlist-gateway/
  giftlist-identity/
  giftlist-giftlists/
  giftlist-reservations/
  giftlist-web/
```

Design documents (`ARCHITECTURE.md`, `CONVENTIONS.md`) live in the workspace repository, not in
any of the seven: they govern all of them, a home inside one is invisible to the other six, and
seven copies is exactly the drift they warn about. Comments here cite them by document and
heading text, never by section number (CONVENTIONS.md "Citing the rules").

## What does not work yet, and whose job it is

Restore needs the local folder feed, and the pieces that wire it up are the next three issues:

| Missing | Issue |
|---|---|
| `../local-feed` and a `nuget.config` per repo pointing at it alongside nuget.org | GL-26 |
| Semantic versioning discipline and consumer pinning | GL-27 |
| `make pack-all` (dependency-ordered, refuses to overwrite a version already in the feed) and `clone-all.sh` | GL-28 |
| Compose mounting the feed into the containers | GL-29 |
| Per-repo CI (build and test only; there is nowhere to publish to) | GL-30 |

Until GL-26 lands, restore by naming the feed on the command line:

```
dotnet restore <Solution>.sln -s https://api.nuget.org/v3/index.json -s ../local-feed
dotnet build <Solution>.sln --no-restore
dotnet test tests/*.UnitTests/*.UnitTests.csproj
```

**A warning for whoever writes GL-26's `nuget.config`.** nuget.org already serves unrelated
packages called `BuildingBlocks` (at 1.0.0) and `Identity.Contracts` (at 1.0.14). A consumer
configured with both nuget.org and the folder feed can satisfy an exact version from either
source, so listing the feed as a second `<add key>` is not enough — use `packageSourceMapping`
to bind these ids to the local feed. The 0.1.0 starting version was picked because it exists on
neither id upstream, which narrows the window but does not close it.
