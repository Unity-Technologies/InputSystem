#!/usr/bin/env bash
set -euo pipefail

RUNTIME_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/Packages/com.unity.inputsystem/InputSystem/Runtime"

# PCRE2 regexes — \b prevents matching identifiers that merely contain these strings.
# (\.[A-Za-z0-9_]+)* covers sub-namespaces (UnityEditor.UI, …Editor.Tools, …).
# Add more lines as needed, e.g. '\bUnityEditorInternal(\.[A-Za-z0-9_]+)*\b'
FORBIDDEN_REGEX=(
    '\bUnityEditor(\.[A-Za-z0-9_]+)*\b'
    '\bUnityEngine\.InputSystem\.Editor(\.[A-Za-z0-9_]+)*\b'
)

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

INCLUDE_COMMENTS=false
for arg in "$@"; do
    case "$arg" in
        --include-comments) INCLUDE_COMMENTS=true ;;
        --help)
            cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Check that Runtime code has no Editor namespace dependencies.
Patterns are PCRE2; edit FORBIDDEN_REGEX in this script to add roots.

Options:
  --include-comments   Also flag references inside XML doc and // comments
  --help               Show this help
EOF
            exit 0 ;;
        *) echo "Unknown option: $arg"; exit 1 ;;
    esac
done

[ -d "$RUNTIME_DIR" ] || { echo -e "${RED}ERROR: Runtime directory not found: $RUNTIME_DIR${NC}" >&2; exit 1; }

COMBINED=$(IFS='|'; echo "${FORBIDDEN_REGEX[*]}")

if [ "$INCLUDE_COMMENTS" = false ]; then
    PATTERN="^(?!\s*//).*(?:$COMBINED)"
else
    PATTERN="(?:$COMBINED)"
fi

VIOLATIONS=$(rg --type cs --line-number --no-heading --color never --pcre2 "$PATTERN" "$RUNTIME_DIR" 2>/dev/null || true)

if [ -z "$VIOLATIONS" ]; then
    echo -e "${GREEN}PASS: No Editor namespace references found in Runtime code.${NC}"
    exit 0
fi

VIOLATION_COUNT=$(echo "$VIOLATIONS" | wc -l | tr -d ' ')
echo -e "${RED}FAIL: Found $VIOLATION_COUNT Editor namespace reference(s) in Runtime code:${NC}"
echo ""
echo "$VIOLATIONS"
echo ""
echo -e "${RED}Active patterns:${NC}"
for re in "${FORBIDDEN_REGEX[@]}"; do
    printf '  \033[0;31m%s\033[0m\n' "$re"
done
exit 1
