using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos; 
using Financeiro.Extensions; 

namespace Financeiro.Controllers
{
    [Authorize]
    public class InstrumentosController : Controller
    {
        private static readonly DateTime MinAppDate = new DateTime(2020, 1, 1);
        private const int TAMANHO_PAGINA = 4; // <--- Paginação ajustada para 4 itens

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
        
        // Carrega APENAS a entidade logada (para travar o combo)
        private async Task CarregarEntidadesAsync(int? selecionada = null)
        {
            int entidadeId = User.ObterEntidadeId();
            string sigla = User.ObterSiglaEntidade();

            var listaUnica = new List<SelectListItem>
            {
                new SelectListItem($"{sigla} - (Unidade Atual)", entidadeId.ToString(), true)
            };

            ViewBag.Entidades = listaUnica;
        }

        private void ValidarDatas(InstrumentoViewModel vm)
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

        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(s, @"\p{Cs}|\u200D|\uFE0F|[\u2600-\u27BF]|[\*\!\@\#']", string.Empty).Trim();
        }

        private static string NormalizarNumero(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var only = System.Text.RegularExpressions.Regex.Replace(s, @"[^\d/]", "");
            return System.Text.RegularExpressions.Regex.Replace(only, @"/{2,}", "/").Trim('/');
        }

        private static int CalcularMeses(DateTime inicio, DateTime fim)
        {
            if (fim < inicio) return 0;
            return ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month + 1;
        }

        /* ---------- LISTAR (PAGINADO E ISOLADO) ---------- */
        [HttpGet]
        [AutorizarPermissao("INSTRUMENTO_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            int entidadeId = User.ObterEntidadeId();
            if (entidadeId == 0) return RedirectToAction("Login", "Conta");

            if (p < 1) p = 1;

            var (listaResumo, totalItens) = await _repo.ListarResumoPaginadoAsync(entidadeId, p, TAMANHO_PAGINA);

            var ultimosPrazo    = new Dictionary<int, string>();
            var valoresVigentes = new Dictionary<int, decimal>();

            foreach (var r in listaResumo)
            {
                var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(r.InstrumentoId);
                if (versaoAtual != null)
                    valoresVigentes[r.InstrumentoId] = versaoAtual.Valor;

                var historico = (await _versaoRepo.ListarPorInstrumentoAsync(r.InstrumentoId))
                                .OrderByDescending(v => v.Versao)
                                .ToList();

                for (int i = 0; i < historico.Count - 1; i++)
                {
                    var atual    = historico[i];
                    var anterior = historico[i + 1];

                    bool mudouPrazo = (atual.VigenciaFim ?? DateTime.MinValue) != (anterior.VigenciaFim ?? DateTime.MinValue);
                    if (mudouPrazo)
                    {
                        string txtDelta;
                        if (atual.VigenciaFim.HasValue && anterior.VigenciaFim.HasValue)
                        {
                            int deltaMeses = ((atual.VigenciaFim.Value.Year  - anterior.VigenciaFim.Value.Year)  * 12)
                                             +  (atual.VigenciaFim.Value.Month - anterior.VigenciaFim.Value.Month);
                            var sinal   = deltaMeses > 0 ? "+" : (deltaMeses < 0 ? "−" : "±");
                            txtDelta    = deltaMeses != 0 ? $"{sinal}{Math.Abs(deltaMeses)} mês(es)" : "ajuste de prazo";
                        }
                        else if (atual.VigenciaFim.HasValue && !anterior.VigenciaFim.HasValue)
                        {
                            txtDelta = $"definiu fim para {atual.VigenciaFim.Value:dd/MM/yyyy}";
                        }
                        else if (!atual.VigenciaFim.HasValue && anterior.VigenciaFim.HasValue)
                        {
                            txtDelta = "removeu fim (vigência aberta)";
                        }
                        else
                        {
                            txtDelta = "ajuste de prazo";
                        }

                        var quando = (atual.DataAssinatura ?? atual.VigenciaInicio).ToString("dd/MM/yyyy");
                        ultimosPrazo[r.InstrumentoId] = $"{txtDelta} em {quando}";
                        break; 
                    }
                }
            }

            ViewBag.UltimosPrazo    = ultimosPrazo;
            ViewBag.ValoresVigentes = valoresVigentes;

            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalItens / TAMANHO_PAGINA);

            return View(listaResumo);
        }

        /* ---------- NOVO ---------- */
        [HttpGet]
        [AutorizarPermissao("INSTRUMENTO_ADD")]
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

            var vm = new InstrumentoViewModel
            {
                Ativo = true,
                Vigente = false,
                DataInicio = hoje,
                DataFim = hoje,
                DataAssinatura = hoje,
                Numero = numeroSugerido ?? string.Empty,
                EntidadeId = User.ObterEntidadeId()
            };

