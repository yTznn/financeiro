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
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
            if (versaoAtual == null)
            {
                var contratoOriginal = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId)
                    ?? throw new Exception("Contrato não encontrado para criar a versão original.");

                versaoAtual = new ContratoVersao
                {
                    ContratoId = contratoOriginal.Id,
                    Versao = 1,
                    ObjetoContrato = contratoOriginal.ObjetoContrato,
                    DataInicio = contratoOriginal.DataInicio,
                    DataFim = contratoOriginal.DataFim,
                    ValorContrato = contratoOriginal.ValorContrato,
                    TipoAditivo = null,
                    Observacao = "Versão original do contrato.",
                    DataRegistro = DateTime.Now,
                    DataInicioAditivo = null // Original não tem data de início de aditivo
                };
                await _versaoRepo.InserirAsync(versaoAtual);
            }

            var novaVersao = new ContratoVersao
            {
                ContratoId = vm.ContratoId,
                Versao = versaoAtual.Versao + 1,
                TipoAditivo = vm.TipoAditivo,
                Observacao = vm.Observacao,
                DataRegistro = DateTime.Now,
                DataInicioAditivo = vm.DataInicioAditivo, // ✅ CORREÇÃO: Passa a data do aditivo

                ObjetoContrato = vm.NovoObjeto ?? versaoAtual.ObjetoContrato,
                ValorContrato = vm.NovoValor ?? versaoAtual.ValorContrato,
                DataInicio = versaoAtual.DataInicio,
                DataFim = vm.NovaDataFim ?? versaoAtual.DataFim
            };
            await _versaoRepo.InserirAsync(novaVersao);

            var contratoParaAtualizar = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId)
                ?? throw new Exception("Contrato não encontrado para atualizar.");
            
            contratoParaAtualizar.ObjetoContrato = novaVersao.ObjetoContrato;
            contratoParaAtualizar.ValorContrato = novaVersao.ValorContrato;
            contratoParaAtualizar.DataFim = novaVersao.DataFim;

            await _contratoRepo.AtualizarAsync(contratoParaAtualizar);
        }
    }
}