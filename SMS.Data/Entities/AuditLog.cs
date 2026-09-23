using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = null!;

    public string? BeforeValue { get; set; }

    public string? AfterValue { get; set; }

    public DateTime TimeStamp { get; set; }

    public string? Username { get; set; }

    public string? Reason { get; set; }

    public string? IpAddress { get; set; }

    public virtual User User { get; set; } = null!;
}
