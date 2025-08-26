using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Servicos;
using Microsoft.Data.SqlClient;

namespace Financeiro.Controllers
{
    public class TipoAcordosController : Controller
    {
        private static readonly DateTime MinAppDate = new DateTime(2020, 1, 1);

        private readonly ITipoAcordoRepositorio _repo;
        private readonly IAditivoRepositorio _aditivoRepo;
        private readonly IEntidadeRepositorio _entidadeRepo;
        private readonly ILogService _logService;

        public TipoAcordosController(
            ITipoAcordoRepositorio repo,
            IAditivoRepositorio aditivoRepo,
            IEntidadeRepositorio entidadeRepo,
            ILogService logService)
        {
            _repo = repo;
            _aditivoRepo = aditivoRepo;
            _entidadeRepo = entidadeRepo;
            _logService = logService;
        }

        /* ---------- helper: carrega dropdown de entidades ---------- */
        private async Task CarregarEntidadesAsync(int? selecionada = null)
        {
            var entidades = await _entidadeRepo.ListAsync();
            ViewBag.Entidades = entidades
                .Select(e => new SelectListItem($"{e.Sigla} - {e.Nome}", e.Id.ToString(),
                    selecionada.HasValue && e.Id == selecionada.Value))
                .ToList();
        }

        private void ValidarDatas(TipoAcordoViewModel vm)
        {
            if (vm.DataInicio < MinAppDate)
                ModelState.AddModelError(nameof(vm.DataInicio), "Data início deve ser a partir de 01/01/2020.");
            if (vm.DataFim < MinAppDate)
                ModelState.AddModelError(nameof(vm.DataFim), "Data fim deve ser a partir de 01/01/2020.");
            if (vm.DataAssinatura.HasValue && vm.DataAssinatura.Value < MinAppDate)
                ModelState.AddModelError(nameof(vm.DataAssinatura), "Data de assinatura deve ser a partir de 01/01/2020.");
            if (vm.DataFim < vm.DataInicio)
                ModelState.AddModelError(nameof(vm.DataFim), "Data fim não pode ser anterior à data início.");
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
        public async Task<IActionResult> Novo()
        {
            await CarregarEntidadesAsync();

            var hoje = DateTime.Today;
            return View("TipoAcordoForm", new TipoAcordoViewModel
            {
                Ativo = true,
                DataInicio = hoje,
                DataFim = hoje,
                DataAssinatura = hoje
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(TipoAcordoViewModel vm)
        {
            vm.Numero = vm.Numero?.Trim();
            ValidarDatas(vm);

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }

            // Duplicidade por Número (global). Se preferir por Entidade, adapto.
            if (await _repo.ExisteNumeroAsync(vm.Numero))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um repasse com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }

            try
            {
                await _repo.InserirAsync(vm);

                await _logService.RegistrarCriacaoAsync("TipoAcordo", new
                {
                    vm.Numero, vm.Valor, vm.Objeto, vm.DataInicio, vm.DataFim,
                    vm.Ativo, vm.Observacao, vm.DataAssinatura, vm.EntidadeId
                }, registroId: 0);

                TempData["Sucesso"] = "Tipo de Acordo criado com sucesso.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um repasse com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }
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
                DataAssinatura = acordo.DataAssinatura,
                EntidadeId = acordo.EntidadeId
            };

            await CarregarEntidadesAsync(vm.EntidadeId);
            return View("TipoAcordoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, TipoAcordoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            vm.Numero = vm.Numero?.Trim();
            ValidarDatas(vm);

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }

            if (await _repo.ExisteNumeroAsync(vm.Numero, ignorarId: id))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um repasse com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }

            var antes = await _repo.ObterPorIdAsync(id);

            try
            {
                await _repo.AtualizarAsync(id, vm);

                var depois = await _repo.ObterPorIdAsync(id);
                await _logService.RegistrarEdicaoAsync("TipoAcordo", antes, depois, id);

                TempData["Sucesso"] = "Tipo de Acordo atualizado com sucesso.";
                return RedirectToAction("Editar", new { id });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um repasse com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                return View("TipoAcordoForm", vm);
            }
        }

        /* ---------- EXCLUIR ---------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var existente = await _repo.ObterPorIdAsync(id);
            if (existente is null) return NotFound();

            await _repo.ExcluirAsync(id);

            await _logService.RegistrarExclusaoAsync("TipoAcordo", existente, registroId: id);

            TempData["Sucesso"] = "Tipo de Acordo excluido com sucesso.";
            return RedirectToAction("Index");
        }

        /* ---------- HISTÓRICO (Partial) ---------- */
        [HttpGet]
        public async Task<IActionResult> Historico(int id)
        {
            var versoes = await _aditivoRepo.ListarPorAcordoAsync(id);
            return PartialView("_HistoricoAditivos", versoes);
        }

        [HttpGet]
        public async Task<IActionResult> HistoricoPagina(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _aditivoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual  = pag;
            return PartialView("_HistoricoAditivosTable", itens);
        }

        /* ---------- AJAX: Sugerir próximo número ---------- */
        [HttpGet]
        public async Task<IActionResult> SugerirNumero(int ano)
        {
            var numero = await _repo.SugerirProximoNumeroAsync(ano);
            return Json(new { proximoNumero = numero });
        }
    }
}