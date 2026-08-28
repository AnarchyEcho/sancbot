#!/bin/bash
export "$(xargs <.env.local)"

#Prep medusae
for dir in /home/"$USERNAME"/Desktop/nadekopi/medusae/*/; do
  dir=${dir%*/}
  /home/"$USERNAME"/Desktop/nadekopi/medusae/publish.sh "${dir##*/}"
done

# Prep data
stow data --adopt -R --target=/home/"$USERNAME"/Desktop/nadekopi/nadekobot/src/NadekoBot/data

#Build the bot
printf "Building bot...\n\n"
dotnet build -c Release /home/"$USERNAME"/Desktop/nadekopi/nadekobot/src/NadekoBot

#Make sure db is correct
if [ -d "/home/$USERNAME/Desktop/nadekopi/nadekobot/src/NadekoBot/bin/Release" ]; then
  rm /home/"$USERNAME"/Desktop/nadekopi/nadekobot/src/NadekoBot/bin/Release/net9.0/data/NadekoBot.db
fi

stow data --adopt -R --target=/home/"$USERNAME"/Desktop/nadekopi/nadekobot/src/NadekoBot/bin/Release/net9.0/data/
printf "\n\nSymlinked data into the build.\n\n"

#Boot the bot
cd /home/"$USERNAME"/Desktop/nadekopi/nadekobot/src/NadekoBot/bin/Release/net9.0 || exit
dotnet ./NadekoBot.dll
