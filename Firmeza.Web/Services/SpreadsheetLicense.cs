using OfficeOpenXml;

namespace Firmeza.Web.Services;

public static class SpreadsheetLicense
{
    public static void Configure(IConfiguration configuration)
    {
        var license = configuration["EPPlus:License"] ?? Environment.GetEnvironmentVariable("EPPlusLicense");
        if (string.IsNullOrWhiteSpace(license)) return;
        if (license.StartsWith("Commercial:", StringComparison.OrdinalIgnoreCase))
            ExcelPackage.License.SetCommercial(license["Commercial:".Length..]);
        else if (license.StartsWith("NonCommercialPersonal:", StringComparison.OrdinalIgnoreCase))
            ExcelPackage.License.SetNonCommercialPersonal(license["NonCommercialPersonal:".Length..]);
        else if (license.StartsWith("NonCommercialOrganization:", StringComparison.OrdinalIgnoreCase))
            ExcelPackage.License.SetNonCommercialOrganization(license["NonCommercialOrganization:".Length..]);
        else throw new InvalidOperationException("EPPlus:License debe tener prefijo Commercial:, NonCommercialPersonal: o NonCommercialOrganization:.");
    }

    public static void EnsureConfigured()
    {
        if (ExcelPackage.License.LicenseType is null)
            throw new InvalidOperationException("Configura EPPlus:License o la variable EPPlusLicense antes de importar o exportar Excel.");
    }
}
