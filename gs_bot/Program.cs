using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace gs_bot
{
    class Program
    {
        private static TelegramBotClient botClient = new TelegramBotClient("<bot_token>");
        private static int seconds = 20;
        private static Timer aTimer;
        private static List<Player> players = new List<Player>();
        private static Message messageWithTime;
        private static string globalParty;
        private static bool partyIsChecked = false;

        static void Main(string[] args)
        {
            Console.WriteLine("bot started");
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
                    foreach (var player in players)
                    {
                        if (player.id == e.CallbackQuery.From.Id)
                        {
                            shouldAdd = false;
                        }
                    }
                }
                if (shouldAdd)
                {
                    players.Add(new Player
                    {
                        id = e.CallbackQuery.From.Id,
                        firstName = e.CallbackQuery.From.FirstName,
                        lastName = e.CallbackQuery.From.LastName,
                        party = null,
                        role = null
                    });
                }
            }
            catch
            {
                botClient.SendTextMessageAsync(<id>, "Ошибка в методе BotOnCallbackQueryRecieved");
            }              
        }

        private static async void HandleMessage(object sender, MessageEventArgs messageEventArgs)
        {         
            try
            {
                var chatId = messageEventArgs.Message.Chat.Id;
                if (messageEventArgs.Message.Text == "/newgame")
                {
                    players.Clear();
                    globalParty = "";
                    partyIsChecked = false;
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                    InlineKeyboardButton.WithCallbackData("Играть")
                });
                    botClient.OnCallbackQuery += BotOnCallbackQueryRecieved;
                    await botClient.SendTextMessageAsync(chatId, "Для участия в игре нажми кнопку 'Играть'", replyMarkup: inlineKeyboard);
                    SetTimer();
                    messageWithTime = await botClient.SendTextMessageAsync(chatId, "Oсталось времени: 20").ConfigureAwait(false);
                }
                if (messageEventArgs.Message.Text == "/start")
                {
                    await botClient.SendTextMessageAsync(chatId, "Хэллоу");
                }
                if (messageEventArgs.Message.Text == "/show")
                {
                    if (!partyIsChecked)
                    {
                        if (players.Count == 0)
                        {
                            await botClient.SendTextMessageAsync(chatId, "Ну и шо ты собрался показывать, дурачок?");
                        }
                        else
                        {
                            int id = players.FindIndex(a => a.id == messageEventArgs.Message.From.Id);
                            globalParty = players[id].party;
                            players.RemoveAt(id);
                            var markup = new ReplyKeyboardMarkup();
                            var rows = new List<KeyboardButton[]>();
                            var cols = new List<KeyboardButton>();
                            StringBuilder sb = new StringBuilder();
                            int i = 1;
                            foreach (var player in players)
                            {
                                sb.AppendLine(i + ". " + player.firstName + " " + player.lastName);
                                i++;
                            }
                            for (var index = 1; index < players.Count + 1; index++)
                            {
                                cols.Add(new KeyboardButton(index.ToString()));
                                rows.Add(cols.ToArray());
                                cols = new List<KeyboardButton>();
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
            catch
            {
                await botClient.SendTextMessageAsync(<id>, "Ошибка обработки сообщения");
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
                    await botClient.SendTextMessageAsync(players[id - 1].id, "Патрия игрока " + message.From.FirstName + " " + message.From.LastName + ": " + globalParty);
                    await botClient.SendTextMessageAsync(message.From.Id, "Сделано");
                    botClient.OnUpdate -= BotClient_OnUpdate;
                    botClient.OnMessage += HandleMessage;
                    partyIsChecked = true;
                }               
            }
            catch
            {
                await botClient.SendTextMessageAsync(<id>, "Требуется номер игрока");
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
            catch
            {
                botClient.SendTextMessageAsync(<id>, "Ошибка таймера");
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
                    Console.WriteLine("Осталось " + seconds + " секунд");
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
                }
            }
            catch
            {
                await botClient.SendTextMessageAsync(<id>, "Ошибка метода OnTimedEvent");
            }
            
        }

        private static void SendRolesAndParties()
        {
            try
            {
                //Добавление данных для тестирования
                //players.Add(new Player() { firstName = "Vasya", lastName = "Gniloy", id = players[0].id });
                //players.Add(new Player() { firstName = "Lesha", lastName = "Pidor", id = players[0].id });
                //players.Add(new Player() { firstName = "Petya", lastName = "Petya", id = players[0].id });
                //players.Add(new Player() { firstName = "Sasha", lastName = "Sasha", id = players[0].id });
                //players.Add(new Player() { firstName = "Sasha", lastName = "Sasha", id = players[0].id });
                int playersCount = players.Count;
                switch (playersCount)
                {
                    case 5:
                        List<string> partyList = new List<string> { "Либерал", "Либерал", "Либерал", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList);
                        break;
                    case 6:
                        List<string> partyList1 = new List<string> { "Либерал", "Либерал", "Либерал", "Либерал", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList1);
                        break;
                    case 7:
                        List<string> partyList2 = new List<string> { "Либерал", "Либерал", "Либерал", "Либерал", "Фашист", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList2);
                        break;
                    case 8:
                        List<string> partyList3 = new List<string> { "Либерал", "Либерал", "Либерал", "Либерал", "Либерал", "Фашист", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList3);
                        break;
                    case 9:
                        List<string> partyList4 = new List<string> { "Либерал", "Либерал", "Либерал", "Либерал", "Либерал", "Фашист", "Фашист", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList4);
                        break;
                    case 10:
                        List<string> partyList5 = new List<string> { "Либерал", "Либерал", "Либерал", "Либерал", "Либерал", "Либерал", "Фашист", "Фашист", "Фашист", "Фашист" };
                        SetRole(playersCount, partyList5);
                        break;
                    default:
                        var chatId = messageWithTime.Chat.Id;
                        botClient.SendTextMessageAsync(chatId, "Слишком мало/много игроков :(");
                        break;
                }
            }
            catch
            {
                botClient.SendTextMessageAsync(<id>, "Ошибка метода SendRolesAndParties");
            }
            
        }

        private static async void SetRole(int numberOfPlayers, List<string> partyList)
        {
            try
            {
                var rand = new Random();
                bool hAdded = false;
                foreach (var player in players)
                {
                    int rnd = rand.Next(0, numberOfPlayers);
                    player.party = partyList[rnd];
                    player.role = partyList[rnd];
                    await botClient.SendTextMessageAsync(player.id, "Твоя партия в этой игре: " + player.party);
                    if (player.party == "Фашист" && hAdded == false)
                    {
                        player.role = "Гитлер";
                        await botClient.SendTextMessageAsync(player.id, "Да ты еще и Гитлер!");
                        hAdded = true;
                    }
                    partyList.RemoveAt(rnd);
                    numberOfPlayers--;
                }
            }
            catch
            {
                await botClient.SendTextMessageAsync(<id>, "Ошибка метода SetRole, во время распределения ролей");
            }           
        }
    }
}
