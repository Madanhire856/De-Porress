# School notices

Administration → School notices lets Admin, Head and Deputy Head create public or staff-only notices. New notices default to **Staff only**. Choose **Everyone — public school website** to publish publicly. Administrators can save a draft, publish, edit a published notice or withdraw it. This uses role-based access; no existing permission bits or user groups are changed.

Visitors see the latest three published notices on the school home page, a paginated listing at /Notices, and individual notice pages. Drafts and withdrawn notices are never returned by the public query, including when someone guesses an individual notice URL. Notice text is displayed as escaped plain text with its paragraph breaks preserved.

Notices use the existing Announcement table: WEBSITE (4) for public notices and STAFF_NOTICE (5) for staff-only notices. Existing PORTAL, SMS and WHATSAPP announcements remain separate. SENT represents published notices and DRAFT represents drafts visible only to notice administrators. CreationDate records the UTC creation time, displayed in Zimbabwe time. Editing retains the original creation date. No database migration is needed.

Signed-in Admin, Head, Deputy Head, Teacher, Staff and Bursar users can read published staff notices through **Notices → Staff notices**. No operational rights are needed for reading. Anonymous users and non-staff roles cannot open these pages or retrieve notices by ID. Staff-only notices are excluded from every public notice query. Drafts and withdrawn notices are excluded from staff reader queries.

In the administration area, select Create notice, choose the audience, enter a title (up to 50 characters) and notice text (up to 10,000 characters), then select Save as draft or Publish notice. Saving a published notice as a draft withdraws it from its audience. Withdraw keeps the record for editing and later republication. Changes use the existing audit system. No emails, SMS or WhatsApp messages are sent by this feature. Notice, subject assignment and class teacher assignment forms do not include Cancel buttons.
