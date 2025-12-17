using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Models.Dto;
using Financeiro.Repositorios;
using Financeiro.Servicos.Anexos;
using Financeiro.Servicos.Seguranca;
using Financeiro.Servicos;                        
using Financeiro.Infraestrutura;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using Financeiro.Atributos; // Necessário para [AutorizarPermissao]

namespace Financeiro.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioRepositorio      _usuarioRepositorio;
        private readonly ICriptografiaService     _criptografiaService;
        private readonly IAnexoService            _anexoService;
        private readonly IArquivoRepositorio      _arquivoRepositorio;
        private readonly IPessoaFisicaRepositorio _pessoaFisicaRepositorio;
        private readonly IPerfilRepositorio       _perfilRepositorio;
        private readonly IEntidadeRepositorio     _entidadeRepositorio;    
        private readonly IUsuarioService          _usuarioService;        
        private readonly IDbConnectionFactory     _connectionFactory;
        private readonly IPermissaoRepositorio    _permissaoRepositorio;
        private readonly ILogService              _logService;

        public UsuariosController(
            IUsuarioRepositorio      usuarioRepositorio,
            ICriptografiaService     criptografiaService,
            IAnexoService            anexoService,
            IArquivoRepositorio      arquivoRepositorio,
            IPessoaFisicaRepositorio pessoaFisicaRepositorio,
            IPerfilRepositorio       perfilRepositorio,
            IEntidadeRepositorio     entidadeRepositorio,   
            IUsuarioService          usuarioService,        
            IDbConnectionFactory     connectionFactory,
            IPermissaoRepositorio    permissaoRepositorio,
            ILogService              logService)
        {
            _usuarioRepositorio      = usuarioRepositorio;
            _criptografiaService     = criptografiaService;
            _anexoService            = anexoService;
            _arquivoRepositorio      = arquivoRepositorio;
            _pessoaFisicaRepositorio = pessoaFisicaRepositorio;
            _perfilRepositorio       = perfilRepositorio;
            _entidadeRepositorio     = entidadeRepositorio;    
            _usuarioService          = usuarioService;        
            _connectionFactory       = connectionFactory;
            _permissaoRepositorio    = permissaoRepositorio;
            _logService              = logService;
        }

        /* =================== LISTAGEM (PAGINADA E COM FILTRO) =================== */
        [HttpGet]
        [AutorizarPermissao("USUARIO_VIEW")]
        public async Task<IActionResult> Index(int p = 1, bool exibirInativos = false)
        {
            const int TAMANHO_PAGINA = 10;

            // Busca dados paginados do repositório (Repo já faz o filtro de inativos)
            var (lista, total) = await _usuarioRepositorio.ListarPaginadoAsync(p, TAMANHO_PAGINA, exibirInativos);

            // Descriptografa os e-mails para exibição
            foreach (var item in lista)
            {
                // O banco retorna o texto cifrado na propriedade Email (devido ao alias no SQL)
                // Aqui convertemos para texto plano
                item.Email = _criptografiaService.DescriptografarEmail(item.Email);
            }

            // ViewBags para controle da paginação e do checkbox na View
            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);
            ViewBag.ExibirInativos = exibirInativos;

            return View(lista);
        }

        /* =================== NOVO (GET) =================== */
        [HttpGet]
        [AutorizarPermissao("USUARIO_ADD")]
        public async Task<IActionResult> Novo()
        {
            var model = new UsuarioViewModel();
            await PreencherPessoasFisicasAsync(model);
            await PreencherPerfisAsync(model);
            await PreencherEntidadesAsync(model);                 
            return View("UsuarioForm", model);
        }

        /* =================== SALVAR (POST) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("USUARIO_ADD")]
        public async Task<IActionResult> Salvar(UsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PreencherPessoasFisicasAsync(model);
                await PreencherPerfisAsync(model);
                await PreencherEntidadesAsync(model);             
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

            // Salva e captura o ID
            var novoId = await _usuarioRepositorio.AdicionarAsync(usuario);
            usuario.Id = novoId;

            // LOG DE CRIAÇÃO
            await _logService.RegistrarCriacaoAsync("Usuario", usuario, novoId);

            // Grava vínculos de entidades
            await _usuarioService.SalvarEntidadesAsync(
                usuario.Id,
                model.EntidadesSelecionadas,
                model.EntidadeAtivaId ?? model.EntidadesSelecionadas.FirstOrDefault());

            TempData["Sucesso"] = "Usuário cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /* =================== EDITAR (GET) =================== */
        [HttpGet]
        [AutorizarPermissao("USUARIO_EDIT")]
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
            await PreencherEntidadesAsync(model, id);          
            return View("UsuarioForm", model);
        }

        /* =================== ATUALIZAR (POST) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("USUARIO_EDIT")]
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

            // Para log de auditoria (Antes)
            var usuarioAntes = await _usuarioRepositorio.ObterPorIdAsync(model.Id); // Busca cópia fresca

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

            // LOG DE EDIÇÃO
            await _logService.RegistrarEdicaoAsync("Usuario", usuarioAntes, usuarioExistente, usuarioExistente.Id);

            await _usuarioService.SalvarEntidadesAsync(
                model.Id,
                model.EntidadesSelecionadas,
                model.EntidadeAtivaId ?? model.EntidadesSelecionadas.FirstOrDefault());

            TempData["Sucesso"] = "Usuário atualizado com sucesso!";
            return RedirectToAction("Index");
        }

        /* =================== GERENCIAR PERMISSÕES =================== */
        [HttpGet]
        [AutorizarPermissao("USUARIO_EDIT")] // Requer permissão de editar usuário
        public async Task<IActionResult> EditarPermissoes(int id)
        {
            var usuario = await _usuarioRepositorio.ObterPorIdAsync(id);
            if (usuario == null) return NotFound();

            var listaPlana = await _permissaoRepositorio.ObterStatusPermissoesUsuarioAsync(id);

            var viewModel = new UsuarioPermissoesEdicaoViewModel
            {
                UsuarioId = usuario.Id,
                NomeUsuario = usuario.NameSkip,
                EmailUsuario = _criptografiaService.DescriptografarEmail(usuario.EmailCriptografado),
                Modulos = listaPlana
                    .GroupBy(p => p.Modulo)
                    .Select(g => new ModuloPermissoesViewModel
                    {
                        NomeModulo = g.Key,
                        Permissoes = g.Select(p => new PermissaoCheckViewModel
                        {
                            PermissaoId = p.Id,
                            Nome = p.Nome,
                            Chave = p.Chave,
                            Descricao = p.Descricao,
                            Concedido = p.TemPeloUsuario,
                            HerdadoDoPerfil = p.TemPeloPerfil
                        }).ToList()
                    }).ToList()
            };

            return View("EditarPermissoes", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("USUARIO_EDIT")]
        public async Task<IActionResult> SalvarPermissoes(int usuarioId, List<int> permissoesSelecionadas)
        {
            var usuario = await _usuarioRepositorio.ObterPorIdAsync(usuarioId);
            if (usuario == null) return NotFound();

            await _permissaoRepositorio.AtualizarPermissoesUsuarioAsync(usuarioId, permissoesSelecionadas);

            await _logService.RegistrarEdicaoAsync(
                "PermissoesUsuario", 
                "Alteração de permissões individuais", 
                $"Permissões concedidas manualmente: {permissoesSelecionadas?.Count ?? 0}", 
                usuarioId
            );

            TempData["Sucesso"] = "Permissões atualizadas com sucesso.";
            return RedirectToAction("EditarPermissoes", new { id = usuarioId });
        }

        /* =================== INATIVAR (ANTIGO EXCLUIR) =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("USUARIO_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var usuario = await _usuarioRepositorio.ObterPorIdAsync(id);
                if (usuario == null) return NotFound();

                // Chama o método renomeado no Repositório
                await _usuarioRepositorio.InativarAsync(id);
                
                // LOG DE EXCLUSÃO
                await _logService.RegistrarExclusaoAsync("Usuario", usuario, id);

                TempData["Sucesso"] = "Usuário inativado com sucesso!";
            }
            catch(Exception ex)
            {
                TempData["Erro"] = "Erro ao inativar: " + ex.Message;
            }
            
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
            // CORREÇÃO: Usar ListarTodosAsync para dropdowns (método criado no passo de Perfil)
            var perfis = await _perfilRepositorio.ListarTodosAsync();
            model.PerfisDisponiveis = perfis.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text  = p.Nome
            }).ToList();
        }

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
        [AllowAnonymous] // Necessário para a tag <img> carregar sem bloquear
        public async Task<IActionResult> ImagemPerfil(string hash)
        {
            if(string.IsNullOrEmpty(hash)) return NotFound();
            var arquivo = await _arquivoRepositorio.ObterPorHashAsync(hash);
            if (arquivo == null || arquivo.Conteudo == null) return NotFound();
            return File(arquivo.Conteudo, arquivo.ContentType);
        }

        /* =================== MEUS DADOS (PERFIL PESSOAL) =================== */
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
            TempData["Sucesso"] = "Seus dados foram atualizados com sucesso!";
            return RedirectToAction("MeusDados");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("USUARIO_EDIT")] // Quem pode editar, pode reativar
        public async Task<IActionResult> Reativar(int id)
        {
            var usuario = await _usuarioRepositorio.ObterPorIdAsync(id);
            if (usuario == null) return NotFound();

            await _usuarioRepositorio.AtivarAsync(id);
            
            await _logService.RegistrarEdicaoAsync(
                "Usuario", 
                "Reativação de Acesso", 
                $"Usuário {usuario.NameSkip} foi reativado.", 
                usuario.Id
            );

            TempData["Sucesso"] = "Acesso do usuário reativado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}