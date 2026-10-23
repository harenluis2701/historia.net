using Firmeza.Web.Models;

namespace Firmeza.Api.Services;

public interface IReceiptPdfService
{
    byte[] Generate(Sale sale);
}
