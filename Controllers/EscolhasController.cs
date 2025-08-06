using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Controllers
{
    public class EscolhasController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }
}