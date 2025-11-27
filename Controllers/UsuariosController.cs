using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Models.Dto;
using Financeiro.Repositorios;
using Financeiro.Servicos.Anexos;
using Financeiro.Servicos.Seguranca;
using Financeiro.Servicos;                        // ← novo (IUsuarioService)
using Financeiro.Infraestrutura;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioRepositorio          _usuarioRepositorio;
        private readonly ICriptografiaService         _criptografiaService;
        private readonly IAnexoService                _anexoService;
        private readonly IArquivoRepositorio          _arquivoRepositorio;
        private readonly IPessoaFisicaRepositorio     _pessoaFisicaRepositorio;
        private readonly IPerfilRepositorio           _perfilRepositorio;
        private readonly IEntidadeRepositorio         _entidadeRepositorio;     // novo
        private readonly IUsuarioService              _usuarioService;          // novo
        private readonly IDbConnectionFactory         _connectionFactory;

        public UsuariosController(
            IUsuarioRepositorio      usuarioRepositorio,
            ICriptografiaService     criptografiaService,
            IAnexoService            anexoService,
            IArquivoRepositorio      arquivoRepositorio,
            IPessoaFisicaRepositorio pessoaFisicaRepositorio,
            IPerfilRepositorio       perfilRepositorio,
            IEntidadeRepositorio     entidadeRepositorio,   // novo
            IUsuarioService          usuarioService,        // novo
            IDbConnectionFactory     connectionFactory)
        {
            _usuarioRepositorio   = usuarioRepositorio;
            _criptografiaService  = criptografiaService;
            _anexoService         = anexoService;
            _arquivoRepositorio   = arquivoRepositorio;
            _pessoaFisicaRepositorio = pessoaFisicaRepositorio;
            _perfilRepositorio    = perfilRepositorio;
            _entidadeRepositorio  = entidadeRepositorio;    // novo
            _usuarioService       = usuarioService;         // novo
            _connectionFactory    = connectionFactory;
        }

        /* =================== LISTAGEM =================== */
        public async Task<IActionResult> Index()
        {
            const string sql = @"
                SELECT u.Id, u.NameSkip, u.EmailCriptografado, u.Ativo, u.HashImagem,
                    CONCAT(p.Nome, ' ', p.Sobrenome) AS NomePessoaFisica
                FROM Usuarios u
                LEFT JOIN PessoaFisica p ON p.Id = u.PessoaFisicaId
                ORDER BY u.DataCriacao DESC";

            using var conn = _connectionFactory.CreateConnection();
            var listaTemp = await conn.QueryAsync<UsuarioListagemTemp>(sql);

            var resultado = listaTemp.Select(u => new UsuarioListagemViewModel
            {
                Id = u.Id,
                NameSkip = u.NameSkip,
                Email = _criptografiaService.DescriptografarEmail(u.EmailCriptografado),
                NomePessoaFisica = u.NomePessoaFisica,
                HashImagem = u.HashImagem,
                Ativo = u.Ativo
            }).ToList();

            return View(resultado);
        }

        /* =================== NOVO (GET) =================== */
        public async Task<IActionResult> Novo()
        {
            var model = new UsuarioViewModel();
            await PreencherPessoasFisicasAsync(model);
            await PreencherPerfisAsync(model);
            await PreencherEntidadesAsync(model);                 // novo
            return View("UsuarioForm", model);
        }

        /* =================== SALVAR (POST) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(UsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PreencherPessoasFisicasAsync(model);
                await PreencherPerfisAsync(model);
                await PreencherEntidadesAsync(model);             // garantir lista
                return View("UsuarioForm", model);
            }

            if (await _usuarioRepositorio.NameSkipExisteAsync(model.NameSkip))
            {
                ModelState.AddModelError("NameSkip", "Já existe um usuário com esse NameSkip.");
                await PreencherPessoasFisicasAsync(model);
                await PreencherPerfisAsync(model);
                await PreencherEntidadesAsync(model);
                return View("UsuarioForm", model);
            }

            var email              = model.Email.Trim().ToLower();
            var emailCriptografado = _criptografiaService.CriptografarEmail(email);
            var emailHash          = _criptografiaService.HashEmailParaLogin(email);
            var senhaHash          = _criptografiaService.GerarHashSenha(model.Senha);

            string? nomeImagem = null;
            string? hashImagem = null;

            if (model.ImagemPerfil != null)
            {
                try
                {
                    var idArquivo = await _anexoService.SalvarAnexoAsync(model.ImagemPerfil, "PerfilUsuario");
                    var arquivo   = await _arquivoRepositorio.ObterPorIdAsync(idArquivo);
                    nomeImagem = Path.GetFileNameWithoutExtension(model.ImagemPerfil.FileName);
                    hashImagem = arquivo?.Hash;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagemPerfil", ex.Message);
                    await PreencherPessoasFisicasAsync(model);
                    await PreencherPerfisAsync(model);
                    await PreencherEntidadesAsync(model);
                    return View("UsuarioForm", model);
                }
            }

            var usuario = new Usuario
            {
                NameSkip           = model.NameSkip,
                EmailCriptografado = emailCriptografado,
                EmailHash          = emailHash,
                SenhaHash          = senhaHash,
                PessoaFisicaId     = model.PessoaFisicaId,
                NomeArquivoImagem  = nomeImagem,
                HashImagem         = hashImagem,
                PerfilId           = model.PerfilId ?? 0,
                DataCriacao        = DateTime.Now,
                Ativo              = true
            };

            await _usuarioRepositorio.AdicionarAsync(usuario);

            /* -------- grava vínculos de entidades -------- */
            await _usuarioService.SalvarEntidadesAsync(
                usuario.Id,
                model.EntidadesSelecionadas,
                model.EntidadeAtivaId ?? model.EntidadesSelecionadas.First());

            TempData["MensagemSucesso"] = "Usuário cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /* =================== EDITAR (GET) =================== */
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _usuarioRepositorio.ObterPorIdAsync(id);
            if (usuario == null) return NotFound();

            var model = new UsuarioViewModel
            {
                Id             = usuario.Id,
                NameSkip       = usuario.NameSkip,
                Email          = _criptografiaService.DescriptografarEmail(usuario.EmailCriptografado),
                PessoaFisicaId = usuario.PessoaFisicaId,
                PerfilId       = usuario.PerfilId,
                HashImagem     = usuario.HashImagem
            };

            await PreencherPessoasFisicasAsync(model);
            await PreencherPerfisAsync(model);
            await PreencherEntidadesAsync(model, id);          // carrega vínculos existentes
            return View("UsuarioForm", model);
        }

        /* =================== ATUALIZAR (POST) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(UsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PreencherPessoasFisicasAsync(model);
                await PreencherPerfisAsync(model);
                await PreencherEntidadesAsync(model, model.Id);
                return View("UsuarioForm", model);
            }

            var usuarioExistente = await _usuarioRepositorio.ObterPorIdAsync(model.Id);
            if (usuarioExistente == null) return NotFound();

            /* ------------ campos de usuário ------------ */
            if (_criptografiaService.DescriptografarEmail(usuarioExistente.EmailCriptografado) != model.Email)
            {
                usuarioExistente.EmailCriptografado = _criptografiaService.CriptografarEmail(model.Email);
                usuarioExistente.EmailHash = _criptografiaService.HashEmailParaLogin(model.Email);
            }
            if (usuarioExistente.NameSkip != model.NameSkip)
                usuarioExistente.NameSkip = model.NameSkip;

            usuarioExistente.PessoaFisicaId = model.PessoaFisicaId;
            usuarioExistente.PerfilId       = model.PerfilId ?? usuarioExistente.PerfilId;

            if (!string.IsNullOrWhiteSpace(model.Senha))
                usuarioExistente.SenhaHash = _criptografiaService.GerarHashSenha(model.Senha);

            if (model.ImagemPerfil != null)
            {
                try
                {
                    var idArquivo = await _anexoService.SalvarAnexoAsync(model.ImagemPerfil, "PerfilUsuario");
                    var arquivo   = await _arquivoRepositorio.ObterPorIdAsync(idArquivo);
                    usuarioExistente.NomeArquivoImagem = Path.GetFileNameWithoutExtension(model.ImagemPerfil.FileName);
                    usuarioExistente.HashImagem        = arquivo?.Hash;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagemPerfil", ex.Message);
                    await PreencherPessoasFisicasAsync(model);
                    await PreencherPerfisAsync(model);
                    await PreencherEntidadesAsync(model, model.Id);
                    return View("UsuarioForm", model);
                }
            }

            await _usuarioRepositorio.AtualizarAsync(usuarioExistente);

            /* -------- grava vínculos de entidades -------- */
            await _usuarioService.SalvarEntidadesAsync(
                model.Id,
                model.EntidadesSelecionadas,
                model.EntidadeAtivaId ?? model.EntidadesSelecionadas.First());

            TempData["MensagemSucesso"] = "Usuário atualizado com sucesso!";
            return RedirectToAction("Index");
        }

        /* =================== EXCLUIR =================== */
        public async Task<IActionResult> Excluir(int id)
        {
            await _usuarioRepositorio.ExcluirAsync(id);
            TempData["MensagemSucesso"] = "Usuário excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /* =================== HELPERS =================== */
        private async Task PreencherPessoasFisicasAsync(UsuarioViewModel model)
        {
            var pessoas = await _pessoaFisicaRepositorio.ListarAsync();
            model.PessoasFisicas = pessoas.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text  = $"{p.Nome} {p.Sobrenome}"
            }).ToList();
        }

        private async Task PreencherPerfisAsync(UsuarioViewModel model)
        {
            var perfis = await _perfilRepositorio.ListarAsync();
            model.PerfisDisponiveis = perfis.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text  = p.Nome
            }).ToList();
        }

        /* ---------- NOVO: preencher lista de entidades ---------- */
        private async Task PreencherEntidadesAsync(UsuarioViewModel model, int? usuarioId = null)
        {
            var entidades = await _entidadeRepositorio.ListAsync();
            model.TodasEntidades = entidades.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text  = $"{e.Sigla} - {e.Nome}"
            });

            if (usuarioId.HasValue)
            {
                var vinculos = await _usuarioService.ListarEntidadesAsync(usuarioId.Value);
                model.EntidadesSelecionadas = vinculos.Select(v => v.EntidadeId).ToList();
                model.EntidadeAtivaId       = vinculos.FirstOrDefault(v => v.Ativo)?.EntidadeId;
            }
        }

        /* =================== IMAGEM PERFIL =================== */
        [HttpGet]
        public async Task<IActionResult> ImagemPerfil(string hash)
        {
            var arquivo = await _arquivoRepositorio.ObterPorHashAsync(hash);
            if (arquivo == null || arquivo.Conteudo == null) return NotFound();
            return File(arquivo.Conteudo, arquivo.ContentType);
        }

        /* =================== MEUS DADOS (GET) =================== */
        [HttpGet]
        public async Task<IActionResult> MeusDados()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim)) return Unauthorized();
            if (!int.TryParse(idClaim, out int usuarioId)) return Unauthorized();

            var usuario = await _usuarioRepositorio.ObterPorIdAsync(usuarioId);
            if (usuario == null) return NotFound();

            var model = new UsuarioViewModel
            {
                Id         = usuario.Id,
                Email      = _criptografiaService.DescriptografarEmail(usuario.EmailCriptografado),
                HashImagem = usuario.HashImagem
            };
            return View("MeusDadosForm", model);
        }

        /* =================== MEUS DADOS (POST) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarMeusDados(UsuarioViewModel model)
        {
            if (!ModelState.IsValid) return View("MeusDadosForm", model);

            var usuario = await _usuarioRepositorio.ObterPorIdAsync(model.Id);
            if (usuario == null) return NotFound();

            usuario.EmailCriptografado = _criptografiaService.CriptografarEmail(model.Email);
            if (!string.IsNullOrWhiteSpace(model.Senha))
                usuario.SenhaHash = _criptografiaService.GerarHashSenha(model.Senha);

            if (model.ImagemPerfil != null)
            {
                try
                {
                    var idArquivo = await _anexoService.SalvarAnexoAsync(model.ImagemPerfil, "PerfilUsuario");
                    var arquivo   = await _arquivoRepositorio.ObterPorIdAsync(idArquivo);
                    usuario.NomeArquivoImagem = Path.GetFileNameWithoutExtension(model.ImagemPerfil.FileName);
                    usuario.HashImagem        = arquivo?.Hash;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImagemPerfil", ex.Message);
                    return View("MeusDadosForm", model);
                }
            }

            await _usuarioRepositorio.AtualizarAsync(usuario);
            TempData["MensagemSucesso"] = "Seus dados foram atualizados com sucesso!";
            return RedirectToAction("MeusDados");
        }
    }
}