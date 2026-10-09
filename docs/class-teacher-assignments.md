# Class teacher assignments

Use **Academics → Class teacher assignments**. Configuration → Classes continues to define only the class name, grade and capacity.

Administrators, Heads and Deputy Heads need **ViewClasses** and **EditClasses** to assign, replace or remove a class teacher. No new rights bits or automatic permission grants are introduced. Teachers with ViewClasses can see their own assignments; the Bursar workspace does not expose this page and direct access is forbidden.

Select the academic year, choose **Assign teacher** or **Change teacher** beside a class, select an active teaching staff member, and save. Choose **Leave unassigned** to remove the current assignment. Only the current academic year can be changed; previous years remain available for reference. Teaching categories are General Teacher, Senior Teacher, Student Teacher and Priest. Teachers must be linked to their user account through the Staff record to see their assigned classes on their dashboard.

Each change closes the previous assignment and creates a dated replacement. A filtered unique index allows one open assignment per class and year. Saving happens in a serializable transaction; a stale editor is rejected rather than overwriting a more recent assignment. The existing ClassTeacherId is updated for compatibility. Teacher dashboard and Staff details use the annual assignment records so last year's assignment does not automatically carry into the new year.

Migration **ClassTeacherAssignments** creates the assignment history table and imports existing class teacher links into the year in which the migration is applied. The import timestamp represents migration time, not an invented original assignment date. Existing classes and staff are retained. The application applies pending migrations at startup.
