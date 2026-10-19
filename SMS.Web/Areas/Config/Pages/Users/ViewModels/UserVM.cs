namespace SMS.Web.Areas.Config.Pages.Users.ViewModels
{
    public class UserVM
    {
        public Guid? Id { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
        public string LoginId { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress,
            System.ComponentModel.DataAnnotations.StringLength(50)]
        public string Email { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string? Name { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(15)]
        public string? Mobile { get; set; }

        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public int RoleId { get; set; }

        public Guid? GroupId { get; set; }

    }
}
