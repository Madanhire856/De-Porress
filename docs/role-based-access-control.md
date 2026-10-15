# Role-based access control

This guide describes the current implementation for administrators, bursars and teachers. The database enum spells the bursar role `BURSER`; the interface displays “Bursar”.

## How access is decided

Roles select the user's workspace. User-group rights select the modules and operations available within that workspace.

For a navigation item to appear, all three conditions must be met:

1. The user is authenticated.
2. The module applies to the user's resolved workspace.
3. The user has the module's required right.

A right does not make an unrelated module applicable. A teacher with finance rights still cannot access the finance workspace. A bursar with learner or sports rights still does not receive teaching modules.

## Sign-in and workspace selection

All staff use the same Microsoft sign-in through `/Identity/Account/StaffLogin`. Users do not select a role at login. A local return URL is accepted; invalid or external return URLs fall back to the home page.

After authentication, the provisioning middleware locates or creates the local user record using the signed-in email. The first user in an empty database becomes `ADMIN`. Later auto-provisioned users receive `STAFF` with no user group; an administrator must assign their rights.

`CurrentUserService` reads `User.RoleId` and `User.GroupId`, then loads the rights mask from `UserGroup.RightsId`. Rights are cached for the request. Updated assignments normally take effect on the next request.

| Assigned role | Resolved workspace | Rights behaviour |
| --- | --- | --- |
| `ADMIN` | Administration | Existing administrator override grants all rights |
| `HEAD`, `DEPUTY_HEAD` | Administration | Requires assigned user-group rights; no automatic administrator override |
| `BURSER` | Bursar | Requires assigned user-group rights |
| `CLASS_TEACHER`, `STAFF` | Teacher | Requires assigned user-group rights |
| `PARENT`, missing or unsupported role | Unassigned | No staff workspace |

Roles are stored as flags. If several are present, precedence is Administration, then Bursar, then Teacher. The application resolves one primary workspace; it does not combine all workspace menus.

## Account configuration

Perform these steps from an administrator account:

1. Open **Administration → User groups**. Create or edit a group and select the rights needed for the person's work.
2. Open **Administration → Users**. Create or edit the user with the email they use for Microsoft sign-in.
3. Set **Role** and **User Group**, then save.
4. Dashboard access is automatic for a supported staff role. No dashboard right is required.
5. For a teacher, open **Academics → Staff**, edit the staff record, and select **Linked User Account**.
6. Open **Academics → Classes** and assign that staff member as the class teacher.
7. Verify the dashboard, visible menu items and direct-page access using that account.

User groups alone do not choose the workspace. Setting the role alone does not grant rights, except for the existing `ADMIN` override. Changing an administrator's group cannot restrict that override.

### Suggested starting rights

These are configuration examples, not automatically created groups.

| Account | Starting rights | Optional additions |
| --- | --- | --- |
| Teacher | `ViewClasses`, `ViewLearners`, `ViewSubjects`, `ViewTerms` | `ViewPrefects`, `NominatePrefects`, `ViewSports`, `ViewHouses`; grant create/edit rights only when required |
| Bursar | `ViewFeeBalances` | `RecordPayments`, `ReversePayments`, `GenerateInvoices`, `ViewCurrencies`, `ViewTerms` |
| Head or deputy head | The view rights required for oversight | Appropriate create/edit or finance rights for assigned duties |
| Administrator | `ADMIN` role | Rights are already granted by the administrator override |

Viewing balances does not grant permission to record or reverse payments. The bursar dashboard loads automatically for the Bursar workspace, even with no group rights. Finance page links and operations still require their corresponding rights; `ViewFeeBalances` controls access to finance pages, not the home dashboard.

## Module applicability

Every listed module also requires its corresponding right.

| Module | Administration | Bursar | Teacher | Required right |
| --- | --- | --- | --- | --- |
| Students | Yes | No | Yes | `ViewLearners` |
| Staff | Yes | No | No | `ViewStaff` |
| Prefects | Yes | No | Yes | `ViewPrefects` |
| Nominate prefects | Yes | No | Yes | `NominatePrefects` |
| Classes | Yes | No | Yes | `ViewClasses` |
| Subjects | Yes | No | Yes | `ViewSubjects` |
| Grades | Yes | No | No | `ViewGrades` |
| Terms | Yes | Yes | Yes | `ViewTerms` |
| House assignments | Yes | No | Yes | `ViewHouses` |
| Sports activities | Yes | No | Yes | `ViewSports` |
| Fees overview, student balances, cash-up, fee structures | Yes | Yes | No | `ViewFeeBalances` |
| Open fee term | Yes | Yes | No | `GenerateInvoices` |
| Currencies | Yes | Yes | No | `ViewCurrencies` |
| Users | Yes | No | No | `ViewUsers` |
| User groups | Yes | No | No | `ViewUserGroups` |
| Sport type setup | Yes | No | No | `ViewSports` |
| House setup | Yes | No | No | `ViewHouses` |
| Villages | Yes | No | No | `ViewVillages` |
| Audit logs | Yes | No | No | `ViewAuditLogs` |

Empty sidebar groups are hidden. Links to unimplemented timetable, liturgy and sponsorship pages are not in the catalogue.

## Dashboard behaviour

