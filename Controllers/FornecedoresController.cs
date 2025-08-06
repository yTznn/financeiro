using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios; // Precisaremos do repositório de contrato que busca na View
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    public class FornecedoresController : Controller
    {
        private readonly IContratoRepositorio _contratoRepo; // Usamos este repo pois ele já tem o método de busca na View

        public FornecedoresController(IContratoRepositorio contratoRepo)
        {
            _contratoRepo = contratoRepo;
        }

        // Ação para a nova lista unificada de fornecedores
        [HttpGet]
        public async Task<IActionResult> Index(string busca = "", int pagina = 1)
        {
            // Usamos o método que já existe para a busca de fornecedores
            var (itens, temMais) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(busca, pagina);

            ViewBag.BuscaAtual = busca;
            ViewBag.PaginaAtual = pagina;
            ViewBag.TemPaginaAnterior = pagina > 1;
            ViewBag.TemPaginaSeguinte = temMais;

            return View(itens);
        }
    }
}