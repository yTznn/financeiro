using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Financeiro.Atributos; // Para [AutorizarPermissao]
using Financeiro.Servicos;  // Para ILogService
using System;

namespace Financeiro.Controllers
{
    [Authorize]
    public class NaturezasController : Controller
    {
        private readonly INaturezaRepositorio _repo;
        private readonly ILogService _logService;

        public NaturezasController(INaturezaRepositorio repo, ILogService logService)
        {
            _repo = repo;
            _logService = logService;
        }

        /* ---------- LISTAR ---------- */
        
        [HttpGet]
        [AutorizarPermissao("NATUREZA_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            const int TAMANHO_PAGINA = 10; // 10 registros por página

            var (lista, total) = await _repo.ListarPaginadoAsync(p, TAMANHO_PAGINA);
            
            // Passa dados para a View desenhar os botões
            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);
            
            return View(lista);
        }

        /* ---------- NOVO ---------- */
        [HttpGet]
        [AutorizarPermissao("NATUREZA_ADD")]
        public IActionResult Novo()
        {
            return View("NaturezaForm", new NaturezaViewModel { Ativo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NATUREZA_ADD")]
        public async Task<IActionResult> Salvar(NaturezaViewModel vm)
        {
            if (!ModelState.IsValid) return View("NaturezaForm", vm);

            try
            {
                // Dica: Se seu repositório retornar o ID criado, capture-o aqui.
                // Caso contrário, passamos 0 ou ajustamos o repo futuramente.
                await _repo.InserirAsync(vm);
                
                // LOG
                await _logService.RegistrarCriacaoAsync("Natureza", vm, 0);

                TempData["Sucesso"] = "Natureza cadastrada com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
                return View("NaturezaForm", vm);
            }
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        [AutorizarPermissao("NATUREZA_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var n = await _repo.ObterPorIdAsync(id);
            if (n == null) return NotFound();

            var vm = new NaturezaViewModel
            {
                Id = n.Id,
                Nome = n.Nome,
                NaturezaMedica = n.NaturezaMedica,
                Ativo = n.Ativo
            };
            return View("NaturezaForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NATUREZA_EDIT")]
        public async Task<IActionResult> Atualizar(int id, NaturezaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("NaturezaForm", vm);

            try
            {
                var antigo = await _repo.ObterPorIdAsync(id);
                if (antigo == null) return NotFound();

                await _repo.AtualizarAsync(id, vm);

                // LOG
                await _logService.RegistrarEdicaoAsync("Natureza", antigo, vm, id);

                TempData["Sucesso"] = "Natureza atualizada com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
                return View("NaturezaForm", vm);
            }
        }

        /* ---------- EXCLUIR (INATIVAR) ---------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("NATUREZA_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var item = await _repo.ObterPorIdAsync(id);
                if (item == null) return NotFound();

                // Implementação da Exclusão Lógica
                // Se o seu repositório já tiver um método "InativarAsync", use-o.
                // Caso contrário, fazemos manualmente aqui:
                
                // Opção A: Se existir InativarAsync no Repo
                // await _repo.InativarAsync(id); 

                // Opção B: Atualização manual para Ativo = false (caso não tenha método específico)
                var vmInativacao = new NaturezaViewModel 
                { 
                    Id = item.Id, 
                    Nome = item.Nome, 
                    NaturezaMedica = item.NaturezaMedica, 
                    Ativo = false // <--- O Pulo do Gato
                };
                await _repo.AtualizarAsync(id, vmInativacao);

                // LOG DE EXCLUSÃO
                await _logService.RegistrarExclusaoAsync("Natureza", item, id);

                TempData["Sucesso"] = "Natureza inativada com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao inativar: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}