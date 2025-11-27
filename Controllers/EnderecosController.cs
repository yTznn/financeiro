using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Controllers
{
    [Authorize]
    public class EnderecosController : Controller
    {
        private readonly IEnderecoService _enderecoService;
        private readonly ILogService _logService;

        public EnderecosController(IEnderecoService enderecoService, ILogService logService)
        {
            _enderecoService = enderecoService;
            _logService = logService;
        }

        /* =========================================================
           NOVO (GET) — PJ
           /Enderecos/Novo?pessoaId=123
        ========================================================= */
        [HttpGet]
        public IActionResult Novo(int pessoaId)
        {
            var vm = new EnderecoViewModel { PessoaJuridicaId = pessoaId };
            return View("EnderecoForm", vm);
        }

        /* =========================================================
           NOVO (GET) — PF
           /Enderecos/NovoPF?pessoaId=123
        ========================================================= */
        [HttpGet]
        public IActionResult NovoPF(int pessoaId)
        {
            var vm = new EnderecoViewModel { PessoaFisicaId = pessoaId };
            return View("EnderecoForm", vm);
        }

        /* =========================================================
           SALVAR (POST) — insere e vincula — PF ou PJ
        ========================================================= */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(EnderecoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("EnderecoForm", vm);

            // 1) Insere Endereco e obtém Id
            var enderecoId = await _enderecoService.InserirRetornandoIdAsync(new Endereco
            {
                Logradouro  = vm.Logradouro,
                Numero      = vm.Numero,
                Complemento = vm.Complemento,
                Cep         = vm.Cep,
                Bairro      = vm.Bairro,
                Municipio   = vm.Municipio,
                Uf          = vm.Uf
            });

            // 2) PJ ou PF?
            if (vm.PessoaJuridicaId > 0)
            {
                await _enderecoService.VincularPessoaJuridicaAsync(vm.PessoaJuridicaId, enderecoId);

                var jaTemPrincipal = await _enderecoService.PossuiPrincipalPessoaJuridicaAsync(vm.PessoaJuridicaId);
                if (vm.DefinirPrincipal || !jaTemPrincipal)
                    await _enderecoService.DefinirPrincipalPessoaJuridicaAsync(vm.PessoaJuridicaId, enderecoId);

                TempData["toast:success"] = "Endereço cadastrado com sucesso.";
                return RedirectToAction(nameof(GerenciarPessoaJuridica), new { pessoaJuridicaId = vm.PessoaJuridicaId });
            }
            else if (vm.PessoaFisicaId.HasValue && vm.PessoaFisicaId.Value > 0)
            {
                await _enderecoService.VincularPessoaFisicaAsync(vm.PessoaFisicaId.Value, enderecoId);

                var jaTemPrincipal = await _enderecoService.PossuiPrincipalPessoaFisicaAsync(vm.PessoaFisicaId.Value);
                if (vm.DefinirPrincipal || !jaTemPrincipal)
                    await _enderecoService.DefinirPrincipalPessoaFisicaAsync(vm.PessoaFisicaId.Value, enderecoId);

                TempData["toast:success"] = "Endereço cadastrado com sucesso.";
                return RedirectToAction(nameof(GerenciarPessoaFisica), new { pessoaFisicaId = vm.PessoaFisicaId.Value });
            }

            return BadRequest("Contexto inválido: informe PessoaJuridicaId ou PessoaFisicaId.");
        }

        /* =========================================================
           EDITAR (GET) — PJ (legado)
           /Enderecos/Editar?pessoaId=123
        ========================================================= */
        [HttpGet]
        public async Task<IActionResult> Editar(int pessoaId)
        {
            var endereco = await _enderecoService.ObterPorPessoaAsync(pessoaId);
            if (endereco is null)
                return RedirectToAction("Novo", new { pessoaId });

            var principal = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaId);

            var vm = new EnderecoViewModel
            {
                Id = endereco.Id,
                PessoaJuridicaId = pessoaId,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Cep = endereco.Cep,
                Bairro = endereco.Bairro,
                Municipio = endereco.Municipio,
                Uf = endereco.Uf,
                EhPrincipal = principal != null && principal.Id == endereco.Id
            };

            return View("EnderecoForm", vm);
        }

        /* =========================================================
           ATUALIZAR (POST)
        ========================================================= */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, EnderecoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("EnderecoForm", vm);

            await _enderecoService.AtualizarAsync(id, vm);

            TempData["toast:success"] = "Endereço atualizado com sucesso.";

            if (vm.PessoaJuridicaId > 0)
                return RedirectToAction(nameof(GerenciarPessoaJuridica), new { pessoaJuridicaId = vm.PessoaJuridicaId });

            if (vm.PessoaFisicaId.HasValue && vm.PessoaFisicaId.Value > 0)
                return RedirectToAction(nameof(GerenciarPessoaFisica), new { pessoaFisicaId = vm.PessoaFisicaId.Value });

            TempData["toast:info"] = "Atualização concluída.";
            return RedirectToAction("Index", "PessoasJuridicas");
        }

        /* ================== ENDEREÇOS — PESSOA JURÍDICA ================== */

        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/Listar")]
        public async Task<IActionResult> ListarPorPessoaJuridica(int pessoaJuridicaId)
        {
            var lista = await _enderecoService.ListarPorPessoaJuridicaAsync(pessoaJuridicaId);
            var principal = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

            var itens = lista.Select(e => new
            {
                id = e.Id,
                logradouro = e.Logradouro,
                numero = e.Numero,
                complemento = e.Complemento,
                cep = e.Cep,
                bairro = e.Bairro,
                municipio = e.Municipio,
                uf = e.Uf,
                principal = principal != null && principal.Id == e.Id
            });

            return Json(new { sucesso = true, itens });
        }

        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/Principal")]
        public async Task<IActionResult> PrincipalPorPessoaJuridica(int pessoaJuridicaId)
        {
            var principal = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);
            if (principal == null)
                return Json(new { sucesso = true, possuiPrincipal = false });

            return Json(new
            {
                sucesso = true,
                possuiPrincipal = true,
                endereco = new
                {
                    id = principal.Id,
                    logradouro = principal.Logradouro,
                    numero = principal.Numero,
                    complemento = principal.Complemento,
                    cep = principal.Cep,
                    bairro = principal.Bairro,
                    municipio = principal.Municipio,
                    uf = principal.Uf
                }
            });
        }

        [HttpPost("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirPrincipalPessoaJuridica(int pessoaJuridicaId, int enderecoId)
        {
            try
            {
                var principalAntes = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);
                await _enderecoService.DefinirPrincipalPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);
                var principalDepois = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

                await _logService.RegistrarEdicaoAsync(
                    "PessoaEndereco",
                    principalAntes,
                    principalDepois,
                    registroId: enderecoId
                );

                return Json(new { sucesso = true, mensagem = "Endereço definido como principal!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}")]
        public IActionResult GerenciarPessoaJuridica(int pessoaJuridicaId)
        {
            return View("ListaPessoaJuridica", pessoaJuridicaId);
        }

        /* ================== ENDEREÇOS — PESSOA FÍSICA ================== */

        [HttpGet("Enderecos/PessoaFisica/{pessoaFisicaId:int}/Listar")]
        public async Task<IActionResult> ListarPorPessoaFisica(int pessoaFisicaId)
        {
            var lista     = await _enderecoService.ListarPorPessoaFisicaAsync(pessoaFisicaId);
            var principal = await _enderecoService.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);
            var principalId = principal?.Id;

            var itens = lista.Select(e => new
            {
                id          = e.Id,
                logradouro  = e.Logradouro,
                numero      = e.Numero,
                complemento = e.Complemento,
                cep         = e.Cep,
                bairro      = e.Bairro,
                municipio   = e.Municipio,
                uf          = e.Uf,
                principal   = principalId.HasValue && principalId.Value == e.Id
            });

            return Json(new { sucesso = true, principalId, itens });
        }

        [HttpGet("Enderecos/PessoaFisica/{pessoaFisicaId:int}/Principal")]
        public async Task<IActionResult> PrincipalPorPessoaFisica(int pessoaFisicaId)
        {
            var principal = await _enderecoService.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);
            if (principal == null)
                return Json(new { sucesso = true, possuiPrincipal = false });

            return Json(new
            {
                sucesso = true,
                possuiPrincipal = true,
                endereco = new
                {
                    id = principal.Id,
                    logradouro = principal.Logradouro,
                    numero = principal.Numero,
                    complemento = principal.Complemento,
                    cep = principal.Cep,
                    bairro = principal.Bairro,
                    municipio = principal.Municipio,
                    uf = principal.Uf
                }
            });
        }

        [HttpPost("Enderecos/PessoaFisica/{pessoaFisicaId:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirPrincipalPessoaFisica(int pessoaFisicaId, int enderecoId)
        {
            try
            {
                var principalAntes = await _enderecoService.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);
                await _enderecoService.DefinirPrincipalPessoaFisicaAsync(pessoaFisicaId, enderecoId);
                var principalDepois = await _enderecoService.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);

                await _logService.RegistrarEdicaoAsync(
                    "PessoaEndereco",
                    principalAntes,
                    principalDepois,
                    registroId: enderecoId
                );

                return Json(new { sucesso = true, mensagem = "Endereço definido como principal!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        [HttpGet("Enderecos/PessoaFisica/{pessoaFisicaId:int}")]
        public IActionResult GerenciarPessoaFisica(int pessoaFisicaId)
        {
            return View("ListaPessoaFisica", pessoaFisicaId);
        }

        // ============== EDITAR (GET) — por EnderecoId — PJ ==============
        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/Editar/{enderecoId:int}")]
        public async Task<IActionResult> EditarEnderecoPJ(int pessoaJuridicaId, int enderecoId)
        {
            var endereco = await _enderecoService.ObterPorIdAsync(enderecoId);
            if (endereco == null)
                return NotFound();

            var principal = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

            var vm = new EnderecoViewModel
            {
                Id = endereco.Id,
                PessoaJuridicaId = pessoaJuridicaId,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Cep = endereco.Cep,
                Bairro = endereco.Bairro,
                Municipio = endereco.Municipio,
                Uf = endereco.Uf,
                EhPrincipal = principal != null && principal.Id == endereco.Id
            };

            return View("EnderecoForm", vm);
        }

        // ============== EXCLUIR (POST) — por EnderecoId — PJ ==============
        [HttpPost("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/Excluir/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPessoaJuridica(int pessoaJuridicaId, int enderecoId)
        {
            try
            {
                await _enderecoService.ExcluirEnderecoPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);
                return Json(new { sucesso = true, mensagem = "Endereço excluído com sucesso." });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        // ============== EDITAR (GET) — por EnderecoId — PF ==============
        [HttpGet("Enderecos/PessoaFisica/{pessoaFisicaId:int}/Editar/{enderecoId:int}")]
        public async Task<IActionResult> EditarEnderecoPF(int pessoaFisicaId, int enderecoId)
        {
            var endereco = await _enderecoService.ObterPorIdAsync(enderecoId);
            if (endereco == null)
                return NotFound();

            var principal = await _enderecoService.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);

            var vm = new EnderecoViewModel
            {
                Id = endereco.Id,
                PessoaFisicaId = pessoaFisicaId,
                Logradouro = endereco.Logradouro,
                Numero = endereco.Numero,
                Complemento = endereco.Complemento,
                Cep = endereco.Cep,
                Bairro = endereco.Bairro,
                Municipio = endereco.Municipio,
                Uf = endereco.Uf,
                EhPrincipal = principal != null && principal.Id == endereco.Id
            };

            return View("EnderecoForm", vm);
        }

        // ============== EXCLUIR (POST) — por EnderecoId — PF ==============
        [HttpPost("Enderecos/PessoaFisica/{pessoaFisicaId:int}/Excluir/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPessoaFisica(int pessoaFisicaId, int enderecoId)
        {
            try
            {
                await _enderecoService.ExcluirEnderecoPessoaFisicaAsync(pessoaFisicaId, enderecoId);
                return Json(new { sucesso = true, mensagem = "Endereço excluído com sucesso." });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }
    }
}