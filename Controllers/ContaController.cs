using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos.Seguranca;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Financeiro.Models;
using Microsoft.AspNetCore.Authorization;

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

        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            Usuario? usuario;

            if (model.Login.Contains("@"))
            {
                var emailHash = _criptografiaService.HashEmailParaLogin(model.Login);
                usuario = await _usuarioRepositorio.ObterPorEmailHashAsync(emailHash);
            }
            else
            {
                usuario = await _usuarioRepositorio.ObterPorNameSkipAsync(model.Login);
            }

            if (usuario is null || !_criptografiaService.VerificarSenha(model.Senha, usuario.SenhaHash))
            {
                model.MensagemErro = "Usuário ou senha inválidos.";
                return View(model);
            }

            if (!usuario.Ativo)
            {
                model.MensagemErro = "Usuário inativo.";
                return View(model);
            }

            if (model.EntidadeId is null || model.EntidadeId == 0)
            {
                model.MensagemErro = "Por favor, selecione uma entidade.";
                return View(model);
            }

            var entidades = await _usuarioRepositorio.ObterEntidadesPorUsuarioIdAsync(usuario.Id);
            var entidadeSelecionada = entidades.FirstOrDefault(e => e.Id == model.EntidadeId);

            if (entidadeSelecionada is null)
            {
                model.MensagemErro = "Entidade selecionada inválida.";
                return View(model);
            }

            await _usuarioRepositorio.AtualizarUltimoAcessoAsync(usuario.Id);

            var perfil = usuario.PerfilId switch
            {
                1 => "Administrador",
                2 => "Financeiro",
                3 => "Gerencial",
                _ => "Comum"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NameSkip),
                new Claim("EmailCriptografado", usuario.EmailCriptografado ?? ""),
                new Claim("SiglaEntidade", entidadeSelecionada.Sigla),
                new Claim("EntidadeId", entidadeSelecionada.Id.ToString()),
                new Claim(ClaimTypes.Role, perfil)
            };

            if (!string.IsNullOrEmpty(usuario.HashImagem))
            {
                claims.Add(new Claim("HashImagem", usuario.HashImagem));
            }

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidade);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Conta");
        }

        [HttpGet]
        public async Task<IActionResult> BuscarEntidadesPorLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return BadRequest("Login inválido.");

            Usuario? usuario;

            if (login.Contains("@"))
            {
                var emailHash = _criptografiaService.HashEmailParaLogin(login.Trim().ToLower());
                usuario = await _usuarioRepositorio.ObterPorEmailHashAsync(emailHash);
            }
            else
            {
                usuario = await _usuarioRepositorio.ObterPorNameSkipAsync(login.Trim());
            }

            if (usuario == null || !usuario.Ativo)
                return NotFound();

            var entidades = await _usuarioRepositorio.ObterEntidadesPorUsuarioIdAsync(usuario.Id);

            var resultado = entidades.Select(e => new
            {
                id = e.Id,
                sigla = e.Sigla
            });

            return Json(resultado);
        }
    }
}