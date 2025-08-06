using Financeiro.Infraestrutura;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Financeiro.Servicos;
using Microsoft.Extensions.Logging;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 1) MVC
builder.Services.AddControllersWithViews();

// 2) Connection-string → fábrica de conexões
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

    var logger = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(connStr, logger);
});

// 3) Repositórios e Validações — Pessoa Jurídica
builder.Services.AddTransient<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddTransient<PessoaJuridicaValidacoes>();

// 4) Repositórios e Validações — Pessoa Física
builder.Services.AddTransient<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddTransient<PessoaFisicaValidacoes>();

// 5) Repositório — Endereço
builder.Services.AddTransient<IEnderecoRepositorio, EnderecoRepositorio>();

// 6) Repositório — Conta Bancária
builder.Services.AddTransient<IContaBancariaRepositorio, ContaBancariaRepositorio>();

// 7) Repositório — Tipo de Acordo
builder.Services.AddTransient<ITipoAcordoRepositorio, TipoAcordoRepositorio>();

// 8) Repositório — Aditivo / Versões de Acordo
builder.Services.AddTransient<IAditivoRepositorio, AditivoRepositorio>();

// 9) Serviço domínio — Versão / Aditivo de Acordo
builder.Services.AddTransient<IVersaoAcordoService, VersaoAcordoService>();

// 10) Repositório — Natureza
builder.Services.AddTransient<INaturezaRepositorio, NaturezaRepositorio>();

// 11) Repositório — Orçamento
builder.Services.AddTransient<IOrcamentoRepositorio, OrcamentoRepositorio>();

// 12) Repositório — Contrato
builder.Services.AddTransient<IContratoRepositorio, ContratoRepositorio>();

// 13) Repositório — Versão de Contrato ✅ NOVO
builder.Services.AddTransient<IContratoVersaoRepositorio, ContratoVersaoRepositorio>();

// 14) Serviço — Versão de Contrato ✅ NOVO
builder.Services.AddTransient<IContratoVersaoService, ContratoVersaoService>();


var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Rota padrão → tela de escolha
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();