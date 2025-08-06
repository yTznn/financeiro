using Financeiro.Infraestrutura;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1) MVC
builder.Services.AddControllersWithViews();

// 2) Connection-string → registra DbConnectionFactory como Singleton
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");
    
    // Logger opcional injetado automaticamente
    var logger = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
    return new DbConnectionFactory(connStr, logger);
});

// 3) Repositórios e Validações — Pessoa Jurídica
builder.Services.AddTransient<IPessoaJuridicaRepositorio, PessoaJuridicaRepositorio>();
builder.Services.AddTransient<PessoaJuridicaValidacoes>();

// 4) Repositórios e Validações — Pessoa Física
builder.Services.AddTransient<IPessoaFisicaRepositorio, PessoaFisicaRepositorio>();
builder.Services.AddTransient<PessoaFisicaValidacoes>();

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