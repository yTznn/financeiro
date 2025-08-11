using Financeiro.Models;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public class LogService : ILogService
    {
        private readonly ILogRepositorio _logRepositorio;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(ILogRepositorio logRepositorio, IHttpContextAccessor httpContextAccessor)
        {
            _logRepositorio = logRepositorio;
            _httpContextAccessor = httpContextAccessor;
        }

        private int ObterUsuarioId()
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        private int ObterEntidadeId()
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("EntidadeId")?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        public async Task RegistrarCriacaoAsync(string tabela, object novoValor, int? registroId = null)
        {
            var log = new Log
            {
                UsuarioId = ObterUsuarioId(),
                EntidadeId = ObterEntidadeId(),
                Tabela = tabela,
                Acao = "Criação",
                DataHora = DateTime.Now,
                ValoresNovos = JsonSerializer.Serialize(novoValor),
                RegistroId = registroId
            };

            await _logRepositorio.RegistrarAsync(log);
        }

        public async Task RegistrarEdicaoAsync(string tabela, object valorAntigo, object valorNovo, int? registroId = null)
        {
            var log = new Log
            {
                UsuarioId = ObterUsuarioId(),
                EntidadeId = ObterEntidadeId(),
                Tabela = tabela,
                Acao = "Edição",
                DataHora = DateTime.Now,
                ValoresAnteriores = JsonSerializer.Serialize(valorAntigo),
                ValoresNovos = JsonSerializer.Serialize(valorNovo),
                RegistroId = registroId
            };

            await _logRepositorio.RegistrarAsync(log);
        }

        public async Task RegistrarExclusaoAsync(string tabela, object valorAntigo, int? registroId = null)
        {
            var log = new Log
            {
                UsuarioId = ObterUsuarioId(),
                EntidadeId = ObterEntidadeId(),
                Tabela = tabela,
                Acao = "Exclusão",
                DataHora = DateTime.Now,
                ValoresAnteriores = JsonSerializer.Serialize(valorAntigo),
                RegistroId = registroId
            };

            await _logRepositorio.RegistrarAsync(log);
        }
    }
}