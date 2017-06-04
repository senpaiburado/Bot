﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot.Types.ReplyMarkups;

namespace revcom_bot
{
    public partial class Form1 : Form
    {
        BackgroundWorker bw;
        Users user;

        List<Game> ActiveGames = new List<Game>();

        public Form1()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            //
            // TODO: Add constructor code after the InitializeComponent() call.
            //

            this.bw = new BackgroundWorker();
            this.bw.DoWork += bw_DoWork;

            this.user = new Users();
            this.user.Init();

            Game.Initialize();
        }

        async void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var key = e.Argument as String;
            List<User> availablePlayers = new List<User>();
            try
            {
                var Bot = new Telegram.Bot.TelegramBotClient(key);
                IHero.bot = Bot;
                await Bot.SetWebhookAsync("");
                //Bot.SetWebhook("");
                int offset = 0;
                while (true)
                {
                    availablePlayers.RemoveAll(x => x.status != User.Status.Searching);
                    ActiveGames.RemoveAll(x => !x.isWorking);

                    while (availablePlayers.Count >= 2)
                    {
                        var firstPlayer = availablePlayers[0];
                        var seciondPlayer = availablePlayers[1];

                        ActiveGames.Add(new Game(firstPlayer, seciondPlayer, Bot));
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
                                    new Telegram.Bot.Types.KeyboardButton(seciondPlayer.lang.YesMessage),
                                    new Telegram.Bot.Types.KeyboardButton(seciondPlayer.lang.NoMessage)
                                }
                            };
                        keyboard2.ResizeKeyboard = true;
                        keyboard2.OneTimeKeyboard = true;

                        string user1_msg = firstPlayer.lang.GameFounded + "\n" + firstPlayer.lang.AcceptGameQuestion;
                        string user2_msg = seciondPlayer.lang.GameFounded + "\n" + seciondPlayer.lang.AcceptGameQuestion;

                        await Bot.SendTextMessageAsync(firstPlayer.ID, user1_msg, replyMarkup: keyboard1);
                        await Bot.SendTextMessageAsync(seciondPlayer.ID, user2_msg, replyMarkup: keyboard2);

                        try
                        {
                            availablePlayers.RemoveRange(0, 2);
                        }
                        catch (System.ArgumentOutOfRangeException ex)
                        {
                            Console.WriteLine(ex);
                            Console.Read();
                        }
                    }

                    var updates = await Bot.GetUpdatesAsync(offset);
                    
