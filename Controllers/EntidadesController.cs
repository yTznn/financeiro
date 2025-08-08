using System.Threading.Tasks;
using AutoMapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Controllers
{
    [Route("Entidades")]
    public class EntidadesController : Controller
    {
        private readonly IEntidadeService _service;
        private readonly IMapper          _mapper;

        public EntidadesController(IEntidadeService service, IMapper mapper)
        {
            _service = service;
            _mapper  = mapper;
        }

        /* ---------- LISTAGEM ---------- */
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var entidades = await _service.ListarAsync();
            return View(entidades);                       // Views/Entidades/Index.cshtml
        }

        /* ---------- CREATE ---------- */
        [HttpGet("Create")]
        public IActionResult Create() => View("Form", new EntidadeViewModel());

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromForm] EntidadeViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

            try
            {
                var entidade = _mapper.Map<Entidade>(vm);
                await _service.CriarAsync(entidade);
                return Json(new { sucesso = true, mensagem = "Entidade criada com sucesso!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* ---------- EDIT ---------- */
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var entidade = await _service.ObterPorIdAsync(id);
            if (entidade == null) return NotFound();

            var vm = _mapper.Map<EntidadeViewModel>(entidade);
            return View("Form", vm);
        }

        [HttpPost("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromForm] EntidadeViewModel vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

            try
            {
                var entidade = _mapper.Map<Entidade>(vm);
                entidade.Id = id;

                await _service.AtualizarAsync(entidade);
                return Json(new { sucesso = true, mensagem = "Entidade atualizada!" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { sucesso = false, mensagem = ex.Message });
            }
        }

        /* ---------- DELETE ---------- */
        [HttpPost("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.ExcluirAsync(id);
            return Json(new { sucesso = true, mensagem = "Entidade excluída!" });
        }

        /* ---------- AUTO-FILL ---------- */
        [HttpGet("AutoFill")]
        public async Task<IActionResult> AutoFill(string cnpj)
        {
            var dto = await _service.AutoFillPorCnpjAsync(cnpj);
            return Json(dto);
        }
    }
}