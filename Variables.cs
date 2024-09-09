using Discord.WebSocket;

namespace TownLibrarian
{
    public struct Guild
    {
        // These are the channel IDs for the discord channels the bot will read from.
        public static DiscordSocketClient client;

        // The ID of the last message the bot sent. Used to delete own message.
        public static Discord.Rest.RestUserMessage LastBotMessageID;

        // The ID for the current Discord.
        public static ulong guildID = 573135393239859222;

        // The channel ID for the teachers-talk channel in the Meta Discord. Used for previewing commands with !.
        public static ulong teacherTalkID = 868060024654667776;
    }
}
