using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class LogRepositorio : ILogRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LogRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task RegistrarAsync(Log log)
        {
            const string sql = @"
                INSERT INTO LogAcao
                    (UsuarioId, EntidadeId, Acao, Tabela, DataHora,
                     ValoresAnteriores, ValoresNovos, RegistroId)
                VALUES
                    (@UsuarioId, @EntidadeId, @Acao, @Tabela, @DataHora,
                     @ValoresAnteriores, @ValoresNovos, @RegistroId);";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, log);   // passa o objeto log direto
        }
        public async Task<(IEnumerable<LogListagemViewModel> Itens, int Total)> ListarLogsPaginadosAsync(int pagina, int tamanho, int? usuarioId)
        {
            var offset = (pagina - 1) * tamanho;
            // O filtro só é aplicado se usuarioId for fornecido
            var filtroUsuario = usuarioId.HasValue ? "AND l.UsuarioId = @UsuarioId" : "";

            var sql = $@"
                SELECT 
                    l.Id, 
                    l.Acao, 
                    l.Tabela, 
                    l.DataHora,
                    l.ValoresNovos, -- Usado como Detalhes
                    u.NameSkip as UsuarioNome,
                    e.Sigla as EntidadeSigla
                FROM LogAcao l
                INNER JOIN Usuarios u ON l.UsuarioId = u.Id
                INNER JOIN Entidade e ON l.EntidadeId = e.Id
                WHERE 1=1 {filtroUsuario}
                ORDER BY l.DataHora DESC
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;

                SELECT COUNT(l.Id) FROM LogAcao l WHERE 1=1 {filtroUsuario};";

            using var conn = _connectionFactory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { 
                Offset = offset, 
                Tamanho = tamanho, 
                UsuarioId = usuarioId 
            });

            var itens = await multi.ReadAsync<LogListagemViewModel>();
            var total = await multi.ReadFirstAsync<int>();

            return (itens, total);
        }

        public async Task<IEnumerable<dynamic>> BuscarUsuariosParaSelectAsync(string termo)
        {
            // Query otimizada para buscar NameSkip ou Nome da Pessoa Física
            const string sql = @"
                SELECT TOP 20 
                    u.Id, 
                    u.NameSkip + ' (' + ISNULL(pf.Nome, 'S/ PF') + ')' as Text 
                FROM Usuarios u
                LEFT JOIN PessoaFisica pf ON u.PessoaFisicaId = pf.Id
                WHERE u.NameSkip LIKE @Termo OR pf.Nome LIKE @Termo OR pf.Sobrenome LIKE @Termo
                ORDER BY u.NameSkip";
            
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync(sql, new { Termo = $"%{termo}%" });
        }
    }
}