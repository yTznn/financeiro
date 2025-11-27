using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Servicos;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Financeiro.Controllers
{
    [Authorize]
    [Route("Entidades")]
    public class EntidadesController : Controller
    {
        private readonly IEntidadeService _service;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        // Endereços (Entidade)
        private readonly IEntidadeEnderecoService _entidadeEnderecoService;
        private readonly IEntidadeEnderecoRepositorio _entidadeEnderecoRepositorio;
        private readonly IEnderecoRepositorio _enderecoRepositorio;

        /// <summary>Construtor com injeção de dependências.</summary>
        public EntidadesController(
            IEntidadeService service,
            IMapper mapper,
            ILogService logService,
            IEntidadeEnderecoService entidadeEnderecoService,
            IEntidadeEnderecoRepositorio entidadeEnderecoRepositorio,
            IEnderecoRepositorio enderecoRepositorio)
        {
            _service = service;
            _mapper = mapper;
            _logService = logService;

            _entidadeEnderecoService = entidadeEnderecoService;
            _entidadeEnderecoRepositorio = entidadeEnderecoRepositorio;
            _enderecoRepositorio = enderecoRepositorio;
        }

        /* ========================== LISTAGEM ========================== */

        /// <summary>Lista todas as Entidades.</summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var entidades = await _service.ListarAsync();
            return View(entidades);
        }

        /* =========================== CREATE =========================== */

        /// <summary>Exibe o formulário de criação de Entidade.</summary>
        [HttpGet("Create")]
        public IActionResult Create() => View("Form", new EntidadeViewModel());

        /// <summary>Cria uma nova Entidade.</summary>
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromForm] EntidadeViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

            try
            {
                var entidade = _mapper.Map<Entidade>(vm);
                await _service.CriarAsync(entidade);

                // LOG DE CRIAÇÃO
                await _logService.RegistrarCriacaoAsync("Entidade", entidade, entidade.Id);

                return Json(new { sucesso = true, mensagem = "Entidade criada com sucesso!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* ============================ EDIT ============================ */

        /// <summary>Exibe o formulário de edição de Entidade.</summary>
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade == null) return NotFound();

            var vm = _mapper.Map<EntidadeViewModel>(entidade);
            return View("Form", vm);
        }

        /// <summary>Atualiza os dados de uma Entidade.</summary>
        [HttpPost("Edit/{id:int}")]
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

                // LOG DE EDIÇÃO
                await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntiga, entidadeAtualizada, id);

                return Json(new { sucesso = true, mensagem = "Entidade atualizada!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* =========================== DELETE =========================== */

        /// <summary>Exclui uma Entidade (depois vira apenas alerta).</summary>
        [HttpPost("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entidadeAntiga = await _service.ObterPorIdAsync(id);
            if (entidadeAntiga == null) return NotFound();

            await _service.ExcluirAsync(id);

            // LOG DE EXCLUSÃO
            await _logService.RegistrarExclusaoAsync("Entidade", entidadeAntiga, id);

            return Json(new { sucesso = true, mensagem = "Entidade excluída!" });
        }

        /* ========================= AUTO-FILL ========================== */

        /// <summary>Auto-preenche dados a partir do CNPJ.</summary>
        [HttpGet("AutoFill")]
        public async Task<IActionResult> AutoFill(string cnpj)
        {
            var dto = await _service.AutoFillPorCnpjAsync(cnpj);
            return Json(dto);
        }

        /* =================== ENDEREÇOS DA ENTIDADE ==================== */

        /// <summary>Abre a tela de gerenciamento de endereços da Entidade.</summary>
        [HttpGet("Enderecos/{id:int}/Gerenciar")]
        public IActionResult GerenciarEnderecos(int id)
        {
            // A view "Views/Entidades/Enderecos.cshtml" espera o Id (int) como model
            return View("Enderecos", id);
        }

        /// <summary>Retorna todos os endereços vinculados à Entidade (principal primeiro).</summary>
        [HttpGet("Enderecos/{id:int}")]
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

        /// <summary>Retorna o endereço principal da Entidade (se houver).</summary>
        [HttpGet("EnderecoPrincipal/{id:int}")]
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

        /// <summary>
        /// Define um endereço existente como principal para a Entidade,
        /// sincronizando Entidade.EnderecoId e registrando logs.
        /// </summary>
        [HttpPost("Enderecos/{id:int}/DefinirPrincipal/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
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

                // LOG 1: alteração no vínculo principal
                await _logService.RegistrarEdicaoAsync(
                    "EntidadeEndereco",
                    principalAntes,
                    principalDepois,
                    registroId: enderecoId
                );

                // LOG 2: alteração na Entidade (ponteiro EnderecoId)
                await _logService.RegistrarEdicaoAsync(
                    "Entidade",
                    entidadeAntes,
                    entidadeDepois,
                    registroId: id
                );

                return Json(new { sucesso = true, mensagem = "Endereço definido como principal!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /// <summary>Abre a tela de cadastro de um novo endereço para a Entidade.</summary>
        [HttpGet("Enderecos/{id:int}/Novo")]
        public IActionResult NovoEnderecoEntidade(int id)
        {
            return View("EnderecoEntidadeForm", id);
        }

        /// <summary>
        /// Salva um novo endereço para a Entidade e (opcionalmente) define como principal.
        /// </summary>
        [HttpPost("SalvarEnderecoEntidade/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarEnderecoEntidade(
            int id,
            [FromForm] EnderecoViewModel vm,
            [FromForm] bool? DefinirPrincipal)
        {
            // valida entidade
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade is null) return NotFound();

            // cria o endereço em Endereco e retorna Id
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

            // vincula na tabela de relação (Principal = 0 por padrão)
            await _entidadeEnderecoRepositorio.VincularAsync(id, novoEnderecoId, ativo: true);

            // LOG: criação do endereço
            await _logService.RegistrarCriacaoAsync("Endereco", new
            {
                Id = novoEnderecoId,
                vm.Logradouro,
                vm.Numero,
                vm.Complemento,
                vm.Cep,
                vm.Bairro,
                vm.Municipio,
                vm.Uf
            }, novoEnderecoId);

            // decidir se precisa definir como principal
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

                // LOG 1: mudança no vínculo principal
                await _logService.RegistrarEdicaoAsync(
                    "EntidadeEndereco",
                    principalAntes,
                    principalDepois,
                    registroId: novoEnderecoId
                );

                // LOG 2: ponteiro em Entidade.EnderecoId
                await _logService.RegistrarEdicaoAsync(
                    "Entidade",
                    entidadeAntes,
                    entidadeDepois,
                    registroId: id
                );
            }

            return RedirectToAction(nameof(GerenciarEnderecos), new { id });
        }

        /// <summary>
        /// Exclui endereço da Entidade. Se o endereço ficar sem vínculos, apaga da tabela Endereco.
        /// Se era principal, escolhe um novo principal (se houver) ou zera Entidade.EnderecoId.
        /// </summary>
        [HttpPost("Enderecos/{id:int}/Excluir/{enderecoId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirEndereco(int id, int enderecoId)
        {
            // estado "antes" para logs
            var enderecosAntes = await _entidadeEnderecoService.ListarPorEntidadeAsync(id);
            var enderecoAntes = enderecosAntes.FirstOrDefault(e => e.Id == enderecoId);
            if (enderecoAntes is null) return NotFound();

            var entidadeAntes = await _service.ObterPorIdAsync(id);

            // execução
            var apagouEndereco = await _entidadeEnderecoRepositorio.ExcluirAsync(id, enderecoId);

            // estado "depois"
            var entidadeDepois = await _service.ObterPorIdAsync(id);

            // logs
            await _logService.RegistrarExclusaoAsync("EntidadeEndereco", new
            {
                EntidadeId = id,
                Endereco = enderecoAntes
            }, registroId: enderecoId);

            if (apagouEndereco)
                await _logService.RegistrarExclusaoAsync("Endereco", enderecoAntes, registroId: enderecoId);

            await _logService.RegistrarEdicaoAsync("Entidade", entidadeAntes, entidadeDepois, registroId: id);

            return Json(new { sucesso = true, mensagem = "Endereço excluído com sucesso." });
        }
    }
}