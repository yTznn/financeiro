using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios; // Agora usa IFornecedorRepositorio
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos;

namespace Financeiro.Controllers
{
    [Authorize]
    public class FornecedoresController : Controller
    {
        // Mudança aqui: Injetamos o especialista em Fornecedores
        private readonly IFornecedorRepositorio _fornecedorRepo;

        public FornecedoresController(IFornecedorRepositorio fornecedorRepo)
        {
            _fornecedorRepo = fornecedorRepo;
        }

        [HttpGet]
        [AutorizarPermissao("FORNECEDOR_VIEW")]
        public async Task<IActionResult> Index(string busca = "", int pagina = 1)
        {
            const int tamanhoPagina = 3; 

            if (pagina < 1) pagina = 1;

            // Chama o novo repositório
            var (itens, totalItens) = await _fornecedorRepo.BuscarTodosPaginadoAsync(busca ?? "", pagina, tamanhoPagina);

            ViewBag.BuscaAtual = busca;
            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return View(itens);
        }
    }
}