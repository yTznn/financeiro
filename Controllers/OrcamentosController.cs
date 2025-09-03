using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using Financeiro.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Financeiro.Controllers
{
    public class OrcamentosController : Controller
    {
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IInstrumentoRepositorio _instrumentoRepo;

        public OrcamentosController(IOrcamentoRepositorio orcamentoRepo, IInstrumentoRepositorio instrumentoRepo)
        {
            _orcamentoRepo = orcamentoRepo;
            _instrumentoRepo = instrumentoRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _orcamentoRepo.ListarAsync();
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Novo()
        {
            // Mantém o nome "TiposDeAcordo" para compatibilidade com a view
            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", new OrcamentoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(OrcamentoViewModel vm, string detalhamentoJson)
        {
            if (!string.IsNullOrEmpty(detalhamentoJson))
            {
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
                return View("OrcamentoForm", vm);
            }

            await _orcamentoRepo.InserirAsync(vm);
            TempData["MensagemSucesso"] = "Orçamento salvo com sucesso!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (orcamentoHeader == null) return NotFound();

            var detalhes = await _orcamentoRepo.ObterDetalhesPorOrcamentoIdAsync(id);
            var detalhamentoHierarquico = ConstruirHierarquia(detalhes.ToList(), null);

            var vm = new OrcamentoViewModel
            {
                Id = orcamentoHeader.Id,
                Nome = orcamentoHeader.Nome,
                // Mantém a propriedade atual (TipoAcordoId) enquanto a model não for renomeada
                TipoAcordoId = orcamentoHeader.TipoAcordoId,
                VigenciaInicio = orcamentoHeader.VigenciaInicio,
                VigenciaFim = orcamentoHeader.VigenciaFim,
                ValorPrevistoTotal = orcamentoHeader.ValorPrevistoTotal,
                Ativo = orcamentoHeader.Ativo,
                Observacao = orcamentoHeader.Observacao,
                Detalhamento = detalhamentoHierarquico
            };

            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(OrcamentoViewModel vm, string detalhamentoJson)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
                return View("OrcamentoForm", vm);
            }

            if (!string.IsNullOrEmpty(detalhamentoJson))
            {
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);
            }

            await _orcamentoRepo.AtualizarAsync(vm.Id, vm);
            TempData["MensagemSucesso"] = "Orçamento atualizado com sucesso!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var orcamento = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (orcamento == null)
            {
                return NotFound();
            }

            await _orcamentoRepo.ExcluirAsync(id);
            TempData["MensagemSucesso"] = "Orçamento excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        private List<OrcamentoDetalheViewModel> ConstruirHierarquia(List<OrcamentoDetalhe> todos, int? parentId)
        {
            return todos
                .Where(i => i.ParentId == parentId)
                .Select(i => new OrcamentoDetalheViewModel
                {
                    Id = i.Id,
                    ParentId = i.ParentId,
                    Nome = i.Nome,
                    ValorPrevisto = i.ValorPrevisto,
                    Filhos = ConstruirHierarquia(todos, i.Id)
                })
                .ToList();
        }
    }
}