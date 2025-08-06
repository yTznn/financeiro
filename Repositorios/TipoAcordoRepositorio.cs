using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Infraestrutura;

namespace Financeiro.Repositorios
{
    public class TipoAcordoRepositorio : ITipoAcordoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public TipoAcordoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InserirAsync(TipoAcordoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
                INSERT INTO TipoAcordo
                    (Numero, Valor, Objeto, DataInicio, DataFim, Ativo, Observacao, DataAssinatura)
                VALUES
                    (@Numero, @Valor, @Objeto, @DataInicio, @DataFim, @Ativo, @Observacao, @DataAssinatura)";
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task AtualizarAsync(int id, TipoAcordoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
                UPDATE TipoAcordo
                SET Numero         = @Numero,
                    Valor          = @Valor,
                    Objeto         = @Objeto,
                    DataInicio     = @DataInicio,
                    DataFim        = @DataFim,
                    Ativo          = @Ativo,
                    Observacao     = @Observacao,
                    DataAssinatura = @DataAssinatura
                WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new
            {
                vm.Numero,
                vm.Valor,
                vm.Objeto,
                vm.DataInicio,
                vm.DataFim,
                vm.Ativo,
                vm.Observacao,
                vm.DataAssinatura,
                Id = id
            });
        }

        public async Task<TipoAcordo?> ObterPorIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM TipoAcordo WHERE Id = @id";
            return await conn.QueryFirstOrDefaultAsync<TipoAcordo>(sql, new { id });
        }

        public async Task<IEnumerable<TipoAcordo>> ListarAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM TipoAcordo ORDER BY DataInicio DESC";
            return await conn.QueryAsync<TipoAcordo>(sql);
        }
    }
}