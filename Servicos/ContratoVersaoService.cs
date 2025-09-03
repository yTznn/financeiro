using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System;
using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public class ContratoVersaoService : IContratoVersaoService
    {
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;

        public ContratoVersaoService(IContratoRepositorio contratoRepo, IContratoVersaoRepositorio versaoRepo)
        {
            _contratoRepo = contratoRepo;
            _versaoRepo = versaoRepo;
        }

        public async Task CriarAditivoAsync(AditivoContratoViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            // 1) Garantir versão atual
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
            if (versaoAtual == null)
            {
                var contratoOriginal = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId)
                    ?? throw new Exception("Contrato não encontrado para criar a versão original.");

                versaoAtual = new ContratoVersao
                {
                    ContratoId      = contratoOriginal.Id,
                    Versao          = 1,
                    ObjetoContrato  = contratoOriginal.ObjetoContrato,
                    DataInicio      = contratoOriginal.DataInicio,
                    DataFim         = contratoOriginal.DataFim,
                    ValorContrato   = contratoOriginal.ValorContrato,
                    TipoAditivo     = null,
                    Observacao      = "Versão original do contrato.",
                    DataRegistro    = DateTime.Now,
                    DataInicioAditivo = null
                };
                await _versaoRepo.InserirAsync(versaoAtual);
            }

            // 2) Regras de valor: somar/subtrair (não substituir)
            decimal valorFinal = versaoAtual.ValorContrato;

            bool afetaValor =
                vm.TipoAditivo == TipoAditivo.Acrescimo ||
                vm.TipoAditivo == TipoAditivo.Supressao ||
                vm.TipoAditivo == TipoAditivo.PrazoAcrescimo ||
                vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            if (afetaValor)
            {
                if (!vm.NovoValor.HasValue || vm.NovoValor.Value <= 0)
                    throw new ArgumentException("Informe um NovoValor maior que zero para este tipo de aditivo.");

                var delta = Math.Abs(vm.NovoValor.Value);

                if (vm.TipoAditivo == TipoAditivo.Acrescimo || vm.TipoAditivo == TipoAditivo.PrazoAcrescimo)
                    valorFinal = versaoAtual.ValorContrato + delta;
                else // Supressao / PrazoSupressao
                    valorFinal = versaoAtual.ValorContrato - delta;

                if (valorFinal < 0)
                    throw new InvalidOperationException("O valor do contrato não pode ficar negativo.");
            }

            // 3) Regras de prazo (apenas quando o tipo envolve prazo)
            bool afetaPrazo =
                vm.TipoAditivo == TipoAditivo.Prazo ||
                vm.TipoAditivo == TipoAditivo.PrazoAcrescimo ||
                vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            var novaDataFim = afetaPrazo ? (vm.NovaDataFim ?? versaoAtual.DataFim) : versaoAtual.DataFim;

            // 4) Criar nova versão
            var novaVersao = new ContratoVersao
            {
                ContratoId       = vm.ContratoId,
                Versao           = versaoAtual.Versao + 1,
                TipoAditivo      = vm.TipoAditivo,
                Observacao       = vm.Observacao,
                DataRegistro     = DateTime.Now,
                DataInicioAditivo= vm.DataInicioAditivo,

                ObjetoContrato   = vm.NovoObjeto ?? versaoAtual.ObjetoContrato,
                ValorContrato    = valorFinal,
                DataInicio       = versaoAtual.DataInicio,
                DataFim          = novaDataFim
            };

            await _versaoRepo.InserirAsync(novaVersao);

            // 5) Refletir no contrato "pai"
            var contrato = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId)
                           ?? throw new Exception("Contrato não encontrado para atualizar.");

            contrato.ObjetoContrato = novaVersao.ObjetoContrato;
            contrato.ValorContrato  = novaVersao.ValorContrato;
            contrato.DataFim        = novaVersao.DataFim;

            await _contratoRepo.AtualizarAsync(contrato);
        }
    }
}