using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Implementação do serviço de endereços para Pessoa Jurídica.
    /// Centraliza validações simples e chama o repositório.
    /// </summary>
    public class EnderecoService : IEnderecoService
    {
        private readonly IEnderecoRepositorio _repo;

        public EnderecoService(IEnderecoRepositorio repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Retorna um endereço vinculado à pessoa jurídica (legado).
        /// </summary>
        public Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId)
            => _repo.ObterPorPessoaAsync(pessoaJuridicaId);

        /// <summary>
        /// Lista todos os endereços de uma Pessoa Jurídica (principal primeiro).
        /// </summary>
        public Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId)
            => _repo.ListarPorPessoaJuridicaAsync(pessoaJuridicaId);

        /// <summary>
        /// Retorna o endereço principal da Pessoa Jurídica (ou null).
        /// </summary>
        public Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId)
            => _repo.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

        /// <summary>
        /// Insere novo endereço e cria vínculo. Se não houver principal, este será marcado como principal.
        /// </summary>
        public Task InserirAsync(EnderecoViewModel vm)
        {
            // Validações mínimas (poderíamos adicionar regras mais específicas aqui)
            // Ex.: if (vm.PessoaJuridicaId <= 0) throw new ApplicationException("Pessoa Jurídica inválida.");
            return _repo.InserirAsync(vm);
        }

        /// <summary>
        /// Atualiza os dados do endereço existente.
        /// </summary>
        public Task AtualizarAsync(int id, EnderecoViewModel vm)
            => _repo.AtualizarAsync(id, vm);

        /// <summary>
        /// Define o endereço como principal para a Pessoa Jurídica (troca atômica).
        /// </summary>
        public Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
            => _repo.DefinirPrincipalPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);
    }
}