namespace Financeiro.Servicos.Seguranca
{
    public interface ICriptografiaService
    {
        string CriptografarEmail(string email);
        string DescriptografarEmail(string emailCriptografado);
        string GerarHashSenha(string senha);
        bool VerificarSenha(string senhaInformada, string senhaHashSalva);
    }
}