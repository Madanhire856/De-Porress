using System;
using System.Collections.Generic;

namespace SMS.Data;

public partial class LiturgicalDay
{
    public Guid Id { get; set; }

    public DateTime? Date { get; set; }

    public string? Season { get; set; }

    public string? Color { get; set; }

    public string? Rank { get; set; }

    public string? Title { get; set; }

    public string? Source { get; set; }
}
