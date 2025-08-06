// Controllers/UsuariosController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos.Anexos;
using Financeiro.Servicos.Seguranca;

namespace Financeiro.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly ICriptografiaService _criptografiaService;
        private readonly IAnexoService _anexoService;
        private readonly IArquivoRepositorio _arquivoRepositorio;
        private readonly IPessoaFisicaRepositorio _pessoaFisicaRepositorio;

        public UsuariosController(
            IUsuarioRepositorio usuarioRepositorio,
            ICriptografiaService criptografiaService,
            IAnexoService anexoService,
            IArquivoRepositorio arquivoRepositorio,
            IPessoaFisicaRepositorio pessoaFisicaRepositorio)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _criptografiaService = criptografiaService;
            _anexoService = anexoService;
            _arquivoRepositorio = arquivoRepositorio;
            _pessoaFisicaRepositorio = pessoaFisicaRepositorio;
        }

        public async Task<IActionResult> Novo()
        {
            var model = new UsuarioViewModel();
            await PreencherPessoasFisicasAsync(model);
            return View("UsuarioForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(UsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PreencherPessoasFisicasAsync(model);
                return View("UsuarioForm", model);
            }

            if (await _usuarioRepositorio.NameSkipExisteAsync(model.NameSkip))
            {
                ModelState.AddModelError("NameSkip", "Já existe um usuário com esse NameSkip.");
                await PreencherPessoasFisicasAsync(model);
                return View("UsuarioForm", model);
            }

            var emailCriptografado = _criptografiaService.CriptografarEmail(model.Email);
            var senhaHash = _criptografiaService.GerarHashSenha(model.Senha);

            string? nomeImagem = null;
            string? hashImagem = null;

            if (model.ImagemPerfil != null)
            {
                try
                {
                    var idArquivo = await _anexoService.SalvarAnexoAsync(
                        model.ImagemPerfil,
                        origem: "PerfilUsuario");

                    var arquivo = await _arquivoRepositorio.ObterPorIdAsync(idArquivo);
                    nomeImagem = Path.GetFileNameWithoutExtension(model.ImagemPerfil.FileName);
                    hashImagem = arquivo?.Hash;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagemPerfil", ex.Message);
                    await PreencherPessoasFisicasAsync(model);
                    return View("UsuarioForm", model);
                }
            }

            var usuario = new Usuario
            {
                NameSkip = model.NameSkip,
                EmailCriptografado = emailCriptografado,
                SenhaHash = senhaHash,
                PessoaFisicaId = model.PessoaFisicaId,
                NomeArquivoImagem = nomeImagem,
                HashImagem = hashImagem,
                DataCriacao = DateTime.Now,
                Ativo = true
            };

            await _usuarioRepositorio.AdicionarAsync(usuario);
            TempData["MensagemSucesso"] = "Usuário cadastrado com sucesso!";
            return RedirectToAction(nameof(Novo));
        }

        private async Task PreencherPessoasFisicasAsync(UsuarioViewModel model)
        {
            var pessoas = await _pessoaFisicaRepositorio.ListarAsync();

            model.PessoasFisicas = pessoas.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Nome} {p.Sobrenome}"
            }).ToList();
        }
    }
}