using System;
using System.Collections.Generic;

namespace CafeManagementSystem.Models.Entities;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string ComplaintType { get; set; } = null!;

    public int? Rating { get; set; }

    public string Content { get; set; } = null!;

    public string? EvidenceImageUrl { get; set; }

    public string ComplaintStatus { get; set; } = null!;

    public string? AdminResponse { get; set; }

    public int? ResolvedByAdminId { get; set; }

    public bool IsRefundApproved { get; set; }

    public decimal? RefundAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual User? ResolvedByAdmin { get; set; }
}
