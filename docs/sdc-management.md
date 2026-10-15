# SDC management

The SDC section manages School Development Committee **members, positions and committee terms**. It is available to signed-in Admin, Head and Deputy Head accounts only. Other staff roles and the public cannot access these records or direct page URLs. No existing rights bits or user groups are changed; SDC access uses the administration role.

SDC members are committee records only. Their contact details do not create login accounts, link them to users or grant system access. CreatorId identifies the administrator who recorded the membership, not a login for the member.

1. Open **SDC → Positions** and define the positions your committee uses.
2. Open **SDC → Committee terms** and enter each committee period's name, start date and end date.
3. Open **SDC → Members** and add members against a term and position. Telephone and email are optional.

Filters sit on the left and live search on the right. Member lists are paginated and can show currently serving members, inactive memberships or records for a selected term. Current service requires an active membership and active committee term whose dates include today in Zimbabwe time.

Archive a committee term or deactivate a position/membership by clearing its Active checkbox when editing. Records are retained. New appointments require active terms and positions. Existing records can retain archived terms or retired positions for historical corrections. To appoint a member to a different term, add another membership record instead of changing the term on the old record.

The SdcManagement migration creates SdcTerm, SdcPosition and SdcMember tables. Unique term and position names, a date-order constraint and restrictive foreign keys protect the records. Pending migrations run during application startup. Changes use the existing audit system. Forms have no Cancel buttons. No committee members or dates are generated automatically, and no login accounts are created for members.
