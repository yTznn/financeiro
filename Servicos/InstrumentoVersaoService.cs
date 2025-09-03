using System;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Serviço responsável por gerenciar o histórico de versões (aditivos) de um Instrumento.
    /// </summary>
    public class InstrumentoVersaoService : IInstrumentoVersaoService
    {
        private readonly IInstrumentoRepositorio _instrRepo;
        private readonly IInstrumentoVersaoRepositorio _versaoRepo;

        public InstrumentoVersaoService(
            IInstrumentoRepositorio instrRepo,
            IInstrumentoVersaoRepositorio versaoRepo)
        {
            _instrRepo  = instrRepo;
            _versaoRepo = versaoRepo;
        }

        /// <summary>
        /// Cria um novo aditivo.
        /// Regra: SEMPRE cria nova versão (mesmo no "mesmo dia").
        /// Valor: NovoValor é DELTA (acréscimo/decréscimo) sobre o valor vigente.
        /// </summary>
        public async Task CriarAditivoAsync(AditivoViewModel vm)
        {
            if (vm is null) throw new ArgumentNullException(nameof(vm), "Dados do aditivo inválidos.");
            if (vm.TipoAcordoId <= 0) throw new ArgumentException("Instrumento inválido.");
            if (!vm.NovaDataInicio.HasValue) throw new ArgumentException("A Nova Data de Início é obrigatória.");

            var instrumento = await _instrRepo.ObterPorIdAsync(vm.TipoAcordoId)
                              ?? throw new InvalidOperationException("Instrumento não encontrado.");

            // Garante existência de V1
            var vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
            if (vigente == null)
            {
                var v1 = new AcordoVersao
                {
                    TipoAcordoId   = instrumento.Id,
                    Versao         = 1,
                    VigenciaInicio = instrumento.DataInicio,
                    Valor          = instrumento.Valor,
                    Objeto         = instrumento.Objeto,
                    Observacao     = "Versão original do instrumento",
                    DataAssinatura = instrumento.DataAssinatura,
                    DataRegistro   = DateTime.Now
                };
                await _versaoRepo.InserirAsync(v1);
                vigente = v1;
            }

            var novaInicio = vm.NovaDataInicio.Value.Date;

            if (novaInicio < vigente.VigenciaInicio.Date)
                throw new ArgumentException("A Data de Início do aditivo não pode ser anterior à vigência da versão atual.");

            // === CÁLCULO CORRIGIDO ===
            // Trata NovoValor como delta (pode ser + ou -). Se null, delta = 0.
            decimal delta = vm.NovoValor ?? 0m;
            decimal novoValorFinal = vigente.Valor + delta;
            if (novoValorFinal < 0m)
                throw new InvalidOperationException("O valor resultante do aditivo não pode ser negativo.");

            // Fecha a vigente no dia anterior ao novo início (mesmo se for “mesmo dia”)
            var fimVigente = novaInicio.AddDays(-1);
            await _versaoRepo.AtualizarVigenciaFimAsync(vigente.Id, fimVigente);

            // Cria a nova versão que passa a vigorar
            var novaVersao = new AcordoVersao
            {
                TipoAcordoId   = vm.TipoAcordoId,
                Versao         = vigente.Versao + 1,
                VigenciaInicio = novaInicio,
                VigenciaFim    = vm.NovaDataFim,           // pode ser null (vigente)
                Valor          = novoValorFinal,           // <<< usa o acumulado!
                Objeto         = instrumento.Objeto,
                TipoAditivo    = vm.TipoAditivo,
                Observacao     = vm.Observacao,
                DataAssinatura = vm.DataAssinatura,
                DataRegistro   = DateTime.Now
            };
            await _versaoRepo.InserirAsync(novaVersao);

            // Instrumento principal reflete valor/datas vigentes
            var vmUpdate = new TipoAcordoViewModel
            {
                Id             = instrumento.Id,
                Numero         = instrumento.Numero,
                Valor          = novoValorFinal,                    // <<< usa o acumulado!
                Objeto         = instrumento.Objeto,
                DataInicio     = novaVersao.VigenciaInicio,
                DataFim        = vm.NovaDataFim ?? instrumento.DataFim,
                Ativo          = instrumento.Ativo,
                Observacao     = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId     = instrumento.EntidadeId
            };
            await _instrRepo.AtualizarAsync(instrumento.Id, vmUpdate);
        }

        /// <summary>
        /// Cancela o último aditivo (remove a versão vigente e reabre a anterior).
        /// </summary>
        public async Task<(AcordoVersao removida, AcordoVersao vigente)> CancelarUltimoAditivoAsync(
            int instrumentoId,
            int versaoEsperada,
            string justificativa)
        {
            if (instrumentoId <= 0) throw new ArgumentException("Instrumento inválido.");
            if (string.IsNullOrWhiteSpace(justificativa)) throw new ArgumentException("Justificativa é obrigatória.");

            var vigente = await _versaoRepo.ObterVersaoAtualAsync(instrumentoId)
                          ?? throw new InvalidOperationException("Nenhuma versão vigente encontrada.");

            // Concorrência
            if (versaoEsperada > 0 && vigente.Versao != versaoEsperada)
                throw new InvalidOperationException("O registro foi alterado por outro usuário. Recarregue a página.");

            if (vigente.Versao <= 1)
                throw new InvalidOperationException(
                    "Não é possível cancelar a versão original. (A partir desta regra, aditivos no mesmo dia também geram nova versão.)");

            var anterior = await _versaoRepo.ObterVersaoAnteriorAsync(instrumentoId, vigente.Versao)
                           ?? throw new InvalidOperationException("Versão anterior não encontrada.");

            // Remove a vigente e reabre a anterior
            await _versaoRepo.ExcluirAsync(vigente.Id);
            await _versaoRepo.AtualizarVigenciaFimAsync(anterior.Id, null);

            // Reflete no Instrumento
            var instrumento = await _instrRepo.ObterPorIdAsync(instrumentoId)
                              ?? throw new InvalidOperationException("Instrumento não encontrado.");

            var vmUpdate = new TipoAcordoViewModel
            {
                Id             = instrumento.Id,
                Numero         = instrumento.Numero,
                Valor          = anterior.Valor,
                Objeto         = instrumento.Objeto,
                DataInicio     = anterior.VigenciaInicio,
                DataFim        = instrumento.DataFim,
                Ativo          = instrumento.Ativo,
                Observacao     = instrumento.Observacao,
                DataAssinatura = instrumento.DataAssinatura,
                EntidadeId     = instrumento.EntidadeId
            };
            await _instrRepo.AtualizarAsync(instrumento.Id, vmUpdate);

            return (vigente, anterior);
        }
    }
}