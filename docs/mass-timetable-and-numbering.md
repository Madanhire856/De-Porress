# Mass timetable and display numbers

The **Faith → Mass timetable** menu opens the school's upcoming and past Masses. Administration accounts can create Masses with **View Timetable** and **Create Timetable**, and edit times or ministry assignments with **View Timetable** and **Edit Timetable**. Teachers with **View Timetable** can view the timetable and ministry rosters. Other workspaces have no Faith access. The existing timetable rights are configured through user groups; no persisted rights bits changed.

Each Mass has its own date/time (Zimbabwe time), liturgical date, venue and celebrant. Ministry rosters assign active students or staff as readers or altar servers. The records use the existing `MassEvent` and `MassRoster` tables, so scheduled Masses continue to feed the teacher dashboard. No database migration is required.

Display numbering covers user groups, users, subjects, classes, grades, houses, villages, terms, sports, students and fee structures. Staff use their existing EC numbers. Receipts retain their separate transactional yearly sequence in `PaymentService`.

The display-number helper ranks the entire entity table by creation date and primary key. This fixes duplicate display numbers for matching timestamps and positive numbering for legacy null timestamps. A list page fetches the ordered IDs once per type rather than counting once per row. Search, filtering and pagination do not alter a record's number. Details and lists use the same ordering.

These remain calculated display numbers, matching the existing design; deleting records or changing creation dates can renumber later records. They are not stored permanent identifiers. Database primary keys and financial receipt numbers do not use this helper.

The shared table pagination retains page controls and removes the “Showing … of …” summary text.
