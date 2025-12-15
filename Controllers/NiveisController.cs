using Financeiro.Models.Dto;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Financeiro.Servicos;  // Importante para Logs
using Financeiro.Atributos; // Importante para Permissões

namespace Financeiro.Controllers
{
    [Authorize]
    [Route("Niveis")]
    public class NiveisController : Controller
    {
        private readonly INivelRepositorio _repo;
        private readonly ILogService _logService; // Serviço de Log

        public NiveisController(INivelRepositorio repo, ILogService logService)
        {
            _repo = repo;
            _logService = logService;
        }

        // =========================================================================
        // MÉTODOS DE PÁGINA (CRUD PADRÃO COM REDIRECT)
        // =========================================================================

/* ---------- LISTAR (PAGINADO) ---------- */
        [HttpGet("")]
        [AutorizarPermissao("NIVEL_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            const int TAMANHO_PAGINA = 8;

            var (lista, total) = await _repo.ListarPaginadoAsync(p, TAMANHO_PAGINA);

            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)System.Math.Ceiling((double)total / TAMANHO_PAGINA);

            return View(lista);
        }

        [HttpGet("Novo")]
        [AutorizarPermissao("NIVEL_ADD")]
        public IActionResult Novo()
        {
            return View("NivelForm", new NivelDto { Ativo = true });
        }

        [HttpPost("Salvar")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NIVEL_ADD")]
        public async Task<IActionResult> Salvar(NivelDto dto)
        {
            if (!ModelState.IsValid) return View("NivelForm", dto);

            if (await _repo.ExisteNomeAsync(dto.Nome))
            {
                ModelState.AddModelError("Nome", "Já existe um nível com esse nome.");
                return View("NivelForm", dto);
            }

            try
            {
                int novoId = await _repo.InserirAsync(dto);
                await _logService.RegistrarCriacaoAsync("Nivel", dto, novoId); // LOG

                TempData["Sucesso"] = "Nível cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
                return View("NivelForm", dto);
            }
        }

        [HttpGet("Editar/{id:int}")]
        [AutorizarPermissao("NIVEL_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var nivel = await _repo.ObterPorIdAsync(id);
            if (nivel == null) return NotFound();
            return View("NivelForm", nivel);
        }

        [HttpPost("Atualizar")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NIVEL_EDIT")]
        public async Task<IActionResult> Atualizar(NivelDto dto)
        {
            if (!ModelState.IsValid) return View("NivelForm", dto);

            if (await _repo.ExisteNomeAsync(dto.Nome, dto.Id))
            {
                ModelState.AddModelError("Nome", "Já existe outro nível com esse nome.");
                return View("NivelForm", dto);
            }

            try 
            {
                var antigo = await _repo.ObterPorIdAsync(dto.Id);
                await _repo.AtualizarAsync(dto);
                await _logService.RegistrarEdicaoAsync("Nivel", antigo, dto, dto.Id); // LOG

                TempData["Sucesso"] = "Nível atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
                return View("NivelForm", dto);
            }
        }

        [HttpPost("Excluir/{id:int}")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NIVEL_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var item = await _repo.ObterPorIdAsync(id);
                if (item == null) return NotFound();

                await _repo.InativarAsync(id);
                await _logService.RegistrarExclusaoAsync("Nivel", item, id); // LOG

                TempData["Sucesso"] = "Nível inativado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao inativar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string term, int? nivel)
        {
            var niveis = await _repo.BuscarAsync(term ?? string.Empty, nivel);
            var resultado = niveis.Select(n => new { id = n.Id, text = n.Nome }); 
            return Ok(new { results = resultado });
        }

        // =========================================================================
        // MÉTODOS ESPECÍFICOS PARA O MODAL (QUE ESTAVAM FALTANDO)
        // =========================================================================

        [HttpGet("NovoNivelPartial")]
        [AutorizarPermissao("NIVEL_ADD")] // Mantivemos a segurança
        public IActionResult NovoNivelPartial(int nivel)
        {
            var dto = new NivelDto { Ativo = true };
            if (nivel == 1) dto.IsNivel1 = true;
            if (nivel == 2) dto.IsNivel2 = true;
            if (nivel == 3) dto.IsNivel3 = true;

            // IMPORTANTE: Certifique-se que o arquivo View se chama "_NivelFormModal.cshtml"
            // Se o arquivo que você criou se chama apenas "_NivelModal.cshtml", altere o nome aqui abaixo.
            return PartialView("_NivelFormModal", dto);
        }

        [HttpPost("SalvarNivelAjax")]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NIVEL_ADD")] // Mantivemos a segurança
        public async Task<IActionResult> SalvarNivelAjax(NivelDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _repo.ExisteNomeAsync(dto.Nome))
            {
                return Conflict($"Já existe um nível com o nome '{dto.Nome}'.");
            }

            try
            {
                int novoId = await _repo.InserirAsync(dto);
                
                // ADICIONAMOS O LOG AQUI TAMBÉM (Melhoria em relação ao antigo)
                await _logService.RegistrarCriacaoAsync("Nivel", dto, novoId);

                var nivelCriado = await _repo.ObterPorIdAsync(novoId);

                return Ok(new
                {
                    message = "Nível salvo com sucesso!",
                    novoNivel = new { id = nivelCriado.Id, text = nivelCriado.Nome }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocorreu um erro inesperado ao salvar o nível.");
            }
        }
    }
}