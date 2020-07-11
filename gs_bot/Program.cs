using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace gs_bot
{
    class Program
    {
        private static TelegramBotClient botClient = new TelegramBotClient(Data.BotId);
        private static int seconds = 20;
        private static Timer aTimer;
        private static List<Player> players = new List<Player>();
        private static Message messageWithTime;
        private static string partyToShow;
        private static int partyCanBeChecked;
        private static bool gameIsStarted;

        static void Main(string[] args)
        {
            Console.WriteLine("Bot started");
            botClient.OnMessage += HandleMessage;
            botClient.StartReceiving();
            Console.ReadLine();
        }

        private static void BotOnCallbackQueryRecieved(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                bool shouldAdd = true;

                if (players.Count > 0)
                {
                    shouldAdd = !players.All(p => p.Id == e.CallbackQuery.From.Id);                                      
                }

                if (shouldAdd)
                {
                    players.Add(new Player(e.CallbackQuery.From.FirstName, e.CallbackQuery.From.LastName, e.CallbackQuery.From.Id));
                }
            }
            catch (Exception ex)
            {
                botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in BotOnCallbackQueryRecieved(): " + ex.Message);
            }              
        }

        private static async void HandleMessage(object sender, MessageEventArgs messageEventArgs)
        {         
            try
            {
                var chatId = messageEventArgs.Message.Chat.Id;
                if (messageEventArgs.Message.Text == "/newgame" && gameIsStarted == false)
                {
                    gameIsStarted = true;
                    players.Clear();
                    partyToShow = null;
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Играть")
                    });
                    botClient.OnCallbackQuery += BotOnCallbackQueryRecieved;
                    await botClient.SendTextMessageAsync(chatId, "Для участия в игре нажми кнопку 'Играть'", replyMarkup: inlineKeyboard);
                    SetTimer();
                    messageWithTime = await botClient.SendTextMessageAsync(chatId, "Oсталось времени: 20").ConfigureAwait(false);
                }
                else if (gameIsStarted == true)
                {
                    await botClient.SendTextMessageAsync(chatId, "А ничо шо игра блять уже началась?").ConfigureAwait(false);
                }
                if (messageEventArgs.Message.Text == "/start")
                {
                    await botClient.SendTextMessageAsync(chatId, "Хэллоу");
                }
                if (messageEventArgs.Message.Text == "/show")
                {
                    if (partyCanBeChecked > 0)
                    {                       
                        if (players.Count == 0)
                        {
                            await botClient.SendTextMessageAsync(chatId, "Ну и шо ты собрался показывать, дурачок?");
                        }
                        else
                        {
                            players[1].Id = 1;
                            players[2].Id = 1;
                            players[3].Id = 1;
                            players[4].Id = 1;
                            players[5].Id = 1;

                            var markup = new ReplyKeyboardMarkup();
                            var rows = new List<KeyboardButton[]>();
                            var cols = new List<KeyboardButton>();
                            StringBuilder sb = new StringBuilder();

                            partyToShow = players[players.FindIndex(a => a.Id == messageEventArgs.Message.From.Id)].Party;

                            var playersToShow = from p 
                                                in players 
                                                where p.Id != messageEventArgs.Message.From.Id 
                                                select new { p.FirstName, p.LastName, p.Party };
                                                    
                            int index = 1;
                            foreach (var player in playersToShow)
                            {
                                sb.AppendLine($"{index}. {player.FirstName} {player.LastName}");
                                cols.Add(new KeyboardButton(index.ToString()));
                                rows.Add(cols.ToArray());
                                cols = new List<KeyboardButton>();
                                index++;                                
                            }

                            markup.Keyboard = rows.ToArray();
                            markup.ResizeKeyboard = true;
                            markup.OneTimeKeyboard = true;

                            await botClient.SendTextMessageAsync(chatId, "Кому ты хочешь показать свою партию?\n" + sb, replyMarkup: markup);

                            botClient.OnUpdate += BotClient_OnUpdate;
                            botClient.OnMessage -= HandleMessage;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Партия уже была проверена");
                    }
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in HandleMessage(): " + ex.Message);
            }           
        }

        private static async void BotClient_OnUpdate(object sender, UpdateEventArgs e)
        {  
            try
            {               
                var message = e.Update.Message;
                if (message != null)
                {
                    var text = message.Text;
                    int id = Int32.Parse(text);

                    await botClient.SendTextMessageAsync(players[id - 1].Id, $"Патрия игрока {message.From.FirstName} {message.From.LastName}: {partyToShow}");
                    await botClient.SendTextMessageAsync(message.From.Id, "Сделано");

                    botClient.OnUpdate -= BotClient_OnUpdate;
                    botClient.OnMessage += HandleMessage;

                    partyCanBeChecked--;
                }               
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Требуется номер игрока.");
                Console.WriteLine("Error in BotClient_OnUpdate(): " + ex.Message);
            }          
        }

        private static void SetTimer()
        {
            try
            {
                aTimer = new Timer(1000);
                aTimer.Start();
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in SetTimer(): " + ex.Message);
            }
        }

        private static async void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                var chatId = messageWithTime.Chat.Id;
                if (seconds > 1)
                {
                    seconds--;

                    if (seconds % 5 == 0)
                    {
                        await botClient.EditMessageTextAsync(chatId, messageWithTime.MessageId, "Oсталось времени: " + seconds);
                    }

                    Console.WriteLine($"Осталось {seconds} секунд");
                    return;
                }
                else
                {
                    seconds = 20;

                    SendRolesAndParties();

                    botClient.OnCallbackQuery -= BotOnCallbackQueryRecieved;

                    await botClient.EditMessageTextAsync(chatId, messageWithTime.MessageId, "Роли распределены, приятной игры!");
             
                    aTimer.Stop();
                    aTimer.Dispose();

                    gameIsStarted = false;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in OnTimedEvent: " + ex.Message);
            }
            
        }

        private static void SendRolesAndParties()
        {
            try
            {
                //Добавление данных для тестирования
                players.Add(new Player("Vasya", "Gniloy", players[0].Id));
                players.Add(new Player("Lesha", "Pidor", players[0].Id));
                players.Add(new Player("Petya", "Petya", players[0].Id));
                players.Add(new Player("Sasha", "Sasha", players[0].Id));
                players.Add(new Player("Lil", "Lil", players[0].Id));

                List<string> partyList = new List<string>();

                switch (players.Count)
                {
                    case 5:
                        partyList.AddRange(AddPartiesRange(3, 2));
                        partyCanBeChecked = 1;
                        break;
                    case 6:
                        partyList.AddRange(AddPartiesRange(4, 2));
                        partyCanBeChecked = 1;
                        break;
                    case 7:
                        partyList.AddRange(AddPartiesRange(4, 3));
                        partyCanBeChecked = 1;
                        break;
                    case 8:
                        partyList.AddRange(AddPartiesRange(5, 3));
                        partyCanBeChecked = 1;
                        break;
                    case 9:
                        partyList.AddRange(AddPartiesRange(5, 4));
                        partyCanBeChecked = 2;
                        break;
                    case 10:
                        partyList.AddRange(AddPartiesRange(6, 4));
                        partyCanBeChecked = 2;
                        break;
                    default:
                        botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Слишком мало/много игроков :(");
                        break;
                }

                SetRole(players.Count, partyList);
            }
            catch (Exception ex)
            {
                botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in SendRolesAndParies(): " + ex.Message);
            }
            
        }

        private static async void SetRole(int numberOfPlayers, List<string> partyListIn)
        {
            try
            {
                var rand = new Random();
                bool hAdded = false;
                foreach (var player in players)
                {
                    int rnd = rand.Next(0, numberOfPlayers);
                    player.Party = partyListIn[rnd];
                    player.Role = partyListIn[rnd];
                    await botClient.SendTextMessageAsync(player.Id, "Твоя партия в этой игре: " + player.Party);
                    if (player.Party == "Фашист" && hAdded == false)
                    {
                        player.Role = "Гитлер";
                        await botClient.SendTextMessageAsync(player.Id, "Да ты еще и Гитлер!");
                        hAdded = true;
                    }
                    partyListIn.RemoveAt(rnd);
                    numberOfPlayers--;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(messageWithTime.Chat.Id, "Произошла ошибка (см. лог)");
                Console.WriteLine("Error in SetRole(): " + ex.Message);
            }           
        }

        private static string[] AddPartiesRange(int numberOfLiberals, int numberOfFascists)
        {
            string[] parties = new string[numberOfLiberals + numberOfFascists];

            int counter = 0;

            while (counter < numberOfLiberals)
            {
                parties[counter] = "Либерал";

                counter++;
            }

            while (counter < parties.Length)
            {
                parties[counter] = "Фашист";

                counter++;
            }

            return parties;
        }
    }
}
