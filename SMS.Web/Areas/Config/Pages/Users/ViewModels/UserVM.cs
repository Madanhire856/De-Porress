namespace SMS.Web.Areas.Config.Pages.Users.ViewModels
{
    public class UserVM
    {
        public Guid? Id { get; set; } = null!;
        public string LoginId { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Name { get; set; }

        public string? Mobile { get; set; }

        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public int RoleId { get; set; }

        public Guid? GroupId { get; set; }

    }
}
