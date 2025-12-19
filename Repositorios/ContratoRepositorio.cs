using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization; 

namespace Financeiro.Repositorios
{
    public class ContratoRepositorio : IContratoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContratoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // ==============================================================================
        // MÉTODOS DE LEITURA E VALIDAÇÃO DE SALDO
        // ==============================================================================
        
        public async Task<decimal> ObterTotalComprometidoPorDetalheAsync(int orcamentoDetalheId, int? ignorarContratoId = null)
        {
            using var conn = _factory.CreateConnection();
            // Soma quanto já foi usado deste item específico em contratos ativos
            const string sql = @"
                SELECT SUM(ci.Valor) 
                FROM ContratoItem ci
                INNER JOIN Contrato c ON ci.ContratoId = c.Id
                WHERE ci.OrcamentoDetalheId = @orcamentoDetalheId 
                  AND c.Ativo = 1
                  AND (@ignorarId IS NULL OR c.Id <> @ignorarId)";

            var total = await conn.ExecuteScalarAsync<decimal?>(sql, new { orcamentoDetalheId, ignorarId = ignorarContratoId });
            return total ?? 0m;
        }

        public async Task<int> SugerirProximoNumeroAsync(int ano, int entidadeId)
        {
            // CORRIGIDO: Join via ContratoItem para chegar no Instrumento e validar a Entidade
            const string sql = @"
                SELECT ISNULL(MAX(c.NumeroContrato), 0) 
                FROM Contrato c
                WHERE c.AnoContrato = @ano
                  AND EXISTS (
                      SELECT 1 
                      FROM ContratoItem ci
                      INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                      INNER JOIN Orcamento o ON od.OrcamentoId = o.Id
                      INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                      WHERE ci.ContratoId = c.Id AND i.EntidadeId = @entidadeId
                  );";
                  
            using var conn = _factory.CreateConnection();
            var ultimoNumero = await conn.ExecuteScalarAsync<int>(sql, new { ano, entidadeId });
            return ultimoNumero + 1;
        }

