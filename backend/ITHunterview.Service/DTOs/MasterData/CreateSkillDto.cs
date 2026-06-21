using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class CreateSkillDto
    {
        [Required(ErrorMessage = "Tên kỹ năng không được để trống.")]
        [MaxLength(255, ErrorMessage = "Tên kỹ năng không được vượt quá 255 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh mục kỹ năng không được để trống.")]
        public int CategoryId { get; set; }

        public SkillStatus Status { get; set; } = SkillStatus.ACTIVE;
    }
}
