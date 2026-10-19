# Feature permissions

Configure the new categories in **Administration → User groups → Add/Edit**. Group details display the saved rights and include them in the totals and category counts.

There are 29 new permissions:

| Category | Permissions |
| --- | --- |
| Trips | View, create, edit, view drafts |
| Notices | View management, create, edit, publish/withdraw, read staff notices |
| SDC members | View, create, edit |
| SDC positions | View, create, edit |
| SDC committee terms | View, create, edit |
| School gallery | View management, add, edit, publish/unpublish, archive/restore |
| Mass timetable | View, create, edit Masses and ministries |
| Reports | View, print/save PDF, export CSV |

## Enforcement

These permissions control navigation, buttons, GET handlers, POST handlers and report download/print handlers. A write permission also requires its module's view permission. SDC positions and committee terms can be viewed without showing their create form or edit links.

Existing workspace boundaries remain: Trips can be read by administration, teachers and bursars; management remains in the administration workspace. Notice management, SDC and gallery management remain in administration. Teachers can read Masses but cannot create/edit them. **ADMIN retains its existing full-access override.** HEAD and DEPUTY_HEAD accounts use their group's permissions. A group grants permissions within a user's role; assigning a group does not change that role.

Trip create/edit permission includes reading drafts needed for that workflow. View draft trips separately allows an administrator-workspace reader to see drafts without creating or editing.

Notice audience selection is unchanged. Creating/editing a draft does not grant publishing. Altering an already-published notice, withdrawing it, or saving it as a draft requires publish permission. Publication status participates in optimistic concurrency checks so a draft editor cannot overwrite a notice published by another request.

Gallery editors without publish permission can edit drafts; a publisher must first unpublish a live photo before they can edit it. Archiving a published photo requires archive and publish permissions. Restore returns an archived photo to draft status. Checks also run inside the gallery store's atomic update callback.

Reports require View reports plus the underlying data rights. Finance reports still require View fee reports and View fee balances. Trips, Masses, SDC and notice reports require their dedicated view permission. Teacher class restrictions, draft visibility and the separate View all bursars' cash-up permission still apply. Full-report print/PDF and CSV endpoints independently require their respective permission. Printing visible browser content with the browser's own controls cannot be prohibited by application permissions. Existing module-specific exports are unchanged.

## Compatibility and migration

`AccessRights` remains unchanged. Its persisted 64-bit mask has no available bits. A separate `FeatureRights` mask is stored in nullable `UserGroup.FeatureRightsId`.

Apply migration `20261018193515_FeatureAccessRights` before deploying this app. It adds **one nullable bigint column** and does not update existing assignments or records. No startup migration is enabled by this change. Use a database backup and the migration account for deployment.

- Existing rows receive NULL. NULL reproduces previous access rules, including mapping System settings to gallery permissions and the old Timetable rights to the corresponding Mass permissions. Existing role and module restrictions still apply.
- Editing an existing NULL group preselects those effective permissions and shows an explanatory message. Saving stores the explicit selections. An explicit zero means no new permissions and never falls back to legacy access.
- New groups start at zero; choose their permissions explicitly.
- Active accounts without a group retain their previous role-scoped access. Assign a configured group to control their new feature permissions.
- Existing ADMIN override, inactive-account rejection, old rights and separate prefect nomination permission remain intact.

The generated migration only adds the column. Announcement publication concurrency metadata requires no SQL schema change. Rolling back the application may leave the new column in place; do not drop assignments as part of an app rollback. Older application versions do not enforce these new permissions and revert to their previous access rules, even if the stored selections remain.

## Verification

`dotnet test tests/SMS.Web.Tests/SMS.Web.Tests.csproj`

148 tests pass, including 48 permission tests covering catalogue uniqueness, role boundaries, real account/group resolution, NULL versus explicit zero, signed legacy rights, group round trips and invalid submissions, read-only and create/edit boundaries, publication races, report download guards, and migration shape/model consistency. Local browser checks use fictional data and never modify production groups.
