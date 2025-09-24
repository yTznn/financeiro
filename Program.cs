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

// 🛠️ Configurações por ambiente
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// 1) Cultura padrão de threads
var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// 2) MVC (com runtime compilation em DEBUG)
#if DEBUG
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
#else
builder.Services.AddControllersWithViews();
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
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnString 'DefaultConnection' não encontrada.");
    var log  = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(conn, log);
});

#region Repositórios / Serviços já existentes
builder.Services.AddTransient<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddTransient<PessoaJuridicaValidacoes>();

builder.Services.AddTransient<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddTransient<PessoaFisicaValidacoes>();

builder.Services.AddTransient<IEnderecoRepositorio, EnderecoRepositorio>();
builder.Services.AddTransient<IContaBancariaRepositorio, ContaBancariaRepositorio>();

builder.Services.AddTransient<INaturezaRepositorio, NaturezaRepositorio>();
builder.Services.AddTransient<IOrcamentoRepositorio, OrcamentoRepositorio>();
builder.Services.AddTransient<IContratoRepositorio, ContratoRepositorio>();
builder.Services.AddTransient<IContratoVersaoRepositorio, ContratoVersaoRepositorio>();
builder.Services.AddTransient<IContratoVersaoService, ContratoVersaoService>();

builder.Services.AddScoped<INivelRepositorio, NivelRepositorio>();
builder.Services.AddScoped<IArquivoRepositorio, ArquivoRepositorio>();
builder.Services.AddScoped<IAnexoService, AnexoService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICriptografiaService, CriptografiaService>();
builder.Services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
builder.Services.AddScoped<ILogRepositorio, LogRepositorio>();
builder.Services.AddScoped<IEntidadeEnderecoRepositorio, EntidadeEnderecoRepositorio>();
#endregion

#region Novos – Entidade e vínculo Usuário↔Entidade
builder.Services.AddScoped<IEntidadeRepositorio, EntidadeRepositorio>();
builder.Services.AddScoped<IEntidadeService, EntidadeService>();
builder.Services.AddScoped<IUsuarioEntidadeRepositorio, UsuarioEntidadeRepositorio>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
#endregion

#region 🚀 Novos – Serviços de Endereço, Logs e Justificativas
// Serviços de Endereço
builder.Services.AddScoped<IEntidadeEnderecoService, EntidadeEnderecoService>();
builder.Services.AddScoped<IEnderecoService, EnderecoService>();

// Logs
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddHttpContextAccessor(); // necessário para obter dados do usuário logado

// 💬 Justificativas
builder.Services.AddScoped<IJustificativaService, JustificativaService>();
#endregion

#region ✅ NOVO – Faixa de Instrumento (com lifetimes Scoped)
builder.Services.AddScoped<IInstrumentoRepositorio, InstrumentoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoRepositorio, InstrumentoVersaoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoService, InstrumentoVersaoService>();
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

// 🌎 Localização no pipeline (garante pt-BR no model binding)
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