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

            // 1) Garantir versão atual (Vigente)
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
            if (versaoAtual == null)
            {
                // Se não existir histórico, cria a versão 1 baseada no contrato atual
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
                    DataInicioAditivo = null
                };
                await _versaoRepo.InserirAsync(versaoAtual);
            }

            // 2) Definir Datas (Lógica de Continuidade)
            // Se o aditivo mexe no prazo, usa a data nova. Se não, mantém a atual.
            bool alteraPrazo = vm.TipoAditivo == TipoAditivo.Prazo ||
                               vm.TipoAditivo == TipoAditivo.PrazoAcrescimo ||
                               vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            var novaDataFim = alteraPrazo ? (vm.NovaDataFim ?? versaoAtual.DataFim) : versaoAtual.DataFim;
            
            // Data de início da vigência deste aditivo (pode ser retroativa ou futura)
            var dataInicioAditivo = vm.DataInicioAditivo ?? DateTime.Today;

            // 3) Cálculo do Novo Valor (Total ou Mensal)
            decimal valorFinal = versaoAtual.ValorContrato;

            bool alteraValor = vm.TipoAditivo == TipoAditivo.Acrescimo ||
                               vm.TipoAditivo == TipoAditivo.Supressao ||
                               vm.TipoAditivo == TipoAditivo.PrazoAcrescimo ||
                               vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                // Usa a propriedade auxiliar que já converteu a string PT-BR para decimal
                if (vm.NovoValorDecimal <= 0)
                    throw new ArgumentException("Informe um valor de aditivo maior que zero.");

                decimal delta = vm.NovoValorDecimal;

                // [LÓGICA MENSAL]
                if (vm.EhValorMensal)
                {
                    // Calcula quantos meses esse aditivo vai impactar
                    // Ex: Aditivo começa em Junho, contrato vai até Dezembro = 7 meses.
                    int mesesAfetados = ((novaDataFim.Year - dataInicioAditivo.Year) * 12) + novaDataFim.Month - dataInicioAditivo.Month + 1;
                    
                    if (mesesAfetados < 0) mesesAfetados = 0; // Segurança

                    // Multiplica o valor mensal pelos meses restantes
                    delta = delta * mesesAfetados;
                }

                // Aplica soma ou subtração no Valor Global
                if (vm.TipoAditivo == TipoAditivo.Acrescimo || vm.TipoAditivo == TipoAditivo.PrazoAcrescimo)
                    valorFinal = versaoAtual.ValorContrato + delta;
                else // Supressao
                    valorFinal = versaoAtual.ValorContrato - delta;

                if (valorFinal < 0)
                    throw new InvalidOperationException("O valor do contrato não pode ficar negativo.");
            }

            // 4) Criar nova versão no histórico
            var novaVersao = new ContratoVersao
            {
                ContratoId = vm.ContratoId,
                Versao = versaoAtual.Versao + 1,
                TipoAditivo = vm.TipoAditivo,
                Observacao = vm.Observacao,
                DataRegistro = DateTime.Now,
                DataInicioAditivo = dataInicioAditivo,

                ObjetoContrato = vm.NovoObjeto ?? versaoAtual.ObjetoContrato,
                ValorContrato = valorFinal,
                DataInicio = versaoAtual.DataInicio, // Data inicio original do contrato geralmente não muda
                DataFim = novaDataFim
            };

            await _versaoRepo.InserirAsync(novaVersao);

            // 5) Atualizar o contrato "Pai" para refletir a realidade atual
            // Isso garante que a busca principal e validações de saldo usem o valor atualizado
            var contratoPaiViewModel = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
            if (contratoPaiViewModel != null)
            {
                // Atualizamos a ViewModel e mandamos salvar
                // Nota: O método AtualizarAsync do repositório espera a ViewModel completa.
                // Aqui estamos atualizando apenas os campos chave que o aditivo mudou.
                
                contratoPaiViewModel.ValorContrato = novaVersao.ValorContrato;
                contratoPaiViewModel.DataFim = novaVersao.DataFim;
                contratoPaiViewModel.ObjetoContrato = novaVersao.ObjetoContrato;

                // Importante: Ao atualizar o pai, precisamos garantir que as Naturezas não se percam.
                // Como estamos usando ObterParaEdicaoAsync, elas já vêm carregadas.
                // Mas se o valor mudou, o rateio das naturezas vai ficar "torto" (soma diferente do total).
                // O ideal seria forçar o usuário a re-ratear, mas num aditivo automático, 
                // podemos aplicar um rateio proporcional ou deixar pendente.
                
                // POR ENQUANTO: Vamos salvar o valor total atualizado.
                // O alerta de divergência vai aparecer se alguém tentar editar o contrato depois.
                
                await _contratoRepo.AtualizarAsync(contratoPaiViewModel);
            }
        }

        public async Task CriarVersaoInicialAsync(ContratoViewModel vm)
        {
            if (vm == null || vm.Id == 0)
                throw new ArgumentException("Dados do contrato inválidos para criar a versão inicial.");

            var versaoInicial = new ContratoVersao
            {
                ContratoId = vm.Id,
                Versao = 1,
                ObjetoContrato = vm.ObjetoContrato,
                DataInicio = vm.DataInicio,
                DataFim = vm.DataFim,
                ValorContrato = vm.ValorContrato,
                TipoAditivo = null,
                Observacao = "Versão original do contrato.",
                DataRegistro = DateTime.Now,
                DataInicioAditivo = null
            };

            await _versaoRepo.InserirAsync(versaoInicial);
        }
    }
}