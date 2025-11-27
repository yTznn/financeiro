using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Controllers
{
    [Authorize]
    public class ContasBancariasController : Controller
    {
        private readonly IContaBancariaRepositorio _repo;
        private readonly ILogService _logService;

        public ContasBancariasController(IContaBancariaRepositorio repo, ILogService logService)
        {
            _repo = repo;
            _logService = logService;
        }

        // ===================== LISTAR =====================
        [HttpGet("ContasBancarias/PessoaFisica/{pessoaId:int}/Listar")]
        public async Task<IActionResult> ListarPF(int pessoaId)
        {
            var contas = await _repo.ListarPorPessoaFisicaAsync(pessoaId);
            return Json(new { sucesso = true, total = contas.Count(), contas });
        }

        [HttpGet("ContasBancarias/PessoaJuridica/{pessoaId:int}/Listar")]
        public async Task<IActionResult> ListarPJ(int pessoaId)
        {
            var contas = await _repo.ListarPorPessoaJuridicaAsync(pessoaId);
            return Json(new { sucesso = true, total = contas.Count(), contas });
        }

        // ===================== NOVO =====================
        [HttpGet]
        public IActionResult Novo(int pessoaId, bool pf = false)
        {
            var vm = new ContaBancariaViewModel
            {
                PessoaJuridicaId = pf ? null : pessoaId,
                PessoaFisicaId = pf ? pessoaId : null,
                IsPrincipal = false
            };
            return View("ContaForm", vm);
        }

        // ===================== SALVAR =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(ContaBancariaViewModel vm, bool pf = false)
        {
            if (!ModelState.IsValid) return View("ContaForm", vm);

            try
            {
                var vinculoId = await _repo.InserirEVincularAsync(vm);

                await TentarRegistrarLog(() =>
                    _logService.RegistrarCriacaoAsync("PessoaConta", vm, vinculoId)
                );

                TempData["Sucesso"] = "Conta bancária cadastrada com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Falha ao cadastrar a conta bancária.";
                await TentarRegistrarLog(() =>
                    _logService.RegistrarCriacaoAsync("PessoaConta", new { vm, Erro = ex.Message })
                );
            }

            return pf
                ? RedirectToAction("Index", "PessoasFisicas")
                : RedirectToAction("Index", "PessoasJuridicas");
        }

        // ===================== EDITAR =====================
        [HttpGet]
        public async Task<IActionResult> Editar(int vinculoId)
        {
            var vinculo = await _repo.ObterVinculoPorIdAsync(vinculoId);
            if (vinculo == null)
            {
                TempData["Erro"] = "Vínculo não encontrado.";
                return RedirectToAction("Index", "PessoasJuridicas");
            }

            var vm = new ContaBancariaViewModel
            {
                Id = vinculo.Id,
                VinculoId = vinculo.VinculoId,
                Banco = vinculo.Banco,
                Agencia = vinculo.Agencia,
                Conta = vinculo.Conta,
                ChavePix = vinculo.ChavePix,
                IsPrincipal = vinculo.IsPrincipal,
                PessoaFisicaId = vinculo.PessoaFisicaId,
                PessoaJuridicaId = vinculo.PessoaJuridicaId
            };

            return View("ContaForm", vm);
        }

        // ===================== ATUALIZAR =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, ContaBancariaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("ContaForm", vm);

            try
            {
                var antes = await _repo.ObterContaPorIdAsync(id);

                await _repo.AtualizarContaAsync(id, vm);

                await TentarRegistrarLog(() =>
                    _logService.RegistrarEdicaoAsync("ContaBancaria", antes, vm, id)
                );

                if (vm.IsPrincipal && vm.VinculoId.HasValue)
                {
                    await TentarRegistrarLog(() =>
                        _logService.RegistrarEdicaoAsync(
                            "PessoaConta",
                            new { IsPrincipal = false },
                            new { IsPrincipal = true },
                            vm.VinculoId.Value
                        )
                    );
                }

                TempData["Sucesso"] = "Conta bancária atualizada com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Falha ao atualizar a conta bancária.";
                await TentarRegistrarLog(() =>
                    _logService.RegistrarEdicaoAsync("ContaBancaria", null, new { vm, Erro = ex.Message }, id)
                );
            }

            if (vm.PessoaFisicaId.HasValue)
                return RedirectToAction("Index", "PessoasFisicas");

            return RedirectToAction("Index", "PessoasJuridicas");
        }

        // ===================== DEFINIR PRINCIPAL =====================
        [HttpPost("ContasBancarias/{vinculoId:int}/DefinirPrincipal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirPrincipal(int vinculoId)
        {
            try
            {
                var vinculoAntes = await _repo.ObterVinculoPorIdAsync(vinculoId);
                if (vinculoAntes == null)
                    return Json(new { sucesso = false, mensagem = "Vínculo não encontrado." });

                await _repo.DefinirPrincipalAsync(vinculoId);

                await TentarRegistrarLog(() =>
                    _logService.RegistrarEdicaoAsync(
                        "PessoaConta",
                        new { vinculoAntes.IsPrincipal },
                        new { IsPrincipal = true },
                        vinculoId
                    )
                );

                TempData["Sucesso"] = "Conta principal definida com sucesso.";
                return Json(new { sucesso = true });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Falha ao definir a conta principal.";
                await TentarRegistrarLog(() =>
                    _logService.RegistrarEdicaoAsync("PessoaConta", null, new { vinculoId, Erro = ex.Message }, vinculoId)
                );
                return Json(new { sucesso = false, mensagem = "Erro ao definir principal." });
            }
        }

        // ===================== REMOVER VÍNCULO =====================
        [HttpPost("ContasBancarias/{vinculoId:int}/Remover")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverVinculo(int vinculoId, bool removerContaSeOrfa = false)
        {
            try
            {
                var vinculoAntes = await _repo.ObterVinculoPorIdAsync(vinculoId);
                if (vinculoAntes == null)
                    return Json(new { sucesso = false, mensagem = "Vínculo não encontrado." });

                await _repo.RemoverVinculoAsync(vinculoId, removerContaSeOrfa);

                await TentarRegistrarLog(() =>
                    _logService.RegistrarExclusaoAsync("PessoaConta", vinculoAntes, vinculoId)
                );

                TempData["Sucesso"] = "Vínculo removido com sucesso.";
                return Json(new { sucesso = true });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Falha ao remover o vínculo.";
                await TentarRegistrarLog(() =>
                    _logService.RegistrarExclusaoAsync("PessoaConta", new { vinculoId, Erro = ex.Message }, vinculoId)
                );
                return Json(new { sucesso = false, mensagem = "Erro ao remover vínculo." });
            }
        }

        // ===================== HELPER =====================
        private async Task TentarRegistrarLog(Func<Task> acaoLog)
        {
            try
            {
                await acaoLog();
            }
            catch
            {
                // Nunca deixar o log quebrar a execução
            }
        }
        // ContasBancariasController (adições)
        // === PRINCIPAL PF ===
        [HttpGet("ContasBancarias/PessoaFisica/{pessoaId:int}/Principal")]
        public async Task<IActionResult> PrincipalPF(int pessoaId)
        {
            var contas = await _repo.ListarPorPessoaFisicaAsync(pessoaId);
            var principal = contas?.FirstOrDefault(c => c.IsPrincipal); // IsPrincipal é bool
            return Json(new { sucesso = true, possuiPrincipal = principal != null, conta = principal });
        }

        // === PRINCIPAL PJ ===
        [HttpGet("ContasBancarias/PessoaJuridica/{pessoaId:int}/Principal")]
        public async Task<IActionResult> PrincipalPJ(int pessoaId)
        {
            var contas = await _repo.ListarPorPessoaJuridicaAsync(pessoaId);
            var principal = contas?.FirstOrDefault(c => c.IsPrincipal); // IsPrincipal é bool
            return Json(new { sucesso = true, possuiPrincipal = principal != null, conta = principal });
        }

        // === INDEXES (apenas para renderizar as telas que já recebem o id como model) ===
        [HttpGet]
        public IActionResult IndexPF(int pessoaId) => View("IndexPF", pessoaId);

        [HttpGet]
        public IActionResult IndexPJ(int pessoaId) => View("IndexPJ", pessoaId);

    }
}