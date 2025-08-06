using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Controllers
{
    public class TipoAcordosController : Controller
    {
        private readonly ITipoAcordoRepositorio _repo;
        private readonly IAditivoRepositorio _aditivoRepo;   // ➜ novo

        public TipoAcordosController(ITipoAcordoRepositorio repo,
                                     IAditivoRepositorio aditivoRepo)   // ➜ injeta o histórico
        {
            _repo = repo;
            _aditivoRepo = aditivoRepo;
        }

        /* ---------- LISTAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _repo.ListarAsync();
            return View(lista);
        }

        /* ---------- NOVO ---------- */
        [HttpGet]
        public IActionResult Novo()
            => View("TipoAcordoForm", new TipoAcordoViewModel());

        [HttpPost]
        public async Task<IActionResult> Salvar(TipoAcordoViewModel vm)
        {
            if (!ModelState.IsValid) return View("TipoAcordoForm", vm);
            await _repo.InserirAsync(vm);
            return RedirectToAction("Index");
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var acordo = await _repo.ObterPorIdAsync(id);
            if (acordo is null) return NotFound();

            var vm = new TipoAcordoViewModel
            {
                Id = acordo.Id,
                Numero = acordo.Numero,
                Valor = acordo.Valor,
                Objeto = acordo.Objeto,
                DataInicio = acordo.DataInicio,
                DataFim = acordo.DataFim,
                Ativo = acordo.Ativo,
                Observacao = acordo.Observacao,
                DataAssinatura = acordo.DataAssinatura
            };
            return View("TipoAcordoForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, TipoAcordoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("TipoAcordoForm", vm);

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Editar", new { id });   // volta para edição p/ ver histórico
        }

        /* ---------- HISTÓRICO (Partial) ---------- */
        [HttpGet]
        public async Task<IActionResult> Historico(int id)
        {
            var versoes = await _aditivoRepo.ListarPorAcordoAsync(id);
            return PartialView("_HistoricoAditivos", versoes);
        }
        // GET /TipoAcordos/HistoricoPagina/123?pag=1
        [HttpGet]
        public async Task<IActionResult> HistoricoPagina(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _aditivoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual  = pag;
            return PartialView("_HistoricoAditivosTable", itens);
        }
    }
}