using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios; // Precisaremos do repositório de contrato que busca na View
using System.Threading.Tasks;
using System; // Necessário para Math.Ceiling

namespace Financeiro.Controllers
{
    public class FornecedoresController : Controller
    {
        private readonly IContratoRepositorio _contratoRepo;

        public FornecedoresController(IContratoRepositorio contratoRepo)
        {
            _contratoRepo = contratoRepo;
        }

        // Ação para a nova lista unificada de fornecedores
        [HttpGet]
        public async Task<IActionResult> Index(string busca = "", int pagina = 1)
        {
            // Definimos o tamanho da página aqui. Agora você pode mudar facilmente quando quiser.
            const int tamanhoPagina = 3;

            // Chamamos o novo método do repositório, passando o tamanho da página
            var (itens, totalItens) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(busca, pagina, tamanhoPagina);

            // Enviamos para a View todas as informações que ela precisa para a busca e paginação numerada
            ViewBag.BuscaAtual = busca;
            ViewBag.PaginaAtual = pagina;
            
            // Calculamos o número total de páginas
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return View(itens);
        }
    }
}