using System.Globalization;
using AutoMapper;
using Financeiro.Infraestrutura;
using Financeiro.Mappings;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Financeiro.Servicos.Anexos;
using Financeiro.Servicos.Seguranca;
using Financeiro.Validacoes;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configurações por ambiente
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 1) Cultura padrão de threads
var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// 2) MVC + BINDER CUSTOMIZADO
// Adicionamos as options para registrar o nosso DecimalModelBinderProvider
#if DEBUG
builder.Services.AddControllersWithViews(options => 
{
    // Essa linha ensina o sistema a ler "25.000,00" corretamente
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
}).AddRazorRuntimeCompilation();
#else
builder.Services.AddControllersWithViews(options => 
{
    // Essa linha ensina o sistema a ler "25.000,00" corretamente
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});
#endif

// 2.1) AutoMapper
builder.Services.AddAutoMapper(typeof(EntidadeProfile).Assembly);

// 3) Autenticação Cookie (com expiração e sliding)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath         = "/Conta/Login";
        opt.LogoutPath        = "/Conta/Logout";
        opt.AccessDeniedPath  = "/Conta/AcessoNegado";
        opt.ExpireTimeSpan    = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// 4) Connection-string
builder.Services.AddScoped<IDbConnectionFactory>(sp =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnString 'DefaultConnection' não encontrada.");
    var log  = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(conn, log);
});

// ============================================================
// INJEÇÃO DE DEPENDÊNCIA (SCOPED)
// ============================================================

#region Repositórios / Serviços já existentes
builder.Services.AddScoped<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddScoped<PessoaJuridicaValidacoes>();

builder.Services.AddScoped<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddScoped<PessoaFisicaValidacoes>();

builder.Services.AddScoped<IEnderecoRepositorio, EnderecoRepositorio>();
builder.Services.AddScoped<IContaBancariaRepositorio, ContaBancariaRepositorio>();

builder.Services.AddScoped<INaturezaRepositorio, NaturezaRepositorio>();
builder.Services.AddScoped<IOrcamentoRepositorio, OrcamentoRepositorio>();
builder.Services.AddScoped<IContratoRepositorio, ContratoRepositorio>();
builder.Services.AddScoped<IContratoVersaoRepositorio, ContratoVersaoRepositorio>();
builder.Services.AddScoped<IContratoVersaoService, ContratoVersaoService>();

// [NOVO] Repositório de Movimentações (Pagamentos)
builder.Services.AddScoped<IMovimentacaoRepositorio, MovimentacaoRepositorio>();
#endregion

#region Repositórios e Serviços que já estavam corretos (Scoped)
builder.Services.AddScoped<INivelRepositorio, NivelRepositorio>();
builder.Services.AddScoped<IArquivoRepositorio, ArquivoRepositorio>();
builder.Services.AddScoped<IAnexoService, AnexoService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICriptografiaService, CriptografiaService>();
builder.Services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
builder.Services.AddScoped<ILogRepositorio, LogRepositorio>();
builder.Services.AddScoped<IEntidadeEnderecoRepositorio, EntidadeEnderecoRepositorio>();

builder.Services.AddScoped<IEntidadeRepositorio, EntidadeRepositorio>();
builder.Services.AddScoped<IEntidadeService, EntidadeService>();
builder.Services.AddScoped<IUsuarioEntidadeRepositorio, UsuarioEntidadeRepositorio>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddScoped<IEntidadeEnderecoService, EntidadeEnderecoService>();
builder.Services.AddScoped<IEnderecoService, EnderecoService>();

builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddHttpContextAccessor(); 

builder.Services.AddScoped<IJustificativaService, JustificativaService>();

builder.Services.AddScoped<IInstrumentoRepositorio, InstrumentoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoRepositorio, InstrumentoVersaoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoService, InstrumentoVersaoService>();
builder.Services.AddScoped<IRecebimentoInstrumentoRepositorio, RecebimentoInstrumentoRepositorio>();

#endregion

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Localização no pipeline (garante pt-BR no model binding)
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();