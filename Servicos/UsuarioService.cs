using Financeiro.Models;
using Financeiro.Repositorios;

namespace Financeiro.Servicos
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepositorio              _usuarioRepo;
        private readonly IUsuarioEntidadeRepositorio      _usuarioEntidadeRepo;

        public UsuarioService(IUsuarioRepositorio usuarioRepo,
                              IUsuarioEntidadeRepositorio usuarioEntidadeRepo)
        {
            _usuarioRepo          = usuarioRepo;
            _usuarioEntidadeRepo  = usuarioEntidadeRepo;
        }

        /* ---------- NOVOS MÉTODOS ---------- */

        public async Task<IEnumerable<UsuarioEntidade>> ListarEntidadesAsync(int usuarioId)
            => await _usuarioEntidadeRepo.ListarPorUsuarioAsync(usuarioId);

        public async Task SalvarEntidadesAsync(int usuarioId,
                                               IEnumerable<int> entidadesSelecionadas,
                                               int entidadeAtivaId)
        {
            // 1) Remove vínculos anteriores
            await _usuarioEntidadeRepo.RemoverTodosPorUsuarioAsync(usuarioId);

            // 2) Insere os novos vínculos
            foreach (var entidadeId in entidadesSelecionadas.Distinct())
            {
                var vinculo = new UsuarioEntidade
                {
                    UsuarioId  = usuarioId,
                    EntidadeId = entidadeId,
                    Ativo      = entidadeId == entidadeAtivaId
                };
                await _usuarioEntidadeRepo.InserirAsync(vinculo);
            }

            // 3) Garantia extra: se o ativa não estava na lista, marque a primeira como ativa
            if (!entidadesSelecionadas.Contains(entidadeAtivaId) && entidadesSelecionadas.Any())
            {
                await _usuarioEntidadeRepo.AtualizarAtivoAsync(usuarioId, entidadesSelecionadas.First());
            }
        }
    }
}