using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Servicos;

namespace Financeiro.Controllers
{
    public class EnderecosController : Controller
    {
        private readonly IEnderecoService _enderecoService;
        private readonly ILogService _logService;

        /// <summary>
        /// Construtor com injeção de dependências.
        /// </summary>
        public EnderecosController(IEnderecoService enderecoService, ILogService logService)
        {
            _enderecoService = enderecoService;
            _logService = logService;
        }

        /* =========================================================
           NOVO  (GET)
           /Enderecos/Novo?pessoaId=123
        ========================================================= */
        /// <summary>
        /// Exibe o formulário de criação de endereço para uma Pessoa Jurídica.
        /// (Agora permite múltiplos endereços — não redireciona mais para edição.)
        /// </summary>
        [HttpGet]
        public IActionResult Novo(int pessoaId)
        {
            var vm = new EnderecoViewModel { PessoaJuridicaId = pessoaId };
            return View("EnderecoForm", vm);
        }

        /* =========================================================
           SALVAR  (POST) — insere e vincula
        ========================================================= */
        /// <summary>
        /// Insere um novo endereço e cria o vínculo com a Pessoa Jurídica.
        /// Se a PJ não tiver principal, este será marcado como principal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(EnderecoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("EnderecoForm", vm);

            await _enderecoService.InserirAsync(vm);

            // (Opcional futuro) RegistrarCriacaoAsync("Endereco", objetoComId, id);
            return RedirectToAction("Index", "PessoasJuridicas");
        }

        /* =========================================================
           EDITAR  (GET)
           /Enderecos/Editar?pessoaId=123
        ========================================================= */
        /// <summary>
        /// Exibe o formulário de edição do endereço "legado" (primeiro/único) da PJ.
        /// Observação: para múltiplos endereços, edições específicas virão depois.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Editar(int pessoaId)
        {
            var endereco = await _enderecoService.ObterPorPessoaAsync(pessoaId);
            if (endereco is null) // se não existir, volta para Novo
                return RedirectToAction("Novo", new { pessoaId });

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
                Uf = endereco.Uf
            };

            return View("EnderecoForm", vm);
        }

        /* =========================================================
           ATUALIZAR  (POST)
        ========================================================= */
        /// <summary>
        /// Atualiza os dados de um endereço existente.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, EnderecoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View("EnderecoForm", vm);

            await _enderecoService.AtualizarAsync(id, vm);

            // (Opcional futuro) RegistrarEdicaoAsync("Endereco", antes, depois, id);
            return RedirectToAction("Index", "PessoasJuridicas");
        }

        /* ================== ENDEREÇOS — PESSOA JURÍDICA ================== */

        /// <summary>
        /// Retorna todos os endereços vinculados à Pessoa Jurídica (principal primeiro).
        /// </summary>
        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/Listar")]
        public async Task<IActionResult> ListarPorPessoaJuridica(int pessoaJuridicaId)
        {
            var lista = await _enderecoService.ListarPorPessoaJuridicaAsync(pessoaJuridicaId);
            var principal = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

            var itens = lista.Select(e => new
            {
                e.Id,
                e.Logradouro,
                e.Numero,
                e.Complemento,
                Cep = e.Cep,
                e.Bairro,
                e.Municipio,
                e.Uf,
                Principal = principal != null && principal.Id == e.Id
            });

            return Json(new { sucesso = true, itens });
        }

        /// <summary>
        /// Retorna o endereço principal da Pessoa Jurídica (se houver).
        /// </summary>
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
                    principal.Id,
                    principal.Logradouro,
                    principal.Numero,
                    principal.Complemento,
                    Cep = principal.Cep,
                    principal.Bairro,
                    principal.Municipio,
                    principal.Uf
                }
            });
        }

        /// <summary>
        /// Define um endereço como principal para a Pessoa Jurídica (troca atômica) e registra logs.
        /// </summary>
        [HttpPost("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirPrincipalPessoaJuridica(int pessoaJuridicaId, int enderecoId)
        {
            try
            {
                // Estado anterior p/ log
                var principalAntes = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

                // Troca atômica
                await _enderecoService.DefinirPrincipalPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);

                // Estado posterior p/ log
                var principalDepois = await _enderecoService.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

                // LOG: alteração no vínculo principal (PessoaEndereco)
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
        /// <summary>
        /// Abre a tela de gerenciamento de endereços da Pessoa Jurídica.
        /// Rota: /Enderecos/PessoaJuridica/{pessoaJuridicaId}
        /// </summary>
        [HttpGet("Enderecos/PessoaJuridica/{pessoaJuridicaId:int}")]
        public IActionResult GerenciarPessoaJuridica(int pessoaJuridicaId)
        {
            // A view "ListaPessoaJuridica.cshtml" espera o Id (int) como model
            return View("ListaPessoaJuridica", pessoaJuridicaId);
        }
    }
}