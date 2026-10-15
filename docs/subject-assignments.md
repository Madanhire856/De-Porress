# Subject assignments

Use **Academics → Subject assignments** to see teaching staff and their subjects. Search by teacher name or subject, choose **Manage subjects** beside an active teacher, select their subjects and save. Clearing a subject removes that assignment; clearing all leaves the teacher without subject assignments. The Staff Details shortcut still opens the same editor. Subjects themselves are defined separately in school configuration.

Administrators, Heads and Deputy Heads need **ViewSubjects** and **EditSubjects** to edit assignments. Teachers with ViewSubjects see only their own linked staff record and cannot change assignments, even if their group has EditSubjects. The Bursar workspace does not expose the page and direct access is denied.

Only active staff in teaching categories (General Teacher, Senior Teacher, Student Teacher and Priest) can receive assignments, following the school's existing teaching eligibility rules. Submitted subject IDs are validated against the subject catalogue. Saving retains existing links, adds new selections and removes deselected links in one transaction. A selection version check rejects stale forms instead of silently replacing someone else's changes. Subject assignment changes use the existing audit system.

These records describe a teacher's teaching subjects; they are not a class-specific timetable or annual class teacher assignment. This improvement uses the existing TeacherSubject table and needs no database migration.
