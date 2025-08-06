using Financeiro.Infraestrutura;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Financeiro.Servicos;          // 👈 novo using
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

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

// 8) Repositório — Aditivo / Versões
builder.Services.AddTransient<IAditivoRepositorio, AditivoRepositorio>();

// 9) Serviço de domínio — Versão / Aditivo  ✅ NOVO
builder.Services.AddTransient<IVersaoAcordoService, VersaoAcordoService>();

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
    pattern: "{controller=Escolhas}/{action=Index}/{id?}");

app.Run();