﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            List<Users.User> availablePlayers = new List<Users.User>();
            try
            {
                var Bot = new Telegram.Bot.TelegramBotClient(key); 
                await Bot.SetWebhookAsync("");
                //Bot.SetWebhook("");
                int offset = 0;
                while (true)
                {
                    availablePlayers.RemoveAll(x => x.status != Users.User.Status.Searching);
                    ActiveGames.RemoveAll(x => !x.isWorking);

                    while (availablePlayers.Count >= 2)
                    {
                            ActiveGames.Add(new Game(availablePlayers.ElementAt(0), availablePlayers.ElementAt(1), Bot));
                            var keyboard1 = new ReplyKeyboardMarkup();
                            keyboard1.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                                {
                                    new Telegram.Bot.Types.KeyboardButton[] 
                                    {
                                        new Telegram.Bot.Types.KeyboardButton(availablePlayers.ElementAt(0).lang.YesMessage),
                                        new Telegram.Bot.Types.KeyboardButton(availablePlayers.ElementAt(0).lang.NoMessage)
                                    }
                                };
                            keyboard1.ResizeKeyboard = true;
                            keyboard1.OneTimeKeyboard = true;

                            var keyboard2 = new ReplyKeyboardMarkup();
                            keyboard2.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                                {
                                    new Telegram.Bot.Types.KeyboardButton[] 
                                    {
                                        new Telegram.Bot.Types.KeyboardButton(availablePlayers.ElementAt(1).lang.YesMessage),
                                        new Telegram.Bot.Types.KeyboardButton(availablePlayers.ElementAt(1).lang.NoMessage)
                                    }
                                };
                            keyboard2.ResizeKeyboard = true;
                            keyboard2.OneTimeKeyboard = true;

                            string user1_msg = availablePlayers.ElementAt(0).lang.GameFounded + "\n" + availablePlayers.ElementAt(0).lang.AcceptGameQuestion;
                            string user2_msg = availablePlayers.ElementAt(1).lang.GameFounded + "\n" + availablePlayers.ElementAt(1).lang.AcceptGameQuestion;

                            await Bot.SendTextMessageAsync(availablePlayers.ElementAt(0).ID, user1_msg, replyMarkup: keyboard1);
                            await Bot.SendTextMessageAsync(availablePlayers.ElementAt(1).ID, user2_msg, replyMarkup: keyboard2);

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
                    
                    foreach(var update in updates)
                    {
                        var message = update.Message;

                        if (!user.Contains(message.Chat.Id))
                            user.AddUser(message.Chat.Id);

                        Users.User _usr = user.getUserByID(message.Chat.Id);

                        if (_usr.status == Users.User.Status.Default)
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

                                _usr.status = Users.User.Status.LanguageChanging;

                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ChangeLanguage, replyMarkup: keyboard);
                            }
                            else if (message.Text == "/online")
                            {
                                if (_usr.Name != "")
                                {
                                    _usr.net_status = Users.User.NetworkStatus.Online;
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SetOnlineStatus);
                                }
                                else
                                {
                                    _usr.status = Users.User.Status.SettingNickname;
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NeedToHaveNickname);
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NeedToSetNickname + ":");
                                }
                            }
                            else if (message.Text == "/offline")
                            {
                                _usr.net_status = Users.User.NetworkStatus.Offline;
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SetOfflineStatus);
                            }
                            else if (message.Text == "/netstatus")
                            {
                                string msg;
                                if (_usr.net_status == Users.User.NetworkStatus.Online)
                                    msg = _usr.lang.GetOnlineStatus;
                                else
                                    msg = _usr.lang.GetOfflineStatus;
                                await Bot.SendTextMessageAsync(message.Chat.Id, msg);
                            }
                            else if (message.Text == "/instruction")
                            {
                                _usr.lang.instruction.SetLanguage(_usr.lang.lang);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step1_Describe);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step2_AboutNetMode);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step3_AboutOnlineMode);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step4_AboutOfflineMode);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step5_AboutLanguage);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step6_AboutGame);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step7_AboutHeroes);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step8_AboutDonate);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step9_AboutDeveloper);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.instruction.step10_TheEnd);
                            }
                            else if (message.Text == "/donate")
                            {

                            }
                            else if (message.Text == "/startgame")
                            {
                                if (_usr.net_status == Users.User.NetworkStatus.Online)
                                {
                                    _usr.status = Users.User.Status.Searching;
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
                                _usr.status = Users.User.Status.DeletingAccount;
                                var kb = new ReplyKeyboardMarkup(new[]{
                                    new[]
                                    {
                                        new Telegram.Bot.Types.KeyboardButton(_usr.lang.YesMessage),
                                        new Telegram.Bot.Types.KeyboardButton(_usr.lang.NoMessage)
                                    }
                                }, true, true);
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ConfirmQuestion, replyMarkup: kb);
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.HelloMessage);
                            }
                        }
                        else if (_usr.status == Users.User.Status.LanguageChanging)
                        {
                            if (message.Text.ToLower().Contains("english") || message.Text.ToLower().Contains("русский"))
                            {
                                _usr.status = Users.User.Status.Default;
                                if (message.Text.ToLower().Contains("english"))
                                    _usr.lang.lang = Users.User.Text.Language.English;
                                else if (message.Text.ToLower().Contains("русский"))
                                    _usr.lang.lang = Users.User.Text.Language.Russian;
                                var hide_keyboard = new ReplyKeyboardHide();
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.ChangedLanguage, replyMarkup: hide_keyboard);
                            }
                        }
                        else if (_usr.status == Users.User.Status.Picking)
                        {
                            foreach (var hero in Game.hero_list)
                            {
                                if (message.Text.ToLower().Contains(hero.Name.ToLower()) ||
                                    message.Text.Contains(">") || message.Text.Contains("<"))
                                {
                                    foreach(var game in ActiveGames)
                                    {
                                        if (_usr.ActiveGameID == game.GameID)
                                        {
                                            if (message.Text.ToLower().Contains(hero.Name.ToLower()))
                                                game.PickHero(hero, message.Chat.Id);
                                            else
                                            {
                                                if (message.Text.Contains(">"))
                                                    game.GetKeyboardNextPage(message.Chat.Id);
                                                else if (message.Text.Contains("<"))
                                                    game.GetKeyboardPrevPage(message.Chat.Id);
                                            }
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        else if (_usr.status == Users.User.Status.Attacking)
                        {

                        }
                        else if (_usr.status == Users.User.Status.Excepting)
                        {

                        }
                        else if (_usr.status == Users.User.Status.Searching)
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
                                _usr.status = Users.User.Status.Default;
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.SearchingModeStopped);
                            }
                        }
                        else if (_usr.status == Users.User.Status.GameConfirming)
                        {
                            if (message.Text.ToLower().Contains(_usr.lang.YesMessage.ToLower())
                                || message.Text.ToLower().Contains(_usr.lang.NoMessage.ToLower()))
                            {
                                if (message.Text.ToLower().Contains(_usr.lang.YesMessage.ToLower()))
                                {
                                    foreach (var game in ActiveGames)
                                    {
                                        if (_usr.ActiveGameID == game.GameID)
                                        {
                                            game.ConfirmGame(true, message.Chat.Id);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var game in ActiveGames)
                                    {
                                        if (_usr.ActiveGameID == game.GameID)
                                        {
                                            game.ConfirmGame(false, message.Chat.Id);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (_usr.status == Users.User.Status.DeletingAccount)
                        {
                            if (message.Text.ToLower().Contains(_usr.lang.YesMessage.ToLower()) ||
                                message.Text.ToLower().Contains(_usr.lang.NoMessage.ToLower()))
                            {
                                var h_kb = new ReplyKeyboardHide();
                                if (message.Text.ToLower().Contains(_usr.lang.YesMessage.ToLower()))
                                {
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.AccountWasDeletedString, replyMarkup: h_kb);
                                    user.DeleteUser(message.Chat.Id);
                                }
                                else
                                    await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.YouCanceledTheActionString,
                                        replyMarkup: h_kb);
                            }
                        }
                        else if (_usr.status == Users.User.Status.SettingNickname)
                        {
                            if (user.NicknameExists(message.Text))
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NickNameIsAlreadyExists);
                            else
                            {
                                _usr.Name = message.Text;
                                _usr.status = Users.User.Status.Default;
                                await Bot.SendTextMessageAsync(message.Chat.Id, _usr.lang.NickNameSet);
                            }
                        }
                        else
                        {
                            var hide_keyboard = new ReplyKeyboardHide();
                            _usr.status = Users.User.Status.Default;
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
    }
}
