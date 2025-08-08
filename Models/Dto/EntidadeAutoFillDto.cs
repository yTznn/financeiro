namespace Financeiro.Models.DTO
{
    public record EntidadeAutoFillDto(
        string Nome,
        string? Sigla,
        int? EnderecoId,
        int? ContaBancariaId
    );
}