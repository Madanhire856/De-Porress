# Internal table and index layout

Use `Areas/Academics/Pages/Students/Index.cshtml` as the reference for internal record lists:

1. Keep page creation actions in `actionbar`, guarded by their existing permissions.
2. Put the record total, filters, table, and existing pagination in one `table-card`.
3. Use the shared `Pages/Shared/Tables/_TableCount.cshtml` partial for new total headers. Existing Students-style total headers remain valid.
4. Use `table-toolbar`, `toolbar-filters`, `filter-wrapper`, `filter-dropdown`, `search-wrapper` and `search-input` for list filters.
5. All tables use `data-table table-compact`. A `table-responsive` wrapper keeps wide tables inside their card. Mobile controls and pagination stay outside the scrolling rows.
6. Use `reg-link` for record links, `status-badge` for status, `empty-state` for no results, `table-copy` for wrapping text and `table-actions` for multiple controls. Gallery management uses `table-photo` thumbnails.
7. Paged lists use `Pages/Shared/Pagination/_pagination.cshtml`. It preserves query filters and repeated query values, adds accessible previous/next labels, and limits the visible page-number range on large lists. Do not restore the removed “Showing … of …” text. Reports supply their existing URL builder to preserve effective dates and repeated invariant date hints.

The updates standardize Trips, Masses, SDC, notices, nominations, teaching assignments, sports/house assignments, gallery administration, report lists/results and remaining table markup, including financial and dashboard tables. Existing setup lists already use the reference structure. Public photo galleries and public reading pages keep their presentation.

Existing list page sizes, permissions, assignment/review/publication handlers, financial calculations, and export scope are retained. Reports still print and export all matching rows, independently of on-screen pagination. Lists that previously loaded all their records remain unpaged.

Verification: Release build and all 148 existing tests; local browser checks with fictional data cover filter-preserving paging, report date links, index actions, search, empty states, mobile overflow and desktop presentation. No schema changes are required.
