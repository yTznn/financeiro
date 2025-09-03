using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;

namespace Financeiro.Controllers
{
    public class InstrumentosController : Controller
    {
        private static readonly DateTime MinAppDate = new DateTime(2020, 1, 1);

        private readonly IInstrumentoRepositorio _repo;
        private readonly IInstrumentoVersaoRepositorio _versaoRepo;
        private readonly IEntidadeRepositorio _entidadeRepo;
        private readonly ILogService _logService;

        public InstrumentosController(
            IInstrumentoRepositorio repo,
            IInstrumentoVersaoRepositorio versaoRepo,
            IEntidadeRepositorio entidadeRepo,
            ILogService logService)
        {
            _repo = repo;
            _versaoRepo = versaoRepo;
            _entidadeRepo = entidadeRepo;
            _logService = logService;
        }

        /* ---------- helpers ---------- */
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

        // remove emojis, símbolos indesejados etc.
        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            // Remove: emojis (via categoria de *surrogates*), ZWJ, variation selector
            // e os caracteres ASCII explícitos: * ! @ # '
            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\p{Cs}|\u200D|\uFE0F|[\u2600-\u27BF]|[\*\!\@\#']",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant
            );

            return cleaned.Trim();
        }

        // mantém só dígitos e '/', colapsa barras repetidas e remove barras nas pontas
        private static string NormalizarNumero(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var only = System.Text.RegularExpressions.Regex.Replace(s, @"[^\d/]", "");
            only = System.Text.RegularExpressions.Regex.Replace(only, @"/{2,}", "/");
            return only.Trim('/');
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
            ViewBag.FormAction = "Salvar";
            ViewBag.VersaoAtual = null;

            var hoje = DateTime.Today;

            string? numeroSugerido;
            try
            {
                numeroSugerido = await _repo.SugerirProximoNumeroAsync(hoje.Year);
                numeroSugerido = NormalizarNumero(numeroSugerido);
            }
            catch
            {
                numeroSugerido = string.Empty;
            }

            var vm = new TipoAcordoViewModel
            {
                Ativo          = true,
                DataInicio     = hoje,
                DataFim        = hoje,
                DataAssinatura = hoje,
                Numero         = numeroSugerido ?? string.Empty
            };

            return View("InstrumentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(TipoAcordoViewModel vm)
        {
            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");
            if (vm.EntidadeId == 0)
                ModelState.AddModelError(nameof(vm.EntidadeId), "Selecione a Entidade.");
            if (vm.Valor <= 0)
                ModelState.AddModelError(nameof(vm.Valor), "Informe um valor maior que zero.");

            ValidarDatas(vm);

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }

            if (await _repo.ExisteNumeroAsync(vm.Numero))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }

            try
            {
                await _repo.InserirAsync(vm);

                await _logService.RegistrarCriacaoAsync("Instrumento", new
                {
                    vm.Numero, vm.Valor, vm.Objeto, vm.DataInicio, vm.DataFim,
                    vm.Ativo, vm.Observacao, vm.DataAssinatura, vm.EntidadeId
                }, registroId: 0);

                TempData["Sucesso"] = "Instrumento criado com sucesso.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var instrumento = await _repo.ObterPorIdAsync(id);
            if (instrumento is null) return NotFound();

            var vm = new TipoAcordoViewModel
            {
                Id             = instrumento.Id,
                Numero         = instrumento.Numero,
                Valor          = instrumento.Valor,
                Objeto         = instrumento.Objeto,
                DataInicio     = instrumento.DataInicio,
                DataFim        = instrumento.DataFim,
                Ativo          = instrumento.Ativo,
                Observacao     = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId     = instrumento.EntidadeId
            };

            await CarregarEntidadesAsync(vm.EntidadeId);

            ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.FormAction  = "Atualizar";

            return View("InstrumentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, TipoAcordoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");
            if (vm.EntidadeId == 0)
                ModelState.AddModelError(nameof(vm.EntidadeId), "Selecione a Entidade.");
            if (vm.Valor <= 0)
                ModelState.AddModelError(nameof(vm.Valor), "Informe um valor maior que zero.");

            ValidarDatas(vm);

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }

            if (await _repo.ExisteNumeroAsync(vm.Numero, ignorarId: id))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }

            var antes = await _repo.ObterPorIdAsync(id);
            if (antes is null) return NotFound();

            bool nenhumaAlteracao =
                string.Equals((antes.Numero ?? "").Trim(), (vm.Numero ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
                antes.Valor == vm.Valor &&
                string.Equals(antes.Objeto ?? "", vm.Objeto ?? "", StringComparison.Ordinal) &&
                antes.DataInicio.Date == vm.DataInicio.Date &&
                antes.DataFim.Date == vm.DataFim.Date &&
                (antes.DataAssinatura?.Date ?? (DateTime?)null) == (vm.DataAssinatura?.Date ?? (DateTime?)null) &&
                antes.Ativo == vm.Ativo &&
                string.Equals(antes.Observacao ?? "", vm.Observacao ?? "", StringComparison.Ordinal) &&
                antes.EntidadeId == vm.EntidadeId;

            if (nenhumaAlteracao)
            {
                TempData["Sucesso"] = "Nenhuma alteração detectada.";
                return RedirectToAction("Editar", new { id });
            }

            try
            {
                await _repo.AtualizarAsync(id, vm);

                var depois = await _repo.ObterPorIdAsync(id);
                await _logService.RegistrarEdicaoAsync("Instrumento", antes, depois, id);

                TempData["Sucesso"] = "Instrumento atualizado com sucesso.";
                return RedirectToAction("Editar", new { id });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
                return View("InstrumentoForm", vm);
            }
        }

        /* ---------- EXCLUIR ---------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var existente = await _repo.ObterPorIdAsync(id);
            if (existente is null) return NotFound();

            try
            {
                await _repo.ExcluirAsync(id);
                await _logService.RegistrarExclusaoAsync("Instrumento", existente, registroId: id);

                TempData["Sucesso"] = "Instrumento excluido com sucesso.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível excluir: há vínculos relacionados.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
                return RedirectToAction("Index");
            }
        }

        /* ---------- HISTÓRICO (Partial) ---------- */
        [HttpGet]
        public async Task<IActionResult> Historico(int id)
        {
            var versoes = await _versaoRepo.ListarPorInstrumentoAsync(id);
            return PartialView("_HistoricoAditivos", versoes);
        }

        [HttpGet]
        public async Task<IActionResult> HistoricoPagina(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual  = pag;
            return PartialView("_HistoricoAditivosTable", itens);
        }

        /* ---------- AJAX: Sugerir próximo número ---------- */
        [HttpGet]
        public async Task<IActionResult> SugerirNumero(int ano)
        {
            var numero = await _repo.SugerirProximoNumeroAsync(ano);
            return Json(new { proximoNumero = NormalizarNumero(numero) });
        }
    }
}