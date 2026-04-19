using Microsoft.AspNetCore.Identity;
namespace LedApp.Models
{
    public class AppUser : IdentityUser
    {
        public nguoiDungs? NguoiDung { get; set; }
    }
}