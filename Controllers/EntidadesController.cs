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
using Financeiro.Atributos; 
using Financeiro.Extensions;

namespace Financeiro.Controllers
{
    [Authorize]
    [Route("Entidades")]
    public class EntidadesController : Controller
    {
        private const int TAMANHO_PAGINA = 3; 

        private readonly IEntidadeService _service;
        private readonly IEntidadeRepositorio _repositorio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IEntidadeEnderecoService _entidadeEnderecoService;
        private readonly IEntidadeEnderecoRepositorio _entidadeEnderecoRepositorio;
        private readonly IEnderecoRepositorio _enderecoRepositorio;
        private readonly IContaBancariaRepositorio _contaBancariaRepo;

        public EntidadesController(
            IEntidadeService service,
            IEntidadeRepositorio repositorio,
            IMapper mapper,
            ILogService logService,
            IEntidadeEnderecoService entidadeEnderecoService,
            IEntidadeEnderecoRepositorio entidadeEnderecoRepositorio,
            IEnderecoRepositorio enderecoRepositorio,
            IContaBancariaRepositorio contaBancariaRepo)
        {
            _service = service;
            _repositorio = repositorio;
            _mapper = mapper;
            _logService = logService;
            _entidadeEnderecoService = entidadeEnderecoService;
            _entidadeEnderecoRepositorio = entidadeEnderecoRepositorio;
            _enderecoRepositorio = enderecoRepositorio;
            _contaBancariaRepo = contaBancariaRepo;
        }

        /* ========================== LISTAGEM ========================== */

        [HttpGet("")]
        [AutorizarPermissao("ENTIDADE_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            if (p < 1) p = 1;
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

                // --- BANK ACCOUNT LOGIC REMOVED FROM CREATE ---
                // We only create the basic entity here. Bank details are added in Edit mode.
                
                await _service.CriarAsync(entidade); 

                await _logService.RegistrarCriacaoAsync("Entidade", entidade, entidade.Id);

                // JSON return tells the front-end success
                return Json(new { sucesso = true, mensagem = "Entidade criada com sucesso! Agora você pode adicionar a conta bancária." });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { sucesso = false, mensagem = "Erro interno ao criar entidade: " + ex.Message });
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

            // --- LOAD BANK DETAILS (Only in Edit) ---
            if (entidade.ContaBancariaId.HasValue && User.TemPermissao("ENTIDADE_CONTA_EDIT"))
            {
                var conta = await _contaBancariaRepo.ObterPorIdAsync(entidade.ContaBancariaId.Value);
                if (conta != null)
                {
                    vm.Banco = conta.Banco;
                    vm.Agencia = conta.Agencia;
                    vm.Conta = conta.Conta;
                    vm.ChavePix = conta.ChavePix;
                    vm.ContaBancariaId = conta.Id;
                }
            }

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

                // --- UPDATE BANK DETAILS (Only in Edit) ---
                if (!User.TemPermissao("ENTIDADE_CONTA_EDIT"))
                {
                    entidadeAtualizada.ContaBancariaId = entidadeAntiga.ContaBancariaId;
                }
                else
                {
                    bool temDadosBancarios = !string.IsNullOrWhiteSpace(vm.Banco) && !string.IsNullOrWhiteSpace(vm.Conta);

                    if (temDadosBancarios)
                    {
                        if (entidadeAntiga.ContaBancariaId.HasValue)
                        {
                            // Update existing
                            var contaExistente = await _contaBancariaRepo.ObterPorIdAsync(entidadeAntiga.ContaBancariaId.Value);
                            if (contaExistente != null)
                            {
                                contaExistente.Banco = vm.Banco;
                                contaExistente.Agencia = vm.Agencia ?? "";
                                contaExistente.Conta = vm.Conta;
                                contaExistente.ChavePix = vm.ChavePix;
                                await _contaBancariaRepo.AtualizarAsync(contaExistente);
                                entidadeAtualizada.ContaBancariaId = contaExistente.Id;
                            }
                            else 
                            {
                                // Re-create if missing reference
                                var novaConta = new ContaBancaria { Banco = vm.Banco, Agencia = vm.Agencia ?? "", Conta = vm.Conta, ChavePix = vm.ChavePix };
                                int idConta = await _contaBancariaRepo.InserirRetornandoIdAsync(novaConta);
                                entidadeAtualizada.ContaBancariaId = idConta;
                            }
                        }
                        else
                        {
                            // Create new
                            var novaConta = new ContaBancaria { Banco = vm.Banco, Agencia = vm.Agencia ?? "", Conta = vm.Conta, ChavePix = vm.ChavePix };
                            int idConta = await _contaBancariaRepo.InserirRetornandoIdAsync(novaConta);
                            entidadeAtualizada.ContaBancariaId = idConta;
                        }
                    }
                    else
                    {
                        // Keep existing if empty (safety)
                        entidadeAtualizada.ContaBancariaId = entidadeAntiga.ContaBancariaId;
                    }
                }

                await _service.AtualizarAsync(entidadeAtualizada);
                await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntiga, entidadeAtualizada, id);

