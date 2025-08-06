using System;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Responsável por registrar aditivos (versões) e manter o histórico.
    /// </summary>
    public class VersaoAcordoService : IVersaoAcordoService
    {
        private readonly ITipoAcordoRepositorio _acordoRepo;
        private readonly IAditivoRepositorio    _versaoRepo;

        public VersaoAcordoService(ITipoAcordoRepositorio acordoRepo,
                                   IAditivoRepositorio versaoRepo)
        {
            _acordoRepo = acordoRepo;
            _versaoRepo = versaoRepo;
        }

        public async Task CriarAditivoAsync(AditivoViewModel vm)
        {
            /* 1) Obtém a versão atual (vigente) */
            var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId)
                        ?? throw new Exception("Versão atual não encontrada.");

            /* 2) Fecha a vigência atual */
            atual.VigenciaFim = (vm.NovaDataInicio ?? DateTime.Today).AddDays(-1);

            // Atualiza a linha antiga (pode ser via update; aqui, sobrescrevemos)
            await _versaoRepo.InserirAsync(atual);

            /* 3) Número da nova versão */
            int novaVersaoNum = atual.Versao + 1;

            /* 4) Cria a nova versão */
            var novaVersao = new AcordoVersao
            {
                TipoAcordoId     = atual.TipoAcordoId,
                Versao           = novaVersaoNum,
                VigenciaInicio   = vm.NovaDataInicio ?? atual.VigenciaInicio,
                VigenciaFim      = null,                         // vigente
                Valor            = vm.NovoValor      ?? atual.Valor,
                Objeto           = atual.Objeto,                // mantém
                TipoAditivo      = vm.TipoAditivo,
                Observacao       = vm.Observacao,
                DataAssinatura   = vm.DataAssinatura,
                DataRegistro     = DateTime.Now
            };

            await _versaoRepo.InserirAsync(novaVersao);

            /* 5) Reflete na tabela TipoAcordo (valor e vigência em vigor) */
            var acordoPai = await _acordoRepo.ObterPorIdAsync(vm.TipoAcordoId)
                          ?? throw new Exception("TipoAcordo não encontrado.");

            acordoPai.Valor      = novaVersao.Valor;
            acordoPai.DataInicio = novaVersao.VigenciaInicio;
            acordoPai.DataFim    = vm.NovaDataFim ?? acordoPai.DataFim;

            // Converte para ViewModel porque AtualizarAsync espera esse tipo
            var vmUpdate = new TipoAcordoViewModel
            {
                Id             = acordoPai.Id,
                Numero         = acordoPai.Numero,
                Valor          = acordoPai.Valor,
                Objeto         = acordoPai.Objeto,
                DataInicio     = acordoPai.DataInicio,
                DataFim        = acordoPai.DataFim,
                Ativo          = acordoPai.Ativo,
                Observacao     = acordoPai.Observacao,
                DataAssinatura = acordoPai.DataAssinatura
            };

            await _acordoRepo.AtualizarAsync(acordoPai.Id, vmUpdate);
        }
    }
}