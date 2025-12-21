#!/bin/sh
printf '\033c\033]0;%s\a' NewFinalProject
base_path="$(dirname "$(realpath "$0")")"
"$base_path/NewFinalProject.x86_64" "$@"
