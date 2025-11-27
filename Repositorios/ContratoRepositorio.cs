using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization; // Necessário para converter decimal para string PT-BR

namespace Financeiro.Repositorios
{
    public class ContratoRepositorio : IContratoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContratoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // [NOVO] Implementação do cálculo de saldo comprometido do orçamento
        public async Task<decimal> ObterTotalComprometidoPorOrcamentoAsync(int orcamentoId, int? ignorarContratoId = null)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                SELECT SUM(ValorContrato) 
                FROM Contrato 
                WHERE OrcamentoId = @orcamentoId 
                  AND Ativo = 1
                  AND (@ignorarId IS NULL OR Id <> @ignorarId)";

            var total = await conn.ExecuteScalarAsync<decimal?>(sql, new { orcamentoId, ignorarId = ignorarContratoId });
            return total ?? 0m;
        }

        public async Task InserirAsync(ContratoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
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
                    Ativo = vm.Ativo,
                    OrcamentoId = vm.OrcamentoId
                };

                const string sqlInsertContrato = @"
                    INSERT INTO Contrato 
                        (PessoaJuridicaId, PessoaFisicaId, NumeroContrato, AnoContrato, ObjetoContrato, DataAssinatura, DataInicio, DataFim, ValorContrato, Observacao, Ativo, OrcamentoId)
                    VALUES 
                        (@PessoaJuridicaId, @PessoaFisicaId, @NumeroContrato, @AnoContrato, @ObjetoContrato, @DataAssinatura, @DataInicio, @DataFim, @ValorContrato, @Observacao, @Ativo, @OrcamentoId);
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                
                var contratoId = await conn.QuerySingleAsync<int>(sqlInsertContrato, contrato, transaction);
                vm.Id = contratoId;

                // Passamos a lista completa de naturezas (com valor)
                await InserirContratoNaturezasAsync(conn, transaction, contratoId, vm.Naturezas);
                
                transaction.Commit();
            }
            catch
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
                    Ativo = vm.Ativo,
                    OrcamentoId = vm.OrcamentoId
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
                        Ativo = @Ativo,
                        OrcamentoId = @OrcamentoId
                    WHERE Id = @Id;";
                
                await conn.ExecuteAsync(sqlUpdateContrato, contrato, transaction);
                
                const string sqlDeleteNaturezas = "DELETE FROM ContratoNatureza WHERE ContratoId = @ContratoId;";
                await conn.ExecuteAsync(sqlDeleteNaturezas, new { ContratoId = vm.Id }, transaction);
                
                // Passamos a lista completa de naturezas (com valor)
                await InserirContratoNaturezasAsync(conn, transaction, vm.Id, vm.Naturezas);
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task ExcluirAsync(int id)
        {
            const string sqlNatureza = "DELETE FROM ContratoNatureza WHERE ContratoId = @id;";
            const string sqlContrato = "DELETE FROM Contrato WHERE Id = @id;";
            
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(sqlNatureza, new { id }, tx);
                await conn.ExecuteAsync(sqlContrato, new { id }, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<ContratoViewModel?> ObterParaEdicaoAsync(int id)
        {
            const string sqlContrato = "SELECT * FROM Contrato WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            
            var contrato = await conn.QuerySingleOrDefaultAsync<Contrato>(sqlContrato, new { id });
            if (contrato == null) return null;

            // [ATUALIZADO] Busca NaturezaId + Valor e o Nome da Natureza (JOIN)
            const string sqlNaturezas = @"
                SELECT cn.NaturezaId, n.Nome AS NomeNatureza, cn.Valor
                FROM ContratoNatureza cn
                INNER JOIN Natureza n ON cn.NaturezaId = n.Id
                WHERE cn.ContratoId = @id;";
                
            var naturezas = await conn.QueryAsync<ContratoNaturezaViewModel>(sqlNaturezas, new { id });

            // Calcula o número de meses para reverter o valor TOTAL -> MENSAL
            int mesesEdicao = 1;
            if (contrato.DataFim >= contrato.DataInicio)
            {
                mesesEdicao = ((contrato.DataFim.Year - contrato.DataInicio.Year) * 12) + contrato.DataFim.Month - contrato.DataInicio.Month + 1;
                if (mesesEdicao < 1) mesesEdicao = 1;
            }

            // Ajusta os valores das naturezas (Banco tem Total -> Tela quer Mensal)
            var listaNaturezas = naturezas.ToList();
            foreach (var n in listaNaturezas)
            {
                n.Valor = n.Valor / mesesEdicao;
            }

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
                OrcamentoId = contrato.OrcamentoId,
                Naturezas = listaNaturezas
            };

            // [CORREÇÃO DO ERRO DE COMPILAÇÃO]
            // Convertemos o cálculo decimal para string formatada em PT-BR
            if (vm.ValorContrato > 0)
            {
                decimal valorMensalCalc = vm.ValorContrato / mesesEdicao;
                // "N2" formata com 2 casas decimais e separadores (ex: "25.000,00")
                vm.ValorMensal = valorMensalCalc.ToString("N2", new CultureInfo("pt-BR"));
            }

            return vm;
        }

        // [MÉTODO ATUALIZADO] Grava o valor na tabela de ligação
        private async Task InserirContratoNaturezasAsync(IDbConnection conn, IDbTransaction transaction, int contratoId, List<ContratoNaturezaViewModel> naturezas)
        {
            if (naturezas == null || !naturezas.Any()) return;

            const string sql = "INSERT INTO ContratoNatureza (ContratoId, NaturezaId, Valor) VALUES (@ContratoId, @NaturezaId, @Valor);";
            foreach (var item in naturezas)
            {
                // Salva apenas naturezas com valor > 0 ou salva zerado se quiser permitir
                if (item.Valor >= 0) 
                {
                    await conn.ExecuteAsync(sql, new { ContratoId = contratoId, item.NaturezaId, item.Valor }, transaction);
                }
            }
        }

        public async Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int pagina, int tamanhoPagina = 10)
        {
            const string sql = @"
                SELECT 
                    c.*, 
                    COALESCE(pj.RazaoSocial, pf.Nome + ' ' + pf.Sobrenome) AS FornecedorNome
                FROM 
                    Contrato c
                LEFT JOIN 
                    PessoaJuridica pj ON c.PessoaJuridicaId = pj.Id
                LEFT JOIN 
                    PessoaFisica pf ON c.PessoaFisicaId = pf.Id
                ORDER BY 
                    c.AnoContrato DESC, c.NumeroContrato DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;

                SELECT COUNT(Id) FROM Contrato;";
            
            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = (pagina - 1) * tamanhoPagina, TamanhoPagina = tamanhoPagina });

            var itens = multi.Read<Contrato, string, ContratoListaViewModel>(
                (contrato, fornecedorNome) => new ContratoListaViewModel { Contrato = contrato, FornecedorNome = fornecedorNome },
                splitOn: "FornecedorNome"
            ).ToList();
            
            var totalItens = await multi.ReadSingleAsync<int>();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }

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
            return await conn.ExecuteScalarAsync<bool>(sql, new { numero, ano, idAtual });
        }
        
        public async Task<(IEnumerable<VwFornecedor> Itens, int TotalItens)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina)
        {
            const string sql = @"
                SELECT * FROM Vw_Fornecedores
                WHERE Nome LIKE @TermoBusca OR Documento LIKE @TermoBusca
                ORDER BY Nome
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;

                SELECT COUNT(*) FROM Vw_Fornecedores
                WHERE Nome LIKE @TermoBusca OR Documento LIKE @TermoBusca;";
            
            using var conn = _factory.CreateConnection();
            
            using var multi = await conn.QueryMultipleAsync(sql, new 
            { 
                TermoBusca = $"%{termoBusca}%",
                Offset = (pagina - 1) * tamanhoPagina,
                TamanhoPagina = tamanhoPagina
            });

            var itens = await multi.ReadAsync<VwFornecedor>();
            var totalItens = await multi.ReadSingleAsync<int>();

            return (itens, totalItens);
        }

        public async Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync()
        {
            const string sql = "SELECT * FROM Natureza WHERE Ativo = 1 ORDER BY Nome;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Natureza>(sql);
        }

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

        private (int? pfId, int? pjId) ParseFornecedorId(string fornecedorIdCompleto)
        {
            if (string.IsNullOrEmpty(fornecedorIdCompleto)) return (null, null);

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