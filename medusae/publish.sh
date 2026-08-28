#!/bin/bash

if [ "$1" = "" ]; then
  echo "Missing folder name"
  echo "Example: ./publish.sh folder_name"
  exit
fi

pwd
cd /home/funfun/Desktop/nadekopi/medusae/"$1" || exit

dotnet publish -o /home/funfun/Desktop/nadekopi/medusae/"$1"/bin/medusae/"$1" /p:DebugType=embedded

echo "Renaming dll to correct file name"
mv /home/funfun/Desktop/nadekopi/medusae/"$1"/bin/medusae/"$1"/medusae.dll bin/medusae/"$1"/"$1".dll

if [ -d "/home/funfun/Desktop/nadekopi/data/medusae/$1" ]; then
  echo "Removed old symlink"
  rm -rf /home/funfun/Desktop/nadekopi/data/medusae/"$1"
fi

cd /home/funfun/Desktop/nadekopi/medusae/"$1"/bin/medusae/"$1" || exit
for file in *; do
  [[ $file = *.yml || $file = *.json || $file = "$1.dll" ]] && continue
  rm -rf "$file"
done

echo "Moving folder"
mv /home/funfun/Desktop/nadekopi/medusae/"$1"/bin/medusae/"$1" /home/funfun/Desktop/nadekopi/data/medusae
