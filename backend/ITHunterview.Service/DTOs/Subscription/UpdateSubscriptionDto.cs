using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Subscription
{
    public class UpdateSubscriptionDto
    {
        [Required(ErrorMessage = "Tên gói dịch vụ không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tên gói không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Giá của gói phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Thời hạn gói (DurationDays) phải ít nhất là 1 ngày.")]
        public int DurationDays { get; set; }

        [Required(ErrorMessage = "Cấu hình tính năng AI (FeaturesConfig) là bắt buộc.")]
        public FeaturesConfigDto FeaturesConfig { get; set; } = null!;
    }
}
