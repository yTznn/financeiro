using Financeiro.Models.Dto;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    [Route("Niveis")]
    public class NiveisController : Controller
    {
        private readonly INivelRepositorio _repo;
        public NiveisController(INivelRepositorio repo) => _repo = repo;

        // --- MÉTODOS CRUD PARA PÁGINAS ---
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

            // A LINHA ABAIXO É A "TRADUTORA" QUE ESTAVA FALTANDO OU INCORRETA.
            // Ela transforma a lista de NivelDto para uma lista com os campos 'id' e 'text'.
            var resultadoParaSelect2 = niveis.Select(n => new
            {
                id = n.Nome,    // A propriedade 'id' do resultado será o Nome do nível
                text = n.Nome   // A propriedade 'text' (o que aparece na tela) também será o Nome
            });

            return Ok(new { results = resultadoParaSelect2 });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] NivelDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nome)) return BadRequest("O nome do nível é obrigatório.");
            if (await _repo.ExisteNomeAsync(dto.Nome)) return Conflict("Já existe um nível com este nome.");
            var novoId = await _repo.InserirAsync(dto);
            var nivelCriado = await _repo.ObterPorIdAsync(novoId);
            return Ok(new { id = nivelCriado.Nome, text = nivelCriado.Nome });
        }
        // NiveisController.cs

        [HttpGet("NovoNivelPartial")]
        public IActionResult NovoNivelPartial(int nivel)
        {
            var dto = new NivelDto { Ativo = true };
            if (nivel == 1) dto.IsNivel1 = true;
            if (nivel == 2) dto.IsNivel2 = true;
            if (nivel == 3) dto.IsNivel3 = true;

            // Retorna uma PartialView que contém o modal e o formulário
            return PartialView("_NivelFormModal", dto);
        }
    }
}