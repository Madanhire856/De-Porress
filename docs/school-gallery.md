# School gallery

The landing page previews the first six published photos. `/Gallery` shows published photos by album, with 24 photos per page and an accessible enlarged photo viewer. The gallery uses the existing public school photos initially.

Administrators manage photos under **Administration → School gallery**. Access requires the Administration workspace and the existing **System Settings** right. They can upload JPEG/PNG photos, edit captions and albums, change display order, publish, move to draft, archive, and restore. Restored photos remain drafts until published again.

The browser resizes new uploads to a maximum of 1600 pixels and creates a 480 pixel thumbnail. The server independently validates file signatures, size and dimensions. Saved images are limited to 2 MiB and thumbnails to 256 KiB. Draft and archived images require administrator access, including when their image URL is known. Image responses use `no-store` so changing visibility is respected on subsequent requests.

## Storage

Production uses `Gallery:StoragePath=/keys/gallery` on the existing persistent EFS mount. The application UID must be able to create directories there. Development defaults to `SMS.Web/App_Data/Gallery`, which is excluded from Git and Docker builds. Include the gallery directory in the school's storage backup procedures.

Each photo has a GUID directory containing `image`, `thumbnail` and `metadata.json`. New photos commit by directory rename and metadata changes use atomic file replacement. Each metadata change records the local administrator ID and timestamp. Concurrent edits to the same photo use the last completed write. Archiving preserves all files. Existing seed photos are not republished after archiving.

There are no database migrations or additional AWS resources for this feature. Keep the gallery storage path stable between deployments.

## Verification

`dotnet test tests/SMS.Web.Tests/SMS.Web.Tests.csproj`

Gallery tests cover draft visibility, publish/archive/restore, persistence across store instances, upload rejection, seed preservation, and authentication/management checks on the image endpoint.
