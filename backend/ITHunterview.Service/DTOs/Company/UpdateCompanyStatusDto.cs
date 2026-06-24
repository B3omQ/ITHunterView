using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Company
{
    public class UpdateCompanyStatusDto
    {
        [Required(ErrorMessage = "Trạng thái công ty là bắt buộc")]
        public CompanyStatus Status { get; set; }
    }
}
