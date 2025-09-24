using System;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    public class InstrumentoVersaoService : IInstrumentoVersaoService
    {
        private readonly IInstrumentoRepositorio _instrRepo;
        private readonly IInstrumentoVersaoRepositorio _versaoRepo;

        public InstrumentoVersaoService(
            IInstrumentoRepositorio instrRepo,
            IInstrumentoVersaoRepositorio versaoRepo)
        {
            _instrRepo = instrRepo;
            _versaoRepo = versaoRepo;
        }

        public async Task CriarAditivoAsync(AditivoInstrumentoViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            var instrumento = await _instrRepo.ObterPorIdAsync(vm.InstrumentoId)
                               ?? throw new ArgumentException("Instrumento não encontrado.");

            // Garante versão inicial
            var vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
            if (vigente == null)
            {
                var original = new InstrumentoVersao
                {
                    InstrumentoId  = instrumento.Id,
                    Versao         = 1,
                    VigenciaInicio = instrumento.DataInicio,
                    VigenciaFim    = null,
                    Valor          = instrumento.Valor,
                    Objeto         = instrumento.Objeto,
                    TipoAditivo    = null,
                    Observacao     = "Versão original",
                    DataAssinatura = instrumento.DataAssinatura,
                    DataRegistro   = DateTime.Now
                };
                await _versaoRepo.InserirAsync(original);
                vigente = original;
            }

            // Calcula novo valor (delta positivo/negativo já foi normalizado no Controller)
            decimal valorFinal = vigente.Valor;
            bool alteraValor = vm.TipoAditivo is TipoAditivo.Acrescimo
                                             or TipoAditivo.Supressao
                                             or TipoAditivo.PrazoAcrescimo
                                             or TipoAditivo.PrazoSupressao;
            if (alteraValor)
            {
                if (!vm.NovoValor.HasValue || vm.NovoValor.Value == 0)
                    throw new ArgumentException("Informe um valor de aditivo diferente de zero.");
                valorFinal = vigente.Valor + vm.NovoValor.Value;
                if (valorFinal < 0) throw new ArgumentException("O valor do instrumento não pode ficar negativo.");
            }

            bool alteraPrazo = vm.TipoAditivo is TipoAditivo.Prazo
                                             or TipoAditivo.PrazoAcrescimo
                                             or TipoAditivo.PrazoSupressao;

            DateTime novaIni = alteraPrazo ? (vm.NovaDataInicio ?? vigente.VigenciaInicio) : vigente.VigenciaInicio;
            DateTime? novaFim = alteraPrazo ? vm.NovaDataFim : vigente.VigenciaFim;

            // Se alterar prazo e houver sobreposição, encerramos a vigente no dia anterior ao novo início
            if (alteraPrazo && vm.NovaDataInicio.HasValue && vm.NovaDataInicio.Value > vigente.VigenciaInicio)
            {
                await _versaoRepo.AtualizarVigenciaFimAsync(vigente.Id, vm.NovaDataInicio.Value.AddDays(-1));
            }

            var novaVersao = new InstrumentoVersao
            {
                InstrumentoId  = vm.InstrumentoId,
                Versao         = vigente.Versao + 1,
                VigenciaInicio = novaIni,
                VigenciaFim    = novaFim,
                Valor          = valorFinal,
                Objeto         = instrumento.Objeto, // pode evoluir para permitir mudança
                TipoAditivo    = vm.TipoAditivo,
                Observacao     = vm.Observacao,
                DataAssinatura = vm.DataAssinatura,
                DataRegistro   = DateTime.Now
            };

            await _versaoRepo.InserirAsync(novaVersao);
        }

        public async Task<(InstrumentoVersao removida, InstrumentoVersao? vigente)> CancelarUltimoAditivoAsync(
            int instrumentoId, int versao, string justificativa)
        {
            var atual = await _versaoRepo.ObterVersaoAtualAsync(instrumentoId)
                       ?? throw new ArgumentException("Não há versão vigente para cancelar.");

            if (atual.Versao != versao)
                throw new ArgumentException("Versão informada não é a vigente.");

            // Remove a vigente
            await _versaoRepo.ExcluirAsync(atual.Id);

            // Busca a nova vigente (versão anterior)
            var anterior = await _versaoRepo.ObterVersaoAtualAsync(instrumentoId);
            return (atual, anterior);
        }
    }
}