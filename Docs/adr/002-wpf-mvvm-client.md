# ADR 002: WPF Desktop Client Architecture (MVVM)

## Status
Accepted

**Date:** 2026-06-22
**Deciders:** Solution owner (João Botelho)
**Related:** [ADR-001 — Edge Security Strategy](./001-waf-strategy.md)

## Context

The Library System backend is a Clean Architecture .NET 10 solution: a gRPC Core Service (`LibrarySystem.Grpc`) fronted by a thin REST gateway (`LibrarySystem.Api`), with the edge protected by a Caddy + Coraza WAF (see ADR-001). A WPF desktop client already exists in `LibrarySystem.Tools` — a dark-themed **"Service Graph Inspector"** that renders the book/author domain as a draggable node graph and streams live WAF audit logs through the `Security.WatchWafLogs` server-streaming RPC.

The client is roughly 70% built on `CommunityToolkit.Mvvm 8.4.0` with an `IHost`/DI bootstrapper, but it carries unfinished scaffolding and several MVVM anti-patterns that would undermine its value as a portfolio artifact:

- Empty service abstractions (`IGraphDataService`, `ILogTailerService`) registered in DI but never implemented; ViewModels talk to the generated gRPC clients directly, so they cannot be unit-tested.
- `MessageBox.Show(...)` called from inside ViewModels (couples view-models to the UI shell, blocks testability).
- `NodeDragBehavior` reaches into `Application.Current.MainWindow.DataContext` to invoke a command (bypasses DI, breaks encapsulation).
- `App.xaml` still declares `StartupUri` while `OnStartup` resolves `MainWindow` from the `IHost` container — a double-initialization conflict.
- gRPC endpoint hardcoded (`http://localhost:5001`); `Google.Protobuf` / `Grpc.Net.Client` versions drift between `LibrarySystem.Tools` (3.33.5 / 2.76.0) and the server projects (3.33.2 / 2.71.0).
- Zero ViewModel tests, despite the rest of the solution having unit + integration suites with coverage gates in CI.

This ADR exists to **finalize the scope and architectural boundaries** of that client so it reads as a deliberate, senior-level engineering artifact to reviewers at top engineering companies — not an unfinished sandbox.

### Forces at play

- **Portfolio differentiation.** A generic CRUD desktop app is forgettable. An observability/security inspector wired to a gRPC backend and a WAF is distinctive and tells one coherent architecture story alongside ADR-001.
- **Minimum-viable-but-credible.** The bar is "lean enough to finish, strong enough that a senior reviewer nods" — not maximal feature coverage.
- **Testability is a hiring signal.** Reviewers explicitly look for ViewModels that can be unit-tested without a UI thread.
- **Consistency.** The desktop client should mirror the discipline already shown server-side (Clean Architecture, DI, CQRS, tests in CI).

## Decision

We will **evolve the existing `LibrarySystem.Tools` project into a focused, single-purpose "gRPC Service Graph Inspector & Security Console"**, built on **`CommunityToolkit.Mvvm`** with a **complete service-abstraction layer** between ViewModels and gRPC, staying on **raw WPF + a self-owned theme (no third-party UI library)**.

Concretely, the architecture is fixed as:

1. **Identity & scope** — a developer/operator inspection tool, not an end-user library-management app. It visualizes the live service/domain graph and streams security telemetry. This deliberately leans into the ADR-001 security narrative.

2. **MVVM framework — `CommunityToolkit.Mvvm`.** Source-generated `[ObservableProperty]` / `[RelayCommand]`, `WeakReferenceMessenger` for decoupled inter-ViewModel communication. No Prism, no ReactiveUI (justification below).

3. **Service-abstraction layer (full).** `IGraphDataService` and `ILogTailerService` become real facades over the generated `Library.LibraryClient` / `Security.SecurityClient`. ViewModels depend only on these interfaces, never on generated gRPC types directly. This is what makes ViewModels mockable and unit-testable.

4. **Composition & configuration.** `IHost` + `Microsoft.Extensions.DependencyInjection` owns the object graph; `MainWindow` is resolved from the container; `StartupUri` is removed. The gRPC endpoint moves to `appsettings.json` bound via `IConfiguration`.

