using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Financeiro.Models.ViewModels
{
    public class UsuarioViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O NameSkip é obrigatório.")]
        [MinLength(8, ErrorMessage = "O NameSkip deve ter no mínimo 8 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9\.]+$", ErrorMessage = "O NameSkip deve conter apenas letras, números e ponto.")]
        public string NameSkip { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarSenha { get; set; }

        [Display(Name = "Pessoa Física")]
        public int? PessoaFisicaId { get; set; }

        [Display(Name = "Foto de Perfil")]
        public IFormFile? ImagemPerfil { get; set; }

        /// <summary>
        /// Hash da imagem para exibição do avatar do usuário.
        /// </summary>
        public string? HashImagem { get; set; }

        /// <summary>
        /// Campo opcional para mensagens de erro na view (ex: login).
        /// </summary>
        public string? MensagemErro { get; set; }

        /// <summary>
        /// Lista de pessoas físicas para preencher o dropdown.
        /// </summary>
        public List<SelectListItem> PessoasFisicas { get; set; } = new();

        /// <summary>
        /// Identificador do perfil selecionado
        /// </summary>
        [Required(ErrorMessage = "O perfil é obrigatório.")]
        [Display(Name = "Perfil")]
        public int? PerfilId { get; set; }

        /// <summary>
        /// Lista de perfis disponíveis para o dropdown
        /// </summary>
        public List<SelectListItem> Perfis { get; set; } = new();
        public List<SelectListItem> PerfisDisponiveis { get; set; } = new();
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var erros = new List<ValidationResult>();

            // Validação da senha: só exige se estiver preenchida (útil em edição)
            if (!string.IsNullOrEmpty(Senha))
            {
                if (!string.IsNullOrEmpty(NameSkip))
                {
                    var partesNome = NameSkip.ToLower().Split('.', '-', '_');

                    foreach (var parte in partesNome)
                    {
                        if (parte.Length >= 4 && Senha.ToLower().Contains(parte))
                        {
                            erros.Add(new ValidationResult("A senha não pode conter partes do nome de usuário.", new[] { "Senha" }));
                            break;
                        }
                    }

                    if (Regex.IsMatch(Senha.ToLower(), @"(abc|123|senha|qwerty|0000|1111)"))
                    {
                        erros.Add(new ValidationResult("A senha não pode conter sequências simples.", new[] { "Senha" }));
                    }
                }

                if (string.IsNullOrEmpty(ConfirmarSenha))
                {
                    erros.Add(new ValidationResult("O campo ConfirmarSenha é obrigatório quando a senha é informada.", new[] { "ConfirmarSenha" }));
                }
            }

            // Validação da imagem
            if (ImagemPerfil != null)
            {
                var extensao = Path.GetExtension(ImagemPerfil.FileName).ToLower();
                var nome = Path.GetFileNameWithoutExtension(ImagemPerfil.FileName);

                if (ImagemPerfil.Length > 10 * 1024 * 1024)
                {
                    erros.Add(new ValidationResult("A imagem não pode ultrapassar 10MB.", new[] { "ImagemPerfil" }));
                }

                if (extensao != ".png" && extensao != ".jpeg" && extensao != ".jpg")
                {
                    erros.Add(new ValidationResult("Apenas arquivos .png ou .jpeg são permitidos para perfil.", new[] { "ImagemPerfil" }));
                }

                if (nome.Length > 10 || Regex.IsMatch(nome, @"[^a-zA-Z0-9\-]"))
                {
                    erros.Add(new ValidationResult("O nome do arquivo deve ter até 10 caracteres e não pode conter espaços ou caracteres especiais.", new[] { "ImagemPerfil" }));
                }
            }

            // Validação manual do Perfil (caso queira reforçar)
            if (PerfilId == null)
            {
                erros.Add(new ValidationResult("O perfil é obrigatório.", new[] { "PerfilId" }));
            }
            return erros;
        }
    }
}
