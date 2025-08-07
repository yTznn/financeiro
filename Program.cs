using Financeiro.Infraestrutura;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Financeiro.Servicos;
using Financeiro.Servicos.Anexos;
using Financeiro.Servicos.Seguranca;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

// 🔧 Criação do builder
var builder = WebApplication.CreateBuilder(args);

// 🛠️ Leitura de configurações por ambiente (ex: Homologacao, Production, Development)
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 1) Cultura padrão (formato de número, data etc.)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 2) MVC
builder.Services.AddControllersWithViews();

// 3) Autenticação com cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Logout";
        options.AccessDeniedPath = "/Conta/AcessoNegado";
    });

builder.Services.AddAuthorization();

// 4) Connection-string → fábrica de conexões
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

    var logger = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(connStr, logger);
});

// 5) Repositórios e Validações — Pessoa Jurídica
builder.Services.AddTransient<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddTransient<PessoaJuridicaValidacoes>();

// 6) Repositórios e Validações — Pessoa Física
builder.Services.AddTransient<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddTransient<PessoaFisicaValidacoes>();

// 7) Repositório — Endereço
builder.Services.AddTransient<IEnderecoRepositorio, EnderecoRepositorio>();

// 8) Repositório — Conta Bancária
builder.Services.AddTransient<IContaBancariaRepositorio, ContaBancariaRepositorio>();

// 9) Repositório — Tipo de Acordo
builder.Services.AddTransient<ITipoAcordoRepositorio, TipoAcordoRepositorio>();

// 10) Repositório — Aditivo / Versões de Acordo
builder.Services.AddTransient<IAditivoRepositorio, AditivoRepositorio>();

// 11) Serviço domínio — Versão / Aditivo de Acordo
builder.Services.AddTransient<IVersaoAcordoService, VersaoAcordoService>();

// 12) Repositório — Natureza
builder.Services.AddTransient<INaturezaRepositorio, NaturezaRepositorio>();

// 13) Repositório — Orçamento
builder.Services.AddTransient<IOrcamentoRepositorio, OrcamentoRepositorio>();

// 14) Repositório — Contrato
builder.Services.AddTransient<IContratoRepositorio, ContratoRepositorio>();

// 15) Repositório — Versão de Contrato
builder.Services.AddTransient<IContratoVersaoRepositorio, ContratoVersaoRepositorio>();

// 16) Serviço — Versão de Contrato
builder.Services.AddTransient<IContratoVersaoService, ContratoVersaoService>();

builder.Services.AddScoped<INivelRepositorio, NivelRepositorio>();

// 17) Serviços adicionais
builder.Services.AddScoped<IArquivoRepositorio, ArquivoRepositorio>();
builder.Services.AddScoped<IAnexoService, AnexoService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICriptografiaService, CriptografiaService>();
builder.Services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();

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

// 💡 IMPORTANTE: ordem correta de middlewares
app.UseAuthentication();
app.UseAuthorization();

// Rota padrão
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();