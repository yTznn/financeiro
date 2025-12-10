using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Servicos;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos; // <--- Importante para [AutorizarPermissao]

namespace Financeiro.Controllers
{
    [Authorize]
    [Route("Entidades")]
    public class EntidadesController : Controller
    {
        private const int TAMANHO_PAGINA = 3; // <--- Paginação de 4 itens

        private readonly IEntidadeService _service;
        private readonly IEntidadeRepositorio _repositorio; // <--- Injetado para paginação direta
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        // Endereços
        private readonly IEntidadeEnderecoService _entidadeEnderecoService;
        private readonly IEntidadeEnderecoRepositorio _entidadeEnderecoRepositorio;
        private readonly IEnderecoRepositorio _enderecoRepositorio;

        public EntidadesController(
            IEntidadeService service,
            IEntidadeRepositorio repositorio, // <--- Injetado
            IMapper mapper,
            ILogService logService,
            IEntidadeEnderecoService entidadeEnderecoService,
            IEntidadeEnderecoRepositorio entidadeEnderecoRepositorio,
            IEnderecoRepositorio enderecoRepositorio)
        {
            _service = service;
            _repositorio = repositorio;
            _mapper = mapper;
            _logService = logService;
            _entidadeEnderecoService = entidadeEnderecoService;
            _entidadeEnderecoRepositorio = entidadeEnderecoRepositorio;
            _enderecoRepositorio = enderecoRepositorio;
        }

        /* ========================== LISTAGEM ========================== */

        [HttpGet("")]
        [AutorizarPermissao("ENTIDADE_VIEW")] // <--- Proteção
        public async Task<IActionResult> Index(int p = 1)
        {
            if (p < 1) p = 1;

            // Busca paginada direto do repositório
            var (itens, totalItens) = await _repositorio.ListarPaginadoAsync(p, TAMANHO_PAGINA);

            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalItens / TAMANHO_PAGINA);

            return View(itens);
        }

        /* =========================== CREATE =========================== */

        [HttpGet("Create")]
        [AutorizarPermissao("ENTIDADE_ADD")]
        public IActionResult Create() => View("Form", new EntidadeViewModel());

