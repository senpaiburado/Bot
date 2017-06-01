using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class PlayerGameContext
    {
        public Users.User User;
        public IHero hero;
        private Telegram.Bot.TelegramBotClient bot;

        public PlayerGameContext(Users.User user, Telegram.Bot.TelegramBotClient bot)
        {
            this.User = user;
            this.bot = bot;
        }

        public async Task SendAsync(Func<Users.User.Text, string> getText, Telegram.Bot.Types.ReplyMarkups.IReplyMarkup replyMarkup=null)
        {
            await bot.SendTextMessageAsync(User.ID, getText(User.lang), replyMarkup: replyMarkup);
        }
    }

    class PlayerController
    {
        public PlayerGameContext player;
        private PlayerGameContext enemyPlayer;
        private Game game;

        public PlayerController(PlayerGameContext player, PlayerGameContext enemyPlayer, Game game)
        {
            this.player = player;
            this.enemyPlayer = enemyPlayer;
            this.game = game;
        }

        public async Task LeaveConfirming()
        {
            await player.SendAsync(lang => lang.SearchingModeStopped);
            await enemyPlayer.SendAsync(lang => lang.PlayerLeftThisLobby);

            game.Reset();
        }

        public async Task ConfirmGame(bool accepted)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            if (accepted)
            {
                await player.SendAsync(lang => lang.GameAccepted);
                if (enemyPlayer.User.status == Users.User.Status.WaitingForRespond)
                {
                    player.User.status = Users.User.Status.Picking;
                    enemyPlayer.User.status = Users.User.Status.Picking;

                    await player.SendAsync(lang => lang.GameStarted, kb);
                    await enemyPlayer.SendAsync(lang => lang.GameStarted, kb);

                    string allHero = string.Join("\n", Game.hero_list.Select(x => x.Name));

                    await player.SendAsync(lang => $"{lang.StringHeroes}:\n{allHero}\n{lang.PickHero}:", GetKeyboardNextPage(player.User));
                    await enemyPlayer.SendAsync(lang => $"{lang.StringHeroes}:\n{allHero}\n{lang.PickHero}:", GetKeyboardNextPage(enemyPlayer.User));
                }
                else
                {
                    player.User.status = Users.User.Status.WaitingForRespond;
                    await player.SendAsync(lang => lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                }
            }
            else
            {
                game.Reset();
                await player.SendAsync(lang => lang.GameCanceled + "\n" + lang.GameNotAccepted, replyMarkup: kb);
                await enemyPlayer.SendAsync(lang => lang.GameCanceled + "\n" + lang.AnotherPlayerDidntAcceptGame, kb);
            }
        }

        public async Task PickHero(IHero hero)
        {
            player.hero = new IHero(hero);


            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();

            await player.SendAsync(lang => $"{lang.PickedHero} {hero.Name} !", kb);
            player.User.HeroName = hero.Name; //??

            if (enemyPlayer.User.status == Users.User.Status.Picked)
            {
                Random random = new Random();
                if (random.Next(0, 2) == 0)
                    SetAttackerAndExcepter(player, enemyPlayer);
                else
                    SetAttackerAndExcepter(enemyPlayer, player);
            }
            else
            {
                player.User.status = Users.User.Status.Picked;
                await player.SendAsync(lang => lang.WaitForPickOfAnotherPlayer);
            }
        }

        private async void SetAttackerAndExcepter(PlayerGameContext attacker, PlayerGameContext excepter)
        {
            attacker.User.status = Users.User.Status.Attacking;
            excepter.User.status = Users.User.Status.Excepting;

            await attacker.SendAsync(lang => lang.YourEnemyMessage + ": " + excepter.User.Name);
            await excepter.SendAsync(lang => lang.YourEnemyMessage + ": " + attacker.User.Name);

            //Временно вызывается из game
            await game.SendHeroesStates();

            await attacker.SendAsync(lang => lang.FirstAttackNotify);
            await excepter.SendAsync(lang => lang.EnemyFirstAttackNotify);

            IHero temp = attacker.hero;

            await attacker.SendAsync(lang => string.Join("\n", attacker.hero.GetMessageAbiliesList(lang)));
            await excepter.SendAsync(lang => lang.WaitingForAnotherPlayerAction);
        }

        public async void LeaveGame()
        {
            player.User.AddLose();
            enemyPlayer.User.AddWin();
            await player.SendAsync(lang => lang.Retreat);
            await enemyPlayer.SendAsync(lang => lang.RetreatEnemy);

            game.Reset();
        }

        public async Task<bool> UseAbility(int number)
        {
            Users.User user_attacker = player.User;
            Users.User user_excepter = enemyPlayer.User;
            IHero attacker = player.hero;
            IHero excepter = enemyPlayer.hero;

            bool finished = false;

            switch (number)
            {
                case 1:
                    if (await attacker.Attack(excepter, user_attacker, user_excepter))
                        finished = true;
                    break;
                case 2:
                    if (await attacker.Heal(user_attacker, user_excepter))
                        finished = true;
                    break;
            }

            if (finished)
            {
                attacker.Update();
                excepter.Update();

                if (Math.Floor(attacker.HP) <= 0.0f || Math.Floor(excepter.HP) <= 0.0f)
                {
                    if (Math.Floor(attacker.HP) <= 0.0f)
                        await game.GameOver(excepter, attacker, user_excepter, user_attacker);
                    else
                        await game.GameOver(attacker, excepter, user_attacker, user_excepter);
                    return true;
                }

                await player.SendAsync(lang => $"{Game.GetMessageForMe(lang, attacker)}\n\n{Game.GetMessageForEnemy(lang, excepter)}");
                await enemyPlayer.SendAsync(lang => $"{Game.GetMessageForMe(lang, excepter)}\n\n{Game.GetMessageForEnemy(lang, attacker)}");

                if (excepter.StunCounter == 0)
                {
                    user_attacker.status = Users.User.Status.Excepting;
                    user_excepter.status = Users.User.Status.Attacking;

                    await enemyPlayer.SendAsync(lang => string.Join("\n", excepter.GetMessageAbiliesList(lang)));
                    await player.SendAsync(lang => lang.WaitingForAnotherPlayerAction);
                }
                else
                    await player.SendAsync(lang => string.Join("\n", attacker.GetMessageAbiliesList(user_attacker.lang)));


                attacker.UpdateStunDuration();
                excepter.UpdateStunDuration();
                return true;
            }
            else
                return false;
        }

        private const short MaxPageValue = 3;

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage()
        {
            return GetKeyboardNextPage(player.User);
        }

        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage(Users.User user)
        { 
            user.HeroListPage++;
            if (user.HeroListPage > MaxPageValue)
                user.HeroListPage = MaxPageValue;
            
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
    }

    class Game
    {
        public static long IGameID = 0L;
        public static List<IHero> hero_list = new List<IHero>();
        public static short MinPageValue = 1;

        public static string smile_hp = "\u2764";
        public static string smile_mp = "🔯";
        public static string smile_dps = "🔥";
        public static string smile_armor = "\u25FB";

        public long GameID;
        private Users.User player_one;
        private Users.User player_two;
        public Telegram.Bot.TelegramBotClient bot;

        private PlayerController player_one_controller;
        private PlayerController player_two_controller;

        public bool isWorking = false;

        public static void Initialize()
        {
            // main += 20
            hero_list.Add(new IHero("Juggernaut", 200, 280, 140, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Faceless Void", 230, 250, 150, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Alchemist", 270, 110, 250, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Abaddon", 250, 170, 210, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Lifestealer", 270, 180, 150, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Silencer", 170, 220, 270, IHero.MainFeature.Intel));
            hero_list.Add(new IHero("Wraith King", 240, 180, 180, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Sniper", 160, 230, 150, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Earthshaker", 240, 120, 160, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Slardar", 230, 170, 150, IHero.MainFeature.Str));
            hero_list.Add(new IHero("Razor", 210, 240, 210, IHero.MainFeature.Agi));
            hero_list.Add(new IHero("Ursa", 230, 200, 160, IHero.MainFeature.Agi));
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

            var player_one_context = new PlayerGameContext(player_one, bot);
            var player_two_context = new PlayerGameContext(player_two, bot);

            player_one_controller = new PlayerController(player_one_context, player_two_context, this);
            player_two_controller = new PlayerController(player_two_context, player_one_context, this);
        }

        public void Reset()
        {
            player_one_controller.player.hero = null;
            player_two_controller.player.hero = null;

            player_one.ActiveGameID = 0L;
            player_two.ActiveGameID = 0L;
            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;
            player_one.HeroName = "";
            player_two.HeroName = "";

            isWorking = false;
        }

        public PlayerController GetController(long PlayerID)
        {
            //сохранить в поля
            if (PlayerID == player_one.ID)
                return player_one_controller;

            if (PlayerID == player_two.ID)
                return player_two_controller;

            Reset();

            //здесь нужна какая-то общая ошибка, т.к. таких вещей по идее никогда не должно происходить
            bot.SendTextMessageAsync(player_one.ID, player_one.lang.PickHeroError);
            bot.SendTextMessageAsync(player_two.ID, player_two.lang.PickHeroError);

            return null;
        }

        public async Task GameOver(IHero winner, IHero loser, Users.User uwinner, Users.User uloser)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            string[] msg1 =
            {
                $"{uwinner.Name} ({winner.Name}) {uwinner.lang.HasWonThisBattle}!",
                $"{uloser.Name} ({loser.Name}) {uwinner.lang.HasLostThisBattle}!",
                $"{uwinner.lang.Result}:",
            };
            string[] msg2 =
            {
                $"{uwinner.Name} ({winner.Name}) {uloser.lang.HasWonThisBattle}!",
                $"{uloser.Name} ({loser.Name}) {uloser.lang.HasLostThisBattle}!",
                $"{uloser.lang.Result}:",
            };
            uwinner.AddWin();
            uloser.AddLose();

            await bot.SendTextMessageAsync(uwinner.ID, uwinner.lang.GameFinished, replyMarkup: kb);
            await bot.SendTextMessageAsync(uloser.ID, uloser.lang.GameFinished, replyMarkup: kb);

            await bot.SendTextMessageAsync(uwinner.ID, string.Join("\n", msg1));
            await bot.SendTextMessageAsync(uloser.ID, string.Join("\n", msg2));

            await bot.SendTextMessageAsync(uwinner.ID, GetMessageForMe(uwinner.lang, winner));
            await bot.SendTextMessageAsync(uloser.ID, GetMessageForMe(uloser.lang, loser));
            await bot.SendTextMessageAsync(uwinner.ID, GetMessageForEnemy(uwinner.lang, loser));
            await bot.SendTextMessageAsync(uloser.ID, GetMessageForEnemy(uloser.lang, winner));

            winner = null;
            loser = null;

            player_one.status = Users.User.Status.Default;
            player_two.status = Users.User.Status.Default;

            isWorking = false;
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

        public static string GetMessageForMe(Users.User.Text playerLang, IHero playerHero)
        {
            string[] lines =
                {
                    playerLang.YouMessage,
                    $"{playerLang.HeroNameMessage}: {playerHero.Name}",
                    $"{playerLang.HpText}: {Convert.ToInt32(playerHero.HP)}/{Convert.ToInt32(playerHero.MaxHP)} {smile_hp}",
                    $"{playerLang.MpText}: {Convert.ToInt32(playerHero.MP)}/{Convert.ToInt32(playerHero.MaxMP)} {smile_mp}",
                    $"{playerLang.DpsText}: {Convert.ToInt32(playerHero.DPS)} {smile_dps}",
                    $"{playerLang.ArmorText}: {Convert.ToInt32(playerHero.Armor)} {smile_armor}",
                };

            return string.Join("\n", lines);
        }

        public static string GetMessageForEnemy(Users.User.Text playerLang, IHero enemyHero)
        {
            string[] lines =
            {
                playerLang.YourEnemyMessage,
                $"{playerLang.HeroNameMessage}: {enemyHero.Name}",
                $"{playerLang.HpText}: {Convert.ToInt32(enemyHero.HP)}/{Convert.ToInt32(enemyHero.MaxHP)} {smile_hp}",
                $"{playerLang.MpText}: {Convert.ToInt32(enemyHero.MP)}/{Convert.ToInt32(enemyHero.MaxMP)} {smile_mp}",
                $"{playerLang.DpsText}: {Convert.ToInt32(enemyHero.DPS)} {smile_dps}",
                $"{playerLang.ArmorText}: {Convert.ToInt32(enemyHero.Armor)} {smile_armor}",
            };

            return string.Join("\n", lines);
        }

        public async Task SendHeroesStates()
        {
            //временно
            var hero_one = player_one_controller.player.hero;
            var hero_two = player_two_controller.player.hero;


            await bot.SendTextMessageAsync(player_one.ID, GetMessageForMe(player_one.lang, hero_one));
            await bot.SendTextMessageAsync(player_two.ID, GetMessageForMe(player_two.lang, hero_two));


            await bot.SendTextMessageAsync(player_one.ID, GetMessageForEnemy(player_one.lang, hero_two));
            await bot.SendTextMessageAsync(player_two.ID, GetMessageForEnemy(player_two.lang, hero_one));
        }
    }
}
