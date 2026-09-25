#!/bin/bash
# gen-structure-v2.sh - FIX del vacio y del arbol feo
# No necesita tree instalado, usa find -prune (mucho mas seguro que grep -Ev)

ROOT=${1:-.}
OUT_TREE="project-structure.txt"
OUT_FILES="project-files.txt"

echo ">>> Generando estructura limpia de $ROOT ..."

# Limpiar anteriores
rm -f "$OUT_TREE" "$OUT_FILES"

# 1. TREE BONITO sin necesidad de 'tree' - usa find con prune
# Esto evita entrar a carpetas pesadas que te quemaban tokens
cat > /tmp/tree.py << 'PY'
import os
import sys

root = sys.argv[1]
ignore_dirs = {'bin','obj','node_modules','dist','dist-ssr','.git','.vs','.idea','TestResults','coverage','.vscode','publish','artifacts','android','ios','.next','.nuxt','.turbo','.cursor','data','brains-raw','letsencrypt','letsencrypt-logs','letsencrypt-work','certs','aspire'}

def should_ignore(path_parts):
    for p in path_parts:
        if p in ignore_dirs:
            return True
    return False

for dirpath, dirnames, filenames in os.walk(root):
    # Filtrar directorios in-place para no entrar
    dirnames[:] = [d for d in dirnames if d not in ignore_dirs and not d.startswith('.')]
    dirnames.sort()
    # Calcular profundidad
    rel = os.path.relpath(dirpath, root)
    if rel == '.':
        depth = 0
    else:
        depth = rel.count(os.sep)
    if depth > 6:
        dirnames[:] = []  # no bajar mas
        continue
    if should_ignore(rel.split(os.sep)):
        continue
    indent = "│   " * depth + ("├── " if depth>0 else "")
    print(f"{indent}{os.path.basename(dirpath)}/" if depth>0 else f"{os.path.basename(os.path.abspath(root))}/")
    # archivos de ese nivel (solo relevantes)
    for f in sorted(filenames)[:20]:  # max 20 por carpeta para no spamear
        if f.endswith(('.cs','.csproj','.sln','.ts','.vue','.js','.json','.yaml','.yml','.md')):
            print(f"{'│   '*(depth+1)}├── {f}")
PY

python3 /tmp/tree.py "$ROOT" > "$OUT_TREE" 2>&1

# Si python falla (Windows sin python), fallback simple
if [ ! -s "$OUT_TREE" ]; then
  echo "Fallback find..."
  find "$ROOT" -type d \( -name bin -o -name obj -o -name node_modules -o -name dist -o -name .git -o -name .vs -o -name TestResults \) -prune -o -type d -print | sort | head -n 200 > "$OUT_TREE"
fi

# 2. LISTA DE ARCHIVOS - FIX: usando -prune correctamente (por eso te salia vacio)
echo ">>> Generando lista de archivos..."
find "$ROOT" \
  \( -type d \( -name bin -o -name obj -o -name node_modules -o -name dist -o -name dist-ssr -o -name .git -o -name .vs -o -name .idea -o -name TestResults -o -name coverage -o -name publish -o -name artifacts -o -name data -o -name letsencrypt \) -prune \) -o \
  -type f \( -name "*.cs" -o -name "*.csproj" -o -name "*.sln" -o -name "*.ts" -o -name "*.vue" -o -name "*.js" -o -name "*.json" \) -print | sort > "$OUT_FILES"

# Filtrar solo los que no son lock files
grep -v -E "package-lock|pnpm-lock|.*\.min\.js" "$OUT_FILES" > "${OUT_FILES}.tmp" && mv "${OUT_FILES}.tmp" "$OUT_FILES"

# 3. RESUMEN
echo "" >> "$OUT_TREE"
echo "--- RESUMEN ---" >> "$OUT_TREE"
CS=$(grep -c "\.cs$" "$OUT_FILES" 2>/dev/null || echo 0)
VUE=$(grep -c "\.vue$" "$OUT_FILES" 2>/dev/null || echo 0)
TS=$(grep -c "\.ts$" "$OUT_FILES" 2>/dev/null || echo 0)
TOTAL=$(wc -l < "$OUT_FILES" 2>/dev/null || echo 0)
echo "Archivos .cs: $CS" >> "$OUT_TREE"
echo "Archivos .vue: $VUE" >> "$OUT_TREE"
echo "Archivos .ts: $TS" >> "$OUT_TREE"
echo "Total archivos codigo: $TOTAL" >> "$OUT_TREE"

echo "✅ Listo:"
echo "  - $OUT_TREE"
echo "  - $OUT_FILES"
echo ""
cat "$OUT_TREE"
echo ""
echo "--- Primeros 30 archivos de $OUT_FILES ---"
head -n 30 "$OUT_FILES"