        public async Task<bool> VerificarUnicidadeAsync(int numero, int ano, int entidadeId, int idAtual = 0)
        {
            // CORRIGIDO: Join via ContratoItem
            const string sql = @"
                SELECT COUNT(1) 
                FROM Contrato c
                WHERE c.NumeroContrato = @numero 
                  AND c.AnoContrato = @ano 
                  AND c.Id != @idAtual
                  AND EXISTS (
                      SELECT 1 
                      FROM ContratoItem ci
                      INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                      INNER JOIN Orcamento o ON od.OrcamentoId = o.Id
                      INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                      WHERE ci.ContratoId = c.Id AND i.EntidadeId = @entidadeId
                  );";
                  
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { numero, ano, entidadeId, idAtual });
        }

        public async Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina = 10)
        {
            // CORRIGIDO: Não usamos mais c.OrcamentoId. 
            // Usamos EXISTS para filtrar contratos que tenham itens pertencentes à Entidade logada.
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
                WHERE 
                    EXISTS (
                        SELECT 1 
                        FROM ContratoItem ci
                        INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                        INNER JOIN Orcamento o ON od.OrcamentoId = o.Id
                        INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                        WHERE ci.ContratoId = c.Id AND i.EntidadeId = @entidadeId
                    )
                ORDER BY 
                    c.AnoContrato DESC, c.NumeroContrato DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;

                SELECT COUNT(c.Id) 
                FROM Contrato c
                WHERE EXISTS (
                        SELECT 1 
                        FROM ContratoItem ci
                        INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                        INNER JOIN Orcamento o ON od.OrcamentoId = o.Id
                        INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                        WHERE ci.ContratoId = c.Id AND i.EntidadeId = @entidadeId
                    );";
            
            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { entidadeId, Offset = (pagina - 1) * tamanhoPagina, TamanhoPagina = tamanhoPagina });

            var itens = multi.Read<Contrato, string, ContratoListaViewModel>(
                (contrato, fornecedorNome) => new ContratoListaViewModel { Contrato = contrato, FornecedorNome = fornecedorNome },
                splitOn: "FornecedorNome"
            ).ToList();
            
            var totalItens = await multi.ReadSingleAsync<int>();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }
        public async Task<ContratoViewModel?> ObterParaEdicaoAsync(int id)
        {
            const string sqlContrato = "SELECT * FROM Contrato WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            
            var contrato = await conn.QuerySingleOrDefaultAsync<Contrato>(sqlContrato, new { id });
            if (contrato == null) return null;

            // RECUPERA OS ITENS
            // O valor vindo do banco (ci.Valor) é o Valor Total do item no contrato
            const string sqlItens = @"
                SELECT ci.OrcamentoDetalheId AS Id, od.Nome AS NomeItem, ci.Valor
                FROM ContratoItem ci
                INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                WHERE ci.ContratoId = @id;";
                
            var itens = await conn.QueryAsync<ContratoItemViewModel>(sqlItens, new { id });

            // Calcula meses apenas para definir o Valor Mensal Global (informativo do cabeçalho)
            int mesesEdicao = 1;
            if (contrato.DataFim >= contrato.DataInicio)
            {
                mesesEdicao = ((contrato.DataFim.Year - contrato.DataInicio.Year) * 12) + contrato.DataFim.Month - contrato.DataInicio.Month + 1;
                if (mesesEdicao < 1) mesesEdicao = 1;
            }

            var listaItens = itens.ToList();
            
            // OBS: O loop de divisão foi removido. 
            // Agora mandamos o valor TOTAL (ex: 400.000) direto para a view.

            // Tenta descobrir o OrcamentoId Pai através do primeiro item
            int? orcamentoId = null;
            if (listaItens.Any())
            {
                orcamentoId = await conn.ExecuteScalarAsync<int?>("SELECT OrcamentoId FROM OrcamentoDetalhe WHERE Id = @Id", new { Id = listaItens.First().Id });
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
                OrcamentoId = orcamentoId,
                Itens = listaItens // Lista com valores TOTAIS
            };

            // Calcula o valor mensal do contrato apenas para o campo de exibição no topo da tela
            if (vm.ValorContrato > 0)
            {
                decimal valorMensalCalc = vm.ValorContrato / mesesEdicao;
                vm.ValorMensal = valorMensalCalc.ToString("N2", new CultureInfo("pt-BR"));
            }

            return vm;
        }

        // ==============================================================================
        // MÉTODOS DE ESCRITA (INSERT / UPDATE / DELETE)
        // ==============================================================================

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
                    Ativo = vm.Ativo
                };

                const string sqlInsertContrato = @"
                    INSERT INTO Contrato 
                        (PessoaJuridicaId, PessoaFisicaId, NumeroContrato, AnoContrato, ObjetoContrato, DataAssinatura, DataInicio, DataFim, ValorContrato, Observacao, Ativo)
                    VALUES 
                        (@PessoaJuridicaId, @PessoaFisicaId, @NumeroContrato, @AnoContrato, @ObjetoContrato, @DataAssinatura, @DataInicio, @DataFim, @ValorContrato, @Observacao, @Ativo);
                    SELECT CAST(SCOPE_IDENTITY() as int);";
                
                var contratoId = await conn.QuerySingleAsync<int>(sqlInsertContrato, contrato, transaction);
                vm.Id = contratoId;

                if (vm.Itens != null && vm.Itens.Any())
                {
                    const string sqlItem = "INSERT INTO ContratoItem (ContratoId, OrcamentoDetalheId, Valor) VALUES (@ContratoId, @OrcamentoDetalheId, @Valor)";
                    foreach (var item in vm.Itens)
                    {
                        await conn.ExecuteAsync(sqlItem, new { ContratoId = contratoId, OrcamentoDetalheId = item.Id, Valor = item.Valor }, transaction);
                    }
                }
                
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
                
                // Limpa e Recria os Itens
                await conn.ExecuteAsync("DELETE FROM ContratoItem WHERE ContratoId = @ContratoId;", new { ContratoId = vm.Id }, transaction);
                
                if (vm.Itens != null && vm.Itens.Any())
                {
                    const string sqlItem = "INSERT INTO ContratoItem (ContratoId, OrcamentoDetalheId, Valor) VALUES (@ContratoId, @OrcamentoDetalheId, @Valor)";
                    foreach (var item in vm.Itens)
                    {
                        await conn.ExecuteAsync(sqlItem, new { ContratoId = vm.Id, OrcamentoDetalheId = item.Id, Valor = item.Valor }, transaction);
                    }
                }
                
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
            // Ordem de Exclusão: 1. Movimentações -> 2. Itens Versão -> 3. Versões -> 4. Itens Contrato -> 5. Contrato
            
            const string sqlDesvincularFinanceiro = "UPDATE MovimentacaoRateio SET ContratoId = NULL WHERE ContratoId = @id;";
            const string sqlDeleteItens = "DELETE FROM ContratoItem WHERE ContratoId = @id;";
            
            const string sqlVersaoItens = @"
                DELETE FROM ContratoVersaoItem 
                WHERE ContratoVersaoId IN (SELECT Id FROM ContratoVersao WHERE ContratoId = @id);";

            const string sqlVersao = "DELETE FROM ContratoVersao WHERE ContratoId = @id;";
            const string sqlContrato = "DELETE FROM Contrato WHERE Id = @id;";
            
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(sqlDesvincularFinanceiro, new { id }, tx);
                await conn.ExecuteAsync(sqlVersaoItens, new { id }, tx); 
                await conn.ExecuteAsync(sqlVersao, new { id }, tx);
                await conn.ExecuteAsync(sqlDeleteItens, new { id }, tx);
                await conn.ExecuteAsync(sqlContrato, new { id }, tx);
                
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        
        public async Task AtualizarVigenciaEValorAsync(int contratoId, DateTime inicio, DateTime fim, decimal valorTotal)
        {
            const string sql = @"
                UPDATE Contrato 
                SET DataInicio = @inicio, 
                    DataFim = @fim, 
                    ValorContrato = @valorTotal
                WHERE Id = @contratoId";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { contratoId, inicio, fim, valorTotal });
        }

        // ==============================================================================
        // MÉTODOS AUXILIARES
        // ==============================================================================

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

        public async Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto)
        {
            var (pfId, pjId) = ParseFornecedorId(idCompleto);
            if (pfId == null && pjId == null) return null;

            const string sql = "SELECT * FROM Vw_Fornecedores WHERE FornecedorId = @Id AND Tipo = @Tipo;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<VwFornecedor>(sql, new { Id = pfId ?? pjId, Tipo = pfId.HasValue ? "PF" : "PJ" });
        }

        private (int? pfId, int? pjId) ParseFornecedorId(string fornecedorIdCompleto)
        {
            if (string.IsNullOrEmpty(fornecedorIdCompleto)) return (null, null);
            var partes = fornecedorIdCompleto.Split('-');
            if (partes.Length != 2) return (null, null);
            var tipo = partes[0].ToUpper();
            var id = int.Parse(partes[1]);
            if (tipo == "PF") return (id, null);
            if (tipo == "PJ") return (null, id);
            return (null, null);
        }

        // Este método recupera contratos para o Rateio (Lançamento)
        public async Task<IEnumerable<dynamic>> ListarAtivosPorFornecedorAsync(int entidadeId, int fornecedorId, string tipoPessoa)
        {
            using var conn = _factory.CreateConnection();
            // CORRIGIDO: Join via ContratoItem para chegar na Entidade
            const string sql = @"
                SELECT DISTINCT
                    c.Id, 
                    c.NumeroContrato, 
                    c.ObjetoContrato 
                FROM Contrato c
                INNER JOIN ContratoItem ci ON c.Id = ci.ContratoId
                INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                INNER JOIN Orcamento o ON od.OrcamentoId = o.Id
                INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                WHERE c.Ativo = 1 
                AND i.EntidadeId = @entidadeId
                AND (
                    (@tipoPessoa = 'PJ' AND c.PessoaJuridicaId = @fornecedorId)
                    OR
                    (@tipoPessoa = 'PF' AND c.PessoaFisicaId = @fornecedorId)
                )
                ORDER BY c.NumeroContrato DESC";

            return await conn.QueryAsync(sql, new { entidadeId, fornecedorId, tipoPessoa });
        }

        // Método auxiliar para buscar os itens (detalhes) de um contrato específico para exibir na tela de Rateio
        public async Task<IEnumerable<dynamic>> ListarItensPorContratoAsync(int contratoId)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                SELECT 
                    ci.OrcamentoDetalheId AS Id, 
                    od.Nome AS Nome,
                    ci.Valor AS ValorTotalItemNoContrato
                FROM ContratoItem ci
                INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                WHERE ci.ContratoId = @contratoId 
                ORDER BY od.Nome";

            return await conn.QueryAsync(sql, new { contratoId });
        }
        public async Task<decimal> ObterTotalComprometidoPorOrcamentoAsync(int orcamentoId, int? ignorarContratoId = null)
        {
            using var conn = _factory.CreateConnection();
            // Soma todos os itens de contratos que pertencem, direta ou indiretamente, a este Orçamento
            const string sql = @"
                SELECT SUM(ci.Valor) 
                FROM ContratoItem ci
                INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                INNER JOIN Contrato c ON ci.ContratoId = c.Id
                WHERE od.OrcamentoId = @orcamentoId 
                    AND c.Ativo = 1
                    AND (@ignorarId IS NULL OR c.Id <> @ignorarId)";

            var total = await conn.ExecuteScalarAsync<decimal?>(sql, new { orcamentoId, ignorarId = ignorarContratoId });
            return total ?? 0m;
        }
    }
}