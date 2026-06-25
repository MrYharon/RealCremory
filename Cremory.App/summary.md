## Progress
### Done
- **💄 Full UI redesign** — warm bakery palette (browns/creams), merged redundant pages, improved layouts
- **🧭 Navigation restructured** — 9→7 flyout items (removed dead pages, merged Finances→Analytics, integrated OrderArchives→Orders)
- **🐘 Database & Cloud** — switched Oracle→PostgreSQL (Npgsql), deployed API to Azure App Service + PostgreSQL Flexible Server, auto-migration on startup
- **📱 Android APK** — Release signed APK at `C:\Users\hughd\Desktop\Cremory-APK\`
- **📊 AnalyticsPage merged** — revenue cards (Today/Week/Month) + profit margin + transactions + popular items + weekly chart + low stock + order sources
- **📦 InventoryPage enhanced** — absorbed sample data fallback (12 real bakery ingredients)
- **📋 OrdersPage enhanced** — Completed filter button, Archive toggle with date/status pickers, search debounce
- **🏠 MainPage fixed** — RefreshView wrapping, live 30s status bar timer
- **➕ WalkInOrderForm improved** — product quick-select chips auto-fill items + estimate total
- **🧑‍🍳 RecipeForm improved** — searchable ingredient picker
- **🎨 Colors/Styles refined** — light accent variants (`SuccessLight`, `DangerLight`, `WarningLight`, `Surface`, `InfoLight`), fixed NavigationPage invisible text contrast
- **🧹 MauiProgram.cs cleaned** — removed dead DI registrations
- **⚡ SignalR made persistent** — connection lives across page navigation; MainPage + OrdersPage get live updates
- **🔁 Shared OrderCardView** — `Controls/OrderCardView.xaml` eliminates duplicate DataTemplates; handles Cancel/Action internally via `App.ApiService`
- **🧮 Shared order state machine** — `OrderSummary.NextStatus()` for Pending→Creating→Completed transitions
- **🔍 Server-side filtering** — Orders API accepts `?status=`, `?search=`, `?dateFrom=`, `?dateTo=`, `?page=`, `?pageSize=`; client updated accordingly
- **🛡️ ApiService error handling + cache** — `HttpGetAsync<T>` helper checks connectivity, retries once after 1s on `HttpRequestException`, falls back to `Preferences`-backed JSON cache; all GET endpoints use it
- **🔄 Pull-to-refresh** — `RefreshView` added to MenuPage and AnalyticsPage with `OnRefreshing` handlers

### In Progress
- (none)

### Blocked
- (none)

## Key Decisions
- **SignalR always-on** — connection starts on first page visit and never stops; pages subscribe/unsubscribe independently
- **OrderCardView with static ApiService** — accesses via `App.ApiService` to avoid DI through DataTemplate chains
- **ApiService cache** — uses `Preferences` key-value store with `api_cache_` prefix; no dependency on file I/O
- **Retry policy** — single retry after 1s for `HttpRequestException`, then cache fallback; immediate cache fallback for `TaskCanceledException` (timeout) or no connectivity
- **Azure for Students** — $100 free credit, ~5 months at $20/mo
- **PostgreSQL over Supabase** — better connection from Azure App Service

## Next Steps
- Add `x:DataType` compiled bindings to all XAML pages to eliminate XamlC warnings
- Fix `Application.MainPage` deprecation warnings (migrate to `CreateWindow` override)
- Add MVVM pattern (BaseViewModel) for consistent error handling
- Server-side pagination for orders
- Push notification integration
