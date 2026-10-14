# Access-rights review

Prefects are student appointment records and SDC members are committee records. Neither creates a user, assigns a role, grants permissions or enables sign-in. There are no Prefect or SDCMember system roles. Creator and reviewer references identify the staff users who maintain records, not accounts for committee members or students.

Operational access belongs to approved staff users:

- Admin has the existing system-administrator override, provided the account is active.
- Head and Deputy Head use the administration workspace with assigned group rights.
- Teacher/Staff users use their relevant academic tools and assigned rights.
- Bursars use finance tools and assigned rights; the dashboard itself remains available without a dashboard permission.
- Prefect appointment creation/editing and nomination review are administration-only and require their corresponding rights. Teachers can nominate only when the separate Nominate Prefects permission is enabled, and only from their current assigned classes.
- SDC management is administration-only. Public notices are anonymous-readable; staff notices require an approved staff role. Draft notices remain administrator-only.

## Corrected conflict

Nominate Prefects and View All Bursars' Cash-Up previously shared bit 59. Nomination is now stored in **UserGroup.CanNominatePrefects**, and remains an independent checkbox under Prefects in the group editor. Bit 59 belongs only to cash-up. All remaining bitmask permissions have distinct values; retired dashboard bit 0 is not reused.

The SeparateNominationPermission migration copies legacy nomination access to groups currently assigned to academic staff roles (Admin, Head, Deputy Head, Teacher or Staff), leaving bursar-only and unassigned groups disabled. Cash-up masks are retained. Mixed groups are still restricted by each user's workspace. Review the separate nomination checkbox after migration when a group's historical intent was ambiguous.

## Additional corrections

Inactive accounts no longer return roles, group rights or administrator override privileges. New Microsoft sign-ins are recorded inactive and without a staff role pending administrator approval; the existing first-user administrator bootstrap is retained. Existing active users are not automatically reassigned. Activate an approved user and select a staff role/group under Users.

The user-group editor now rejects undefined permission values and cannot edit another group's rights by changing a hidden ID on a create form. Group Details and checkbox counts include the separate nomination permission.

Only an existing system administrator can assign the ADMIN role or edit system-administrator accounts. User editing rights alone cannot elevate a Head or Deputy Head to system administrator. Active accounts must have a defined role and an existing group when a group is selected.

No existing prefect or SDC member records are converted to users. No existing roles, active accounts or group settings are directly changed by this source update. Rebuild and restart to apply the schema migration and new checks.