- **Administration:** school statistics, enrolment charts, readiness and quick actions. Queries and links are gated by the relevant rights. Heads and deputies use this view with their assigned rights.
- **Bursar:** collections, term balances, account settlement, receipts and finance actions. Recording payments requires `RecordPayments`. Collection queries are scoped to the current user's payments unless `ViewAllBursarsCashUp` is granted.
- **Teacher:** assigned classes, the term calendar when permitted, and teaching tools from the navigation catalogue. A class appears when its class teacher's `Staff.UserId` matches the current user. Student counts additionally require `ViewLearners`.
- **No dashboard access:** an authenticated user without a supported workspace sees an explanatory welcome panel. Supported staff roles can open their dashboard without a group or dashboard permission.

The “My classes” dashboard is scoped to the teacher's assignments. That does not impose class-level restrictions on the general Students or Classes directories: those pages retain their existing data access behaviour.

## Server-side enforcement

Hiding links is not the only control. `WorkspacePageFilter` is registered globally for Razor Pages and currently protects the `Academics`, `Config`, `Sports` and `Finance` areas.

For those areas:

1. Anonymous requests receive an authentication challenge.
2. Pages outside the resolved workspace receive a forbidden result.
3. Read pages named `Index`, `Details`, `Board`, `Balances`, `Ledger`, `CashUp`, `FeesStructures` or `FeesStructureDetails` must pass the catalogue's view-right check. Details pages can inherit the right from their folder's index feature.
4. Existing create, edit, assignment and payment handlers remain responsible for their operation-specific checks.

The workspace filter checks relevance for all handlers in the protected areas, but it is not a universal operation-right filter. Pages in other areas are outside this filter's scope and need their own authentication and authorization controls. Dashboard access is role-based; module and operation access remains rights-based. The retired dashboard bit (bit 0) is ignored and must not be reused.

## Known limitations

- `NominatePrefects` and `ViewAllBursarsCashUp` both use `1L << 59` in `AccessRights`. They cannot be assigned independently using the current mask. Workspace separation prevents this collision from exposing finance to teachers, but administration and bursar permissions can still be affected. Correcting it requires a rights-storage change and migration of existing group assignments; do not simply renumber a persisted right.
- The workspace layer does not implement inactive-account blocking. The current local-user lookup and provisioning flow do not enforce `User.IsActive`; do not rely on workspace checks for account suspension.
- Live Microsoft authentication and database-backed dashboard behaviour require verification with configured accounts. Automated policy checks do not replace that verification.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| No dashboard | Supported staff role; no dashboard right is required |
| No sidebar modules | User group assignment, relevant view rights and role applicability |
| Teacher sees “No class assigned yet” | Staff record's Linked User Account and class teacher assignment |
| Class has no student count | `ViewLearners` right |
| Bursar cannot see finance content | `ViewFeeBalances` right |
| Payment action is unavailable | `RecordPayments` or `ReversePayments`, as appropriate |
| Head/deputy sees fewer tools than administrator | Assigned rights; heads/deputies do not receive the `ADMIN` override |
| Finance is forbidden for a teacher with finance rights | Expected role separation; changing rights does not change workspace |

## Maintaining the implementation

| File | Responsibility |
| --- | --- |
| `SMS.Lib/UserRole.cs` | Role flags |
| `SMS.Lib/AccessRights.cs` | Persisted rights flags |
| `SMS.Web/Services/CurrentUserService.cs` | Local user, group rights and administrator override |
| `SMS.Web/Services/WorkspaceAccess.cs` | Workspace resolution and feature catalogue |
| `SMS.Web/Services/WorkspacePageFilter.cs` | Direct-page relevance and read-right checks |
| `SMS.Web/Program.cs` | Service and global filter registration |
| `SMS.Web/Pages/Index.cshtml.cs` | Dashboard data and rights checks |
| `SMS.Web/Pages/Index.cshtml` | Dashboard selection |
| `SMS.Web/Pages/Shared/Sidebars/_staff.cshtml` | Rights-filtered navigation |
| `SMS.Web/Pages/Shared/Dashboards/` | Administration, bursar, teacher and no-access views |
| `tests/WorkspaceAccess.Checks/` | Policy and page-filter regression checks |

When adding a module, register its real route, applicable workspaces and view right in `WorkspaceAccess.Features`. Add operation-right checks to its handlers and ensure its area is covered by server-side authorization. Review dashboard links and data queries against the same rules, then add regression cases for both permitted and denied roles.

Run the checks from the repository root:

```powershell
dotnet run --project tests/WorkspaceAccess.Checks/WorkspaceAccess.Checks.csproj
```

The current suite contains 40 checks covering role precedence, missing rights, cross-role access with broad rights, shared modules, anonymous requests and forbidden direct URLs.

## Dashboard reporting tables

Administration (including Head and Deputy Head) receives a school-wide fees summary: net collections over the last seven calendar days, current-term outstanding balances, settled accounts and overdue accounts. This dashboard summary is available by workspace role; links to finance pages still require the corresponding rights.

The collections table covers today and the preceding six calendar days using payment dates. It includes every day, including days with zero activity. Amounts use recorded base-currency values, with original amounts used only when the recorded currency matches the base currency. Entries without a usable base-currency amount are excluded from money totals and flagged in the dashboard. Reversals reduce net collections.

Administration reports cover all bursars. Bursar reports cover the current user unless View All Bursars Cash-Up is granted. Recent receipts display the latest ten entries within the same window, with original currency labels and payment/reversal types.

Recent enrolment tables use enrolment dates, show up to ten rows and require View Learners. Administration sees school-wide enrolments; teachers see only students in classes assigned to their linked staff account. These tables contain school operational records, not system audit logs.
