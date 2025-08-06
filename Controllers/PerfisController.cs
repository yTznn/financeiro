using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using System.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Infraestrutura;

namespace Financeiro.Controllers
{
    [Authorize]
    public class PerfisController : Controller
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PerfisController(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // GET: /Perfis
        public async Task<IActionResult> Index()
        {
            using var conn = _connectionFactory.CreateConnection();
            var perfis = await conn.QueryAsync<Perfil>("SELECT * FROM Perfis ORDER BY Nome");
            return View(perfis);
        }

        // GET: /Perfis/Novo
        public IActionResult Novo()
        {
            return View("PerfilForm", new Perfil());
        }

        // POST: /Perfis/Salvar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(Perfil perfil)
        {
            if (!ModelState.IsValid)
                return View("PerfilForm", perfil);

            using var conn = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Perfis (Nome, Ativo) VALUES (@Nome, @Ativo)";
            await conn.ExecuteAsync(sql, perfil);

            return RedirectToAction("Index");
        }

        // GET: /Perfis/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var perfil = await conn.QueryFirstOrDefaultAsync<Perfil>(
                "SELECT * FROM Perfis WHERE Id = @Id", new { Id = id });

            if (perfil == null)
                return NotFound();

            return View("PerfilForm", perfil);
        }

        // POST: /Perfis/Atualizar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(Perfil perfil)
        {
            if (!ModelState.IsValid)
                return View("PerfilForm", perfil);

            using var conn = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Perfis SET Nome = @Nome, Ativo = @Ativo WHERE Id = @Id";
            await conn.ExecuteAsync(sql, perfil);

            return RedirectToAction("Index");
        }

        // POST: /Perfis/Excluir/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM Perfis WHERE Id = @Id", new { Id = id });
            return RedirectToAction("Index");
        }
    }
}