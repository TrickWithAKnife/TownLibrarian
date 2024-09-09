using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TownLibrarian
{
    class Program
    {
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();





        public async Task MainAsync()
        {
            // Read the credentials with login information.
            string tokenFromTestFile = System.IO.File.ReadAllText("./Credentials/Token.txt");
            Console.WriteLine("Loaded credentials.");

            // This is needed to make sure the bot can get messages.
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            // Create a new Discord Socket Client to handle anything discord related.
            Guild.client = new DiscordSocketClient(config);

            // Log in to Discord.
            Console.WriteLine("Logging in...");
            await Guild.client.LoginAsync(TokenType.Bot, tokenFromTestFile);
            await Guild.client.StartAsync();
            Console.WriteLine("Connected to Discord.");

            // If a message is posted on discord, analyze it.
            Guild.client.MessageReceived += ProcessMessage;

            // If a user uses a slash command, reply if appropriate.
            Guild.client.SlashCommandExecuted += HandleSlashCommand;

            // Uncomment to register commands every time the bot is started.
            // Guild.client.Ready += RegisterAllSlashCommands;

            // Stop the console from closing.
            await Task.Delay(-1);
        }





        private async Task ProcessMessage(SocketMessage Message)
        {
            // Ignore bots
            if (Message.Author.IsBot) return;


            if (Message.Content.ToLower() == "!list")
            {
                await Say.It(Message, "From now on, Town Librarian commands will start with slash (/). To see a list of commands, type slash, then click the Town Librarian icon.");
            }

            // Allow members of the teacher-talk channel to preview commands by using ! instead of /.
            else if (Message.Content.StartsWith("!") && Message.Channel.Id == Guild.teacherTalkID)
            {
                string commandToPreview = Message.Content.ToLower().Substring(1);

                await PreviewSlashCommand(Message, commandToPreview);

            }

            else if (Message.Content.ToLower().StartsWith("help "))
            {
                // Remove the "help " from the start of the string.
                string moduleToView = Message.Content.ToLower().Substring(5);

                // Remove all punctuation from the string.
                moduleToView = new string(moduleToView.Where(c => !char.IsPunctuation(c)).ToArray());

                // Search for a suitable module.
                await PreviewModuleCommand(Message, moduleToView);

            }

            // Discord IDs of users who are permitted to register slash commands.
            ulong[] discordIDsOfUsersWhoCanUpdateSlashCommands = { 229586364532916224, 150767157045624832, 339046573969506305, 496805794143010816 }; // CJ, Poi, Hazy, OddMellon


            // Ignore anyone who isn't an approved user.
            if (!discordIDsOfUsersWhoCanUpdateSlashCommands.Contains(Message.Author.Id)) return;


            // Only update if this command phrase is used. Used for all commands.
            if (Message.Content.ToLower() == "update slash commands")
            {
                await Say.It(Message, "Registering all slash commands. This may take 5-10 minutes.");
                await RegisterAllSlashCommands(Message);
                await Say.It(Message, "Slash commands registered. Any changes will take a while to show up on Discord.");
            }


            // Only update if this command phrase is used. Used for one command.
            else if (Message.Content.ToLower().StartsWith("update slash command ") && Message.Content.Length >= 23)
            {
                string commandName = Message.Content.ToLower().Substring(21);
                await Say.It(Message, "Registering slash command **" + commandName + "**.");
                await RegisterSingleSlashCommand(Message, commandName);
            }


            // Only remove the slash command if this command phrase is used.
            else if (Message.Content.ToLower().StartsWith("remove slash command"))
            {
                try
                {
                    // Just get the ID number after "remove slash command".
                    string commandIDAsString = Message.Content.ToLower().Substring(Message.Content.ToLower().IndexOf("remove slash command") + 21);

                    // Change the Slash ID from a string to an ulong.
                    ulong commandID = ulong.Parse(commandIDAsString);

                    // Remove the slash command.
                    await RemoveSlashCommand(Message, commandID);
                }
                catch
                {
                    Console.WriteLine("Unable to remove slash command.");
                }
            }
        }





        // Removes a specific command. The ID can be obtained by typing the /command into discord and right clicking.
        public async Task RemoveSlashCommand(SocketMessage Message, ulong commandID)
        {
            var slashCommandToRemove = await Guild.client.GetGlobalApplicationCommandAsync(commandID);

            Console.WriteLine("Going to try to remove command ID " + commandID + ".");

            try
            {
                if (slashCommandToRemove != null) await slashCommandToRemove.DeleteAsync();
                else await Say.It(Message, "Couldn't find that ID number.");
            }
            catch
            {
                await Say.It(Message, "Something went wrong.");
            }
        }






        public async Task RegisterSingleSlashCommand(SocketMessage message, string commandName)
        {
            // Create a list called data, containing everything from the spreadsheet.
            List<Database.CommandData> data = Database.init();

            // Cycle through every command in the database.
            foreach (Database.CommandData myData in data)
            {
                if (myData.Trigger == commandName.ToLower())
                {
                    // Build a new slash command to add to Discord later.
                    var tempCommand = new SlashCommandBuilder();

                    Console.WriteLine("Trying to register: " + commandName + ".");

                    tempCommand.WithName(commandName);

                    // Add an optional option. 
                    tempCommand.AddOption("show", ApplicationCommandOptionType.String, "true", isRequired: false);

                    // Adds a description if appropriate data is found in the title field of the database. I believe the max length is 100 characters.
                    if (myData.Description != null && myData.Title.Length <= 100 && myData.Title.Length != null)
                    {
                        tempCommand.WithDescription(myData.Title);
                    }

                    try
                    {
                        // Add global slash command.
                        await Guild.client.Rest.CreateGlobalCommand(tempCommand.Build());

                        await Say.It(message, "Command **" + commandName + "** added successfully.");

                        return;
                    }
                    catch //(HttpException exception)
                    {
                        try
                        {
                            // Discord has a 100 global command and 100 guild command limit for slash commands.
                            // If the global command limit is reached, we will try registering as a guild command.
                            await Guild.client.Rest.CreateGuildCommand(tempCommand.Build(), Guild.guildID);

                            await Say.It(message, "Command **" + commandName + "** added successfully.");

                            return;
                        }
                        catch (HttpException exception)
                        {
                            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                            Console.WriteLine(json);
                            await Say.It(message, "Slash command **" + commandName + "** not added.");

                            return;
                        }
                    }
                }
            }
            //Console.WriteLine("Finished building commands. They may take time to show up on discord.");
        }





        public async Task RegisterAllSlashCommands(SocketMessage Message)
        {
            // Create a list called data, containing everything from the spreadsheet.
            List<Database.CommandData> data = Database.init();

            // Cycle through every command in the database.
            foreach (Database.CommandData myData in data)
            {
                // Build a new slash command to add to Discord later.
                var tempCommand = new SlashCommandBuilder();

                // In future, all this should be changed when the database triggers have been renamed.
                string[] tempnameArray = myData.Trigger.Split(',');
                string tempname = tempnameArray[0].ToLower();
                tempname = tempname.Replace(' ', '-');

                Console.WriteLine("Trying to register: " + tempname + ".");

                tempCommand.WithName(tempname);

                // Add an optional option. 
                tempCommand.AddOption("show", ApplicationCommandOptionType.String, "true", isRequired: false);

                // Adds a description if appropriate data is found in the title field of the database. I believe the max length is 100 characters.
                if (myData.Description != null && myData.Title.Length <= 100 && myData.Title.Length != null)
                {
                    tempCommand.WithDescription(myData.Title);
                }

                try
                {
                    // Add global slash command.
                    await Guild.client.Rest.CreateGlobalCommand(tempCommand.Build());

                    Console.WriteLine("Global command \"" + tempname + "\" added successfully.");
                    //await Say.It(Message, "Slash command **" + tempname + "** added as a global command.");
                }
                catch (HttpException exception)
                {
                    try
                    {
                        // Discord has a 100 global command and 100 guild command limit for slash commands.
                        // If the global command limit is reached, we will try registering as a guild command.
                        await Guild.client.Rest.CreateGuildCommand(tempCommand.Build(), Guild.guildID);

                        Console.WriteLine("Guild command \"" + tempname + "\" added successfully.");
                        //await Say.It(Message, "Slash command **" + tempname + "** added as a guild command.");

                    }
                    catch
                    {
                        var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                        Console.WriteLine(json);
                        Console.WriteLine("Command not added.");
                        await Say.It(Message, "Slash command **" + tempname + "** not added.");
                    }
                }
            }
            Console.WriteLine("Finished building commands. They may take time to show up on discord.");
        }





        private async Task PreviewSlashCommand(SocketMessage Message, string commandName)
        {
            Console.WriteLine("Will attempt to preview the " + commandName + " command.");

            // Create a list called data, containing everything from the spreadsheet.
            List<Database.CommandData> data = Database.init();

            foreach (Database.CommandData myData in data)
            {
                if (myData.Trigger == commandName)
                {
                    var FeedbackEmbed = new EmbedBuilder();

                    // Sets the title for the embed.
                    if (myData.Title != null)
                    {
                        FeedbackEmbed.WithTitle(myData.Title);
                    }
                    // Stop checking this row, as there is no title, suggesting it's incomplete.
                    else break;

                    // Adds an image at the bottom of the embed if one is found.
                    if (myData.Image != "")
                    {
                        FeedbackEmbed.WithImageUrl(myData.Image);
                    }

                    // Adds a description field if appropriate data is found.
                    if (myData.Description != null)
                    {
                        FeedbackEmbed.WithDescription(myData.Description);
                    }

                    // Set the side colour.
                    FeedbackEmbed.WithColor(Color.Green);

                    // These add new fields to the embed if values are found.
                    // Also checks that there are no empty fields, to protect from errors.
                    if (myData.Field_1_Data != null && myData.Field_1_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_1_Name, myData.Field_1_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_2_Data != null && myData.Field_2_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_2_Name, myData.Field_2_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_3_Data != null && myData.Field_3_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_3_Name, myData.Field_3_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_4_Data != null && myData.Field_4_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_4_Name, myData.Field_4_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_5_Data != null && myData.Field_5_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_5_Name, myData.Field_5_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_6_Data != null && myData.Field_6_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_6_Name, myData.Field_6_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_7_Data != null && myData.Field_7_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_7_Name, myData.Field_7_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_8_Data != null && myData.Field_8_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_8_Name, myData.Field_8_Data, false);
                    }

                    // If a footer is found, create the discord embed footer.
                    if (myData.Footer != null)
                    {
                        FeedbackEmbed.WithFooter(myData.Footer);
                    }

                    // If a number is found in the Counter column, incriment it.
                    /*if (myData.Counter != null)
                    {
                        int newCounter = myData.Counter++;

                        // Save the changed figure to the database.
                        // TO DO
                    }*/

                    // Respond with the embed.
                    await Say.EmbedForPreview(Message, FeedbackEmbed);

                    return;
                }
            }
        }









        // Actually show the command module info as a discord embed.
        private async Task PreviewModuleCommand(SocketMessage Message, string moduleName)
        {
            Console.WriteLine("Will attempt to preview the " + moduleName + " module.");

            // Create a list called data, containing everything from the spreadsheet.
            List<ModuleDatabase.ModuleData> data = ModuleDatabase.init();

            foreach (ModuleDatabase.ModuleData myData in data)
            {
                if (myData.Trigger == moduleName)
                {
                    var FeedbackEmbed = new EmbedBuilder();

                    // Set the side colour.
                    FeedbackEmbed.WithColor(Color.Green);

                    // Sets the title for the embed.
                    if (myData.Title != null)
                    {
                        FeedbackEmbed.WithTitle(myData.Title);
                    }
                    // Stop checking this row, as there is no title, suggesting it's incomplete.
                    else 
                    {
                        Console.WriteLine("There was no title");
                        break;
                    }

                    // If a footer is found, create the discord embed footer.
                    if (myData.Footer != null)
                    {
                        FeedbackEmbed.WithFooter(myData.Footer);
                    }

                    // These add new fields to the embed if values are found.
                    // Also checks that there are no empty fields, to protect from errors.
                    if (myData.Field_1_Data != null && myData.Field_1_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_1_Name, myData.Field_1_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_2_Data != null && myData.Field_2_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_2_Name, myData.Field_2_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_3_Data != null && myData.Field_3_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_3_Name, myData.Field_3_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_4_Data != null && myData.Field_4_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_4_Name, myData.Field_4_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_5_Data != null && myData.Field_5_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_5_Name, myData.Field_5_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_6_Data != null && myData.Field_6_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_6_Name, myData.Field_6_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_7_Data != null && myData.Field_7_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_7_Name, myData.Field_7_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_8_Data != null && myData.Field_8_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_8_Name, myData.Field_8_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_9_Data != null && myData.Field_9_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_9_Name, myData.Field_9_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_10_Data != null && myData.Field_10_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_10_Name, myData.Field_10_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_11_Data != null && myData.Field_11_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_11_Name, myData.Field_11_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_12_Data != null && myData.Field_12_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_12_Name, myData.Field_12_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_13_Data != null && myData.Field_13_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_13_Name, myData.Field_13_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_14_Data != null && myData.Field_14_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_14_Name, myData.Field_14_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_15_Data != null && myData.Field_15_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_15_Name, myData.Field_15_Data, false);
                    }
                    
                    // Respond with the embed.
                    await Say.EmbedForPreview(Message, FeedbackEmbed);

                    return;
                }
            }
        }

















        private async Task HandleSlashCommand(SocketSlashCommand command)
        {
            string commandName = command.Data.Name.ToString();

            // Set default value. True means only the person who wrote the slash command will see the result.
            bool isEphemeral = true;

            try
            {
                // Check if there is an option added to the slash command.
                string userInput = command.Data.Options.First().Value.ToString();
                //Console.WriteLine("The option is: " + userInput + ".");

                // If the option is "show", make the slash command result visible to everyone.
                if (userInput.ToLower() == "true")
                {
                    isEphemeral = false;
                    //Console.WriteLine("Is not ephemeral.");
                }
            }
            catch
            {
                // If there are no options added to the slash command, do nothing.
                //Console.WriteLine("No arguments");
            }

            Console.WriteLine("\nCOMMAND:" + commandName + ". Invisible to others: " + isEphemeral + ".");

            // Create a list called data, containing everything from the spreadsheet.
            List<Database.CommandData> data = Database.init();

            foreach (Database.CommandData myData in data)
            {
                string[] tempnameArray = myData.Trigger.Split(',');
                string tempname = tempnameArray[0].ToLower();
                tempname = tempname.Replace(' ', '-');

                if (tempname == commandName)
                {
                    var FeedbackEmbed = new EmbedBuilder();

                    // Sets the title for the embed.
                    if (myData.Title != null)
                    {
                        FeedbackEmbed.WithTitle(myData.Title);
                    }
                    // Stop checking this row, as there is no title, suggesting it's incomplete.
                    else break;

                    // Adds an image at the bottom of the embed if one is found.
                    if (myData.Image != "")
                    {
                        FeedbackEmbed.WithImageUrl(myData.Image);
                    }

                    // Adds a description field if appropriate data is found.
                    if (myData.Description != null)
                    {
                        FeedbackEmbed.WithDescription(myData.Description);
                    }

                    // Set the side colour.
                    FeedbackEmbed.WithColor(Color.Green);

                    // These add new fields to the embed if values are found.
                    // Also checks that there are no empty fields, to protect from errors.
                    if (myData.Field_1_Data != null && myData.Field_1_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_1_Name, myData.Field_1_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_2_Data != null && myData.Field_2_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_2_Name, myData.Field_2_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_3_Data != null && myData.Field_3_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_3_Name, myData.Field_3_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_4_Data != null && myData.Field_4_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_4_Name, myData.Field_4_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_5_Data != null && myData.Field_5_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_5_Name, myData.Field_5_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_6_Data != null && myData.Field_6_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_6_Name, myData.Field_6_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_7_Data != null && myData.Field_7_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_7_Name, myData.Field_7_Data, false);
                    }

                    // If a name and data is found, create the discord embed field.
                    if (myData.Field_8_Data != null && myData.Field_8_Name != null)
                    {
                        FeedbackEmbed.AddField(myData.Field_8_Name, myData.Field_8_Data, false);
                    }

                    // If a footer is found, create the discord embed footer.
                    if (myData.Footer != null)
                    {
                        FeedbackEmbed.WithFooter(myData.Footer);
                    }

                    // Respond with the embed.
                    await Say.EmbedForSlash(command, FeedbackEmbed, isEphemeral);

                    return;
                }
            }
        }
    }
}