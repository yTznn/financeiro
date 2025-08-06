using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos.Seguranca;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Financeiro.Controllers
{
    public class ContaController : Controller
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly ICriptografiaService _criptografiaService;

        public ContaController(IUsuarioRepositorio usuarioRepositorio,
                               ICriptografiaService criptografiaService)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _criptografiaService = criptografiaService;
        }

        // GET: /Conta/Login
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Conta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Verifica se é e-mail
            bool isEmail = model.Login.Contains("@");
            var emailCriptografado = isEmail ? _criptografiaService.CriptografarEmail(model.Login) : null;

            var usuario = isEmail
                ? await _usuarioRepositorio.ObterPorEmailAsync(emailCriptografado!)
                : await _usuarioRepositorio.ObterPorNameSkipAsync(model.Login);

            if (usuario == null || !_criptografiaService.VerificarSenha(model.Senha, usuario.SenhaHash))
            {
                model.MensagemErro = "Usuário ou senha inválidos.";
                return View(model);
            }

            if (!usuario.Ativo)
            {
                model.MensagemErro = "Usuário inativo.";
                return View(model);
            }

            // Atualiza data do último acesso
            await _usuarioRepositorio.AtualizarUltimoAcessoAsync(usuario.Id);

            // Cria claims de identidade
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NameSkip),
                new Claim("EmailCriptografado", usuario.EmailCriptografado)
            };

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidade);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home"); // ou a tela desejada pós login
        }

        // GET: /Conta/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Conta");
        }
    }
}