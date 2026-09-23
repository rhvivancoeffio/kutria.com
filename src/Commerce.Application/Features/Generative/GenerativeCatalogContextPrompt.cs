using System.Text;

namespace Commerce.Application.Features.Generative;

/// <summary>
/// Builds prompt fragments for brand/category suggest-vs-validate on generative content agents.
/// </summary>
public static class GenerativeCatalogContextPrompt
{
    public static string Build(
        string? brandId,
        string? brandName,
        string? categoryId,
        string? categoryPath)
    {
        var hasBrand = !string.IsNullOrWhiteSpace(brandId) || !string.IsNullOrWhiteSpace(brandName);
        var hasCategory = !string.IsNullOrWhiteSpace(categoryId) || !string.IsNullOrWhiteSpace(categoryPath);

        var sb = new StringBuilder();
        if (hasBrand || hasCategory)
        {
            sb.Append(" Contexto de catálogo ya elegido por el usuario (valida coherencia; no inventes otra marca/categoría):");
            if (hasBrand)
            {
                sb.Append(" Marca");
                if (!string.IsNullOrWhiteSpace(brandName))
                    sb.Append($"=\"{brandName.Trim()}\"");
                if (!string.IsNullOrWhiteSpace(brandId))
                    sb.Append($" (id={brandId.Trim()})");
                sb.Append('.');
            }

            if (hasCategory)
            {
                sb.Append(" Categoría");
                if (!string.IsNullOrWhiteSpace(categoryPath))
                    sb.Append($"=\"{categoryPath.Trim()}\"");
                if (!string.IsNullOrWhiteSpace(categoryId))
                    sb.Append($" (id={categoryId.Trim()})");
                sb.Append('.');
            }

            sb.Append(
                " Devuelve brandHint/categoryHint alineados con ese contexto, con isNewBrand/isNewCategory=false "
                + "y brandId/categoryId cuando ya existen.");
        }
        else
        {
            sb.Append(
                " El usuario no eligió marca ni categoría: sugiere brandHint/categoryHint y decide isNewBrand/isNewCategory "
                + "usando el catálogo de categorías que entrega la tool.");
        }

        return sb.ToString();
    }
}