5. **Cross-cutting UI concerns as services.** An `INotificationService` (status-bar / toast bound to shared state) replaces every `MessageBox.Show` in ViewModels. `IMessenger` replaces the `NodeDragBehavior → MainWindow.DataContext` coupling.

6. **Testability.** A `LibrarySystem.Tools.UnitTests` (or ViewModel tests inside the existing `LibrarySystem.UnitTests`) project covers ViewModels against mocked services, wired into the same CI coverage gate as the rest of the solution.

7. **Dependency hygiene.** Introduce `Directory.Packages.props` (central package management) to pin `Google.Protobuf`, `Grpc.Net.Client`, and `Grpc.Tools` to a single version across all projects.

## Options Considered

### Identity of the client

#### Option A: Evolve the Inspector (CHOSEN)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low — ~70% already built |
| Portfolio differentiation | High — distinctive, pairs with ADR-001 |
| New surface to build | Small (refactors + service layer + tests) |
| Risk of "generic" perception | Low |

**Pros:** Maximizes distinctiveness per unit of effort; coherent end-to-end story (Clean Arch → gRPC → WAF → live inspector); shows streaming, real-time UI, and graph rendering — uncommon in portfolios.
**Cons:** Narrow feature surface; exercises fewer of the 13 backend RPCs.

#### Option B: Full library-management app
| Dimension | Assessment |
|-----------|------------|
| Complexity | High — 13 RPCs, full CRUD + lending + analytics UI |
| Portfolio differentiation | Low — common pattern |
| New surface to build | Large |
| Risk of "generic" perception | High |

**Pros:** Exercises the whole API surface; demonstrates product breadth.
**Cons:** Large effort for a commodity result; dilutes the security/observability narrative; conflicts with the "minimum-viable" constraint.

#### Option C: Hybrid dashboard (management + observability with navigation)
**Pros:** Showcases a navigation architecture; broadest scope.
**Cons:** Most effort of the three; over-builds relative to the goal; navigation framework becomes a project in itself. Deferred — a future ADR-003 could promote the Inspector into a shell if the management module is ever added.

### MVVM framework

#### CommunityToolkit.Mvvm (CHOSEN)
| Dimension | Assessment |
|-----------|------------|
| Complexity | Low |
| Boilerplate | Minimal (source generators) |
| Team familiarity | High — already in use |
| Testability | High — POCO ViewModels, no UI-thread dependency |

**Pros:** First-party (Microsoft), modern, source-generated, trivially unit-testable, already the project's dependency.
**Cons:** No built-in navigation/region manager (acceptable — the tool is effectively single-view).

#### Prism / ReactiveUI
**Prism cons:** Regions/modularity are overkill for a single-window inspector; heavier learning-curve signal without payoff here.
**ReactiveUI cons:** Rx mental model adds complexity reviewers may read as over-engineering for this scope; steeper to test for a tool this size.

### UI styling

**Raw WPF + self-owned theme (CHOSEN):** keeps the dependency footprint minimal and demonstrates the candidate can hand-craft `Style`/`ControlTemplate`/`DataTemplate` — a stronger WPF signal than importing a theme. A third-party UI library (WPF-UI, MahApps, HandyControl) is explicitly **out of scope** to honor the minimum-viable constraint; it can be revisited if the client ever expands.

## Trade-off Analysis

The central trade-off is **breadth vs. narrative coherence under a minimum-viable budget.** Option B/C would touch more of the API but produce a more generic artifact and break the effort ceiling. Evolving the Inspector concentrates effort on the things reviewers actually grade — clean MVVM separation, testable ViewModels, DI composition, real-time streaming — while preserving a distinctive, ADR-001-aligned story.

The "full service-abstraction layer" decision is the one deliberate place we spend *more* than the strict minimum, because the payoff (unit-testable ViewModels, mockable gRPC, parity with the server-side testing discipline) is exactly the senior-level signal the portfolio needs. Choosing raw WPF over a UI library buys that budget back.

## Scope

