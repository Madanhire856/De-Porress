using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class HouseTeacher
{
    public Guid Id { get; set; }

    public Guid HouseId { get; set; }

    public Guid StaffId { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual House House { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;
}
