public class QuotaRuleResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public int WeeklyQuota { get; set; }
    public DateTime UpdatedAt { get; set; }
}
