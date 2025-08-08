using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.DTO;
using Financeiro.Repositorios;
using Financeiro.Utils;
using Microsoft.Extensions.Logging;

namespace Financeiro.Servicos
{
    public class EntidadeService : IEntidadeService
    {
        private readonly IEntidadeRepositorio        _entidadeRepo;
        private readonly IPessoaJuridicaRepositorio  _pessoaJuridicaRepo;
        private readonly ILogger<EntidadeService>    _logger;

        public EntidadeService(
            IEntidadeRepositorio        entidadeRepo,
            IPessoaJuridicaRepositorio  pessoaJuridicaRepo,
            ILogger<EntidadeService>    logger)
        {
            _entidadeRepo       = entidadeRepo;
            _pessoaJuridicaRepo = pessoaJuridicaRepo;
            _logger             = logger;
        }

        #region CRUD público

        public async Task<int> CriarAsync(Entidade entidade)
        {
            await ValidarEntidadeAsync(entidade, isUpdate: false);
            return await _entidadeRepo.AddAsync(entidade);
        }

        public async Task AtualizarAsync(Entidade entidade)
        {
            await ValidarEntidadeAsync(entidade, isUpdate: true);
            await _entidadeRepo.UpdateAsync(entidade);
        }

        public async Task ExcluirAsync(int id) =>
            await _entidadeRepo.DeleteAsync(id);

        public async Task<Entidade?> ObterPorIdAsync(int id) =>
            await _entidadeRepo.GetByIdAsync(id);

        public async Task<IEnumerable<Entidade>> ListarAsync() =>
            await _entidadeRepo.ListAsync();

        #endregion

        #region Auto-fill por CNPJ

        public async Task<EntidadeAutoFillDto?> AutoFillPorCnpjAsync(string cnpj)
        {
            cnpj = DocumentoUtils.ApenasNumeros(cnpj);

            // 1) Já existe Entidade com esse CNPJ?
            var entidadeExistente = await _entidadeRepo.GetByCnpjAsync(cnpj);
            if (entidadeExistente != null)
            {
                return new EntidadeAutoFillDto(
                    entidadeExistente.Nome,
                    entidadeExistente.Sigla,
                    entidadeExistente.EnderecoId,
                    entidadeExistente.ContaBancariaId
                );
            }

            // 2) Procura na tabela PessoaJuridica
            var pj = await _pessoaJuridicaRepo.ObterPorCnpjAsync(cnpj);
            if (pj == null) return null;

            // Ainda não temos relação de endereço/conta → devolvemos null
            int? enderecoId = null;
            int? contaId    = null;

            return new EntidadeAutoFillDto(
                pj.RazaoSocial,
                pj.NomeFantasia,   // usando Nome Fantasia como Sigla provisória
                enderecoId,
                contaId
            );
        }

        #endregion

        #region Validações internas

        private async Task ValidarEntidadeAsync(Entidade entidade, bool isUpdate)
        {
            entidade.Cnpj = DocumentoUtils.ApenasNumeros(entidade.Cnpj);

            if (!DocumentoUtils.CnpjEhValido(entidade.Cnpj))
                throw new ApplicationException("CNPJ inválido.");

            var existente = await _entidadeRepo.GetByCnpjAsync(entidade.Cnpj);
            if (existente != null && existente.Id != entidade.Id)
                throw new ApplicationException("Já existe uma entidade cadastrada com esse CNPJ.");
        }

        #endregion
    }
}