                return Json(new { sucesso = true, mensagem = "Entidade atualizada!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { sucesso = false, mensagem = "Erro técnico: " + ex.Message });
            }
        }

        // ... (Other methods Delete, AutoFill, Enderecos... kept same) ...
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
                 return BadRequest(new { sucesso = false, mensagem = "Não foi possível excluir. " + ex.Message });
            }
        }

        [HttpGet("AutoFill")]
        [AutorizarPermissao("ENTIDADE_ADD")] 
        public async Task<IActionResult> AutoFill(string cnpj)
        {
            var dto = await _service.AutoFillPorCnpjAsync(cnpj);
            return Json(dto);
        }

        [HttpGet("Enderecos/{id:int}/Gerenciar")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public IActionResult GerenciarEnderecos(int id) => View("Enderecos", id);

        [HttpGet("Enderecos/{id:int}")]
        [AutorizarPermissao("ENTIDADE_VIEW")]
        public async Task<IActionResult> Enderecos(int id)
        {
            var lista = await _entidadeEnderecoService.ListarPorEntidadeAsync(id);
            var principal = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);
            var itens = lista.Select(e => new { e.Id, e.Logradouro, e.Numero, e.Complemento, e.Cep, e.Bairro, e.Municipio, e.Uf, Principal = principal != null && principal.Id == e.Id });
            return Json(new { sucesso = true, itens });
        }

        [HttpGet("EnderecoPrincipal/{id:int}")]
        [AutorizarPermissao("ENTIDADE_VIEW")]
        public async Task<IActionResult> EnderecoPrincipal(int id)
        {
            var principal = await _entidadeEnderecoService.ObterPrincipalPorEntidadeAsync(id);
            if (principal == null) return Json(new { sucesso = true, possuiPrincipal = false });
            return Json(new { sucesso = true, possuiPrincipal = true, endereco = new { principal.Id, principal.Logradouro, principal.Numero, principal.Complemento, principal.Cep, principal.Bairro, principal.Municipio, principal.Uf } });
        }

        [HttpPost("Enderecos/{id:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> DefinirPrincipal(int id, int enderecoId)
        {
            try {
                await _entidadeEnderecoService.DefinirPrincipalEntidadeAsync(id, enderecoId);
                return Json(new { sucesso = true, mensagem = "Endereço definido como principal!" });
            } catch (Exception ex) { return BadRequest(new { sucesso = false, mensagem = ex.Message }); }
        }

        [HttpGet("Enderecos/{id:int}/Novo")]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public IActionResult NovoEnderecoEntidade(int id) => View("EnderecoEntidadeForm", id);

        [HttpPost("SalvarEnderecoEntidade/{id:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> SalvarEnderecoEntidade(int id, [FromForm] EnderecoViewModel vm, [FromForm] bool? DefinirPrincipal)
        {
            var novoEnderecoId = await _enderecoRepositorio.InserirRetornandoIdAsync(new Endereco { Logradouro = vm.Logradouro, Numero = vm.Numero, Complemento = vm.Complemento, Cep = vm.Cep, Bairro = vm.Bairro, Municipio = vm.Municipio, Uf = vm.Uf });
            await _entidadeEnderecoRepositorio.VincularAsync(id, novoEnderecoId, ativo: true);
            
            var jaTemPrincipal = await _entidadeEnderecoRepositorio.PossuiPrincipalAsync(id);
            if ((DefinirPrincipal ?? false) || !jaTemPrincipal)
            {
                await _entidadeEnderecoService.DefinirPrincipalEntidadeAsync(id, novoEnderecoId);
            }
            return RedirectToAction(nameof(GerenciarEnderecos), new { id });
        }

        [HttpPost("Enderecos/{id:int}/Excluir/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ENTIDADE_EDIT")]
        public async Task<IActionResult> ExcluirEndereco(int id, int enderecoId)
        {
            await _entidadeEnderecoRepositorio.ExcluirAsync(id, enderecoId);
            return Json(new { sucesso = true, mensagem = "Endereço excluído com sucesso." });
        }
    }
}