using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CBAS.Web.Pages
{
    [Authorize]
    public class HostModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
