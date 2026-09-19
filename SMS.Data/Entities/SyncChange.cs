using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class SyncChange
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTime ClientTimeStamp { get; set; }

    public int SyncStatusId { get; set; }

    public string? ConflictResolutionNotesJson { get; set; }
}
