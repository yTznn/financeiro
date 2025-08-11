using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Implementação das regras de negócio para endereços de Entidade.
    /// Orquestra repositórios de Endereco e EntidadeEndereco.
    /// </summary>
    public class EntidadeEnderecoService : IEntidadeEnderecoService
    {
        private readonly IEnderecoRepositorio _enderecoRepo;
        private readonly IEntidadeEnderecoRepositorio _entidadeEnderecoRepo;

        public EntidadeEnderecoService(
            IEnderecoRepositorio enderecoRepo,
            IEntidadeEnderecoRepositorio entidadeEnderecoRepo)
        {
            _enderecoRepo = enderecoRepo;
            _entidadeEnderecoRepo = entidadeEnderecoRepo;
        }

        public Task<IEnumerable<Endereco>> ListarPorEntidadeAsync(int entidadeId)
            => _entidadeEnderecoRepo.ListarPorEntidadeAsync(entidadeId);

        public Task<Endereco?> ObterPrincipalPorEntidadeAsync(int entidadeId)
            => _entidadeEnderecoRepo.ObterPrincipalPorEntidadeAsync(entidadeId);

        public Task DefinirPrincipalEntidadeAsync(int entidadeId, int enderecoId)
            => _entidadeEnderecoRepo.DefinirPrincipalEntidadeAsync(entidadeId, enderecoId);

        /// <summary>
        /// 1) Insere em Endereco, 2) Vincula na Entidade, 3) Define principal quando:
        ///    - o caller pediu (definirComoPrincipal == true), ou
        ///    - não existe principal ainda para a Entidade.
        /// </summary>
        public async Task<(int enderecoId, bool principalAlterado)> CriarEnderecoParaEntidadeAsync(
            int entidadeId, EnderecoViewModel vm, bool definirComoPrincipal)
        {
            // 1) Endereco
            var novoEnderecoId = await _enderecoRepo.InserirRetornandoIdAsync(new Endereco
            {
                Logradouro  = vm.Logradouro,
                Numero      = vm.Numero,
                Complemento = vm.Complemento,
                Cep         = vm.Cep,
                Bairro      = vm.Bairro,
                Municipio   = vm.Municipio,
                Uf          = vm.Uf
            });

            // 2) Vincular (Principal = 0 por padrão)
            await _entidadeEnderecoRepo.VincularAsync(entidadeId, novoEnderecoId, ativo: true);

            // 3) Principal?
            var temPrincipal = await _entidadeEnderecoRepo.PossuiPrincipalAsync(entidadeId);
            var precisaDefinir = definirComoPrincipal || !temPrincipal;

            if (precisaDefinir)
            {
                await _entidadeEnderecoRepo.DefinirPrincipalEntidadeAsync(entidadeId, novoEnderecoId);
                return (novoEnderecoId, principalAlterado: true);
            }

            return (novoEnderecoId, principalAlterado: false);
        }
    }
}