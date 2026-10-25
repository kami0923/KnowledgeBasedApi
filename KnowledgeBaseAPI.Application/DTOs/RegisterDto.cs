using System.ComponentModel.DataAnnotations;

namespace KnowledgeBaseAPI.Application.DTOs;

    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

    }

