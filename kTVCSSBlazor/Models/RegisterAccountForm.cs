using System.ComponentModel.DataAnnotations;

namespace kTVCSSBlazor.Models
{
    public class RegisterAccountForm
    {
        [Required(ErrorMessage = "Поле должно быть заполнено")]
        [StringLength(16, ErrorMessage = "Логин не может быть более 16 символов")]
        public string? Login { get; set; }

        [Required(ErrorMessage = "Поле должно быть заполнено")]
        public string? Password { get; set; }
    }
}
