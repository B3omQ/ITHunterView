namespace ITHunterview.Service.DTOs.Dashboard;

public class DashboardFilterRequest
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
