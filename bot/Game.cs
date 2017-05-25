using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class Game
    {
        public static long IGameID = 0L;
        public static List<IHero> hero_list = new List<IHero>();
        public static short MaxPageValue = 3;
        public static short MinPageValue = 1;

        public static string smile_hp = "\u2764";
        public static string smile_mp = "\u1F537";
        public static string smile_dps = "\u1F52A";
        public static string smile_armor = "\u25FB";

        public long GameID;
        private Users.User player_one;
        private Users.User player_two;
        Telegram.Bot.TelegramBotClient bot;

        private IHero hero_one;
        private IHero hero_two;

        public bool isWorking = false;

        public long GetIDofPlayerOne()
        {
            return player_one.ID;
        }

        public long GetIDofPlayerTwo()
        {
            return player_two.ID;
        }

        public static void Initialize()
        {
            hero_list.Add(new IHero("Juggernaut", 35, 60, 22, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Faceless Void", 23, 71, 27, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Alchemist", 50, 32, 30, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Abaddon", 40, 24, 50, IHero.MainFeature.Intel));
            hero_list.Add(new IHero("Lifestealer", 52, 15, 34, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Silencer", 37, 20, 77, IHero.MainFeature.Intel));
            hero_list.Add(new IHero("Wraith King", 70, 35, 21, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Sniper", 25, 80, 30, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Earthshaker", 70, 25, 25, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Slardar", 91, 15, 30, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Razor", 34, 101, 39, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Ursa", 41, 80, 35, IHero.MainFeature.Agi));
        }

        public Game(Users.User user_one, Users.User user_two, Telegram.Bot.TelegramBotClient _bot)
        {
            player_one = user_one;
            player_two = user_two;
            bot = _bot;
            IGameID++;
            GameID = IGameID;

            player_one.ActiveGameID = GameID;
            player_two.ActiveGameID = GameID;

            player_one.status = Users.User.Status.GameConfirming;
            player_two.status = Users.User.Status.GameConfirming;

            isWorking = true;
        }

        public void PickHeroes()
        {
            bot.SendTextMessageAsync(player_one.ID, "Pick hero!");
            player_one.status = Users.User.Status.Picking;
            bot.SendTextMessageAsync(player_two.ID, "Pick hero!");
            player_two.status = Users.User.Status.Picking;
        }

        public IHero Copy(IHero hero)
        {
            return new IHero(hero);
        }

        private void Reset()
        {
            hero_one = null;
            hero_two = null;

            player_one.ActiveGameID = 0L;
            player_two.ActiveGameID = 0L;
            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;

            isWorking = false;
        }

        public void PickHero(IHero hero, long PlayerID)
        {
            ///// Player one
            if (PlayerID == player_one.ID)
            {
                hero_one = Copy(hero);
                bot.SendTextMessageAsync(player_one.ID, player_one.lang.PickedHero + " " + hero_one.Name + "!");

                if (player_two.status == Users.User.Status.Picked)
                {
                    Random random = new Random();
                    if (random.Next(0, 2) == 0)
                    {
                        player_one.status = Users.User.Status.Attacking;
                        player_two.status = Users.User.Status.Excepting;

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.YourEnemyMessage + ": " + player_two.Name);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.YourEnemyMessage + ": " + player_one.Name);

                        SendHeroesStates();

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.FirstAttackNotify);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.EnemyFirstAttackNotify);
                    }
                    else
                    {
                        player_one.status = Users.User.Status.Excepting;
                        player_two.status = Users.User.Status.Attacking;

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.YourEnemyMessage + ": " + player_two.Name);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.YourEnemyMessage + ": " + player_one.Name);

                        SendHeroesStates();

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.EnemyFirstAttackNotify);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.FirstAttackNotify);
                    }
                }
                else
                {
                    player_one.status = Users.User.Status.Picked;
                    bot.SendTextMessageAsync(player_one.ID, player_one.lang.WaitForPickOfAnotherPlayer);
                }
            }
                ////Player two
            else if (PlayerID == player_two.ID)
            {
                hero_two = Copy(hero);
                bot.SendTextMessageAsync(player_two.ID, player_two.lang.PickedHero + " " + hero_two.Name + "!");

                if (player_one.status == Users.User.Status.Picked)
                {
                    Random random = new Random();
                    if (random.Next(0, 2) == 0)
                    {
                        player_one.status = Users.User.Status.Attacking;
                        player_two.status = Users.User.Status.Excepting;

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.YourEnemyMessage + ": " + player_two.Name);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.YourEnemyMessage + ": " + player_one.Name);

                        SendHeroesStates();

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.FirstAttackNotify);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.EnemyFirstAttackNotify);
                    }
                    else
                    {
                        player_one.status = Users.User.Status.Excepting;
                        player_two.status = Users.User.Status.Attacking;

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.YourEnemyMessage + ": " + player_two.Name);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.YourEnemyMessage + ": " + player_one.Name);

                        SendHeroesStates();

                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.EnemyFirstAttackNotify);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.FirstAttackNotify);
                    }
                }
                else
                {
                    player_two.status = Users.User.Status.Picked;
                    bot.SendTextMessageAsync(player_two.ID, player_two.lang.WaitForPickOfAnotherPlayer);
                }
            }
            else
            {
                Reset();
                bot.SendTextMessageAsync(player_one.ID, player_one.lang.PickHeroError);
                bot.SendTextMessageAsync(player_two.ID, player_two.lang.PickHeroError);
            }
        }

        public void ConfirmGame(bool accepted, long PlayerID)
        {
            lock (this)
            {
                var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
                if (PlayerID == player_one.ID)
                {
                    if (accepted)
                    {
                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.GameAccepted);
                        if (player_two.status != Users.User.Status.WaitingForRespond)
                        {
                            player_one.status = Users.User.Status.WaitingForRespond;
                            bot.SendTextMessageAsync(player_one.ID, player_one.lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                        }
                        else
                        {
                            player_one.status = Users.User.Status.Picking;
                            player_two.status = Users.User.Status.Picking;

                            bot.SendTextMessageAsync(player_one.ID, player_one.lang.GameStarted, replyMarkup: kb);
                            bot.SendTextMessageAsync(player_two.ID, player_two.lang.GameStarted, replyMarkup: kb);

                            string msg = "";
                            msg += player_one.lang.StringHeroes + ":\n";
                            foreach (var hero in hero_list)
                                msg += hero.Name + "\n";
                            msg += player_one.lang.PickHero + ":";

                            string msg1 = "";
                            msg1 += player_two.lang.StringHeroes + ":\n";
                            foreach (var hero in hero_list)
                                msg1 += hero.Name + "\n";
                            msg1 += player_two.lang.PickHero + ":";

                            bot.SendTextMessageAsync(player_one.ID, msg, replyMarkup: GetKeyboardNextPage(player_one.ID));
                            bot.SendTextMessageAsync(player_two.ID, msg1, replyMarkup: GetKeyboardNextPage(player_two.ID));
                        }
                    }
                    else
                    {
                        Reset();
                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.GameCanceled + "\n" + player_one.lang.GameNotAccepted, replyMarkup: kb);
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.GameCanceled + "\n" + player_two.lang.AnotherPlayerDidntAcceptGame, replyMarkup: kb);
                    }
                }
                else if (PlayerID == player_two.ID)
                {
                    if (accepted)
                    {
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.GameAccepted, replyMarkup: kb);
                        if (player_two.status != Users.User.Status.WaitingForRespond)
                        {
                            player_two.status = Users.User.Status.WaitingForRespond;
                            bot.SendTextMessageAsync(player_two.ID, player_two.lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                        }
                        else
                        {
                            player_one.status = Users.User.Status.Picking;
                            player_two.status = Users.User.Status.Picking;

                            bot.SendTextMessageAsync(player_one.ID, player_one.lang.GameStarted, replyMarkup: kb);
                            bot.SendTextMessageAsync(player_two.ID, player_two.lang.GameStarted, replyMarkup: kb);

                            string msg = "";
                            msg += player_one.lang.StringHeroes + ":\n";
                            foreach (var hero in hero_list)
                                msg += hero.Name + "\n";
                            msg += player_one.lang.PickHero + ":";

                            string msg1 = "";
                            msg1 += player_two.lang.StringHeroes + ":\n";
                            foreach (var hero in hero_list)
                                msg1 += hero.Name + "\n";
                            msg1 += player_two.lang.PickHero + ":";

                            bot.SendTextMessageAsync(player_one.ID, msg, replyMarkup: GetKeyboardNextPage(player_one.ID));
                            bot.SendTextMessageAsync(player_two.ID, msg1, replyMarkup: GetKeyboardNextPage(player_two.ID));
                        }
                    }
                    else
                    {
                        Reset();
                        bot.SendTextMessageAsync(player_two.ID, player_two.lang.GameCanceled + "\n" + player_two.lang.GameNotAccepted, replyMarkup: kb);
                        bot.SendTextMessageAsync(player_one.ID, player_one.lang.GameCanceled + "\n" + player_one.lang.AnotherPlayerDidntAcceptGame, replyMarkup: kb);
                    }
                }
            }
        }

        public void UseAbility(int number, long PlayerID)
        {
            if (PlayerID == player_one.ID)
            {

            }
            else
            {

            }
        }

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage(long PlayerID)
        {
            Users.User user = PlayerID == player_one.ID ? player_one : player_two;

            if (user.HeroListPage >= MaxPageValue)
                user.HeroListPage = MaxPageValue;
            else
                user.HeroListPage++;

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.OneTimeKeyboard = true;
            keyboard.ResizeKeyboard = true;

            switch (user.HeroListPage)
            {
                case 1:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Juggernaut"),
                            new Telegram.Bot.Types.KeyboardButton("Faceless Void")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Alchemist"),
                            new Telegram.Bot.Types.KeyboardButton("Abaddon")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 2:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Lifestealer"),
                            new Telegram.Bot.Types.KeyboardButton("Silencer")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Wraith King"),
                            new Telegram.Bot.Types.KeyboardButton("Sniper")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<"),
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 3:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Earthshaker"),
                            new Telegram.Bot.Types.KeyboardButton("Slardar")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Razor"),
                            new Telegram.Bot.Types.KeyboardButton("Ursa")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<")
                        }
                    };
                    break;
            }
            return keyboard;
        }

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardPrevPage(long PlayerID)
        {
            Users.User user = PlayerID == player_one.ID ? player_one : player_two;

            if (user.HeroListPage <= MinPageValue)
                user.HeroListPage = MinPageValue;
            else
                user.HeroListPage--;

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.OneTimeKeyboard = true;
            keyboard.ResizeKeyboard = true;

            switch (user.HeroListPage)
            {
                case 1:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Juggernaut"),
                            new Telegram.Bot.Types.KeyboardButton("Faceless Void")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Alchemist"),
                            new Telegram.Bot.Types.KeyboardButton("Abaddon")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 2:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Lifestealer"),
                            new Telegram.Bot.Types.KeyboardButton("Silencer")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Wraith King"),
                            new Telegram.Bot.Types.KeyboardButton("Sniper")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<"),
                            new Telegram.Bot.Types.KeyboardButton(">")
                        }
                    };
                    break;
                case 3:
                    keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Earthshaker"),
                            new Telegram.Bot.Types.KeyboardButton("Slardar")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Razor"),
                            new Telegram.Bot.Types.KeyboardButton("Ursa")
                        },
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("<")
                        }
                    };
                    break;
            }
            return keyboard;
        }

        private async void SendHeroesStates()
        {
            string msg_you = "";
            msg_you += player_one.lang.YouMessage + ":\n";
            msg_you += player_one.lang.HeroNameMessage + ": " + hero_one.Name + "\n";
            msg_you += player_one.lang.HpText + ": " + Convert.ToInt32(hero_one.HP).ToString() + "/"
                + Convert.ToInt32(hero_one.MaxHP).ToString() + smile_hp + "\n";
            msg_you += player_one.lang.MpText + ": " + Convert.ToInt32(hero_one.MP).ToString() + "/"
                + Convert.ToInt32(hero_one.MaxMP).ToString() + smile_mp + "\n";
            msg_you += player_one.lang.DpsText + ": " + Convert.ToInt32(hero_one.DPS).ToString() + smile_dps + "\n";
            msg_you += player_one.lang.ArmorText + ": " + Convert.ToInt32(hero_one.Armor).ToString() + smile_armor + "\n";

            string msg_enemy = "";
            msg_enemy += player_one.lang.YourEnemyMessage + ":\n";
            msg_enemy += player_one.lang.HeroNameMessage + ": " + hero_two.Name + "\n";
            msg_enemy += player_one.lang.HpText + ": " + Convert.ToInt32(hero_two.HP).ToString() + "/"
                + Convert.ToInt32(hero_two.MaxHP).ToString() + smile_hp + "\n";
            msg_enemy += player_one.lang.MpText + ": " + Convert.ToInt32(hero_two.MP).ToString() + "/"
                + Convert.ToInt32(hero_two.MaxMP).ToString() + smile_mp + "\n";
            msg_enemy += player_one.lang.DpsText + ": " + Convert.ToInt32(hero_two.DPS).ToString() + smile_dps + "\n";
            msg_enemy += player_one.lang.ArmorText + ": " + Convert.ToInt32(hero_two.Armor).ToString() + smile_armor + "\n";

            string msg1_you = "";
            msg1_you += player_two.lang.YouMessage + ":\n";
            msg1_you += player_two.lang.HeroNameMessage + ": " + hero_two.Name + "\n";
            msg1_you += player_two.lang.HpText + ": " + Convert.ToInt32(hero_two.HP).ToString() + "/"
                + Convert.ToInt32(hero_two.MaxHP).ToString() + smile_hp + "\n";
            msg1_you += player_two.lang.MpText + ": " + Convert.ToInt32(hero_two.MP).ToString() + "/"
                + Convert.ToInt32(hero_two.MaxMP).ToString() + smile_mp + "\n";
            msg1_you += player_two.lang.DpsText + ": " + Convert.ToInt32(hero_two.DPS).ToString() + smile_dps + "\n";
            msg1_you += player_two.lang.ArmorText + ": " + Convert.ToInt32(hero_two.Armor).ToString() + smile_armor + "\n";

            string msg1_enemy = "";
            msg1_enemy += player_two.lang.YourEnemyMessage + ":\n";
            msg1_enemy += player_two.lang.HeroNameMessage + ": " + hero_one.Name + "\n";
            msg1_enemy += player_two.lang.HpText + ": " + Convert.ToInt32(hero_one.HP).ToString() + "/"
                + Convert.ToInt32(hero_one.MaxHP).ToString() + smile_hp + "\n";
            msg1_enemy += player_two.lang.MpText + ": " + Convert.ToInt32(hero_one.MP).ToString() + "/"
                + Convert.ToInt32(hero_one.MaxMP).ToString() + smile_mp + "\n";
            msg1_enemy += player_two.lang.DpsText + ": " + Convert.ToInt32(hero_one.DPS).ToString() + smile_dps + "\n";
            msg1_enemy += player_two.lang.ArmorText + ": " + Convert.ToInt32(hero_one.Armor).ToString() + smile_armor + "\n";

            await bot.SendTextMessageAsync(player_one.ID, msg_you);
            await bot.SendTextMessageAsync(player_two.ID, msg1_you);
            await bot.SendTextMessageAsync(player_one.ID, msg_enemy);
            await bot.SendTextMessageAsync(player_two.ID, msg1_enemy);
        }
    }
}
