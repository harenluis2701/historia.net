using System.ComponentModel.DataAnnotations;

namespace Firmeza.Api.Dtos;

public record CustomerDto(int Id, string Document, string FullName, string Email, string Phone);

public class CustomerInputDto
{
    [Required, StringLength(30)] public string Document { get; set; } = string.Empty;
    [Required, StringLength(160)] public string FullName { get; set; } = string.Empty;
    [EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [StringLength(40)] public string Phone { get; set; } = string.Empty;
}
