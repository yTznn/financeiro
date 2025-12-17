using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using Financeiro.Atributos;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Financeiro.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Rotativa.AspNetCore;
using Financeiro.Extensions;

namespace Financeiro.Controllers
{
    [Authorize]
    public class RelatoriosController : Controller
    {
        private readonly IRelatorioRepositorio _relatorioRepositorio;
        private readonly IInstrumentoRepositorio _instrumentoRepositorio;

        // REMOVEMOS A DEPENDÊNCIA DO IPdfService DO CONSTRUTOR
        public RelatoriosController(
            IRelatorioRepositorio relatorioRepositorio,
            IInstrumentoRepositorio instrumentoRepositorio)
        {
            _relatorioRepositorio = relatorioRepositorio;
            _instrumentoRepositorio = instrumentoRepositorio;
        }

        // 1. TELA DE FILTRO (VIEW)
        [HttpGet]
        [AutorizarPermissao("RELATORIO_VIEW")]
        public async Task<IActionResult> Index()
        {
            var instrumentosRaw = await _instrumentoRepositorio.ListarInstrumentosParaSelectAsync();
            ViewBag.Instrumentos = new SelectList(instrumentosRaw ?? Enumerable.Empty<object>(), "Id", "Text"); 
            
            return View();
        }

        // 2. GERAÇÃO E EXIBIÇÃO DO PDF NO NAVEGADOR
        [HttpGet]
        [AutorizarPermissao("RELATORIO_VIEW")]
        public async Task<IActionResult> LancamentosXRecebimentosPDF(int instrumentoId, DateTime dataReferencia)
        {
            if (instrumentoId == 0)
            {
                TempData["Erro"] = "Selecione um Instrumento válido.";
                return RedirectToAction(nameof(Index));
            }

            // Define o período como o mês inteiro da data de referência
            var dataInicio = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);
            var dataFim = dataInicio.AddMonths(1).AddDays(-1);

            // 1. Obtém os dados do Repositório
            var relatorioData = await _relatorioRepositorio.GerarLancamentosXRecebimentosAsync(
                instrumentoId, dataInicio, dataFim);
            
            if (relatorioData == null)
            {
                TempData["Erro"] = "Nenhum Instrumento encontrado ou erro ao gerar relatório.";
                return RedirectToAction(nameof(Index));
            }

            // 2. USAMOS ViewAsPdf DA ROTATIVA
            
            // CORREÇÃO CRÍTICA: Normaliza a string para remover acentos e caracteres especiais ('ç', 'ã', etc.)
            // para evitar o erro "Invalid non-ASCII" nos cabeçalhos HTTP.
            string nomeBase = $"Relatorio_Lancamentos_{relatorioData.InstrumentoNumero}_{relatorioData.ReferenciaPeriodo.Replace("/", "_")}";
            
            // Aplica a extensão para limpar a string e anexa a extensão .pdf
            string nomeArquivo = $"{nomeBase.RemoverAcentosEspeciais()}.pdf"; 
            
            return new ViewAsPdf("LancamentosXRecebimentos", relatorioData)
            {
                // Configuração para exibir inline (no navegador)
                FileName = nomeArquivo,
                // Optional: Configuração de cabeçalho, rodapé, margens, etc.
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = { Top = 10, Bottom = 10 }
            };
        }
    }
}