using Financeiro.Models.Dto;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    [Route("Niveis")]
    public class NiveisController : Controller
    {
        private readonly INivelRepositorio _repo;
        public NiveisController(INivelRepositorio repo) => _repo = repo;

        // --- MÉTODOS CRUD PARA PÁGINAS (sem alterações) ---
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var lista = await _repo.BuscarAsync(string.Empty, null);
            return View(lista);
        }

        [HttpGet("Novo")]
        public IActionResult Novo() => View("NivelForm", new NivelDto { Ativo = true });

        [HttpPost("Salvar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(NivelDto dto)
        {
            if (!ModelState.IsValid) return View("NivelForm", dto);
            if (await _repo.ExisteNomeAsync(dto.Nome))
            {
                ModelState.AddModelError("Nome", "Já existe um nível com esse nome.");
                return View("NivelForm", dto);
            }
            await _repo.InserirAsync(dto);
            TempData["MensagemSucesso"] = "Nível cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Editar/{id:int}")]
        public async Task<IActionResult> Editar(int id)
        {
            var nivel = await _repo.ObterPorIdAsync(id);
            return nivel == null ? NotFound() : View("NivelForm", nivel);
        }

        [HttpPost("Atualizar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(NivelDto dto)
        {
            if (!ModelState.IsValid) return View("NivelForm", dto);
            if (await _repo.ExisteNomeAsync(dto.Nome, dto.Id))
            {
                ModelState.AddModelError("Nome", "Já existe um nível com esse nome.");
                return View("NivelForm", dto);
            }
            await _repo.AtualizarAsync(dto);
            TempData["MensagemSucesso"] = "Nível atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Excluir/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            await _repo.InativarAsync(id);
            TempData["MensagemSucesso"] = "Nível inativado!";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet("Search")]
        public async Task<IActionResult> Search(string term, int? nivel)
        {
            var niveis = await _repo.BuscarAsync(term ?? string.Empty, nivel);
            var resultadoParaSelect2 = niveis.Select(n => new
            {
                id = n.Nome,
                text = n.Nome
            });
            return Ok(new { results = resultadoParaSelect2 });
        }

        // --- MÉTODO PARA CARREGAR O MODAL (sem alterações) ---
        [HttpGet("NovoNivelPartial")]
        public IActionResult NovoNivelPartial(int nivel)
        {
            var dto = new NivelDto { Ativo = true };
            if (nivel == 1) dto.IsNivel1 = true;
            if (nivel == 2) dto.IsNivel2 = true;
            if (nivel == 3) dto.IsNivel3 = true;

            // Retorna a PartialView que contém o modal e o formulário
            return PartialView("_NivelFormModal", dto);
        }
        
        // --- NOVA ACTION [HttpPost] EXCLUSIVA PARA O MODAL ---
        [HttpPost("SalvarNivelAjax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarNivelAjax(NivelDto dto)
        {
            // Validação 1: O modelo de dados é válido?
            if (!ModelState.IsValid)
            {
                // Retorna 400 Bad Request com os detalhes do erro para o JavaScript tratar
                return BadRequest(ModelState);
            }

            // Validação 2: Já existe um nível com este nome?
            if (await _repo.ExisteNomeAsync(dto.Nome))
            {
                // Retorna 409 Conflict, que é o código HTTP ideal para "recurso já existe"
                return Conflict($"Já existe um nível com o nome '{dto.Nome}'.");
            }

            try
            {
                // Se tudo estiver OK, salva no banco
                var novoId = await _repo.InserirAsync(dto);
                var nivelCriado = await _repo.ObterPorIdAsync(novoId);

                // Retorna 200 OK com uma mensagem e os dados do objeto criado, para o JavaScript usar
                return Ok(new
                {
                    message = "Nível salvo com sucesso!",
                    novoNivel = new { id = nivelCriado.Id, text = nivelCriado.Nome }
                });
            }
            catch (Exception)
            {
                // Em caso de erro inesperado no banco, retorna um erro 500
                return StatusCode(500, "Ocorreu um erro inesperado ao salvar o nível.");
            }
        }
    }
}