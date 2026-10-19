using System;
using System.Collections.Generic;
using System.Linq;

namespace SMS.Lib;

// Separate persisted mask: AccessRights has no free bits. Never reuse its retired bit.
[Flags]
public enum FeatureRights : long
{
    None = 0,
    ViewTrips = 1L << 0, CreateTrips = 1L << 1, EditTrips = 1L << 2, ViewTripDrafts = 1L << 3,
    ViewNotices = 1L << 4, CreateNotices = 1L << 5, EditNotices = 1L << 6, PublishNotices = 1L << 7,
    ViewStaffNotices = 1L << 8,
    ViewSdcMembers = 1L << 9, CreateSdcMembers = 1L << 10, EditSdcMembers = 1L << 11,
    ViewSdcPositions = 1L << 12, CreateSdcPositions = 1L << 13, EditSdcPositions = 1L << 14,
    ViewSdcTerms = 1L << 15, CreateSdcTerms = 1L << 16, EditSdcTerms = 1L << 17,
    ViewGallery = 1L << 18, CreateGalleryPhotos = 1L << 19, EditGalleryPhotos = 1L << 20,
    PublishGalleryPhotos = 1L << 21, ArchiveGalleryPhotos = 1L << 22,
    ViewMasses = 1L << 23, CreateMasses = 1L << 24, EditMasses = 1L << 25,
    ViewReports = 1L << 26, PrintReports = 1L << 27, ExportReports = 1L << 28
}

public record FeatureRightItem(FeatureRights Right, string DisplayName);
public record FeatureRightCategory(string Title, string Icon, params FeatureRightItem[] Rights);
public static class FeatureRightsCatalog
{
    public static IReadOnlyList<FeatureRightCategory> Categories { get; } = [
        new("Trips", "fa-bus", new(FeatureRights.ViewTrips,"View trips"), new(FeatureRights.CreateTrips,"Create trips"), new(FeatureRights.EditTrips,"Edit trips"), new(FeatureRights.ViewTripDrafts,"View draft trips")),
        new("Notices", "fa-bullhorn", new(FeatureRights.ViewNotices,"View notice management"), new(FeatureRights.CreateNotices,"Create notices"), new(FeatureRights.EditNotices,"Edit notices"), new(FeatureRights.PublishNotices,"Publish and withdraw notices"), new(FeatureRights.ViewStaffNotices,"Read staff notices")),
        new("SDC members", "fa-users", new(FeatureRights.ViewSdcMembers,"View SDC members"), new(FeatureRights.CreateSdcMembers,"Create SDC members"), new(FeatureRights.EditSdcMembers,"Edit SDC members")),
        new("SDC positions", "fa-user-tie", new(FeatureRights.ViewSdcPositions,"View SDC positions"), new(FeatureRights.CreateSdcPositions,"Create SDC positions"), new(FeatureRights.EditSdcPositions,"Edit SDC positions")),
        new("SDC committee terms", "fa-calendar-alt", new(FeatureRights.ViewSdcTerms,"View committee terms"), new(FeatureRights.CreateSdcTerms,"Create committee terms"), new(FeatureRights.EditSdcTerms,"Edit committee terms")),
        new("School gallery", "fa-images", new(FeatureRights.ViewGallery,"View gallery management"), new(FeatureRights.CreateGalleryPhotos,"Add gallery photos"), new(FeatureRights.EditGalleryPhotos,"Edit gallery photos"), new(FeatureRights.PublishGalleryPhotos,"Publish and unpublish photos"), new(FeatureRights.ArchiveGalleryPhotos,"Archive and restore photos")),
        new("Mass timetable", "fa-cross", new(FeatureRights.ViewMasses,"View Mass timetable"), new(FeatureRights.CreateMasses,"Create Masses"), new(FeatureRights.EditMasses,"Edit Masses and ministries")),
        new("Reports", "fa-chart-column", new(FeatureRights.ViewReports,"View reports"), new(FeatureRights.PrintReports,"Print reports / save PDF"), new(FeatureRights.ExportReports,"Export reports to CSV"))
    ];
    public static bool IsValid(long value) => Enum.GetValues<FeatureRights>().Any(x => x != FeatureRights.None && (long)x == value);
    public static long All => Enum.GetValues<FeatureRights>().Aggregate(0L, (mask, right) => mask | (long)right);

    // Null means an existing group has not yet been configured. Role/workspace
    // boundaries still apply; these defaults reproduce the previous rules.
    public static long Legacy(long oldRights)
    {
        var result = FeatureRights.ViewTrips | FeatureRights.CreateTrips | FeatureRights.EditTrips | FeatureRights.ViewTripDrafts
            | FeatureRights.ViewNotices | FeatureRights.CreateNotices | FeatureRights.EditNotices | FeatureRights.PublishNotices | FeatureRights.ViewStaffNotices
            | FeatureRights.ViewSdcMembers | FeatureRights.CreateSdcMembers | FeatureRights.EditSdcMembers
            | FeatureRights.ViewSdcPositions | FeatureRights.CreateSdcPositions | FeatureRights.EditSdcPositions
            | FeatureRights.ViewSdcTerms | FeatureRights.CreateSdcTerms | FeatureRights.EditSdcTerms
            | FeatureRights.ViewReports | FeatureRights.PrintReports | FeatureRights.ExportReports;
        bool Has(AccessRights right) => (oldRights & (long)right) == (long)right;
        if (Has(AccessRights.SystemSettings)) result |= FeatureRights.ViewGallery | FeatureRights.CreateGalleryPhotos | FeatureRights.EditGalleryPhotos | FeatureRights.PublishGalleryPhotos | FeatureRights.ArchiveGalleryPhotos;
        if (Has(AccessRights.ViewTimetable)) result |= FeatureRights.ViewMasses;
        if (Has(AccessRights.CreateTimetable)) result |= FeatureRights.CreateMasses;
        if (Has(AccessRights.EditTimetable)) result |= FeatureRights.EditMasses;
        return (long)result;
    }
}
