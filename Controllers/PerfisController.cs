using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Repositorios; // Novo Repo
using Financeiro.Servicos;     // Logs
using Financeiro.Atributos;    // Permissões
using System;
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    [Authorize]
    public class PerfisController : Controller
    {
        private readonly IPerfilRepositorio _repo;
        private readonly ILogService _logService;

        public PerfisController(IPerfilRepositorio repo, ILogService logService)
        {
            _repo = repo;
            _logService = logService;
        }

        /* ---------- LISTAR (Paginado) ---------- */
        [HttpGet]
        [AutorizarPermissao("PERFIL_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            const int TAMANHO_PAGINA = 10;
            
            // Busca dados paginados do repositório
            var (lista, total) = await _repo.ListarPaginadoAsync(p, TAMANHO_PAGINA);

            // Passa informações de paginação para a View
            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);

            return View(lista);
        }

        /* ---------- NOVO ---------- */
        [HttpGet]
        [AutorizarPermissao("PERFIL_ADD")]
        public IActionResult Novo()
        {
            // Inicia com Ativo = true por padrão
            return View("PerfilForm", new Perfil { Ativo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("PERFIL_ADD")]
        public async Task<IActionResult> Salvar(Perfil perfil)
        {
            if (!ModelState.IsValid) return View("PerfilForm", perfil);

            try
            {
                await _repo.InserirAsync(perfil);
                
                // Log de criação
                await _logService.RegistrarCriacaoAsync("Perfil", perfil, 0);

                TempData["Sucesso"] = "Perfil cadastrado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
                return View("PerfilForm", perfil);
            }
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        [AutorizarPermissao("PERFIL_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var perfil = await _repo.ObterPorIdAsync(id);
            if (perfil == null) return NotFound();
            
            return View("PerfilForm", perfil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("PERFIL_EDIT")]
        public async Task<IActionResult> Atualizar(Perfil perfil)
        {
            if (!ModelState.IsValid) return View("PerfilForm", perfil);

            try
            {
                // Busca o anterior para logar o "Antes e Depois"
                var antigo = await _repo.ObterPorIdAsync(perfil.Id);
                
                await _repo.AtualizarAsync(perfil);
                
                // Log de edição
                await _logService.RegistrarEdicaoAsync("Perfil", antigo, perfil, perfil.Id);

                TempData["Sucesso"] = "Perfil atualizado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
                return View("PerfilForm", perfil);
            }
        }

        /* ---------- EXCLUIR (Inativar) ---------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("PERFIL_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var item = await _repo.ObterPorIdAsync(id);
                if (item == null) return NotFound();

                await _repo.InativarAsync(id);
                
                // Log de exclusão
                await _logService.RegistrarExclusaoAsync("Perfil", item, id);

                TempData["Sucesso"] = "Perfil inativado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao inativar: {ex.Message}";
            }
            
            return RedirectToAction("Index");
        }
    }
}