// Infraestrutura/DbConnectionFactory.cs
using System;
using System.Data;
using Microsoft.Data.SqlClient;          // <- provider oficial atualizado
using Microsoft.Extensions.Logging;

namespace Financeiro.Infraestrutura
{
    /// <summary>
    /// Implementação concreta da fábrica de conexões Dapper.
    /// Mantém a connection-string e devolve conexões desconectadas.
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<DbConnectionFactory>? _logger;

        public DbConnectionFactory(string connectionString,
                                   ILogger<DbConnectionFactory>? logger = null)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public IDbConnection CreateConnection()
        {
            _logger?.LogDebug("Criando conexão SQL Server.");
            return new SqlConnection(_connectionString);
        }
    }
}