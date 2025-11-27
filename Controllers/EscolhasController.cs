using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Financeiro.Controllers
{
    [Authorize]
    public class EscolhasController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }
}