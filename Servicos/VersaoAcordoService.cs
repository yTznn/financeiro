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
        private readonly IInstrumentoRepositorio _instrRepo;
        private readonly IInstrumentoVersaoRepositorio _versaoRepo;

        public VersaoAcordoService(IInstrumentoRepositorio instrRepo,
                                   IInstrumentoVersaoRepositorio versaoRepo)
        {
            _instrRepo  = instrRepo;
            _versaoRepo = versaoRepo;
        }

        public async Task CriarAditivoAsync(AditivoViewModel vm)
        {
            if (vm is null) throw new ArgumentNullException(nameof(vm));
            if (vm.TipoAcordoId <= 0) throw new ArgumentException("Instrumento inválido.");

            // 1) Obtém a versão vigente do instrumento
            var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId)
                       ?? throw new Exception("Versão atual não encontrada.");

            var novaDataInicio = (vm.NovaDataInicio ?? DateTime.Today).Date;

            if (novaDataInicio < atual.VigenciaInicio.Date)
                throw new ArgumentException("A Data de Início do aditivo não pode ser anterior à vigência atual.");

            if (novaDataInicio == atual.VigenciaInicio.Date)
            {
                // Aditivo no MESMO dia: apenas atualiza os detalhes da versão vigente
                var novoValorFinal = vm.NovoValor ?? atual.Valor;

                await _versaoRepo.AtualizarDetalhesAsync(
                    versaoId: atual.Id,
                    novoValor: novoValorFinal,
                    tipoAditivo: vm.TipoAditivo,
                    observacao: vm.Observacao,
                    dataAssinatura: vm.DataAssinatura
                );
            }
            else
            {
                // Aditivo em DATA FUTURA:
                // 2) Fecha a vigência da versão atual no dia anterior
                await _versaoRepo.AtualizarVigenciaFimAsync(atual.Id, novaDataInicio.AddDays(-1));

                // 3) Cria a nova versão que passa a vigorar
                var novaVersao = new AcordoVersao
                {
                    TipoAcordoId   = atual.TipoAcordoId,
                    Versao         = atual.Versao + 1,
                    VigenciaInicio = novaDataInicio,
                    VigenciaFim    = null, // vigente
                    Valor          = vm.NovoValor ?? atual.Valor,
                    Objeto         = atual.Objeto,      // mantém
                    TipoAditivo    = vm.TipoAditivo,
                    Observacao     = vm.Observacao,
                    DataAssinatura = vm.DataAssinatura,
                    DataRegistro   = DateTime.Now
                };

                await _versaoRepo.InserirAsync(novaVersao);
            }

            // 4) Reflete no Instrumento pai (valor/datas)
            var instrumento = await _instrRepo.ObterPorIdAsync(vm.TipoAcordoId)
                             ?? throw new Exception("Instrumento não encontrado.");

            // Obtém novamente a versão vigente já atualizada/criada acima
            var vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId)
                         ?? throw new Exception("Versão vigente não encontrada.");

            var vmUpdate = new TipoAcordoViewModel
            {
                Id             = instrumento.Id,
                Numero         = instrumento.Numero,
                Valor          = vigente.Valor,
                Objeto         = instrumento.Objeto,
                DataInicio     = vigente.VigenciaInicio,
                DataFim        = vm.NovaDataFim ?? instrumento.DataFim,
                Ativo          = instrumento.Ativo,
                Observacao     = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId     = instrumento.EntidadeId
            };

            await _instrRepo.AtualizarAsync(instrumento.Id, vmUpdate);
        }
    }
}