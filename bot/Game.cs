using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame
{
    class PlayerGameContext
    {
        public User User;
        public IHero hero;
        public Telegram.Bot.TelegramBotClient bot;

        public PlayerGameContext(User user, Telegram.Bot.TelegramBotClient bot)
        {
            this.User = user;
            this.bot = bot;
        }

        public async Task SendAsync(Func<User.Text, string> getText, Telegram.Bot.Types.ReplyMarkups.IReplyMarkup replyMarkup=null)
        {
            await bot.SendTextMessageAsync(User.ID, getText(User.lang), replyMarkup: replyMarkup);
        }

        internal void Reset()
        {
            hero = null;
            User.ActiveGameID = 0L;
            User.status = User.Status.Default;
            User.HeroName = "";
            User.LastMoveTime = 0L;
        }
    }

    class PlayerController
    {
        public PlayerGameContext player;
        public PlayerGameContext enemyPlayer;
        private Game game;
        public long LastMove = 0L;

        public static readonly string smile_hp = "\u2764";
        public static readonly string smile_mp = "🔯";
        public static readonly string smile_dps = "🔥";
        public static readonly string smile_armor = "\u25FB";

        public PlayerController(PlayerGameContext player, PlayerGameContext enemyPlayer, Game game)
        {
            this.player = player;
            this.enemyPlayer = enemyPlayer;
            this.game = game;
        }

        public async Task LeaveConfirming()
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            await player.SendAsync(lang => lang.SearchingModeStopped, kb);
            await enemyPlayer.SendAsync(lang => lang.PlayerLeftThisLobby, kb);

            game.Reset();
        }

        public async Task ConfirmGame(bool accepted)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            if (accepted)
            {
                await player.SendAsync(lang => lang.GameAccepted);
                if (enemyPlayer.User.status == User.Status.WaitingForRespond)
                {
                    player.User.status = User.Status.Picking;
                    enemyPlayer.User.status = User.Status.Picking;

                    await player.SendAsync(lang => lang.GameStarted, kb);
                    await enemyPlayer.SendAsync(lang => lang.GameStarted, kb);

                    string allHero = string.Join("\n", Game.hero_list.Select(x => x.Name));

                    await player.SendAsync(lang => $"{lang.StringHeroes}:\n{allHero}\n{lang.PickHero}:", GetKeyboardNextPage(player.User));
                    await enemyPlayer.SendAsync(lang => $"{lang.StringHeroes}:\n{allHero}\n{lang.PickHero}:", GetKeyboardNextPage(enemyPlayer.User));
                }
                else
                {
                    player.User.status = User.Status.WaitingForRespond;
                    await player.SendAsync(lang => lang.AnotherPlayerGameAcceptWaiting, replyMarkup: kb);
                }
            }
            else
            {
                game.Reset();
                await player.SendAsync(lang => lang.GameCanceled + "\n" + lang.GameNotAccepted, replyMarkup: kb);
                await enemyPlayer.SendAsync(lang => lang.GameCanceled + "\n" + lang.AnotherPlayerDidntAcceptGame, kb);
                //end
            }
        }

        public async Task PickHero(IHero hero)
        {
            player.hero = hero.Copy(new Sender(player.User.ID, player.User.lang, player.bot));

            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();

            await player.SendAsync(lang => $"{lang.PickedHero} {hero.Name} !", kb);
            player.User.HeroName = hero.Name; //??

            if (enemyPlayer.User.status == User.Status.Picked)
            {
                Random random = new Random();
                if (random.Next(0, 2) == 0)
                    SetAttackerAndExcepter(player, enemyPlayer);
                else
                    SetAttackerAndExcepter(enemyPlayer, player);
            }
            else
            {
                player.User.status = User.Status.Picked;
                await player.SendAsync(lang => lang.WaitForPickOfAnotherPlayer);
            }
        }

        private async void SetAttackerAndExcepter(PlayerGameContext attacker, PlayerGameContext excepter)
        {
            attacker.User.LastMoveTime = Main.Time;
            excepter.User.LastMoveTime = Main.Time;
            attacker.User.status = User.Status.Attacking;
            excepter.User.status = User.Status.Excepting;

            await attacker.SendAsync(lang => lang.YourEnemyMessage + ": " + excepter.User.Name);
            await excepter.SendAsync(lang => lang.YourEnemyMessage + ": " + attacker.User.Name);

            //Временно вызывается из game
            await SendHeroesStates();

            await attacker.SendAsync(lang => lang.FirstAttackNotify);
            await excepter.SendAsync(lang => lang.EnemyFirstAttackNotify);

            IHero temp = attacker.hero;

            await attacker.SendAsync(lang => attacker.hero.GetMessageAbilitesList(lang));
            await excepter.SendAsync(lang => lang.WaitingForAnotherPlayerAction);
        }

        public async Task SendHeroesStates()
        {
            //временно
            await player.SendAsync(lang => GetMessageForMe(lang, player.hero));
            await player.SendAsync(lang => GetMessageForEnemy(lang, enemyPlayer.hero));

            await enemyPlayer.SendAsync(lang => GetMessageForMe(lang, enemyPlayer.hero));
            await enemyPlayer.SendAsync(lang => GetMessageForEnemy(lang, player.hero));
        }

        public static string GetMessageForMe(User.Text playerLang, IHero playerHero)
        {
            string[] lines =
                {
                    playerLang.YouMessage,
                    $"{playerLang.HeroNameMessage}: {playerHero.Name}",
                    $"{playerLang.HpText}: {Convert.ToInt32(playerHero.HP)}/{Convert.ToInt32(playerHero.MaxHP)} {smile_hp}",
                    $"{playerLang.MpText}: {Convert.ToInt32(playerHero.MP)}/{Convert.ToInt32(playerHero.MaxMP)} {smile_mp}",
                    $"{playerLang.DpsText}: {Convert.ToInt32(playerHero.DPS)} {smile_dps}",
                    $"{playerLang.ArmorText}: {Convert.ToInt32(playerHero.Armor)} {smile_armor}",
                    $"{playerHero.GetEffects(playerLang)}"
                };

            return string.Join("\n", lines);
        }

        public static string GetMessageForEnemy(User.Text playerLang, IHero enemyHero)
        {
            string[] lines =
            {
                playerLang.YourEnemyMessage,
                $"{playerLang.HeroNameMessage}: {enemyHero.Name}",
                $"{playerLang.HpText}: {Convert.ToInt32(enemyHero.HP)}/{Convert.ToInt32(enemyHero.MaxHP)} {smile_hp}",
                $"{playerLang.MpText}: {Convert.ToInt32(enemyHero.MP)}/{Convert.ToInt32(enemyHero.MaxMP)} {smile_mp}",
                $"{playerLang.DpsText}: {Convert.ToInt32(enemyHero.DPS)} {smile_dps}",
                $"{playerLang.ArmorText}: {Convert.ToInt32(enemyHero.Armor)} {smile_armor}",
                $"{enemyHero.GetEffects(playerLang)}"
            };

            return string.Join("\n", lines);
        }

        public async Task LeaveGame()
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
            player.User.AddLose();
            enemyPlayer.User.AddWin();
            await player.SendAsync(lang => lang.Retreat, kb);
            await enemyPlayer.SendAsync(lang => lang.RetreatEnemy, kb);

            game.Reset();
        }

        public async void CheckInactive()
        {
            if (Main.Time - enemyPlayer.User.LastMoveTime >= 500)
            {
                await player.SendAsync(lang => lang.TimeLeftEnemy);
                game.GetController(enemyPlayer.User.ID)?.LeaveGame();
            }
            else
                await player.SendAsync(lang => lang.GetMessageCantReportNow(Convert.ToInt32(
                    500 - (Main.Time - enemyPlayer.User.LastMoveTime))));
        }

        public async Task<bool> UseAbility(int number)
        {
            User user_attacker = player.User;
            User user_excepter = enemyPlayer.User;
            IHero attacker = player.hero;
            IHero excepter = enemyPlayer.hero;

            bool finished = false;

            excepter.UpdatePerStep();

            switch (number)
            {
                case 1:
                    if (await attacker.Attack(excepter))
                        finished = true;
                    break;
                case 2:
                    if (await attacker.Heal(excepter))
                        finished = true;
                    break;
                case 3:
                    if (await attacker.UseAbilityOne(excepter))
                        finished = true;
                    break;
                case 4:
                    if (await attacker.UseAbilityTwo(excepter))
                        finished = true;
                    break;
                case 5:
                    if (await attacker.UseAbilityThree(excepter))
                        finished = true;
                    break;
                default:
                    Console.WriteLine("Switch bug!");
                    break;
            }

            if (finished)
            {
                //attacker.UpdatePerStep();
                user_attacker.LastMoveTime = Main.Time;
                user_excepter.LastMoveTime = Main.Time;
                attacker.Update();
                excepter.Update();

                if (Math.Floor(attacker.HP) <= 0.0f || Math.Floor(excepter.HP) <= 0.0f)
                {
                    if (Math.Floor(attacker.HP) <= 0.0f)
                        await GameOver(enemyPlayer, player);
                    else
                        await GameOver(player, enemyPlayer);

                    game.isWorking = false;
                    return true;
                }

                await player.SendAsync(lang => $"{GetMessageForMe(lang, attacker)}\n\n{GetMessageForEnemy(lang, excepter)}");
                await enemyPlayer.SendAsync(lang => $"{GetMessageForMe(lang, excepter)}\n\n{GetMessageForEnemy(lang, attacker)}");

                if (excepter.StunCounter == 0 && !excepter.IsFullDisabled)
                {
                    user_attacker.status = User.Status.Excepting;
                    user_excepter.status = User.Status.Attacking;

                    await enemyPlayer.SendAsync(lang => excepter.GetMessageAbilitesList(lang), GetAbilitiesKeyboard());
                    await player.SendAsync(lang => lang.WaitingForAnotherPlayerAction, GetHideKeyboard());
                }
                else
                    await player.SendAsync(lang => attacker.GetMessageAbilitesList(user_attacker.lang), GetAbilitiesKeyboard());

                attacker.UpdateStunAndDisableDuration();
                excepter.UpdateStunAndDisableDuration();
                return true;
            }
            else
                return false;
        }

        private static string GetWinMessage(PlayerGameContext winner, PlayerGameContext loser, User.Text lang)
        {
            string[] msg =
            {
                $"{winner.User.Name} ({winner.hero.Name}) {lang.HasWonThisBattle}!",
                $"{loser.User.Name} ({loser.hero.Name}) {lang.HasLostThisBattle}!",
                $"{winner.User.lang.Result}:",
            };

            return string.Join("\n", msg);
        }

        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetAbilitiesKeyboard()
        {
            Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup keyboard = new
                Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.ResizeKeyboard = true;
            keyboard.Keyboard = new Telegram.Bot.Types.KeyboardButton[][]
            {
                new Telegram.Bot.Types.KeyboardButton[]
                {
                    new Telegram.Bot.Types.KeyboardButton("1"),
                    new Telegram.Bot.Types.KeyboardButton("2"),
                    new Telegram.Bot.Types.KeyboardButton("3")
                },
                new Telegram.Bot.Types.KeyboardButton[]
                {
                    new Telegram.Bot.Types.KeyboardButton("4"),
                    new Telegram.Bot.Types.KeyboardButton("5"),
                }
            };
            return keyboard;
        }

        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide GetHideKeyboard()
        {
            return new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();
        }

        public static async Task GameOver(PlayerGameContext winner, PlayerGameContext loser)
        {
            var kb = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardHide();

            winner.User.AddWin();
            winner.User.status = User.Status.Default;
            await winner.SendAsync(lang => lang.GameFinished, kb);
            await winner.SendAsync(lang => GetWinMessage(winner, loser, lang));
            await winner.SendAsync(lang => GetMessageForMe(lang, winner.hero));
            await winner.SendAsync(lang => GetMessageForEnemy(lang, loser.hero));

            loser.User.AddLose();
            loser.User.status = User.Status.Default;
            await loser.SendAsync(lang => lang.GameFinished, kb);
            await loser.SendAsync(lang => GetWinMessage(winner, loser, lang));
            await loser.SendAsync(lang => GetMessageForMe(lang, loser.hero));
            await loser.SendAsync(lang => GetMessageForEnemy(lang, winner.hero));

            await winner.User.Sender.SendPhotoWithText(lang => lang.GetAds(), "http://cdn1.savepice.ru/uploads/2017/6/18/f3a68821810058281cb2e19aa0dd1bc0-full.png");
            await loser.User.Sender.SendPhotoWithText(lang => lang.GetAds(), "http://cdn1.savepice.ru/uploads/2017/6/18/f3a68821810058281cb2e19aa0dd1bc0-full.png");
        }

        private const short MaxPageValue = 3;

        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage()
        {
            return GetKeyboardNextPage(player.User);
        }

        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardNextPage(User user)
        { 
            user.HeroListPage++;
            if (user.HeroListPage > MaxPageValue)
                user.HeroListPage = MaxPageValue;
            
            return GetKeyboard(user.HeroListPage);
        }

        public const short MinPageValue = 1;
        public Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboardPrevPage()
        {
            User user = player.User;

            user.HeroListPage--;
            if (user.HeroListPage < MinPageValue)
                user.HeroListPage = MinPageValue;

            return GetKeyboard(user.HeroListPage);
        }

        private Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup GetKeyboard(int heroListPage)
        {
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup();
            keyboard.OneTimeKeyboard = true;
            keyboard.ResizeKeyboard = true;

            switch (heroListPage)
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
                            new Telegram.Bot.Types.KeyboardButton("Dragon Knight"),
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

        public long GameID;
        public Telegram.Bot.TelegramBotClient bot;

        private PlayerController player_one_controller;
        private PlayerController player_two_controller;

        public bool isWorking = false;

        public Game(User user_one, User user_two, Telegram.Bot.TelegramBotClient _bot)
        {
            bot = _bot;
            IGameID++;
            GameID = IGameID;

            user_one.ActiveGameID = GameID;
            user_one.status = User.Status.GameConfirming;


            user_two.ActiveGameID = GameID;
            user_two.status = User.Status.GameConfirming;

            isWorking = true;

            var player_one_context = new PlayerGameContext(user_one, bot);
            var player_two_context = new PlayerGameContext(user_two, bot);

            player_one_controller = new PlayerController(player_one_context, player_two_context, this);
            player_two_controller = new PlayerController(player_two_context, player_one_context, this);
        }

        public void Reset()
        {
            player_one_controller.player.Reset();
            player_two_controller.player.Reset();

            isWorking = false;
        }

        public PlayerController GetController(long PlayerID)
        {
            //сохранить в поля
            if (PlayerID == player_one_controller.player.User.ID)
                return player_one_controller;

            if (PlayerID == player_two_controller.player.User.ID)
                return player_two_controller;

            //здесь нужна какая-то общая ошибка, т.к. таких вещей по идее никогда не должно происходить
            //TODO избавиться от Wait
            player_one_controller.player.SendAsync(lang => lang.PickHeroError).Wait();
            player_two_controller.player.SendAsync(lang => lang.PickHeroError).Wait();

            Reset();
            
            return null;
        }

        public static List<IHero> hero_list = new List<IHero>();
        public static void Initialize()
        {
            hero_list.Add(new Heroes.Juggernaut("Juggernaut", 215, 240, 145, IHero.MainFeature.Agi));
            hero_list.Add(new Heroes.FacelessVoid("Faceless Void", 230, 250, 150, IHero.MainFeature.Agi));
            hero_list.Add(new Heroes.Alchemist("Alchemist", 270, 110, 200, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Abaddon("Abaddon", 250, 170, 210, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Lifestealer("Lifestealer", 270, 180, 150, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Silencer("Silencer", 195, 225, 230, IHero.MainFeature.Intel));
            hero_list.Add(new Heroes.WraithKing("Wraith King", 240, 180, 180, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Sniper("Sniper", 170, 235, 155, IHero.MainFeature.Agi));
            hero_list.Add(new Heroes.DragonKnight("Dragon Knight", 210, 190, 150, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Slardar("Slardar", 245, 170, 150, IHero.MainFeature.Str));
            hero_list.Add(new Heroes.Agility.Razor("Razor", 210, 240, 210, IHero.MainFeature.Agi));
            hero_list.Add(new Heroes.Agility.Ursa("Ursa", 230, 200, 160, IHero.MainFeature.Agi));
        }
    }
}
