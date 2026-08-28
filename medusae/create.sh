#!/bin/sh

if [ "$1" = "" ]; then
  echo "Missing medusa name"
  echo "Example: ./create.sh medusa_name"
  exit
fi

mkdir "$1"
cd "$1" || exit
dotnet new nadeko-medusa
