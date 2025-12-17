using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using Financeiro.Atributos; // Para [AutorizarPermissao]
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using System.Linq;
using Dapper;
using Financeiro.Infraestrutura; // Necessário para IDbConnectionFactory

namespace Financeiro.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly ILogRepositorio _logRepositorio;
        private readonly IDbConnectionFactory _connectionFactory;

        public LogsController(ILogRepositorio logRepositorio, IDbConnectionFactory connectionFactory)
        {
            _logRepositorio = logRepositorio;
            _connectionFactory = connectionFactory;
        }

        // 1. TELA PRINCIPAL (LISTAGEM E FILTRO)
        [HttpGet]
        [AutorizarPermissao("LOG_VIEW")] // Permissão para ver os logs
        public async Task<IActionResult> Index(int p = 1, int? usuarioId = null)
        {
            const int TAMANHO_PAGINA = 5;
            
            var (logs, total) = await _logRepositorio.ListarLogsPaginadosAsync(p, TAMANHO_PAGINA, usuarioId);

            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);
            ViewBag.UsuarioId = usuarioId;
            
            // Se tiver filtro, tenta buscar o NameSkip do usuário para pré-selecionar o Select2
            if (usuarioId.HasValue && logs.Any())
            {
                ViewBag.UsuarioNomeSelecionado = logs.First().UsuarioNome;
            }

            return View(logs);
        }

        // 2. ENDPOINT AJAX PARA O SELECT2
        // Esta rota é chamada pelo JavaScript na View para popular o dropdown de filtro dinamicamente.
        [HttpGet]
        public async Task<IActionResult> BuscarUsuarios(string term)
        {
            // Condição mínima para evitar buscas vazias que consomem recursos
            if (string.IsNullOrEmpty(term) || term.Length < 2) 
            {
                // Select2 espera um objeto com uma propriedade "results"
                return Json(new { results = new object[] { } });
            }

            // O LogRepositorio já tem a query otimizada
            var usuarios = await _logRepositorio.BuscarUsuariosParaSelectAsync(term);
            
            // O Dapper já retorna um IEnumerable<dynamic> com { Id, Text }, que é o formato ideal.
            return Json(new { results = usuarios }); 
        }
    }
}