### In scope
- Refactor `LibrarySystem.Tools` to remove the `StartupUri`/`IHost` conflict; `MainWindow` resolved from DI.
- Implement `IGraphDataService` (over `Library.LibraryClient`) and `ILogTailerService` (over `Security.SecurityClient`); ViewModels depend only on interfaces.
- Replace in-ViewModel `MessageBox.Show` with `INotificationService`.
- Replace `NodeDragBehavior → MainWindow.DataContext` coupling with `WeakReferenceMessenger`.
- Move the gRPC endpoint to `appsettings.json` via `IConfiguration`.
- Add `Directory.Packages.props` and align gRPC/protobuf versions solution-wide.
- ViewModel unit tests (graph load, node selection, inspector save, WAF stream lifecycle) against mocked services, in CI.
- Feature surface, bounded to the inspector mission: `GetAllBooks` (graph), `UpdateBook` (inspector edit), `WatchWafLogs` (security console), and optionally `GetBookAvailability` to enrich the inspector panel.
- Remove dead scaffolding (`GraphViewModel`) and finish `NodeType`-based visual differentiation (Author vs Book) via XAML `DataTemplate`/triggers.

### Out of scope (explicitly)
- The remaining backend RPCs as UI flows (`CreateBook`, `DeleteBook`, `CreateLending`, `ReturnBook`, `GetMostBorrowedBooks`, `GetTopBorrowers`, `GetUserLendingHistory`, `GetAlsoBorrowedBooks`, `EstimateReadingRate`) — candidates for a future ADR-003.
- Third-party UI/theme libraries.
- Multi-window shells, region managers, or a navigation router.
- gRPC connection resilience (Polly retry policies, auto-reconnect of the WAF stream) — noted as a known limitation; a possible follow-up.
- Localization / i18n (current code mixes PT comments + EN code; standardize on English, but full localization is out).
- Authentication / authorization in the client.

## Consequences

### Positive
- **Testable by construction:** ViewModels depend on interfaces, so the client joins the existing CI coverage gate — a concrete hiring signal.
- **Clean composition:** a single DI-owned object graph, configuration externalized, no view-shell coupling in ViewModels.
- **Coherent portfolio narrative:** Clean Architecture → gRPC → WAF (ADR-001) → live inspector, told end to end.
- **Lean dependency surface:** no UI-framework lock-in; hand-crafted WPF demonstrates depth.
- **Version hygiene:** central package management removes the protobuf/gRPC drift.

### Negative / Risks
- **Narrow feature surface:** only ~4 of 13 RPCs are exercised in the UI; mitigated by the explicit, documented scope and the ADR-003 path.
- **No resilience layer:** a dropped gRPC channel or interrupted WAF stream is surfaced but not auto-recovered; acceptable for a local inspector, documented as a limitation.
- **Hand-rolled theming cost:** raw WPF styling is more verbose than importing a library; accepted as a deliberate signal.
- **Known backend gaps the UI must not over-promise:** `GetMostBorrowedBooks` and `GetAlsoBorrowedBooks` handlers currently return hardcoded `0` counts — these analytics are out of scope precisely so the client never renders misleading data.

## Action Items
1. [ ] Remove `StartupUri` from `App.xaml`; confirm `MainWindow` is resolved via `IHost`.
2. [ ] Implement `IGraphDataService` + `ILogTailerService`; refactor `MainViewModel`, `InspectorViewModel`, `WafLogViewModel` to consume them.
3. [ ] Add `INotificationService`; remove all `MessageBox.Show` from ViewModels.
4. [ ] Introduce `WeakReferenceMessenger`; refactor `NodeDragBehavior` selection to publish a message.
5. [ ] Externalize the gRPC endpoint to `appsettings.json` + `IConfiguration`.
6. [ ] Add `Directory.Packages.props`; pin `Google.Protobuf`, `Grpc.Net.Client`, `Grpc.Tools` solution-wide.
7. [ ] Add ViewModel unit tests with mocked services; wire into CI coverage.
8. [ ] Implement `NodeType` visual differentiation in XAML; delete unused `GraphViewModel`.
9. [ ] Standardize code/comment language to English across `LibrarySystem.Tools`.
10. [ ] Update the solution README to describe the Inspector and link this ADR.
