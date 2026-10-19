# Trips and mobile notice creation

## Notices

The staff notice reading page now includes **Create notice** and **Manage notices** for administrators, including on mobile. Both School notices and Staff notices appear together under the Notices menu. The existing creation form, audience choices, publishing/draft behavior and public/staff filtering are preserved.

Regression tests cover both audience choices on creation and editing, draft withdrawal, administrator action visibility, and exclusion of staff-only notices from the public website.

## Trips

Open **Academics → Trips**. Administrators create and edit trips. Signed-in teachers and bursars can read planned, completed and cancelled trips; drafts remain visible only to administrators. Trips are not published to the public website.

A trip captures:

- Name, main destination, purpose and ordered route/stops.
- Leader selected from active staff, and one or more school grades.
- Departure and return date/time in Zimbabwe time, with calculated calendar days and nights.
- Amount per student, configured currency and payment deadline. Free trips may omit a deadline.
- Meeting point, transport, accommodation, student capacity, emergency contact, parental-consent requirement and other notes.
- Draft, Planned, Completed or Cancelled status.

The list supports search, grade/status filters and pagination. Editing retains the creator and records the last editor/time. Optimistic concurrency prevents saving over another administrator's changes. Cancel trips using their status; there is no destructive delete action.

Trip costs are planning records. This change does not create student charges, payment records or financial receipts. Student enrolment, payment collection and consent submissions are separate workflows and are not implemented by this change.

## Database and deployment

The `SchoolTripManagement` EF migration adds only `SchoolTrip` and `SchoolTripGrade`, with foreign keys to existing Staff, Grade, Currency and User records. It adds date, cost, capacity, status and payment-deadline constraints and does not alter existing tables or notice records.

Take a database backup and apply this migration with the normal migration account before deploying the app that exposes Trips. Production startup migrations remain disabled. Rolling the app back can leave the new tables in place; do not drop trip data to roll back application code.

Migration command (with the production connection supplied through the approved deployment environment):

```powershell
dotnet ef database update SchoolTripManagement --project SMS.Data --startup-project SMS.Web
```

An idempotent SQL script can be generated from `SeparateNominationPermission` to `SchoolTripManagement` for review. The design-time factory supports offline migration scaffolding and reads `ConnectionStrings__SMSDb` when an explicit database connection is needed.

## Verification

`dotnet test tests/SMS.Web.Tests/SMS.Web.Tests.csproj`

The trip tests cover saving multiple grades, durations, free/paid costs, date and deadline validation, staff/grade/currency validation, draft access, role boundaries, editing and stale-update protection. Browser checks use a separate local preview host with fictional in-memory data. The preview host is under ignored `artifacts/` and is excluded from the production image.
