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

        // remove emojis, símbolos indesejados etc.
        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

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

        // DATEDIFF(MONTH) + 1 (inclui mês de início e de fim)
        private static int CalcularMeses(DateTime inicio, DateTime fim)
        {
            return ((fim.Year - inicio.Year) * 12) + (fim.Month - inicio.Month) + 1;
        }
        /* ---------- LISTAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Resumo consolidado (valor total/mensal atual, vigência atual e saldo)
            var listaResumo = await _repo.ListarResumoAsync();

            // Dicionários para a View:
            // - Último texto de aditivo de prazo
            // - Valor vigente (da versão atual)
            var ultimosPrazo    = new Dictionary<int, string>();
            var valoresVigentes = new Dictionary<int, decimal>();

            foreach (var r in listaResumo)
            {
                // Valor vigente: usa a versão vigente (se existir)
                var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(r.InstrumentoId);
                if (versaoAtual != null)
                    valoresVigentes[r.InstrumentoId] = versaoAtual.Valor;

                // Detecta o último aditivo que alterou o PRAZO comparando fim de vigência com a versão anterior
                var historico = (await _versaoRepo.ListarPorInstrumentoAsync(r.InstrumentoId))
                                .OrderByDescending(v => v.Versao)
                                .ToList();

                for (int i = 0; i < historico.Count - 1; i++)
                {
                    var atual    = historico[i];
                    var anterior = historico[i + 1];

                    // Considera mudança de prazo se o FIM mudou (tratando null como "aberto")
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
                            // Antes era aberto, agora definiu fim
                            txtDelta = $"definiu fim para {atual.VigenciaFim.Value:dd/MM/yyyy}";
                        }
                        else if (!atual.VigenciaFim.HasValue && anterior.VigenciaFim.HasValue)
                        {
                            // Antes tinha fim, agora ficou aberto
                            txtDelta = "removeu fim (vigência aberta)";
                        }
                        else
                        {
                            txtDelta = "ajuste de prazo";
                        }

                        var quando = (atual.DataAssinatura ?? atual.VigenciaInicio).ToString("dd/MM/yyyy");
                        ultimosPrazo[r.InstrumentoId] = $"{txtDelta} em {quando}";
                        break; // já achamos o mais recente que mexeu no prazo
                    }
                }
            }

            ViewBag.UltimosPrazo    = ultimosPrazo;
            ViewBag.ValoresVigentes = valoresVigentes;

            return View(listaResumo);
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

            var vm = new InstrumentoViewModel
            {
                Ativo = true,
                DataInicio = hoje,
                DataFim = hoje,
                DataAssinatura = hoje,
                Numero = numeroSugerido ?? string.Empty
            };

            return View("InstrumentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(InstrumentoViewModel vm)
        {
            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            // Valida datas base
            ValidarDatas(vm);

            // Converte mensal ↔ total conforme seleção
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

            // ✅ Revalidação após conversão (evita erro de “Valor deve ser maior que zero” quando usa mensal)
            ModelState.Remove(nameof(InstrumentoViewModel.Valor));
            ModelState.Remove(nameof(InstrumentoViewModel.ValorMensal));
            TryValidateModel(vm);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");
            if (vm.EntidadeId == 0)
                ModelState.AddModelError(nameof(vm.EntidadeId), "Selecione a Entidade.");

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
                    vm.Numero,
                    vm.Valor,
                    vm.ValorMensal,
                    vm.UsarValorMensal,
                    vm.Objeto,
                    vm.DataInicio,
                    vm.DataFim,
                    vm.Ativo,
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
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
            }
            catch
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            await CarregarEntidadesAsync(vm.EntidadeId);
            ViewBag.FormAction = "Salvar";
            ViewBag.VersaoAtual = null;
            return View("InstrumentoForm", vm);
        }
        /* ---------- EDITAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var instrumento = await _repo.ObterPorIdAsync(id);
            if (instrumento is null) return NotFound();

            var vm = new InstrumentoViewModel
            {
                Id = instrumento.Id,
                Numero = instrumento.Numero,
                Valor = instrumento.Valor,
                Objeto = instrumento.Objeto,
                DataInicio = instrumento.DataInicio,
                DataFim = instrumento.DataFim,
                Ativo = instrumento.Ativo,
                Observacao = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId = instrumento.EntidadeId
            };

            await CarregarEntidadesAsync(vm.EntidadeId);

            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.VersaoAtual  = versaoAtual;
            ViewBag.FormAction   = "Atualizar";

            // Vigência/valor (compatibilidade visual que você já usava)
            ViewBag.ValorVigente  = versaoAtual?.Valor ?? instrumento.Valor;
            ViewBag.InicioVigente = versaoAtual?.VigenciaInicio ?? instrumento.DataInicio;
            ViewBag.FimVigente    = versaoAtual != null ? versaoAtual.VigenciaFim : (DateTime?)instrumento.DataFim;

            // **NOVO**: resumo consolidado para card read-only no Form
            var resumo = await _repo.ObterResumoAsync(id);
            ViewBag.ResumoAtual = resumo;

            return View("InstrumentoForm", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, InstrumentoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            vm.Numero     = NormalizarNumero(Sanitize(vm.Numero));
            vm.Objeto     = Sanitize(vm.Objeto);
            vm.Observacao = Sanitize(vm.Observacao);

            // Valida datas base
            ValidarDatas(vm);

            // Converte mensal ↔ total conforme seleção
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

            // ✅ Revalidação após conversão
            ModelState.Remove(nameof(InstrumentoViewModel.Valor));
            ModelState.Remove(nameof(InstrumentoViewModel.ValorMensal));
            TryValidateModel(vm);

            if (string.IsNullOrWhiteSpace(vm.Numero))
                ModelState.AddModelError(nameof(vm.Numero), "Informe o número do instrumento.");
            if (vm.EntidadeId == 0)
                ModelState.AddModelError(nameof(vm.EntidadeId), "Selecione a Entidade.");

            if (!ModelState.IsValid)
            {
                await CarregarEntidadesAsync(vm.EntidadeId);
                ViewBag.FormAction = "Atualizar";
                var v = await _versaoRepo.ObterVersaoAtualAsync(id);
                ViewBag.VersaoAtual = v;
                ViewBag.ValorVigente = v?.Valor ?? vm.Valor;
                ViewBag.InicioVigente = v?.VigenciaInicio ?? vm.DataInicio;
                ViewBag.FimVigente = v != null ? v.VigenciaFim : (DateTime?)vm.DataFim;
                ViewBag.ResumoAtual = await _repo.ObterResumoAsync(id);
                return View("InstrumentoForm", vm);
            }

            if (await _repo.ExisteNumeroAsync(vm.Numero, ignorarId: id))
            {
                ModelState.AddModelError(nameof(vm.Numero), "Já existe um instrumento com este número.");
                await CarregarEntidadesAsync(vm.EntidadeId);
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
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
            }
            catch
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            await CarregarEntidadesAsync(vm.EntidadeId);
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
        // Agora Historico aceita 'pag' e devolve a TABELA paginada (_HistoricoAditivosTable)
        [HttpGet]
        public async Task<IActionResult> Historico(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = pag;
            ViewBag.InstrumentoId = id;
            return PartialView("_HistoricoAditivosTable", itens);
        }

        // (Opcional) mantém a rota antiga, se alguém ainda usar
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