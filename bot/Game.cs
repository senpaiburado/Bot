﻿using System;
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
                PickHero(player_one, hero_one.Name, player_two);
            }
            ////Player two
            else if (PlayerID == player_two.ID)
            {
                hero_two = Copy(hero);
                PickHero(player_two, hero_two.Name, player_one);
            }
            else
            {
                Reset();
                bot.SendTextMessageAsync(player_one.ID, player_one.lang.PickHeroError);
                bot.SendTextMessageAsync(player_two.ID, player_two.lang.PickHeroError);
            }
        }

        private void PickHero(Users.User firstPlayer, string heroName, Users.User secondPlayer)
        {
            bot.SendTextMessageAsync(firstPlayer.ID, $"{firstPlayer.lang.PickedHero} {heroName} !");

            if (secondPlayer.status == Users.User.Status.Picked)
            {
                Random random = new Random();
                if (random.Next(0, 2) == 0)
                    SetAttackerAndExcepter(firstPlayer, secondPlayer);
                else
                    SetAttackerAndExcepter(secondPlayer, firstPlayer);
            }
            else
            {
                firstPlayer.status = Users.User.Status.Picked;
                bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.WaitForPickOfAnotherPlayer);
            }
        }

        private void SetAttackerAndExcepter(Users.User attacker, Users.User excepter)
        {
            attacker.status = Users.User.Status.Attacking;
            excepter.status = Users.User.Status.Excepting;

            bot.SendTextMessageAsync(attacker.ID, attacker.lang.YourEnemyMessage + ": " + excepter.Name);
            bot.SendTextMessageAsync(excepter.ID, excepter.lang.YourEnemyMessage + ": " + attacker.Name);

            SendHeroesStates();

            bot.SendTextMessageAsync(attacker.ID, attacker.lang.FirstAttackNotify);
            bot.SendTextMessageAsync(excepter.ID, excepter.lang.EnemyFirstAttackNotify);
        }

        private void confirmGame(Users.User firstPlayer, bool accepted, Users.User secondPlayer)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            if (accepted)
            {
                bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameAccepted);
                if (secondPlayer.status == Users.User.Status.WaitingForRespond)
                {
                    firstPlayer.status = Users.User.Status.Picking;
                    secondPlayer.status = Users.User.Status.Picking;

                    bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameStarted, replyMarkup: kb);
                    bot.SendTextMessageAsync(secondPlayer.ID, secondPlayer.lang.GameStarted, replyMarkup: kb);

                    string allHero = string.Join("\n", hero_list.Select(x => x.Name));
                    string msg = $"{firstPlayer.lang.StringHeroes}:\n{allHero}\n{firstPlayer.lang.PickHero}:";
                    string msg1 = $"{secondPlayer.lang.StringHeroes}:\n{allHero}\n{secondPlayer.lang.PickHero}:";

                    bot.SendTextMessageAsync(firstPlayer.ID, msg, replyMarkup: GetKeyboardNextPage(firstPlayer.ID));
                    bot.SendTextMessageAsync(secondPlayer.ID, msg1, replyMarkup: GetKeyboardNextPage(secondPlayer.ID));
                }
                else
                {
                    firstPlayer.status = Users.User.Status.WaitingForRespond;
                    bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                }
            }
            else
            {
                Reset();
                bot.SendTextMessageAsync(firstPlayer.ID, firstPlayer.lang.GameCanceled + "\n" + firstPlayer.lang.GameNotAccepted, replyMarkup: kb);
                bot.SendTextMessageAsync(secondPlayer.ID, secondPlayer.lang.GameCanceled + "\n" + secondPlayer.lang.AnotherPlayerDidntAcceptGame, replyMarkup: kb);
            }
        }

        public void ConfirmGame(bool accepted, long PlayerID)
        {
            lock (this)
            {
                if (PlayerID == player_one.ID)
                {
                    confirmGame(player_one, accepted, player_two);
                }
                else if (PlayerID == player_two.ID)
                {
                    confirmGame(player_two, accepted, player_one);
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

        private static string GetMessageForMe(Users.User.Text playerLang, IHero playerHero)
        {
            string[] lines =
                {
                    playerLang.YouMessage,
                    $"{playerLang.HeroNameMessage}: {playerHero.Name}",
                    $"{playerLang.HpText}: {Convert.ToInt32(playerHero.HP)}/{Convert.ToInt32(playerHero.MaxHP)}{smile_hp}",
                    $"{playerLang.MpText}: {Convert.ToInt32(playerHero.MP)}/{Convert.ToInt32(playerHero.MaxMP)}{smile_mp}",
                    $"{playerLang.DpsText}: {Convert.ToInt32(playerHero.DPS)}{smile_dps}",
                    $"{playerLang.ArmorText}: {Convert.ToInt32(playerHero.Armor)}{smile_armor}",
                };

            return string.Join("\n", lines);
        }

        private static string GetMessageForEnemy(Users.User.Text playerLang, IHero enemyHero)
        {
            string[] lines =
            {
                playerLang.YourEnemyMessage,
                $"{playerLang.HeroNameMessage}: {enemyHero.Name}",
                $"{playerLang.HpText}: {Convert.ToInt32(enemyHero.HP)}/{Convert.ToInt32(enemyHero.MaxHP)}{smile_hp}",
                $"{playerLang.MpText}: {Convert.ToInt32(enemyHero.MP)}/{Convert.ToInt32(enemyHero.MaxMP)}{smile_mp}",
                $"{playerLang.DpsText}: {Convert.ToInt32(enemyHero.DPS)}{smile_dps}",
                $"{playerLang.ArmorText}: {Convert.ToInt32(enemyHero.Armor)}{smile_armor}",
            };

            return string.Join("\n", lines);
        }

        private async void SendHeroesStates()
        {
            await bot.SendTextMessageAsync(player_one.ID, GetMessageForMe(player_one.lang, hero_one));
            await bot.SendTextMessageAsync(player_one.ID, GetMessageForEnemy(player_one.lang, hero_two));

            await bot.SendTextMessageAsync(player_two.ID, GetMessageForMe(player_two.lang, hero_two));
            await bot.SendTextMessageAsync(player_two.ID, GetMessageForMe(player_two.lang, hero_one));
        }
    }
}
