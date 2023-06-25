using app;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;


class Program
{
    static ITelegramBotClient bot = new TelegramBotClient(constants.botId);
    static HttpClient httpClient = new HttpClient();
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
        {
            var message = update.Message;
            User user = message.From;
            string user_firstname = user.FirstName;
            long user_id = user.Id;

            var saved_games = new List<string>();

            var document = new BsonDocument
                    {
                        { "user_id", user_id},
                        { "user_firstname", user_firstname },
                {"bot_is_waiting_for_genre", false },
                {"bot_is_waiting_for_name_of_game_to_find_dlc", false },
                {"bot_is_waiting_for_name_of_game_to_add_to_list", false},
                {"bot_is_waiting_for_name_of_game_to_remove_from_list", false},
                {"bot_is_waiting_for_name_of_game_to_get_link", false },
                {"saved_games", new BsonArray(saved_games.Select(t => t.ToBsonDocument())) }
                };

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var exists = constants.collection.Find(filter).Any();

            if (!exists)
            {
                constants.collection.InsertOne(document);
            }

            var resp = await httpClient.GetAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_genre?id={user_id}");
            var res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_genre = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_find_dlc?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_name_of_game_to_find_dlc = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_add_to_list?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_name_of_game_to_add_to_list = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_remove_from_list?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_name_of_game_to_remove_from_list = Convert.ToBoolean(res);

            resp = await httpClient.GetAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_get_link?id={user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_name_of_game_to_get_link = Convert.ToBoolean(res);
            

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(user_id, $"Hello, {user_firstname}! To use my services choose one of the commands");
                return;
            }
            if (message.Text.ToLower() == "/top100earlyaccess")
            {
                await bot.SendTextMessageAsync(user_id, "Okay! Here it is:");
                var response = await httpClient.GetAsync($"https://{constants.host}/Steam_/get_games_by_genre?genre=Early%2BAccess");
                var result = await response.Content.ReadAsStringAsync();
                List<GameInfo> games = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameInfo>>(result);
                string msg = "";
                int i = 1;
                foreach (var game in games)
                {
                    msg += $"{i}. {game.Name}\n";
                    i++;
                }
                await bot.SendTextMessageAsync(user_id, msg);
                return;
            }
            if (message.Text.ToLower() == "/searchgames")
            {
                await bot.SendTextMessageAsync(user_id, "Enter the genre of games you want to watch.");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_genre?id={user_id}&b=true", null);
                return;
            }
            if (message.Text == "/dlcgame")
            {
                await bot.SendTextMessageAsync(user_id, "Enter the name of game to see DLC.");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_find_dlc?id={user_id}&b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/top100gamesforever")
            {
                await bot.SendTextMessageAsync(user_id, "Okay! Here it is:");
                var response = await httpClient.GetAsync($"https://{constants.host}/Steam_/get_top_100_games");
                var result = await response.Content.ReadAsStringAsync();
                List<GameInfo> games = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameInfo>>(result);
                string msg = "";
                int i = 1;
                foreach (var game in games)
                {
                    msg += $"{i}. {game.Name}\n";
                    i++;
                }
                await bot.SendTextMessageAsync(user_id, msg);
                return;
            }
            if (message.Text.ToLower() == "/addfavorites")
            {
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_add_to_list?id={user_id}&b=true", null);
                await bot.SendTextMessageAsync(user_id, "Ok, enter the name of the game you want to add to the list.");
                return;
            }
            if (message.Text.ToLower() == "/deletefavorites")
            {
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_remove_from_list?id={user_id}&b=true", null);
                await bot.SendTextMessageAsync(user_id, "Ok, enter the number of the game you want to remove from the list.");
                return;
            }
            if (message.Text.ToLower() == "/listfavorites")
            {
                await httpClient.PostAsync($"https://{constants.host}/Steam_/post_my_games_list?id={user_id}", null);
                return;
            }
            if (message.Text.ToLower() == "/getlink")
            {
                await bot.SendTextMessageAsync(user_id, "Enter the name of game to get link.");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_get_link?id={user_id}&b=true", null);
                return;
            }
            if (bot_is_waiting_for_genre)
            {
                string genre = message.Text;
                var response = await httpClient.GetAsync($"https://{constants.host}/Steam_/get_games_by_genre?genre={genre}");


                var result = await response.Content.ReadAsStringAsync();
                if (result != "[]")
                {
                    await bot.SendTextMessageAsync(user_id, $"Here it is games by genre \"{genre}\":");
                    List<GameInfo> games = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameInfo>>(result);
                    string msg = "";
                    int i = 1;
                    foreach (var game in games)
                    {
                        msg += $"{i}. {game.Name}\n";
                        i++;
                    }
                    await bot.SendTextMessageAsync(user_id, msg);
                }
                else await bot.SendTextMessageAsync(user_id, "This genre does not exist");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_genre?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_name_of_game_to_find_dlc)
            {
                string name = message.Text;
                var response = await httpClient.GetAsync($"https://{constants.host}/Steam_/get_dlc_for_game?name={name}");
                var result = await response.Content.ReadAsStringAsync();
                DLCList dlcList = JsonConvert.DeserializeObject<DLCList>(result);
                if (result != "{\"total\":0,\"items\":[]}")
                {
                    await bot.SendTextMessageAsync(user_id, $"Here it is DLC for game \"{name}\":");
                    foreach (DLCItem item in dlcList.items)
                    {
                        string itemstring = "";
                        itemstring += $"Name: {item.name}\n";
                        //itemstring += $"{item.tiny_image}";
                        await bot.SendTextMessageAsync(user_id, itemstring);
                        InputFile inputFile = InputFile.FromUri(item.tiny_image);
                        await botClient.SendPhotoAsync(user_id, inputFile
                            );
                    }
                }
                else await bot.SendTextMessageAsync(user_id, "This game does not exist");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_find_dlc?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_name_of_game_to_add_to_list)
            {
                string name = message.Text;
                await httpClient.PutAsync($"https://{constants.host}/Steam_/put_game_to_list?id={user_id}&game_name={name}", null);
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_add_to_list?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_name_of_game_to_remove_from_list)
            {
                string number = message.Text;
                await httpClient.DeleteAsync($"https://{constants.host}/Steam_/delete_game_from_list?id={user_id}&gameIndex={number}");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_remove_from_list?id={user_id}&b=false", null);
                return;
            }
            if (bot_is_waiting_for_name_of_game_to_get_link)
            {
                string name = message.Text;
                //https://localhost:7069/Steam_/get_link_for_game?name=Dota%202
                var response = await httpClient.GetAsync($"https://{constants.host}/Steam_/get_link_for_game?name={name}");
                var result = await response.Content.ReadAsStringAsync();
                if(response.StatusCode == HttpStatusCode.OK)
                {
                    await bot.SendTextMessageAsync(user_id, $"Here is your link:\n{result}");
                }
                else await bot.SendTextMessageAsync(user_id, $"This game doesn`t exist.");
                await httpClient.PutAsync($"https://{constants.host}/Steam_/bot_is_waiting_for_name_of_game_to_get_link?id={user_id}&b=false", null);
                return;

            }
            await botClient.SendTextMessageAsync(user_id, "I do not understand what you want");
            return;
        }
        else
        {
            if (update.Message != null && update.Message.From != null)
            {
                long user_id = update.Message.From.Id;
                await botClient.SendTextMessageAsync(user_id, "I can only receive text messages");
            }
        }
    }
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }




    static void Main(string[] args)
    {

        Console.WriteLine("Запущен бот" + bot.GetMeAsync().Result.FirstName);

        constants.mongoClient = new MongoClient("mongodb+srv://vladkagoodik:polkipolki4@cluster0.iavetvm.mongodb.net/");
        constants.database = constants.mongoClient.GetDatabase("SteamBase");
        constants.collection = constants.database.GetCollection<BsonDocument>("SteamCollection");
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}