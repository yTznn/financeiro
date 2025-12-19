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
using Microsoft.AspNetCore.Mvc.Razor;
using Rotativa.AspNetCore; // <-- NOVO: Adicionado para a configuração da Rotativa
using Microsoft.Extensions.Logging; // Garantindo que o ILogger seja reconhecido

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURAÇÃO DE CULTURA (PT-BR FORÇADO)
// ============================================================
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configurações por ambiente
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ============================================================
// 2. SERVIÇOS MVC E LOCALIZAÇÃO
// ============================================================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var mvcBuilder = builder.Services.AddControllersWithViews(options => 
{
    // 2.1 Binder Customizado para Decimal (R$ 1.000,00)
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

    // 2.2 Tradução Manual de Erros de Sistema (ModelBinding)
    // Isso garante que erros técnicos virem mensagens amigáveis em PT-BR
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "O campo é obrigatório.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (valor, campo) => $"O valor '{valor}' não é válido para o campo {campo}.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(
        _ => "Um valor é obrigatório para este campo.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor(
        (valor) => $"O valor '{valor}' não é válido.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
        (valor) => $"O valor '{valor}' é inválido.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
        (campo) => $"O campo {campo} deve ser um número.");
});

// Ativa a localização de DataAnnotations ([Required], [StringLength]) e Views
mvcBuilder
    .AddDataAnnotationsLocalization() 
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddRazorRuntimeCompilation(); // Mantido para facilitar desenvolvimento

// 2.3 AutoMapper
builder.Services.AddAutoMapper(typeof(EntidadeProfile).Assembly);

// ============================================================
// 3. SEGURANÇA (COOKIES)
// ============================================================
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

// ============================================================
// 4. BANCO DE DADOS
// ============================================================
builder.Services.AddScoped<IDbConnectionFactory>(sp =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnString 'DefaultConnection' não encontrada.");
    var log  = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(conn, log);
});

// ============================================================
// 5. INJEÇÃO DE DEPENDÊNCIA (SCOPED)
// ============================================================

// Cadastros Gerais
builder.Services.AddScoped<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddScoped<PessoaJuridicaValidacoes>();
builder.Services.AddScoped<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddScoped<PessoaFisicaValidacoes>();
builder.Services.AddScoped<IFornecedorRepositorio, FornecedorRepositorio>();

// Endereços
builder.Services.AddScoped<IEnderecoRepositorio, EnderecoRepositorio>();
builder.Services.AddScoped<IEnderecoService, EnderecoService>();

// Financeiro e Contratos
builder.Services.AddScoped<IContaBancariaRepositorio, ContaBancariaRepositorio>();
builder.Services.AddScoped<IOrcamentoRepositorio, OrcamentoRepositorio>();
builder.Services.AddScoped<IMovimentacaoRepositorio, MovimentacaoRepositorio>();

// Contratos
builder.Services.AddScoped<IContratoRepositorio, ContratoRepositorio>();
builder.Services.AddScoped<IContratoVersaoRepositorio, ContratoVersaoRepositorio>();
builder.Services.AddScoped<IContratoVersaoService, ContratoVersaoService>();

// Instrumentos (Repasses)
builder.Services.AddScoped<IInstrumentoRepositorio, InstrumentoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoRepositorio, InstrumentoVersaoRepositorio>();
builder.Services.AddScoped<IInstrumentoVersaoService, InstrumentoVersaoService>();
builder.Services.AddScoped<IRecebimentoInstrumentoRepositorio, RecebimentoInstrumentoRepositorio>();

// Sistema / Segurança
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPermissaoRepositorio, PermissaoRepositorio>();
builder.Services.AddScoped<ICriptografiaService, CriptografiaService>();
builder.Services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();

// LOGS (Repositório e Serviço)
builder.Services.AddScoped<ILogRepositorio, LogRepositorio>();
builder.Services.AddScoped<ILogService, LogService>(); 
// builder.Services.AddScoped<IPdfService, PdfService>(); // <-- REMOVIDO (Não é mais necessário com Rotativa)

// Entidades (Escolas/Unidades)
builder.Services.AddScoped<IEntidadeRepositorio, EntidadeRepositorio>();
builder.Services.AddScoped<IEntidadeService, EntidadeService>();
builder.Services.AddScoped<IUsuarioEntidadeRepositorio, UsuarioEntidadeRepositorio>();
builder.Services.AddScoped<IEntidadeEnderecoRepositorio, EntidadeEnderecoRepositorio>();
builder.Services.AddScoped<IEntidadeEnderecoService, EntidadeEnderecoService>();

// Apoio
builder.Services.AddScoped<INivelRepositorio, NivelRepositorio>();
builder.Services.AddScoped<IArquivoRepositorio, ArquivoRepositorio>();
builder.Services.AddScoped<IAnexoService, AnexoService>();
builder.Services.AddScoped<IJustificativaService, JustificativaService>();
builder.Services.AddHttpContextAccessor(); 

// Repositório de Relatórios
builder.Services.AddScoped<IRelatorioRepositorio, RelatorioRepositorio>();

var app = builder.Build();

// ============================================================
// 6. PIPELINE DE EXECUÇÃO
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// [IMPORTANTE] Configuração do Middleware de Localização
var supportedCultures = new[] { cultureInfo };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// === CONFIGURAÇÃO DA ROTATIVA ===
// Inicializa a Rotativa (wkhtmltopdf)
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa"); 

app.Run();