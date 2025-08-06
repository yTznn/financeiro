using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class ContratoRepositorio : IContratoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContratoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // --- Operações de Gravação (CRUD) ---

        public async Task InserirAsync(ContratoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Converte o FornecedorIdCompleto ("PF-123") em IDs separados
                var (pessoaFisicaId, pessoaJuridicaId) = ParseFornecedorId(vm.FornecedorIdCompleto);

                var contrato = new Contrato
                {
                    PessoaFisicaId = pessoaFisicaId,
                    PessoaJuridicaId = pessoaJuridicaId,
                    NumeroContrato = vm.NumeroContrato,
                    AnoContrato = vm.AnoContrato,
                    ObjetoContrato = vm.ObjetoContrato,
                    DataAssinatura = vm.DataAssinatura,
                    DataInicio = vm.DataInicio,
                    DataFim = vm.DataFim,
                    ValorContrato = vm.ValorContrato,
                    Observacao = vm.Observacao,
                    Ativo = vm.Ativo
                };

                const string sqlInsertContrato = @"
                    INSERT INTO Contrato (PessoaJuridicaId, PessoaFisicaId, NumeroContrato, AnoContrato, ObjetoContrato, DataAssinatura, DataInicio, DataFim, ValorContrato, Observacao, Ativo)
                    VALUES (@PessoaJuridicaId, @PessoaFisicaId, @NumeroContrato, @AnoContrato, @ObjetoContrato, @DataAssinatura, @DataInicio, @DataFim, @ValorContrato, @Observacao, @Ativo);
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                
                var contratoId = await conn.QuerySingleAsync<int>(sqlInsertContrato, contrato, transaction);

                // Insere os vínculos na tabela ContratoNatureza
                await InserirContratoNaturezasAsync(conn, transaction, contratoId, vm.NaturezasIds);

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task AtualizarAsync(ContratoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var (pessoaFisicaId, pessoaJuridicaId) = ParseFornecedorId(vm.FornecedorIdCompleto);

                var contrato = new Contrato
                {
                    Id = vm.Id,
                    PessoaFisicaId = pessoaFisicaId,
                    PessoaJuridicaId = pessoaJuridicaId,
                    NumeroContrato = vm.NumeroContrato,
                    AnoContrato = vm.AnoContrato,
                    ObjetoContrato = vm.ObjetoContrato,
                    DataAssinatura = vm.DataAssinatura,
                    DataInicio = vm.DataInicio,
                    DataFim = vm.DataFim,
                    ValorContrato = vm.ValorContrato,
                    Observacao = vm.Observacao,
                    Ativo = vm.Ativo
                };

                const string sqlUpdateContrato = @"
                    UPDATE Contrato SET
                        PessoaJuridicaId = @PessoaJuridicaId,
                        PessoaFisicaId = @PessoaFisicaId,
                        NumeroContrato = @NumeroContrato,
                        AnoContrato = @AnoContrato,
                        ObjetoContrato = @ObjetoContrato,
                        DataAssinatura = @DataAssinatura,
                        DataInicio = @DataInicio,
                        DataFim = @DataFim,
                        ValorContrato = @ValorContrato,
                        Observacao = @Observacao,
                        Ativo = @Ativo
                    WHERE Id = @Id;";
                
                await conn.ExecuteAsync(sqlUpdateContrato, contrato, transaction);

                // Remove os vínculos antigos e insere os novos
                const string sqlDeleteNaturezas = "DELETE FROM ContratoNatureza WHERE ContratoId = @ContratoId;";
                await conn.ExecuteAsync(sqlDeleteNaturezas, new { ContratoId = vm.Id }, transaction);
                
                await InserirContratoNaturezasAsync(conn, transaction, vm.Id, vm.NaturezasIds);

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task ExcluirAsync(int id)
        {
            const string sql = "DELETE FROM Contrato WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { id });
        }

        // --- Operações de Consulta ---

        public async Task<ContratoViewModel?> ObterParaEdicaoAsync(int id)
        {
            const string sqlContrato = "SELECT * FROM Contrato WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            
            var contrato = await conn.QuerySingleOrDefaultAsync<Contrato>(sqlContrato, new { id });
            if (contrato == null) return null;

            const string sqlNaturezas = "SELECT NaturezaId FROM ContratoNatureza WHERE ContratoId = @id;";
            var naturezasIds = await conn.QueryAsync<int>(sqlNaturezas, new { id });

            var vm = new ContratoViewModel
            {
                Id = contrato.Id,
                FornecedorIdCompleto = contrato.PessoaFisicaId.HasValue ? $"PF-{contrato.PessoaFisicaId}" : $"PJ-{contrato.PessoaJuridicaId}",
                NumeroContrato = contrato.NumeroContrato,
                AnoContrato = contrato.AnoContrato,
                ObjetoContrato = contrato.ObjetoContrato,
                DataAssinatura = contrato.DataAssinatura,
                DataInicio = contrato.DataInicio,
                DataFim = contrato.DataFim,
                ValorContrato = contrato.ValorContrato,
                Observacao = contrato.Observacao,
                Ativo = contrato.Ativo,
                NaturezasIds = naturezasIds.ToList()
            };

            return vm;
        }

        public async Task<(IEnumerable<Contrato> Itens, int TotalPaginas)> ListarPaginadoAsync(int pagina, int tamanhoPagina = 10)
        {
            const string sql = @"
                SELECT * FROM Contrato
                ORDER BY AnoContrato DESC, NumeroContrato DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;

                SELECT COUNT(Id) FROM Contrato;";
            
            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = (pagina - 1) * tamanhoPagina, TamanhoPagina = tamanhoPagina });

            var itens = await multi.ReadAsync<Contrato>();
            var totalItens = await multi.ReadSingleAsync<int>();

            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }

        // --- Métodos de Negócio e Validação ---

        public async Task<int> SugerirProximoNumeroAsync(int ano)
        {
            const string sql = "SELECT ISNULL(MAX(NumeroContrato), 0) FROM Contrato WHERE AnoContrato = @ano;";
            using var conn = _factory.CreateConnection();
            var ultimoNumero = await conn.QuerySingleAsync<int>(sql, new { ano });
            return ultimoNumero + 1;
        }

        public async Task<bool> VerificarUnicidadeAsync(int numero, int ano, int idAtual = 0)
        {
            const string sql = "SELECT COUNT(1) FROM Contrato WHERE NumeroContrato = @numero AND AnoContrato = @ano AND Id != @idAtual;";
            using var conn = _factory.CreateConnection();
            var existe = await conn.ExecuteScalarAsync<bool>(sql, new { numero, ano, idAtual });
            return existe;
        }

        // --- Métodos de Busca para Formulário ---

        public async Task<(IEnumerable<VwFornecedor> Itens, bool TemMais)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina = 10)
        {
            const string sql = @"
                SELECT * FROM Vw_Fornecedores
                WHERE Nome LIKE @TermoBusca
                ORDER BY Nome
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;";
            
            using var conn = _factory.CreateConnection();
            var itens = await conn.QueryAsync<VwFornecedor>(sql, new 
            { 
                TermoBusca = $"%{termoBusca}%",
                Offset = (pagina - 1) * tamanhoPagina,
                TamanhoPagina = tamanhoPagina + 1 
            });

            var temMais = itens.Count() > tamanhoPagina;
            return (itens.Take(tamanhoPagina), temMais);
        }

        public async Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync()
        {
            const string sql = "SELECT * FROM Natureza WHERE Ativo = 1 ORDER BY Nome;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Natureza>(sql);
        }

        // ✅ NOVO MÉTODO COMPLETO
        public async Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto)
        {
            var (pfId, pjId) = ParseFornecedorId(idCompleto);
            if (pfId == null && pjId == null) return null;

            const string sql = "SELECT * FROM Vw_Fornecedores WHERE FornecedorId = @Id AND Tipo = @Tipo;";
            
            using var conn = _factory.CreateConnection();
            
            return await conn.QuerySingleOrDefaultAsync<VwFornecedor>(sql, new 
            { 
                Id = pfId ?? pjId, 
                Tipo = pfId.HasValue ? "PF" : "PJ"
            });
        }

        // --- Métodos Auxiliares ---

        private async Task InserirContratoNaturezasAsync(IDbConnection conn, IDbTransaction transaction, int contratoId, List<int> naturezasIds)
        {
            if (naturezasIds == null || !naturezasIds.Any()) return;

            const string sql = "INSERT INTO ContratoNatureza (ContratoId, NaturezaId) VALUES (@ContratoId, @NaturezaId);";
            foreach (var naturezaId in naturezasIds)
            {
                await conn.ExecuteAsync(sql, new { ContratoId = contratoId, NaturezaId = naturezaId }, transaction);
            }
        }

        private (int? pfId, int? pjId) ParseFornecedorId(string fornecedorIdCompleto)
        {
            if (string.IsNullOrEmpty(fornecedorIdCompleto))
            {
                return (null, null);
            }

            var partes = fornecedorIdCompleto.Split('-');
            if (partes.Length != 2) throw new ArgumentException("FornecedorId inválido.");

            var tipo = partes[0].ToUpper();
            var id = int.Parse(partes[1]);

            if (tipo == "PF") return (id, null);
            if (tipo == "PJ") return (null, id);
            
            return (null, null);
        }
    }
}