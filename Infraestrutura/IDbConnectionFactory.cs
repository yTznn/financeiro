// Infraestrutura/IDbConnectionFactory.cs
using System.Data;

namespace Financeiro.Infraestrutura
{
    /// <summary>
    /// Fábrica de conexões IDbConnection (SQL Server) para uso pelos repositórios.
    /// </summary>
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}