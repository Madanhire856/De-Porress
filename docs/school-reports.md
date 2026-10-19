# School reporting

Open **Reports → Reports** to see reports available to the signed-in account. Reporting is read-only and does not replace or change cash-up, payment, notice, trip or other capture workflows. Report cards and exam results are intentionally excluded.

## Available reports

| Group | Reports |
| --- | --- |
| Learners | Learner register, attendance register, prefect appointments, prefect nominations |
| Staffing | Staff register, class teacher assignments, subject assignments |
| Finance | Current fee balances, collections and reversals, fee structures |
| School activities | Trip plans, Mass timetable, house assignments, sports coaching assignments |
| Administration | SDC membership, notice publication, user access |

Each report has relevant date, grade, class, term or status filters. Attendance and collections default to the last 30 calendar days inclusive. Other date filters are optional; the report description states which date is used. Notice and nomination creation dates, assignment timestamps and generated timestamps display Zimbabwe time. Payment dates, trip dates and Mass dates follow their existing school-time conventions.

Tables paginate at 25, 50 or 100 rows. Totals cover every matching row, rather than only the visible page. CSV downloads and print views include the entire matching dataset. Print opens a separate full report suitable for the browser's Print or Save as PDF function. Printed reports repeat headings and identify the school, selected filters, generation time and user. CSV files contain column headings and data rows for spreadsheet use.

Reports exceeding 25,000 rows require narrower filters; neither exports nor totals silently truncate results. Private report responses use `no-store`. CSV export escapes embedded quotes/newlines and neutralizes spreadsheet formulas in text while preserving ordinary negative numeric amounts.

## Permissions and data limits

- The catalog, normal report, CSV handler and print handler check the same report permissions.
- Learner reports require View Learners. Teacher learner data is restricted to classes currently assigned to that teacher in the current academic year. Changing a filter does not expand this scope.
- Staffing and activity reports require the corresponding existing view rights. Class and subject assignment reports require both their module view right and View Staff. Teacher class/subject assignment reports are restricted to their own assignments.
- Finance reports require **View Fee Reports** and **View Fee Balances** in the Administration or Bursar workspace. Without **View All Bursars Cash-Up**, collection records are restricted to the signed-in recorder, including for administrators.
- Prefect reports also require View Learners. Teachers see their own nominations within their current assigned classes.
- Trip report visibility matches Trip management, including hiding drafts from teachers and bursars. Mass reports require View Timetable.
- SDC, notice-publication and user-access reports are administrative reports. User access additionally requires View Users. Account authentication secrets are not selected or exported.

No rights bits are added or repurposed. Existing module permissions remain authoritative.

## Reading the figures

- Fee balances are current ledger balances, not historical snapshots. Outstanding debt and credits are displayed separately for each recorded currency. Grade and class labels refer to current placement.
- Collections use payment dates. Reversals reduce net amounts. Original currencies remain separate. Base totals use stored base amounts, or an original amount only when its recorded currency is the configured base currency. Missing foreign-exchange values are excluded and explicitly flagged. A single configured base currency is required for a base total; no default currency is invented.
- Fee structures describe configured charges, not collections or revenue forecasts.
- Attendance counts describe actual records, not attendance percentages or unrecorded absences.
- Trip costs are planned amounts per student; the app has no trip participant or payment records to summarize yet.
- Mass rosters describe assignments, not attendance. Notice publication is not evidence of email delivery or readership.

## Deployment and verification

Reporting adds no database schema changes. The earlier `SchoolTripManagement` migration must be applied before deploying a release that includes Trip management and its report. Production startup migrations remain disabled.

`dotnet test tests/SMS.Web.Tests/SMS.Web.Tests.csproj`

Tests cover all 17 report types with empty data, access checks on all handlers, teacher and bursar limits, inclusive dates, separate currency totals, reversals, missing base amounts, credits versus debt, complete exports/print data across pages, filter-link preservation and CSV formula protection. Local browser checks use fictional in-memory data and verify mobile filters, action visibility, and report rendering. Local HTTP checks verify every report route, CSV download headers/filtered rows and full print output.
