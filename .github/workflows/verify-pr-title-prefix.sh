#!/bin/bash

exit_with_failure()
{
    echo "❌ $*" 1>&2 ; exit 1;
}

TITLE_STRING="$1"
shift  # Shift all arguments to the left, so $2 becomes $1, $3 becomes $2, etc.

# The remaining arguments are treated as an array of strings
PREFIXES_REQUIRED=("$@")

# Validate the title string prefix based on prefixes required
for PREFIX in "${PREFIXES_REQUIRED[@]}";
do
    if [[ "$TITLE_STRING" =~ ^$PREFIX ]]; then
        echo "✅"
        exit 0
    fi
done

PREFIXES_REQUIRED_STRING="${PREFIXES_REQUIRED[*]}"

exit_with_failure "PR Title needs the required prefixes: $PREFIXES_REQUIRED_STRING"