            return View("InstrumentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("INSTRUMENTO_ADD")]
        public async Task<IActionResult> Salvar(InstrumentoViewModel vm)
        {
            // SEGURANÇA: Força o ID da entidade logada
            vm.EntidadeId = User.ObterEntidadeId();

            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            ValidarDatas(vm);

            var meses = CalcularMeses(vm.DataInicio.Date, vm.DataFim.Date);
            if (meses <= 0)
                ModelState.AddModelError(nameof(vm.DataFim), "Período inválido. Ajuste as datas de vigência.");

            if (vm.UsarValorMensal)
            {
                if (!vm.ValorMensal.HasValue || vm.ValorMensal.Value <= 0)
                    ModelState.AddModelError(nameof(vm.ValorMensal), "Informe um valor mensal maior que zero.");

                if (meses > 0 && vm.ValorMensal.HasValue && vm.ValorMensal.Value > 0)
                    vm.Valor = decimal.Round(vm.ValorMensal.Value * meses, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                if (vm.Valor <= 0)
                    ModelState.AddModelError(nameof(vm.Valor), "Informe um valor total maior que zero.");

                if (meses > 0 && vm.Valor > 0)
                    vm.ValorMensal = decimal.Round(vm.Valor / meses, 2, MidpointRounding.AwayFromZero);
            }

            ModelState.Remove(nameof(InstrumentoViewModel.Valor));
            ModelState.Remove(nameof(InstrumentoViewModel.ValorMensal));
            TryValidateModel(vm);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");
            
            if (vm.EntidadeId == 0)
                ModelState.AddModelError(nameof(vm.EntidadeId), "Erro ao identificar a unidade logada.");

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync();
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }

            // CORREÇÃO AQUI: Passando o EntidadeId para validar duplicidade apenas na unidade
            if (await _repo.ExisteNumeroAsync(vm.Numero, vm.EntidadeId))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número nesta unidade.");
                await CarregarEntidadesAsync();
                ViewBag.FormAction = "Salvar";
                ViewBag.VersaoAtual = null;
                return View("InstrumentoForm", vm);
            }

            try
            {
                await _repo.InserirAsync(vm);

                await _logService.RegistrarCriacaoAsync("Instrumento", new
                {
                    vm.Numero,
                    vm.Valor,
                    vm.ValorMensal,
                    vm.UsarValorMensal,
                    vm.Objeto,
                    vm.DataInicio,
                    vm.DataFim,
                    vm.Ativo,
                    vm.Vigente, 
                    vm.Observacao,
                    vm.DataAssinatura,
                    vm.EntidadeId
                }, registroId: 0);

                TempData["Sucesso"] = "Instrumento criado com sucesso.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            await CarregarEntidadesAsync();
            ViewBag.FormAction = "Salvar";
            ViewBag.VersaoAtual = null;
            return View("InstrumentoForm", vm);
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        [AutorizarPermissao("INSTRUMENTO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var instrumento = await _repo.ObterPorIdAsync(id);
            if (instrumento is null) return NotFound();

            if (instrumento.EntidadeId != User.ObterEntidadeId())
            {
                return Forbid();
            }

            var vm = new InstrumentoViewModel
            {
                Id = instrumento.Id,
                Numero = instrumento.Numero,
                Valor = instrumento.Valor,
                Objeto = instrumento.Objeto,
                DataInicio = instrumento.DataInicio,
                DataFim = instrumento.DataFim,
                Ativo = instrumento.Ativo,
                Vigente = instrumento.Vigente, 
                Observacao = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId = instrumento.EntidadeId
            };

            await CarregarEntidadesAsync();

            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.VersaoAtual  = versaoAtual;
            ViewBag.FormAction   = "Atualizar";

            ViewBag.ValorVigente  = versaoAtual?.Valor ?? instrumento.Valor;
            ViewBag.InicioVigente = versaoAtual?.VigenciaInicio ?? instrumento.DataInicio;
            ViewBag.FimVigente    = versaoAtual != null ? versaoAtual.VigenciaFim : (DateTime?)instrumento.DataFim;
            ViewBag.ResumoAtual = await _repo.ObterResumoAsync(id);

            return View("InstrumentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("INSTRUMENTO_EDIT")]
        public async Task<IActionResult> Atualizar(int id, InstrumentoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            vm.EntidadeId = User.ObterEntidadeId();

            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            ValidarDatas(vm);

            var meses = CalcularMeses(vm.DataInicio.Date, vm.DataFim.Date);
            if (meses <= 0)
                ModelState.AddModelError(nameof(vm.DataFim), "Período inválido. Ajuste as datas de vigência.");

            if (vm.UsarValorMensal)
            {
                if (!vm.ValorMensal.HasValue || vm.ValorMensal.Value <= 0)
                    ModelState.AddModelError(nameof(vm.ValorMensal), "Informe um valor mensal maior que zero.");

                if (meses > 0 && vm.ValorMensal.HasValue && vm.ValorMensal.Value > 0)
                    vm.Valor = decimal.Round(vm.ValorMensal.Value * meses, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                if (vm.Valor <= 0)
                    ModelState.AddModelError(nameof(vm.Valor), "Informe um valor total maior que zero.");

                if (meses > 0 && vm.Valor > 0)
                    vm.ValorMensal = decimal.Round(vm.Valor / meses, 2, MidpointRounding.AwayFromZero);
            }

            ModelState.Remove(nameof(InstrumentoViewModel.Valor));
            ModelState.Remove(nameof(InstrumentoViewModel.ValorMensal));
            TryValidateModel(vm);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync();
                ViewBag.FormAction = "Atualizar";
                var v = await _versaoRepo.ObterVersaoAtualAsync(id);
                ViewBag.VersaoAtual = v;
                ViewBag.ValorVigente = v?.Valor ?? vm.Valor;
                ViewBag.InicioVigente = v?.VigenciaInicio ?? vm.DataInicio;
                ViewBag.FimVigente = v != null ? v.VigenciaFim : (DateTime?)vm.DataFim;
                ViewBag.ResumoAtual = await _repo.ObterResumoAsync(id);
                return View("InstrumentoForm", vm);
            }

            // CORREÇÃO AQUI: Passando o EntidadeId para validar duplicidade apenas na unidade
            if (await _repo.ExisteNumeroAsync(vm.Numero, vm.EntidadeId, ignorarId: id))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número nesta unidade.");
                await CarregarEntidadesAsync();
                ViewBag.FormAction = "Atualizar";
                var v = await _versaoRepo.ObterVersaoAtualAsync(id);
                ViewBag.VersaoAtual = v;
                ViewBag.ValorVigente = v?.Valor ?? vm.Valor;
                ViewBag.InicioVigente = v?.VigenciaInicio ?? vm.DataInicio;
                ViewBag.FimVigente = v != null ? v.VigenciaFim : (DateTime?)vm.DataFim;
                ViewBag.ResumoAtual = await _repo.ObterResumoAsync(id);
                return View("InstrumentoForm", vm);
            }

            var antes = await _repo.ObterPorIdAsync(id);
            if (antes is null) return NotFound();

            if (antes.EntidadeId != vm.EntidadeId) return Forbid();

            bool nenhumaAlteracao =
                string.Equals((antes.Numero ?? "").Trim(), (vm.Numero ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
                antes.Valor == vm.Valor &&
                string.Equals(antes.Objeto ?? "", vm.Objeto ?? "", StringComparison.Ordinal) &&
                antes.DataInicio.Date == vm.DataInicio.Date &&
                antes.DataFim.Date == vm.DataFim.Date &&
                (antes.DataAssinatura?.Date ?? (DateTime?)null) == (vm.DataAssinatura?.Date ?? (DateTime?)null) &&
                antes.Ativo == vm.Ativo &&
                antes.Vigente == vm.Vigente && 
                string.Equals(antes.Observacao ?? "", vm.Observacao ?? "", StringComparison.Ordinal);

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
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            await CarregarEntidadesAsync();
            ViewBag.FormAction = "Atualizar";
            var v2 = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.VersaoAtual = v2;
            ViewBag.ValorVigente = v2?.Valor ?? vm.Valor;
            ViewBag.InicioVigente = v2?.VigenciaInicio ?? vm.DataInicio;
            ViewBag.FimVigente = v2 != null ? v2.VigenciaFim : (DateTime?)vm.DataFim;
            ViewBag.ResumoAtual = await _repo.ObterResumoAsync(id);
            return View("InstrumentoForm", vm);
        }

        /* ---------- EXCLUIR ---------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("INSTRUMENTO_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            var existente = await _repo.ObterPorIdAsync(id);
            if (existente is null) return NotFound();

            if (existente.EntidadeId != User.ObterEntidadeId()) return Forbid();

            try
            {
                await _repo.ExcluirAsync(id);
                await _logService.RegistrarExclusaoAsync("Instrumento", existente, registroId: id);

                TempData["Sucesso"] = "Instrumento excluido com sucesso.";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Nao foi possivel excluir: ha vinculos relacionados.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro tecnico ao excluir: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        /* ---------- HISTÓRICO (Partial) ---------- */
        [HttpGet]
        public async Task<IActionResult> Historico(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = pag;
            ViewBag.InstrumentoId = id;
            return PartialView("_HistoricoAditivosTable", itens);
        }

        [HttpGet]
        public async Task<IActionResult> HistoricoPagina(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = pag;
            ViewBag.InstrumentoId = id;
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