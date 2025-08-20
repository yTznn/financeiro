using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Financeiro.Servicos
{
    public class JustificativaService : IJustificativaService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JustificativaService(IDbConnectionFactory connectionFactory, IHttpContextAccessor httpContextAccessor)
        {
            _connectionFactory = connectionFactory;
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

        public async Task RegistrarAsync(string tabela, string acao, int registroId, string texto)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                INSERT INTO Justificativa (UsuarioId, EntidadeId, Tabela, Acao, RegistroId, Texto)
                VALUES (@UsuarioId, @EntidadeId, @Tabela, @Acao, @RegistroId, @Texto)";

            await connection.ExecuteAsync(sql, new
            {
                UsuarioId  = ObterUsuarioId(),
                EntidadeId = ObterEntidadeId(),
                Tabela     = tabela,
                Acao       = acao,
                RegistroId = registroId,
                Texto      = texto
            });
        }
    }
}