        [HttpPost("Create")]
        [AutorizarPermissao("ENTIDADE_ADD")]
        public async Task<IActionResult> Create([FromForm] EntidadeViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

            try
            {
                var entidade = _mapper.Map<Entidade>(vm);
                await _service.CriarAsync(entidade); // Serviço cuida de regras de negócio

                await _logService.RegistrarCriacaoAsync("Entidade", entidade, entidade.Id);

                return Json(new { sucesso = true, mensagem = "Entidade criada com sucesso!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* ============================ EDIT ============================ */

        [HttpGet("Edit/{id:int}")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade == null) return NotFound();

            var vm = _mapper.Map<EntidadeViewModel>(entidade);
            return View("Form", vm);
        }

        [HttpPost("Edit/{id:int}")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> Edit(int id, [FromForm] EntidadeViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

            try
            {
                var entidadeAntiga = await _service.ObterPorIdAsync(id);
                if (entidadeAntiga == null) return NotFound();

                var entidadeAtualizada = _mapper.Map<Entidade>(vm);
                entidadeAtualizada.Id = id;

                await _service.AtualizarAsync(entidadeAtualizada);

                await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntiga, entidadeAtualizada, id);

                return Json(new { sucesso = true, mensagem = "Entidade atualizada!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* =========================== DELETE =========================== */

        [HttpPost("Delete/{id:int}")]
        [AutorizarPermissao("ENTIDADE_DEL")]
        public async Task<IActionResult> Delete(int id)
        {
            var entidadeAntiga = await _service.ObterPorIdAsync(id);
            if (entidadeAntiga == null) return NotFound();

            try 
            {
                await _service.ExcluirAsync(id);
                await _logService.RegistrarExclusaoAsync("Entidade", entidadeAntiga, id);
                return Json(new { sucesso = true, mensagem = "Entidade excluída!" });
            }
            catch (Exception ex)
            {
                 // Captura erro de FK se houver
                 return BadRequest(new { sucesso = false, mensagem = "Não foi possível excluir (possui vínculos). Detalhes: " + ex.Message });
            }
        }

        /* ========================= AUTO-FILL ========================== */

        [HttpGet("AutoFill")]
        [AutorizarPermissao("ENTIDADE_ADD")] // Quem pode criar, pode usar autofill
        public async Task<IActionResult> AutoFill(string cnpj)
        {
            var dto = await _service.AutoFillPorCnpjAsync(cnpj);
            return Json(dto);
        }

        /* =================== ENDEREÇOS DA ENTIDADE ==================== */
        // Mantive as permissões de EDIÇÃO aqui, pois gerenciar endereço é editar a entidade

        [HttpGet("Enderecos/{id:int}/Gerenciar")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public IActionResult GerenciarEnderecos(int id)
        {
            return View("Enderecos", id);
        }

        [HttpGet("Enderecos/{id:int}")]
        [AutorizarPermissao("ENTIDADE_VIEW")]
        public async Task<IActionResult> Enderecos(int id)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade == null) return NotFound();

            var lista = await _entidadeEnderecoService.ListarPorEntidadeAsync(id);
            var principal = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);

            var itens = lista.Select(e => new
            {
                e.Id,
                e.Logradouro,
                e.Numero,
                e.Complemento,
                e.Cep,
                e.Bairro,
                e.Municipio,
                e.Uf,
                Principal = principal != null && principal.Id == e.Id
            });

            return Json(new { sucesso = true, itens });
        }

        [HttpGet("EnderecoPrincipal/{id:int}")]
        [AutorizarPermissao("ENTIDADE_VIEW")]
        public async Task<IActionResult> EnderecoPrincipal(int id)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade == null) return NotFound();

            var principal = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);
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
                    principal.Cep,
                    principal.Bairro,
                    principal.Municipio,
                    principal.Uf
                }
            });
        }

        [HttpPost("Enderecos/{id:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> DefinirPrincipal(int id, int enderecoId)
        {
            try
            {
                var entidadeAntes = await _service.ObterPorIdAsync(id);
                if (entidadeAntes == null) return NotFound();

                var principalAntes = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);

                await _entidadeEnderecoService.DefinirPrincipalEntidadeAsync(id, enderecoId);

                var entidadeDepois = await _service.ObterPorIdAsync(id);
                var principalDepois = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);

                await _logService.RegistrarEdicaoAsync("EntidadeEndereco", principalAntes, principalDepois, registroId: enderecoId);
                await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntes, entidadeDepois, registroId: id);

                return Json(new { sucesso = true, mensagem = "Endereço definido como principal!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        [HttpGet("Enderecos/{id:int}/Novo")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public IActionResult NovoEnderecoEntidade(int id)
        {
            return View("EnderecoEntidadeForm", id);
        }

        [HttpPost("SalvarEnderecoEntidade/{id:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> SalvarEnderecoEntidade(int id, [FromForm] EnderecoViewModel vm, [FromForm] bool? DefinirPrincipal)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade is null) return NotFound();

            var novoEnderecoId = await _enderecoRepositorio.InserirRetornandoIdAsync(new Endereco
            {
                Logradouro  = vm.Logradouro,
                Numero      = vm.Numero,
                Complemento = vm.Complemento,
                Cep         = vm.Cep,
                Bairro      = vm.Bairro,
                Municipio   = vm.Municipio,
                Uf          = vm.Uf
            });

            await _entidadeEnderecoRepositorio.VincularAsync(id, novoEnderecoId, ativo: true);

            await _logService.RegistrarCriacaoAsync("Endereco", new { Id = novoEnderecoId, vm.Logradouro, vm.Numero, vm.Cep }, novoEnderecoId);

            var marcarComoPrincipal = (DefinirPrincipal ?? false);
            var jaTemPrincipal = await _entidadeEnderecoRepositorio.PossuiPrincipalAsync(id);
            if (!jaTemPrincipal) marcarComoPrincipal = true;

            if (marcarComoPrincipal)
            {
                var principalAntes = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);
                var entidadeAntes  = entidade;

                await _entidadeEnderecoService.DefinirPrincipalEntidadeAsync(id, novoEnderecoId);

                var principalDepois = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);
                var entidadeDepois  = await _service.ObterPorIdAsync(id);

                await _logService.RegistrarEdicaoAsync("EntidadeEndereco", principalAntes, principalDepois, registroId: novoEnderecoId);
                await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntes, entidadeDepois, registroId: id);
            }

            return RedirectToAction(nameof(GerenciarEnderecos), new { id });
        }

        [HttpPost("Enderecos/{id:int}/Excluir/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> ExcluirEndereco(int id, int enderecoId)
        {
            var enderecosAntes = await _entidadeEnderecoService.ListarPorEntidadeAsync(id);
            var enderecoAntes = enderecosAntes.FirstOrDefault(e => e.Id == enderecoId);
            if (enderecoAntes is null) return NotFound();

            var entidadeAntes = await _service.ObterPorIdAsync(id);

            var apagouEndereco = await _entidadeEnderecoRepositorio.ExcluirAsync(id, enderecoId);

            var entidadeDepois = await _service.ObterPorIdAsync(id);

            await _logService.RegistrarExclusaoAsync("EntidadeEndereco", new { EntidadeId = id, Endereco = enderecoAntes }, registroId: enderecoId);

            if (apagouEndereco)
                await _logService.RegistrarExclusaoAsync("Endereco", enderecoAntes, registroId: enderecoId);

            await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntes, entidadeDepois, registroId: id);

            return Json(new { sucesso = true, mensagem = "Endereço excluído com sucesso." });
        }
    }
}