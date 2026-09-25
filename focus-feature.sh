#!/bin/bash
# focus-feature.sh - Cambia el foco low-cost para Cline+Groq y Cursor
# Uso: ./focus-feature.sh Brands
#      ./focus-feature.sh Catalog
#      ./focus-feature.sh Agents
#      ./focus-feature.sh Frontend
#      ./focus-feature.sh All (resetea)

FEATURE=${1:-Catalog}
ROOT="."

echo ">>> Enfocando en: $FEATURE (low-cost mode)"

# Base ignore siempre
cat > .cursorignore << 'EOF'
**/bin/
**/obj/
**/Migrations/
**/node_modules/
**/dist/
**/.git/
**/.vs/
**/letsencrypt/
**/data/
**/brains-raw/
**/certs/
**/TestResults/
**/publish/
*.dll
*.pdb
package-lock.json
EOF

# Copiar a .clineignore (Cline NO lee .cursorignore)
cp .cursorignore .clineignore

# Generar .cursorindexingignore segun feature
cat > .cursorindexingignore << EOF
# Auto-generado por focus-feature.sh para $FEATURE - $(date)
**/bin/
**/obj/
**/Migrations/
**/node_modules/
**/dist/
**/letsencrypt/
**/data/
**/brains-raw/
**/certs/
**/aspire/
**/public/
**/skills/
**/TestResults/
EOF

case "$FEATURE" in
  Brands|Catalog|Products)
    echo "src/Commerce.Application/Features/Agents/" >> .cursorindexingignore
    echo "src/Commerce.Application/Features/Brains/" >> .cursorindexingignore
    echo "src/Commerce.Application/Features/Generative/" >> .cursorindexingignore
    echo "src/Commerce.Application/Features/Integrations/" >> .cursorindexingignore
    echo "src/Commerce.Application/Features/Policies/" >> .cursorindexingignore
    echo "src/Commerce.Infrastructure/Agents/" >> .cursorindexingignore
    echo "src/Commerce.Infrastructure/Vectors/" >> .cursorindexingignore
    echo "src/Commerce.Worker/" >> .cursorindexingignore
    echo "src/Commerce.Mcp/" >> .cursorindexingignore
    echo "src/Commerce.Web/" >> .cursorindexingignore
    echo ">>> Foco: solo Catalog (Brands/Categories/Products) + Common + Domain"
    ;;
  Agents)
    echo "src/Commerce.Application/Features/Catalog/" >> .cursorindexingignore
    echo "src/Commerce.Application/Features/Billing/" >> .cursorindexingignore
    echo "src/Commerce.Web/" >> .cursorindexingignore
    echo ">>> Foco: solo Agents"
    ;;
  Frontend|Web)
    echo "src/Commerce.Application/Features/" >> .cursorindexingignore
    echo "src/Commerce.Infrastructure/" >> .cursorindexingignore
    echo "src/Commerce.Domain/" >> .cursorindexingignore
    echo ">>> Foco: solo Commerce.Web (VueJS)"
    ;;
  All)
    echo "# All - indexa todo (883 archivos, caro)" > .cursorindexingignore
    echo "**/bin/" >> .cursorindexingignore
    echo "**/obj/" >> .cursorindexingignore
    echo "**/node_modules/" >> .cursorindexingignore
    echo "**/Migrations/" >> .cursorindexingignore
    echo ">>> Foco: TODO (883 archivos - caro, solo para busquedas globales)"
    ;;
  *)
    echo "Feature no reconocido. Usa: Brands, Catalog, Agents, Frontend, All"
    ;;
esac

# Copiar a .clineignore el indexingignore tambien para Cline+Groq
cat .cursorindexingignore >> .clineignore

echo ""
echo "✅ Archivos generados:"
echo "  .cursorignore ($(wc -l < .cursorignore) lineas)"
echo "  .clineignore ($(wc -l < .clineignore) lineas) - para Cline+Groq"
echo "  .cursorindexingignore ($(wc -l < .cursorindexingignore) lineas) - para Cursor embeddings"
echo ""
echo "Ahora reinicia Cursor (Cmd+Shift+P > Clear Index) y Cline detecta automaticamente."