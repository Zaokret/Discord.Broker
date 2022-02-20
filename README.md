Discord bot called Attarcoin Broker written in C# on top of [Discord.Net](https://github.com/discord-net/Discord.Net) library.

Uses json files for storing data.

Bot is fully functional only in a server [Attar's Smokehouse and Scotch Bar](https://discord.gg/bUQFErEr) created by [Attar](https://linktr.ee/Attar).

It listens and records specific emote reaction <img src="https://cdn.discordapp.com/emojis/710382504196178000.png" alt="attarcoin emote" width="20"/> called attarcoin.

User's wallets contain number of coins a user currently has.

Coin related commands:

 - $coins - show the command issuer his wallet
 - $rich - show an ordered list of 5 richest users, command issuer's wallet and his rank
 - $transfer NUMBER @user - transfers coins from the command issuer's wallet to the @user's wallet 

Top 5 richest user's are granted @Rich role that gives users advanced server permissions, special username color with a <img src="https://cdn.discordapp.com/emojis/710382504196178000.png" alt="attarcoin emote" width="20"/> next to it.

Bot can help increase visibility of users messages by replying to a message with a command:

 - $award - transfers 25 coins from the command issuer to the message' s author, copies the text part of the message with a link to the original message, name of the user who issued the command and message's author to the separate channel used only for awarded messages
 - $pin - transfers 100 coins from the command issuer to the message's author, copies the message in the same way as $award command, then pins the message to the award channel.

Bot can help increase user's participation with commands:

 - $poll - creates a poll with custom title, description and number of options coded by number emotes
 - $bet - creates a bet with custom title and description, bet options with custom title, description and odds used to calculate rewards
 - $auction @user - creates a room for an original two player text game in which players compete to win coins, 100% skill based
 - $write #channel "message content" - used only by users with a moderator role to write a message as the bot in the specified #channel

Bot uses [media recommendation API](https://tastedive.com) to recommend similar movies, books, tv shows, podcasts and music.

 - $recommend "media term" - shows 5 most similar pieces of media with a text description from wikipedia, link to wikipedia and youtube 

This is a high level overview of the commands and doesn't represent exactly how these commands are used. For the full list of commands and their exact explanation you need to join the server issue command:

 - $commands

Attarcoin Broker has it's own [twitter account](https://twitter.com/attarcoin) where discord users repost awarded messages and Attar's content.

Keep Attarcoin Broker in business by donating to his [paypal](https://www.paypal.com/paypalme/attarcoinbroker).