                    foreach(var update in updates.Where(x => x.Message != null && x.Message.Type == Telegram.Bot.Types.Enums.MessageType.TextMessage))
                    {
                        var message = update.Message;

                        if (!user.Contains(message.Chat.Id))
                        {
                            user.AddUser(message.Chat.Id);
                            Console.WriteLine("LOL");
                        }

                        User _usr = user.getUserByID(message.Chat.Id);

                        if (_usr.status == User.Status.Default)
                        {
                            if (message.Text == "/start")
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "\u2764");
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

                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ChangeLanguage, replyMarkup: keyboard);
                            }
                            else if (message.Text == "/profile")
                            {
                                if (_usr.net_status == User.NetworkStatus.Online)
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.GetStatisctisMessage());
                                else
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ErrorByStatusOffline);
                            }
                            else if (message.Text == "/online")
                            {
                                if (_usr.Name != "")
                                {
                                    _usr.net_status = User.NetworkStatus.Online;
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SetOnlineStatus);
                                }
                                else
                                {
                                    _usr.status = User.Status.SettingNickname;
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NeedToHaveNickname);
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NeedToSetNickname + ":");
                                }
                            }
                            else if (message.Text == "/offline")
                            {
                                _usr.net_status = User.NetworkStatus.Offline;
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SetOfflineStatus);
                            }
                            else if (message.Text == "/netstatus")
                            {
                                string msg;
                                if (_usr.net_status == User.NetworkStatus.Online)
                                    msg = _usr.lang.GetOnlineStatus;
                                else
                                    msg = _usr.lang.GetOfflineStatus;
                                await Bot.SendTextMessageAsync(message.Chat.Id, msg);
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
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SearchingModeNotify);
                                }
                                else
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ErrorByStatusOffline);
                            }
                            else if (message.Text == "/stopsearching")
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SeachingModeErrorNotStarted);
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
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ConfirmQuestion, replyMarkup: kb);
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
                                    find_user = user.GetUserByName(name);

                                    if (find_user != null && rating > 0)
                                    {
                                        find_user.rate = rating;
                                        find_user.SaveToFile();
                                        await Bot.SendTextMessageAsync(message.Chat.Id,
                                            _usr.lang.GetMessageAdminCommandSuccesful(message.Text) + $" : {find_user.ID}");
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Send to all:") && message.Chat.Id == Users.AdminID)
                            {
                                string res = System.Text.RegularExpressions.Regex.Match(message.Text, "\"(.*)\"")
                                    .Groups[1].ToString();
                                if (res != "")
                                {
                                    foreach (var item in user.GetIDs())
                                    {
                                        //await Bot.SendTextMessageAsync(item, res);
                                    }
                                    await Bot.SendTextMessageAsync(message.Chat.Id,
                                        _usr.lang.GetMessageAdminCommandSuccesful(message.Text));
                                }
                                    
                            }
                            else if (message.IsCommand("[ADMIN] Get list of names") && message.Chat.Id ==
                                Users.AdminID)
                            {
                                string msg = $"{_usr.lang.List}:\n{string.Join("\n", user.GetNames())}\n";
                                await Bot.SendTextMessageAsync(message.Chat.Id, msg);
                            }
                            else if (message.IsCommand("[ADMIN] Send to one:") && message.Chat.Id == Users.AdminID)
                            {
                                string name = System.Text.RegularExpressions.Regex.Match(message.Text, @"\{(.*)\}")
                                    .Groups[1].ToString();
                                string text = System.Text.RegularExpressions.Regex.Match(message.Text, "\"(.*)\"")
                                    .Groups[1].ToString();

                                long id = 0;
                                id = user.GetIdByName(name);

                                if (id != -1)
                                {
                                    if (text != "")
                                    {
                                        await Bot.SendTextMessageAsync(id, text);
                                        await Bot.SendTextMessageAsync(message.Chat.Id,
                                        _usr.lang.GetMessageAdminCommandSuccesful(message.Text));
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Get profile of user:") && message.Chat.Id ==
                                Users.AdminID)
                            {
                                string name = Regex.Match(message.Text, @"\{(.*)\}").Groups[1].ToString();
                                if (name != "")
                                {
                                    if (user.GetUserByName(name) != null)
                                    {
                                        await Bot.SendTextMessageAsync(message.Chat.Id, user.GetUserByName(name)
                                            .GetStatisctisMessage());
                                        await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang
                                            .GetMessageAdminCommandSuccesful(message.Text));
                                    }
                                }
                            }
                            else if (message.IsCommand("[ADMIN] Delete user:") && message.Chat.Id == Users.AdminID)
                            {
                                string name = Regex.Match(message.Text, @"\{(.*)\}").Groups[1].ToString();
                                if (name != "")
                                {
                                    if (user.GetUserByName(name) != null)
                                    {
                                        user.DeleteUser(user.GetUserByName(name).ID);
                                        await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.GetMessageAdminCommandSuccesful(
                                            message.Text));
                                    }
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.HelloMessage);
                            }
                        }
                        else if (_usr.status == User.Status.LanguageChanging)
                        {
                            if (message.IsCommand("english") || message.IsCommand("русский"))
                            {
                                _usr.status = User.Status.Default;
                                if (message.IsCommand("english"))
                                    _usr.lang.lang = User.Text.Language.English;
                                else if (message.IsCommand("русский"))
                                    _usr.lang.lang = User.Text.Language.Russian;
                                var hide_keyboard = new ReplyKeyboardHide();
                                _usr.SaveToFile();
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ChangedLanguage, replyMarkup: hide_keyboard);
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
                                await Bot.SendTextMessageAsync(message.Chat.Id, ".", replyMarkup: kb);
                                Console.WriteLine(">");
                            }
                            else if (message.IsCommand("<"))
                            {
                                var kb = GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id).GetKeyboardPrevPage();
                                await Bot.SendTextMessageAsync(message.Chat.Id, ".", replyMarkup: kb);
                                Console.WriteLine("<");
                            }
                            else
                            {
                                var hero = Game.hero_list.SingleOrDefault(x => message.IsCommand(x.Name));
                                if (hero != null)
                                    GetActiveGame(_usr.ActiveGameID)?.GetController(message.Chat.Id)?.PickHero(hero);
                            }
                        }
                        else if (_usr.status == User.Status.Attacking)
                        {
                            CheckLeave(_usr, GetActiveGame(_usr.ActiveGameID), message.Text);
                            if (message.IsDigits(message.Text))
                            {
                                foreach (var game in ActiveGames)
                                {
                                    if (game.GameID == _usr.ActiveGameID)
                                    {
                                        await game.GetController(message.Chat.Id).UseAbility(Convert.ToInt32(message.Text));
                                        break;
                                    }
                                }
                            }
                            else
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.IncorrectSelection);
                        }
                        else if (_usr.status == User.Status.Excepting)
                        {
                            CheckLeave(_usr, GetActiveGame(_usr.ActiveGameID), message.Text);
                        }
                        else if (_usr.status == User.Status.Searching)
                        {
                            if (message.Text == "/stopsearching")
                            {
                                foreach (var player in availablePlayers)
                                {
                                    if (player.ID == _usr.ID)
                                    {
                                        availablePlayers.Remove(player);
                                        break;
                                    }
                                }
                                _usr.status = User.Status.Default;
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SearchingModeStopped);
                            }
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
                        }
                        else if (_usr.status == User.Status.DeletingAccount)
                        {
                            if (message.IsCommand(_usr.lang.YesMessage) || message.IsCommand(_usr.lang.NoMessage))
                            {
                                var h_kb = new ReplyKeyboardHide();
                                if (message.IsCommand(_usr.lang.YesMessage))
                                {
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.AccountWasDeletedString, replyMarkup: h_kb);
                                    user.DeleteUser(message.Chat.Id);
                                }
                                else
                                {
                                    _usr.status = User.Status.Default;
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.YouCanceledTheActionString, replyMarkup: h_kb);
                                }
                            }
                        }
                        else if (_usr.status == User.Status.SettingNickname)
                        {
                            if (user.NicknameExists(message.Text))
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NickNameIsAlreadyExists);
                            else
                            {
                                _usr.Name = message.Text;
                                _usr.status = User.Status.Default;
                                _usr.SaveToFile();
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NickNameSet);
                            }
                        }
                        else
                        {
                            var hide_keyboard = new ReplyKeyboardHide();
                            _usr.status = User.Status.Default;
                            await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.StatusUndefinedError, replyMarkup: hide_keyboard);
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

        private void BtnRun_Click(object sender, EventArgs e)
        {
            var text = @txtKey.Text;
            if (text != "" && this.bw.IsBusy != true)
            {
                this.bw.RunWorkerAsync(text);
                BtnRun.Text = "Бот запущен...";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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
