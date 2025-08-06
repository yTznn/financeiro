namespace Financeiro.Servicos.Seguranca
{
    public interface ICriptografiaService
    {
        string CriptografarEmail(string email);
        string DescriptografarEmail(string emailCriptografado);
        string GerarHashSenha(string senha);
        bool VerificarSenha(string senhaInformada, string senhaHashSalva);

        /// <summary>
        /// Gera um hash seguro e determinístico a partir do e-mail, utilizado exclusivamente para login.
        /// </summary>
        string HashEmailParaLogin(string email);
    }
}