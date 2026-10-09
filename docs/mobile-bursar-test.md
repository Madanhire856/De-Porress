# Bursar mobile workflow test

Date: 8 October 2026 (Africa/Harare)

Status: **Authenticated mobile-layout checks completed; payment-entry and saving checks remain blocked by missing terms and ledgers.** No payment was submitted and no production data was modified. Desktop viewport emulation does not verify a physical phone's keyboard, touch behaviour or browser-specific features.

## Completed checks

| Check | Result | Evidence |
| --- | --- | --- |
| Local application is reachable | Passed | `http://localhost:5194` redirects to `https://localhost:7246` and displays the school public page |
| Staff portal starts authentication | Passed | Staff portal opens the Microsoft sign-in page |
| Authenticated bursar session | Passed | User signed in; Bursar dashboard and finance navigation displayed |
| Payment rights check | Source review only | RecordPayment handler requires RecordPayments |
| Basic payment validation | Source review only | Required fields, positive amount and future-date rejection are present |
| Successful payment feedback | Source review only | Successful submission redirects to the ledger with PaymentSuccess feedback |

## Source-review findings

1. **Duplicate submissions are not prevented.** The payment form has no submit lock, and RecordPaymentRequest/PaymentService.RecordAsync has no request idempotency token. Each valid request allocates a new receipt and creates a payment. Fast repeated submission or retrying after an uncertain network result could create duplicate payments. A unique receipt number does not deduplicate the underlying payment request. This has not been reproduced with a live payment.
2. **No review-before-save step.** The Record Payment button posts the form immediately. A dedicated review step showing student, amount, currency, method and date is absent.
3. **Payment date needs touch testing.** The date input uses a jQuery UI calendar and blocks normal keyboard entry. Verify opening, month/year changes, date selection and dismissal on a phone; native date selection is not currently used.

Relevant source files:

- `SMS.Web/Areas/Finance/Pages/Fees/RecordPayment.cshtml`
- `SMS.Web/Areas/Finance/Pages/Fees/RecordPayment.cshtml.cs`
- `SMS.Web/Services/PaymentService.cs`
- `SMS.Web/Areas/Finance/Pages/Fees/Ledger.cshtml`

## Authenticated browser results (8 October 2026)

Tested dashboard and balances at 320×568, 390×844, 430×932 and 844×390. The viewport override was reset after testing.

| Check | Result |
| --- | --- |
| Bursar dashboard loads after sign-in | Passed |
| Sidebar opens, Finance expands, balances link navigates | Passed |
| Tapping the overlay closes the sidebar | Passed |
| Dashboard and balances have no page-content horizontal overflow | Passed at all four sizes |
| Wide tables remain in horizontal scroll containers | Passed |
| Changing the balance status filter | Passed; selection and GET query updated |
| Cash-up page opens on a 390px viewport | Passed |
| Download PDF from cash-up | Passed; an empty-date report was downloaded |
| Payment entry, currency conversion, validation, saving and receipt outcome | Not exercised; there are no terms or student ledgers |

Additional observed findings:

- The Bank link on balances resolves back to the balances page instead of opening a bank page.
- Balance and cash-up filter controls compute to 14px text and approximately 38.6px height at 390px width. Search computes to 16px. Filter controls should be made more comfortable for touch use.
- The term selector has zero options and the balances table contains no ledger rows. Dashboard empty states correctly indicate no current term and no receipts.

## Remaining workflow checks

Test portrait viewports of 320×568, 390×844 and 430×932, and landscape 844×390. Verify actual phone behaviour separately.

| Workflow | Checks |
| --- | --- |
| Dashboard | Role-specific content, readable totals, table scrolling within its container, no page-wide horizontal overflow |
| Sidebar | Open/close from the mobile menu, correct finance links, overlay dismissal, no obstruction of controls |
| Balances | Term/grade/status filters, student search, readable rows, access to student ledger |
| Payment entry | Student identity, amount/currency/method fields, date picker, rate loading, conversion preview, manual override reason, keyboard focus and scrolling |
| Validation | Blank required fields, zero/negative amount, future dates and network/rate failures; confirm no records are created |
| Submission | Review/confirmation and duplicate-submission safeguards require improvement; use an explicitly disposable test ledger in an isolated test environment for saving |
| Receipt/ledger | Success feedback, receipt identifier, original/base amounts, updated balance and visibility on mobile |

Do not submit a live payment for testing. Saving, duplicate requests and retry scenarios must use an isolated test environment with disposable test data. Completing the remaining payment checks requires a configured term and disposable student ledger in an isolated test environment. No authentication bypass has been introduced.

## Fixes implemented after testing

- Removed the Bank link because no Bank page is implemented. The currently running app must be rebuilt/restarted to pick up this Razor change.
- Enlarged balances and cash-up filters to 16px text and at least 44px height. Authenticated browser measurements confirmed 16px/44px at 390px and 320px widths, with no content-wide horizontal overflow.
- Replaced the payment form's desktop jQuery calendar with a native date input and a school-time maximum date.
- Added a server-rendered review page before recording. It shows student, payment date, amount/currency, method, exchange rate, converted amount and reference. Editing preserves entry values. Only confirmation invokes the save operation.
- Confirmation uses data-protected, user-bound payment details with a 15-minute expiry. The submission identifier is protected and bound to the user and ledger.
- Repeated confirmation uses the same persisted Payment.Id. Payment recording acquires a SQL Server transaction-owned application lock for the ledger, returns the existing receipt for a matching submission, rejects mismatched reuse, and commits the receipt and balance update together. This requires no schema migration and protects retries across requests/app instances.
- Added client-side submit locking. Invalid or cancelled submissions remain editable; handler-specific edit actions are preserved.

Validation: `node tests/payment-submit.checks.js` passed checks for valid submission, repeats, HTML and jQuery validation, cancelled submission and editing. Server compilation is checked separately. Database concurrency/retry behaviour and the full review/save/receipt flow still require an isolated environment with a disposable ledger; no live payment was created during this work.
