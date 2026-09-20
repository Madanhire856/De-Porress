using System.ComponentModel.DataAnnotations;

namespace SMS.Web.Areas.Config.Pages.UserGroups.ViewModels
{
    public class UserGroupVM
    {
        [Required(ErrorMessage ="Group name is required")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
