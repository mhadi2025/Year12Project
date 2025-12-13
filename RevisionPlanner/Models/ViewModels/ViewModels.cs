using System.ComponentModel.DataAnnotations;
namespace RevisionPlanner.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string EmailPassword { get; set; } = string.Empty;
    }
}
