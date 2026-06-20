using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class UpdateMajorDto
    {
        [Required(ErrorMessage = "Tên chuyên ngành không được để trống.")]
        [MaxLength(255, ErrorMessage = "Tên chuyên ngành không được vượt quá 255 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã chuyên ngành không được để trống.")]
        [MaxLength(50, ErrorMessage = "Mã chuyên ngành không được vượt quá 50 ký tự.")]
        public string Code { get; set; } = string.Empty;
    }
}
