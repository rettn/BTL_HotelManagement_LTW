using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CaiDatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}