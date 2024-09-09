using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownLibrarian
{
    public class Say
    {
        // This is called to send a message on discord.
        public async static Task It(SocketMessage MessageToRespondTo, string whatToSay)
        {
            Guild.LastBotMessageID = await MessageToRespondTo.Channel.SendMessageAsync(whatToSay);
        }





        // This is called to send an embed to discord.
        public async static Task EmbedForSlash(SocketSlashCommand slashCommand, EmbedBuilder importedEmbedBuilder, bool isEphemeral = true)
        {
            await slashCommand.RespondAsync(embed: importedEmbedBuilder.Build(), ephemeral: isEphemeral);
        }





        // This is called to send an preview embed to discord.
        public async static Task EmbedForPreview(SocketMessage Message, EmbedBuilder importedEmbedBuilder)
        {
            var responseChannel = Guild.client.GetChannel(Message.Channel.Id) as IMessageChannel;

            await responseChannel.SendMessageAsync("", false, importedEmbedBuilder.Build());
        }
    }
}
