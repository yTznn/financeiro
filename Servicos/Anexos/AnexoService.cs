using Financeiro.Models;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Financeiro.Servicos.Anexos
{
    public class AnexoService : IAnexoService
    {
        private readonly IArquivoRepositorio _arquivoRepositorio;

        public AnexoService(IArquivoRepositorio arquivoRepositorio)
        {
            _arquivoRepositorio = arquivoRepositorio;
        }

        public async Task<int> SalvarAnexoAsync(IFormFile arquivo, string origem, int? chaveReferencia = null)
        {
            using var stream = new MemoryStream();
            await arquivo.CopyToAsync(stream);
            var bytes = stream.ToArray();

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(bytes);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "");

            var entidade = new Arquivo
            {
                NomeOriginal = Path.GetFileNameWithoutExtension(arquivo.FileName),
                Extensao = Path.GetExtension(arquivo.FileName),
                Conteudo = bytes,
                Tamanho = arquivo.Length,
                ContentType = arquivo.ContentType,
                Hash = hash,
                Origem = origem,
                ChaveReferencia = chaveReferencia
            };

            return await _arquivoRepositorio.AdicionarAsync(entidade);
        }
    }
}