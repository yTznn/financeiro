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

            // 1. Garante que existe uma versão inicial (Vigente)
            var vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
            if (vigente == null)
            {
                var original = new InstrumentoVersao
                {
                    InstrumentoId  = instrumento.Id,
                    Versao         = 1,
                    VigenciaInicio = instrumento.DataInicio,
                    VigenciaFim    = instrumento.DataFim, // Pega do pai se for o primeiro
                    Valor          = instrumento.Valor,
                    Objeto         = instrumento.Objeto,
                    TipoAditivo    = null,
                    Observacao     = "Versão original",
                    DataAssinatura = instrumento.DataAssinatura,
                    DataRegistro   = DateTime.Now
                };
                await _versaoRepo.InserirAsync(original);
                vigente = original; // Agora temos uma base para calcular
            }

            // 2. Define as NOVAS Datas de Vigência (Prioridade: Informada no Form > Vigente Atual)
            DateTime novaIni = vm.NovaDataInicio ?? vigente.VigenciaInicio;
            DateTime? novaFim = vm.NovaDataFim ?? vigente.VigenciaFim;

            // 3. Cálculo do Novo Valor (Total ou Mensal)
            decimal valorFinal = vigente.Valor;
            
            bool alteraValor = vm.TipoAditivo is TipoAditivo.Acrescimo 
                            or TipoAditivo.Supressao 
                            or TipoAditivo.PrazoAcrescimo 
                            or TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                if (!vm.NovoValor.HasValue || vm.NovoValor.Value == 0)
                    throw new ArgumentException("Informe um valor de aditivo diferente de zero.");

                decimal delta = vm.NovoValor.Value; // O sinal (+/-) já vem tratado do Controller

                // --- LÓGICA CORRIGIDA: ADITIVO MENSAL ---
                if (vm.EhValorMensal)
                {
                    // Se o valor informado é mensal (ex: +100k/mês), precisamos saber quantos meses
                    // isso representa no total do período para somar ao Valor Global do contrato.
                    
                    if (novaFim.HasValue)
                    {
                        int meses = CalcularMeses(novaIni, novaFim.Value);
                        // Ex: 100k * 12 meses = 1.2 Milhões de acréscimo total
                        delta = delta * meses; 
                    }
                    else
                    {
                        // Se for indeterminado, assumimos que o delta mensal é somado apenas uma vez?
                        // Ou bloqueamos? Por segurança, vamos manter a lógica simples:
                        // Em contratos sem fim, o aditivo mensal é aplicado como valor nominal único por enquanto
                        // (Para evitar multiplicar por infinito).
                    }
                }
                // ----------------------------------------

                valorFinal = vigente.Valor + delta;
                if (valorFinal < 0) throw new ArgumentException("O valor do instrumento não pode ficar negativo.");
            }

            // 4. Criação da Nova Versão
            var novaVersao = new InstrumentoVersao
            {
                InstrumentoId  = vm.InstrumentoId,
                Versao         = vigente.Versao + 1,
                VigenciaInicio = novaIni,
                VigenciaFim    = novaFim,
                Valor          = valorFinal,
                Objeto         = instrumento.Objeto, 
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

        // Helper privado para cálculo de meses (igual ao que corrigimos no JS/Controller)
        private int CalcularMeses(DateTime inicio, DateTime fim)
        {
            if (fim < inicio) return 0;
            // Cálculo de meses inclusivo (ex: jan a dez = 12 meses)
            return ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month + 1;
        }
    }
}