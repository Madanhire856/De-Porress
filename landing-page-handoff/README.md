# Landing page designer handoff

Open index.html directly in a browser. No .NET installation, server, package install,
or internet connection is needed. Send the entire folder (or its ZIP), not just
the HTML: the local images, icon fonts and styles must stay together.

## What to edit and return

- index.html: complete public landing page, including header and footer.
- school-landing.css: all design styles. This starts as a copy of the live theme.
- assets/Images/: existing school photographs and crest; add new local assets here.
- school-landing.js: existing optional reveal animation; return it if changed.

Return the whole folder. Keep file names and the marked HEADER, LANDING CONTENT
and FOOTER boundaries so the finished design can be pasted back easily.
Do not introduce a build system, framework, CDN, external font dependency or
backend code. Prefer HTML and CSS; document any new JavaScript or dependencies.

preview-base.css only simulates the project's existing base layout. Do not edit
it: add design rules to school-landing.css. Bootstrap and Font Awesome are bundled
for compatibility with the project; leave the vendor files unchanged.

Scope design CSS under .school-site (or its descendants) to avoid changing staff
dashboards and login pages. Keep the .public-wrapper and .school-site wrappers,
the main element's school-content ID, and unique section IDs used by navigation.
Keep image descriptions, heading order, keyboard focus styles and reduced-motion
support. Check narrow mobile screens and desktop layouts. Keep factual school
content unless the owner supplies replacements.

The Notices and Staff portal links point to application routes. They work after
integration into the running app; they will not open application pages from a
local file preview. Do not replace those links with dummy links.

## Paste the finished design back into the project

The app uses Razor layouts, so do NOT paste the entire HTML document into
Index.cshtml. That file also selects the authenticated dashboards.

1. Replace SMS.Web/wwwroot/css/school-landing.css with the returned file.
   The application already loads it; no stylesheet registration is needed.
   This stylesheet is shared with other public pages, so review Notices too.
2. In SMS.Web/Pages/Shared/Dashboards/_landing.cshtml, keep these first lines:
       @model SMS.Web.Pages.IndexModel
       @using SMS.Web.Services
   Replace the markup below them with the content BETWEEN the LANDING CONTENT
   markers in index.html. Do not include the enclosing main element.
3. If the header changed, replace the header markup in
   SMS.Web/Pages/Shared/_PublicHeader.cshtml with the HEADER section.
   Keep the existing first Razor line that defines publicHome.
4. If the footer changed, replace the footer markup in
   SMS.Web/Pages/Shared/_PublicFooter.cshtml with the FOOTER section.
   Keep the existing first Razor line that defines publicHome.
5. In all pasted sections, change image paths from assets/Images/ to ~/Images/.
   Copy new image files into SMS.Web/wwwroot/Images/ using the same filenames.
   Other new CSS asset URLs must be relative to wwwroot/css/school-landing.css,
   e.g. ../Images/photo.jpg. Copy their assets into the matching wwwroot folder.
6. Restore these Razor values in the pasted header/footer:
   - href="index.html" becomes href="@publicHome"
   - Home section links such as href="#about" become
     href="@(publicHome + "#about")" (also faith, learning and contact).
     This ensures navigation works from other public pages.
   - href="/Notices" becomes asp-area="" asp-page="/Notices/Index"
   - href="/Identity/Account/StaffLogin" becomes
     asp-area="Identity" asp-page="/Account/StaffLogin"
   - The footer year 2026 becomes @DateTime.UtcNow.AddHours(2).Year
   Landing content section links such as href="#contact" stay as they are.
7. If school-landing.js changed, replace SMS.Web/wwwroot/js/school-landing.js.
   It is already loaded by _PublicLayout.cshtml.
8. Keep _PublicLayout.cshtml and Index.cshtml in place. Do not copy the preview
   head, body, wrappers, CSS links, script tag or preview-base.css into Razor:
   the existing layouts already provide them.

The standalone header/footer use static URLs and a sample year solely to preview
without Razor. The substitutions above preserve authentication-aware home links,
application routing and the dynamic year.

## Final review in the application

Run the project using its normal development setup. Check the logged-out home
page, the public website while logged in (?Website=true), Notices navigation,
Staff portal, mobile layout, images, keyboard navigation and page scrolling.
The active application was not modified when this handoff was prepared.
