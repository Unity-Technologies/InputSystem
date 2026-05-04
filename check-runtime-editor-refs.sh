#!/bin/bash
[ -n "$BASH_VERSION" ] || { echo "ERROR: This script requires bash. Run with: bash $0" >&2; exit 1; }
set -euo pipefail

RUNTIME_DIR="Packages/com.unity.inputsystem/InputSystem/Runtime"

# POSIX ERE patterns for grep -E, compatible with macOS (BSD grep) and Ubuntu (GNU grep).
# No \b word boundaries — not portable across both. False positive risk is negligible
# since no real identifiers contain these namespace roots as substrings.
# (\.[A-Za-z0-9_]+)* covers sub-namespaces (UnityEditor.UI, …Editor.Tools, …).
# Add more lines as needed, e.g. 'UnityEditorInternal(\.[A-Za-z0-9_]+)*'
FORBIDDEN_REGEX=(
    'UnityEditor(\.[A-Za-z0-9_]+)*'
    'UnityEngine\.InputSystem\.Editor(\.[A-Za-z0-9_]+)*'
)

RED=$'\033[0;31m'
GREEN=$'\033[0;32m'
NC=$'\033[0m'

INCLUDE_COMMENTS=false
for arg in "$@"; do
    case "$arg" in
        --include-comments) INCLUDE_COMMENTS=true ;;
        --help)
            cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Check that Runtime code has no Editor namespace dependencies.
Edit FORBIDDEN_REGEX in this script to add patterns.

Options:
  --include-comments   Also flag references inside XML doc and // comments
  --help               Show this help
EOF
            exit 0 ;;
        *) echo "Unknown option: $arg"; exit 1 ;;
    esac
done

[ -d "$RUNTIME_DIR" ] || { echo "${RED}ERROR: Runtime directory not found: $RUNTIME_DIR${NC}" >&2; exit 1; }

COMBINED=$(IFS='|'; echo "${FORBIDDEN_REGEX[*]}")

GREP_OUTPUT=$(grep -rnw --include='*.cs' --color=never -E "$COMBINED" "$RUNTIME_DIR" || true)

if [ "$INCLUDE_COMMENTS" = false ]; then
    VIOLATIONS=$(echo "$GREP_OUTPUT" | grep -Ev ':[0-9]+:[[:space:]]*//' || true)
else
    VIOLATIONS="$GREP_OUTPUT"
fi

if [ -z "$VIOLATIONS" ]; then
    echo "${GREEN}PASS: No Editor namespace references found in Runtime code.${NC}"
    exit 0
fi

VIOLATION_COUNT=$(echo "$VIOLATIONS" | wc -l | tr -d ' ')
echo "${RED}FAIL: Found $VIOLATION_COUNT Editor namespace reference(s) in Runtime code:${NC}"
echo ""
echo "$VIOLATIONS"
echo ""
echo "${RED}Active patterns:${NC}"
for re in "${FORBIDDEN_REGEX[@]}"; do
    printf '  \033[0;31m%s\033[0m\n' "$re"
done
exit 1
