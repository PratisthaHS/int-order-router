namespace int_order_router.Models;

public class QuotaRule
{
    public string CustomerName { get; set; } = string.Empty; // User-provided
    public int WeeklyQuota { get; set; }
    public string? CreatedBy { get; set; } 
    public string? Comment { get; set; }   // Optional reason for change
}
