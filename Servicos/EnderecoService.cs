using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Implementação do serviço de endereços para Pessoa Jurídica e Pessoa Física.
    /// Centraliza validações simples e delega persistência ao repositório.
    /// </summary>
    public class EnderecoService : IEnderecoService
    {
        private readonly IEnderecoRepositorio _repo;

        public EnderecoService(IEnderecoRepositorio repo)
        {
            _repo = repo;
        }

        /* ===================== LEGADO (único endereço PJ) ===================== */
        public Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId)
            => _repo.ObterPorPessoaAsync(pessoaJuridicaId);

        public Task InserirAsync(EnderecoViewModel vm)
            => _repo.InserirAsync(vm);

        public Task AtualizarAsync(int id, EnderecoViewModel vm)
            => _repo.AtualizarAsync(id, vm);

        /* ===================== PJ (múltiplos endereços) ===================== */
        public Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId)
            => _repo.ListarPorPessoaJuridicaAsync(pessoaJuridicaId);

        public Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId)
            => _repo.ObterPrincipalPorPessoaJuridicaAsync(pessoaJuridicaId);

        public Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
            => _repo.DefinirPrincipalPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);

        public Task VincularPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId, bool ativo = true)
            => _repo.VincularPessoaJuridicaAsync(pessoaJuridicaId, enderecoId, ativo);

        public Task<bool> PossuiPrincipalPessoaJuridicaAsync(int pessoaJuridicaId)
            => _repo.PossuiPrincipalPessoaJuridicaAsync(pessoaJuridicaId);

        /* ===================== PF (múltiplos endereços) ===================== */
        public Task<IEnumerable<Endereco>> ListarPorPessoaFisicaAsync(int pessoaFisicaId)
            => _repo.ListarPorPessoaFisicaAsync(pessoaFisicaId);

        public Task<Endereco?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId)
            => _repo.ObterPrincipalPorPessoaFisicaAsync(pessoaFisicaId);

        public Task DefinirPrincipalPessoaFisicaAsync(int pessoaFisicaId, int enderecoId)
            => _repo.DefinirPrincipalPessoaFisicaAsync(pessoaFisicaId, enderecoId);

        public Task VincularPessoaFisicaAsync(int pessoaFisicaId, int enderecoId, bool ativo = true)
            => _repo.VincularPessoaFisicaAsync(pessoaFisicaId, enderecoId, ativo);

        public Task<bool> PossuiPrincipalPessoaFisicaAsync(int pessoaFisicaId)
            => _repo.PossuiPrincipalPessoaFisicaAsync(pessoaFisicaId);

        /* ===================== UTILIDADE (reuso geral) ===================== */
        public Task<int> InserirRetornandoIdAsync(Endereco endereco)
            => _repo.InserirRetornandoIdAsync(endereco);

        // NOVO
        public Task<Endereco?> ObterPorIdAsync(int enderecoId)
            => _repo.ObterPorIdAsync(enderecoId);

        public Task ExcluirEnderecoPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
            => _repo.ExcluirEnderecoPessoaJuridicaAsync(pessoaJuridicaId, enderecoId);

        public Task ExcluirEnderecoPessoaFisicaAsync(int pessoaFisicaId, int enderecoId)
            => _repo.ExcluirEnderecoPessoaFisicaAsync(pessoaFisicaId, enderecoId);
    }
}