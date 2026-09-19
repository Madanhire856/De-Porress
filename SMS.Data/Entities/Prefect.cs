using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class Prefect
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public int PostTitleId { get; set; }

    public int StatusId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Guid? NominatorId { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual Staff? Nominator { get; set; }
}
