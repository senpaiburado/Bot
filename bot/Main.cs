using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace DotaTextGame
{
    class Main
    {
        Users users;
        Timer timer;
        public static ulong Time = 0L;

        List<Game> ActiveGames = new List<Game>();

        public Main()
        {
            //this.bw = new BackgroundWorker();

            this.users = new Users();

            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += AddSecondToTime;
            timer.Enabled = true;

            Game.Initialize();
        }

        private void AddSecondToTime(Object source, ElapsedEventArgs e)
        {
            Time += 1L;
        }

        public async void bw_DoWork()
        {
            var key = "347404910:AAHBQ4as_Z05pDctUYD5go0rtbXM78Fk92E";
            List<User> availablePlayers = new List<User>();
            try
            {
                var Bot = new Telegram.Bot.TelegramBotClient(key);
                await users.Init(Bot);
                //IHero.bot = Bot;
                await Bot.SetWebhookAsync("");
                //Bot.SetWebhook("");
                int offset = 0;
                timer.Start();
                while (true)
                {
                    availablePlayers.RemoveAll(x => x.status != User.Status.Searching);
                    ActiveGames.RemoveAll(x => !x.isWorking);

                    while (availablePlayers.Count >= 2)
                    {
                        var firstPlayer = availablePlayers[0];
                        var secondPlayer = availablePlayers[1];

                        ActiveGames.Add(new Game(firstPlayer, secondPlayer, Bot));
                        var keyboard1 = new ReplyKeyboardMarkup();
                        keyboard1.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                            {
                                new Telegram.Bot.Types.KeyboardButton[]
                                {
                                    new Telegram.Bot.Types.KeyboardButton(firstPlayer.lang.YesMessage),
                                    new Telegram.Bot.Types.KeyboardButton(firstPlayer.lang.NoMessage)
                                }
                            };
                        keyboard1.ResizeKeyboard = true;
                        keyboard1.OneTimeKeyboard = true;

                        var keyboard2 = new ReplyKeyboardMarkup();
                        keyboard2.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                            {
                                new Telegram.Bot.Types.KeyboardButton[]
                                {
                                    new Telegram.Bot.Types.KeyboardButton(secondPlayer.lang.YesMessage),
                                    new Telegram.Bot.Types.KeyboardButton(secondPlayer.lang.NoMessage)
                                }
                            };
                        keyboard2.ResizeKeyboard = true;
                        keyboard2.OneTimeKeyboard = true;

                        string user1_msg = firstPlayer.lang.GameFounded + "\n" + firstPlayer.lang.AcceptGameQuestion;
                        string user2_msg = secondPlayer.lang.GameFounded + "\n" + secondPlayer.lang.AcceptGameQuestion;

                        await firstPlayer.Sender.SendAsync(lang => user1_msg, keyboard1);
                        await secondPlayer.Sender.SendAsync(lang => user2_msg, keyboard2);

                        try
                        {
                            availablePlayers.RemoveRange(0, 2);
                        }
                        catch (System.ArgumentOutOfRangeException ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }

                    var updates = await Bot.GetUpdatesAsync(offset);

                    foreach (var update in updates.Where(x => x.Message != null && x.Message.Type == Telegram.Bot.Types.Enums.MessageType.TextMessage))
                    {
                        var message = update.Message;

                        if (!users.Contains(message.Chat.Id))
                        {
                            users.AddUser(message.Chat.Id);
                            Console.WriteLine("LOL");
                        }

                        User _usr = users.getUserByID(message.Chat.Id);

                        if (_usr.Sender == null)
                            _usr.InitSender(Bot);

                        if (_usr.status == User.Status.Default)
                        {
                            if (message.Text == "/start")
                            {
                                await _usr.Sender.SendAsync(lang => "\u2764");
                            }
                            else if (message.Text == "/language")
                            {
                                var keyboard = new ReplyKeyboardMarkup();
                                keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                                {
                                    new Telegram.Bot.Types.KeyboardButton[]
                                    {
                                        new Telegram.Bot.Types.KeyboardButton("English"),
                                        new Telegram.Bot.Types.KeyboardButton("Русский")
                                    }
                                };
                                keyboard.ResizeKeyboard = true;
                                keyboard.OneTimeKeyboard = true;

                                _usr.status = User.Status.LanguageChanging;

                                await _usr.Sender.SendAsync(lang => lang.ChangeLanguage, keyboard);
                            }
                            else if (message.Text == "/profile")
                            {
                                if (_usr.net_status == User.NetworkStatus.Online)
                                    await _usr.Sender.SendAsync(lang => _usr.GetStatisctisMessage());
                                else
                                    await _usr.Sender.SendAsync(lang => lang.ErrorByStatusOffline);
                            }
                            else if (message.Text == "/online")
                            {
                                if (_usr.Name != "")
                                {
                                    _usr.net_status = User.NetworkStatus.Online;
                                    await _usr.Sender.SendAsync(lang => lang.SetOnlineStatus);
                                }
                                else
                                {
                                    _usr.status = User.Status.SettingNickname;
                                    await _usr.Sender.SendAsync(lang => lang.NeedToHaveNickname);
                                    await _usr.Sender.SendAsync(lang => $"{lang.NeedToSetNickname}:");
                                }
                            }
                            else if (message.Text == "/offline")
                            {
                                _usr.net_status = User.NetworkStatus.Offline;
                                await _usr.Sender.SendAsync(lang => lang.SetOfflineStatus);
                            }
                            else if (message.Text == "/netstatus")
                            {
                                string msg;
                                if (_usr.net_status == User.NetworkStatus.Online)
                                    msg = _usr.lang.GetOnlineStatus;
                                else
                                    msg = _usr.lang.GetOfflineStatus;
                                await _usr.Sender.SendAsync(lang => msg);
                            }
                            else if (message.Text == "/instruction")
                            {
                                _usr.lang.instruction.SetLanguage(_usr.lang.lang);
                                //await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step1_Describe);
                                //await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step2_AboutNetMode);
                                //await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step3_AboutOnlineMode);
                                // await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step4_AboutOfflineMode);
                                // await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step5_AboutLanguage);
                                //await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step6_AboutGame);
                                // await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step7_AboutHeroes);
                                //await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step8_AboutDonate);
                                // await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step9_AboutDeveloper);
                                // await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step10_TheEnd);
                            }
                            else if (message.Text == "/donate")
                            {

                            }
                            else if (message.Text == "/startgame")
                            {
                                if (_usr.net_status == User.NetworkStatus.Online)
                                {
                                    _usr.status = User.Status.Searching;
                                    availablePlayers.Add(_usr);
                                    await _usr.Sender.SendAsync(lang => lang.SearchingModeNotify);
                                }
                                else
                                    await _usr.Sender.SendAsync(lang => lang.ErrorByStatusOffline);
                            }
                            else if (message.Text == "/stopsearching")
                            {
                                await _usr.Sender.SendAsync(lang => lang.SeachingModeErrorNotStarted);
                            }
                            else if (message.Text == "/delete")
                            {
                                _usr.status = User.Status.DeletingAccount;
                                var kb = new ReplyKeyboardMarkup(new[]{
                                    new[]
                                    {
                                        new Telegram.Bot.Types.KeyboardButton(_usr.lang.YesMessage),
                                        new Telegram.Bot.Types.KeyboardButton(_usr.lang.NoMessage)
                                    }
                                }, true, true);
                                await _usr.Sender.SendAsync(lang => lang.ConfirmQuestion, kb);
                            }
                            else if (message.IsCommand("[ADMIN] Set rating:") && message.Chat.Id == Users.AdminID)
                            {
                                int rating = 0;
                                string res = System.Text.RegularExpressions.Regex.Match(message.Text, @"\d+").Value;
                                if (int.TryParse(res, out rating))
                                    rating = int.Parse(res);
                                Console.WriteLine($"Parse {rating}");
                                User find_user = null;

                                string name = System.Text.RegularExpressions.Regex.Match(
                                    message.Text, @"\{(.*)\}").Groups[1].ToString();

                                if (name != "")
                                {
                                    find_user = users.GetUserByName(name);

                                    if (find_user != null && rating > 0)
                                    {
                                        find_user.rate = rating;
                                        find_user.SaveToFile();
                                        await _usr.Sender.SendAsync(lang => lang.GetMessageAdminCommandSuccesful(
                                            $"{message.Text} : {find_user.ID}"));
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Send to all:") && message.Chat.Id == Users.AdminID)
                            {
                                string res = System.Text.RegularExpressions.Regex.Match(message.Text, "\"(.*)\"")
                                    .Groups[1].ToString();
                                if (res != "")
                                {
                                    int counter = 0;
                                    var list = users.GetUserList();
                                    for (int i = 0; i < list.Count; i++)
                                    {
                                        User user = list.Values.ElementAt(i);
                                        try
                                        {
                                            await user.Sender.SendAsync(lang => res);
                                        }
                                        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                            list.Remove(user.ID);
                                        }
                                    }
                                    await _usr.Sender.SendAsync(lang => lang.GetMessageAdminCommandSuccesful(message.Text));
                                }

                            }
                            else if (message.IsCommand("[ADMIN] Get list of names") && message.Chat.Id ==
                                Users.AdminID)
                            {
                                string msg = $"{_usr.lang.List}:\n{string.Join("\n", users.GetNames())}\n";
                                await _usr.Sender.SendAsync(lang => msg);
                            }
                            else if (message.IsCommand("[ADMIN] Send to one:") && message.Chat.Id == Users.AdminID)
                            {
                                string name = Regex.Match(message.Text, @"\{(.*)\}").Groups[1].ToString();
                                string text = Regex.Match(message.Text, "\"(.*)\"").Groups[1].ToString();

                                long id = 0;
                                id = users.GetIdByName(name);

                                if (id != -1)
                                {
                                    if (text != "")
                                    {
                                        try
                                        {
                                            await users.getUserByID(id)?.Sender.SendAsync(lang => text);
                                        }
                                        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                            users.GetUserList().Remove(id);
                                        }
                                        await _usr.Sender.SendAsync(lang => lang.GetMessageAdminCommandSuccesful(message.Text));
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Get profile of user:") && message.Chat.Id ==
                                Users.AdminID)
                            {
                                string name = Regex.Match(message.Text, @"\{(.*)\}").Groups[1].ToString();
                                if (name != "")
                                {
                                    if (users.GetUserByName(name) != null)
                                    {
                                        await _usr.Sender.SendAsync(lang => users.GetUserByName(name)
                                            ?.GetStatisctisMessage());
                                        await _usr.Sender.SendAsync(lang => lang.GetMessageAdminCommandSuccesful(message.Text));
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Delete user:") && message.Chat.Id == Users.AdminID)
                            {
                                string name = Regex.Match(message.Text, @"\{(.*)\}").Groups[1].ToString();
                                if (name != "")
                                {
                                    if (users.GetUserByName(name) != null)
                                    {
                                        users.DeleteUser(users.GetUserByName(name).ID);
                                        await _usr.Sender.SendAsync(lang => lang.GetMessageAdminCommandSuccesful(message.Text));
                                    }
                                }
                            }
                            else
                            {
                                await _usr.Sender.SendAsync(lang => lang.HelloMessage);
                            }
                        }
                        else if (_usr.status == User.Status.LanguageChanging)
                        {
                            if (message.IsCommand("english") || message.IsCommand("русский"))
                            {
                                _usr.status = User.Status.Default;
                                if (message.IsCommand("english"))
                                    _usr.LanguageSet(User.Text.Language.English);
                                else if (message.IsCommand("русский"))
                                    _usr.LanguageSet(User.Text.Language.Russian);
                                var hide_keyboard = new ReplyKeyboardHide();
                                _usr.SaveToFile();
                                await _usr.Sender.SendAsync(lang => lang.ChangedLanguage, hide_keyboard);
                            }
                        }
                        else if (_usr.status == User.Status.Picking)
                        {
                            if (message.Text == "/stopsearching")
                            {
                                GetActiveGame(_usr.ActiveGameID).GetController(message.Chat.Id)?.LeaveConfirming();
                            }
                            else if (message.IsCommand(">"))
                            {
                                var kb = GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.GetKeyboardNextPage();
                                await _usr.Sender.SendAsync(lang => ".", kb);
                                Console.WriteLine(">");
                            }
                            else if (message.IsCommand("<"))
                            {
                                var kb = GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id).GetKeyboardPrevPage();
                                await _usr.Sender.SendAsync(lang => ".", kb);
                                Console.WriteLine("<");
                            }
                            else
                            {
                                var hero = Game.hero_list.SingleOrDefault(x => message.IsCommand(x.Name));
                                if (hero != null)
                                    GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.PickHero(hero);
                            }
                        }
                        else if (_usr.status == User.Status.Picked)
                        {
                            if (message.Text == "/stopsearching")
                            {
                                GetActiveGame(_usr.ActiveGameID).GetController(message.Chat.Id)?.LeaveConfirming();
                            }
                            else
                                await _usr.Sender.SendAsync(lang => lang.ErrorPickingIncorrectCommand);
                        }
                        else if (_usr.status == User.Status.Attacking)
                        {
                            CheckLeave(_usr, GetActiveGame(_usr.ActiveGameID), message.Text);
                            if (message.IsDigits(message.Text))
                            {
                                await GetActiveGame(_usr.ActiveGameID)?.GetController(_usr.ID)?.UseAbility(
                                    Convert.ToInt32(message.Text));
                            }
                            else
                                await _usr.Sender.SendAsync(lang => lang.IncorrectSelection);
                        }
                        else if (_usr.status == User.Status.Excepting)
                        {
                            CheckLeave(_usr, GetActiveGame(_usr.ActiveGameID), message.Text);
                            if (message.Text == "/report")
                            {
                                GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.CheckInactive();
                            }
                        }
                        else if (_usr.status == User.Status.Searching)
                        {
                            if (message.Text == "/stopsearching")
                            {
                                availablePlayers.Remove(availablePlayers.Find(x => x.ID == _usr.ID));
                                _usr.status = User.Status.Default;
                                await _usr.Sender.SendAsync(lang => lang.SearchingModeStopped);
                            }
                            else
                                await _usr.Sender.SendAsync(lang => lang.ErrorSearchingIncorrectCommand);
                        }
                        else if (_usr.status == User.Status.GameConfirming)
                        {
                            if (message.IsCommand(_usr.lang.YesMessage) || message.IsCommand(_usr.lang.NoMessage))
                            {
                                bool isConfirm = message.IsCommand(_usr.lang.YesMessage);
                                GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.ConfirmGame(isConfirm);
                            }
                        }
                        else if (_usr.status == User.Status.WaitingForRespond)
                        {
                            if (message.Text == "/stopsearching")
                            {
                                await GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.LeaveConfirming();
                            }
                            else
                                await _usr.Sender.SendAsync(lang => lang.ErrorPickingIncorrectCommand);
                        }
                        else if (_usr.status == User.Status.DeletingAccount)
                        {
                            if (message.IsCommand(_usr.lang.YesMessage) || message.IsCommand(_usr.lang.NoMessage))
                            {
                                var h_kb = new ReplyKeyboardHide();
                                if (message.IsCommand(_usr.lang.YesMessage))
                                {
                                    await _usr.Sender.SendAsync(lang => lang.AccountWasDeletedString, h_kb);
                                    users.DeleteUser(message.Chat.Id);
                                }
                                else
                                {
                                    _usr.status = User.Status.Default;
                                    await _usr.Sender.SendAsync(lang => lang.YouCanceledTheActionString, h_kb);
                                }
                            }
                        }
                        else if (_usr.status == User.Status.SettingNickname)
                        {
                            if (users.NicknameExists(message.Text))
                                await _usr.Sender.SendAsync(lang => lang.NickNameIsAlreadyExists);
                            else
                            {
                                _usr.Name = message.Text;
                                _usr.status = User.Status.Default;
                                _usr.SaveToFile();
                                await _usr.Sender.SendAsync(lang => lang.NickNameSet);
                            }
                        }
                        else
                        {
                            var hide_keyboard = new ReplyKeyboardHide();
                            _usr.status = User.Status.Default;
                            await _usr.Sender.SendAsync(lang => lang.StatusUndefinedError, hide_keyboard);
                        }

                        offset = update.Id + 1;
                    }

                    //return;
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private Game GetActiveGame(long gameID)
        {
            return ActiveGames.SingleOrDefault(x => x.GameID == gameID);
        }
        private void CheckLeave(User user, Game game, string text)
        {
            if (text == "/leavegame")
                game.GetController(user.ID)?.LeaveGame();
        }
    }

    public static class CommandExtension
    {
        public static bool IsCommand(this Telegram.Bot.Types.Message message, string text)
        {
            return message.Text.ToLower().Contains(text.ToLower());
        }
        public static bool IsDigits(this Telegram.Bot.Types.Message message, string text)
        {
            int value = 0;
            if (int.TryParse(text, out value))
                value = int.Parse(text);
            if (value >= 1 && value <= 5)
                return true;
            else
                return false;
        }
    }
}

