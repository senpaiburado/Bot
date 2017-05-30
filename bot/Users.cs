using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace revcom_bot
{
    class Users
    {
        private List<long> IDs;
        private List<User> users;

        public static long AdminID = 295568848;

        public void Init()
        {
            System.IO.Directory.CreateDirectory("Users");
            IDs = new List<long>();
            users = new List<User>();
            InitializeFromFiles();
        }

        public bool Contains(long _Id)
        {
            return IDs.Contains(_Id);
        }

        public List<long> GetIDs()
        {
            return IDs;
        }

        public bool Contains(string name)
        {
            name = name.ToLower();
            foreach (var item in users)
            {
                if (item.Name.ToLower() == name)
                    return true;
            }
            return false;
        }

        public List<string> GetNames()
        {
            List<string> strs = new List<string>();
            foreach (var item in users)
            {
                strs.Add(item.Name);
            }
            return strs;
        }

        public User GetUserByName(string name)
        {
            name = name.ToLower();
            foreach (var item in users)
            {
                if (item.Name.ToLower() == name)
                {
                    return item;
                }
            }
            return null;
        }

        public long GetIdByName(string name)
        {
            name = name.ToLower();
            foreach (var item in users)
            {
                if (name == item.Name.ToLower())
                    return item.ID;
            }
            return -1L;
        }

        public bool NicknameExists(string nick)
        {
            foreach (var user in users)
            {
                if (nick == user.Name)
                    return true;
            }
            return false;
        }

        private async void InitializeFromFiles()
        {
            string[] files = Directory.GetFiles("Users", "*.ini");
            foreach (var file in files)
            {
                StreamReader sr = new StreamReader(@file);

                string _Name = "";
                long _Id = 0L;
                User.Text.Language _Lang = 0;
                int _Wines = 0;
                int _Loses = 0;
                int _Rate = 0;

                _Name = await sr.ReadLineAsync();
                _Id = Convert.ToInt64(await sr.ReadLineAsync());
                _Lang = (User.Text.Language)Enum.Parse(typeof(User.Text.Language), await sr.ReadLineAsync());
                _Wines = int.Parse(await sr.ReadLineAsync());
                _Loses = int.Parse(await sr.ReadLineAsync());
                _Rate = int.Parse(await sr.ReadLineAsync());

                User user = new User();
                user.Name = _Name;
                user.ID = _Id;
                user.lang.lang = _Lang;
                user.wins = _Wines;
                user.loses = _Loses;
                user.rate = _Rate;
                user.winrate = (user.wins + user.loses) == 0 ? 0 : (user.wins * 100.0f) / (user.wins + user.loses);
                user.Init();
                AddUser(user);

                sr.Close();
            }
        }

        public bool AddUser(long _Id)
        {
            if (IDs.Contains(_Id))
                return false;
            User user = new User();
            user.Name = "";
            user.ID = _Id;
            user.Init();
            user.CreateFile();
            users.Add(user);
            IDs.Add(_Id);
            IDs.Sort();
            return true;
        }

        public bool AddUser(User user)
        {
            if (IDs.Contains(user.ID))
                return false;
            users.Add(user);
            IDs.Add(user.ID);
            return true;
        }

        public bool DeleteUser(long UserID)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].ID == UserID)
                {
                    File.Delete("Users/ " + users[i].ID.ToString() + "_userdata.ini");
                    IDs.Remove(users[i].ID);
                    users.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public User getUserByID(long _Id)
        {
            foreach (var user in users)
            {
                if (user.ID == _Id)
                    return user;
            }
            return null;
        }


        /// <summary>
        /// ///////////////////////////////////////////////////////////////////
        /// </summary>
        public class User
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public Status status { get; set; }
            public NetworkStatus net_status { get; set; }
            public Text lang;

            public long ActiveGameID = 0L;
            public short HeroListPage = 0;
            public string HeroName = "";

            public int wins = 0;
            public int loses = 0;
            public float winrate = 0;

            public int rate = 1000;

            public void AddWin()
            {
                wins++;
                winrate = loses == 0 ? 0 : (Convert.ToSingle(wins) * 100.0f) / Convert.ToSingle(wins + loses);
                rate += 25;
                CreateFile();
            }

            public void AddLose()
            {
                loses++;
                winrate = loses == 0 ? 0 : (Convert.ToSingle(wins) * 100.0f) / Convert.ToSingle(wins + loses);
                rate -= 25;
                if (rate < 0)
                    rate = 0;
                CreateFile();
            }

            public string GetStatisctisMessage()
            {
                string[] lines =
                {
                    $"{lang.NameMessage}: {Name}",
                    $"{lang.GamesCountString}: {wins+loses}",
                    $"{lang.WinsCountString}: {wins}",
                    $"{lang.LosesCountString}: {loses}",
                    $"{lang.WinrateString}: {winrate}",
                    $"{lang.RatingString}: {rate}",
                };
                return string.Join("\n", lines);
            }

            public enum Status
            {
                Default, LanguageChanging, Attacking, Excepting, Picking, Searching,
                GameConfirming, WaitingForRespond, Picked, DeletingAccount,
                SettingNickname
            }

            public enum NetworkStatus
            {
                Online, Offline
            }

            public void Init()
            {
                status = Status.Default;
                net_status = NetworkStatus.Offline;
            }

            public async void CreateFile()
            {
                File.Create("Users/ " + ID.ToString() + "_userdata.ini").Close();

                string[] text =
                {
                    $"{Name}",
                    $"{ID}",
                    $"{lang.lang}",
                    $"{wins}",
                    $"{loses}",
                    $"{rate}",
                };
                StreamWriter sw = new StreamWriter("Users/ " + ID.ToString() + "_userdata.ini");
                foreach (var str in text)
                    await sw.WriteLineAsync(str);
                sw.Close();
                
            }

            public struct Text
            {
                public enum Language
                {
                    English, Russian
                }

                public Language lang;
                public InstructionText instruction;

                public string @StartMessage
                {
                    get
                    {
                        if (lang == Language.English)
                        {

                        }
                        else if (lang == Language.Russian)
                        {

                        }

                        return "";
                    }
                }
                public string @ChangeLanguage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Available languages: English | Russian. Select one: ";
                        else if (lang == Language.Russian)
                            return "Доступные языки: Английский | Русский. Выберите: ";
                        return "";
                    }
                }
                public string @ChangedLanguage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Language has been changed to English.";
                        else if (lang == Language.Russian)
                            return "Язык был изменён на русский.";
                        return "";
                    }
                }
                public string @StatusUndefinedError
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Undefined user status! Try again.";
                        else if (lang == Language.Russian)
                            return "Статус пользователя не определён! Попробуйте снова.";
                        return "";
                    }
                }
                public string @SetOnlineStatus
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You are online now!";
                        else if (lang == Language.Russian)
                            return "Вы теперь в сети!";
                        return "";
                    }
                }
                public string @SetOfflineStatus
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You are offline now!";
                        else if (lang == Language.Russian)
                            return "Вы больше не в сети!";
                        return "";
                    }
                }
                public string @GetOnlineStatus
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You are online!";
                        else if (lang == Language.Russian)
                            return "Вы в сети!";
                        return "";
                    }
                }
                public string @GetOfflineStatus
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You are offline!";
                        else if (lang == Language.Russian)
                            return "Вы не в сети!";
                        return "";
                    }
                }
                public string @ErrorByStatusOffline
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string ret_val = "";
                            ret_val += "You can't use this function being offline.\n";
                            ret_val += "Write /online to become online.";
                            return ret_val;
                        }
                        else if (lang == Language.Russian)
                        {
                            string ret_val = "";
                            ret_val += "Вы не можете использовать эту функцию в автономном режиме.\n";
                            ret_val += "Напишите /online, чтобы быть в сети.";
                            return ret_val;
                        }
                        return "";
                    }
                }

                public string @HelloMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Hello!";
                        else if (lang == Language.Russian)
                            return "Привет!";
                        return "";
                    }
                }
                public string @YesMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Yes";
                        else if (lang == Language.Russian)
                            return "Да";
                        return "";
                    }
                }
                public string @NoMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "No";
                        else if (lang == Language.Russian)
                            return "Нет";
                        return "";
                    }
                }
                public string @YouMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You";
                        else if (lang == Language.Russian)
                            return "Вы";
                        return "";
                    }
                }
                public string @YourEnemyMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Your enemy";
                        else if (lang == Language.Russian)
                            return "Ваш противник";
                        return "";
                    }
                }
                public string @EnemyMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Enemy";
                        else if (lang == Language.Russian)
                            return "Противник";
                        return "";
                    }
                }
                public string @NameMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Name";
                        else if (lang == Language.Russian)
                            return "Имя";
                        return "";
                    }
                }
                public string @HeroMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Hero";
                        else if (lang == Language.Russian)
                            return "Герой";
                        return "";
                    }
                }
                public string @HeroNameMessage
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Hero name";
                        else if (lang == Language.Russian)
                            return "Имя героя";
                        return "";
                    }
                }

                public string @WinsCountString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Count of wins";
                        else if (lang == Language.Russian)
                            return "Количество побед";
                        return "";
                    }
                }
                public string @LosesCountString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Count of loses";
                        else if (lang == Language.Russian)
                            return "Количество поражений";
                        return "";
                    }
                }
                public string @GamesCountString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Count of games";
                        else if (lang == Language.Russian)
                            return "Количество игр";
                        return "";
                    }
                }
                public string @RatingString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Rating";
                        else if (lang == Language.Russian)
                            return "Рейтинг";
                        return "";
                    }
                }
                public string @WinrateString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Winrate";
                        else if (lang == Language.Russian)
                            return "Процент побед";
                        return "";
                    }
                }

                public string @HpText
                {
                    get
                    {
                        if (lang == Language.English)
                            return "HP";
                        else if (lang == Language.Russian)
                            return "ХП";
                        return "";
                    }
                }
                public string @MpText
                {
                    get
                    {
                        if (lang == Language.English)
                            return "MP";
                        else if (lang == Language.Russian)
                            return "МП";
                        return "";
                    }
                }
                public string @DpsText
                {
                    get
                    {
                        if (lang == Language.English)
                            return "DPS";
                        else if (lang == Language.Russian)
                            return "Урон";
                        return "";
                    }
                }
                public string @ArmorText
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Armor";
                        else if (lang == Language.Russian)
                            return "Защита";
                        return "";
                    }
                }

                public string @SearchingModeNotify
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You are searching a game...";
                        else if (lang == Language.Russian)
                            return "Вы ищете игру...";
                        return "";
                    }
                }
                public string @SearchingModeStopped
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have stopped the searching.";
                        else if (lang == Language.Russian)
                            return "Вы прекратили поиск.";
                        return "";
                    }
                }
                public string @SeachingModeErrorNotStarted
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Error! You haven't started a searching!";
                        else if (lang == Language.Russian)
                            return "Ошибка! Вы не начинали поиск!";
                        return "";
                    }
                }
                public string @GameFounded
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have found the game!";
                        else if (lang == Language.Russian)
                            return "Вы нашли игру!";
                        return "";
                    }
                }
                public string @AcceptGameQuestion
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Do you accept the game?";
                        else if (lang == Language.Russian)
                            return "Принимаете игру?";
                        return "";
                    }
                }
                public string @GameAccepted
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You accepted the game!";
                        else if (lang == Language.Russian)
                            return "Вы приняли игру!";
                        return "";
                    }
                }
                public string @GameNotAccepted
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You didn't accept the game.";
                        else if (lang == Language.Russian)
                            return "Вы не приняли игру.";
                        return "";
                    }
                }
                public string @AnotherPlayerGameAcceptWaiting
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Waiting for another player to respond...";
                        else if (lang == Language.Russian)
                            return "Ожидание ответа другого игрока...";
                        return "";
                    }
                }
                public string @AnotherPlayerDidntAcceptGame
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The other player didn't accept the game.";
                        else if (lang == Language.Russian)
                            return "Другой игрок не принял игру.";
                        return "";
                    }
                }
                public string @GameCanceled
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The game has been canceled.";
                        else if (lang == Language.Russian)
                            return "Игра отменена.";
                        return "";
                    }
                }
                public string @GameStarted
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The game has been started!";
                        else if (lang == Language.Russian)
                            return "Игра началась!";
                        return "";
                    }
                }
                public string @BattleStarted
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The battle started!";
                        else if (lang == Language.Russian)
                            return "Битва началась!";
                        return "";
                    }
                }
                public string @GameFinished
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The game is finished.";
                        else if (lang == Language.Russian)
                            return "Игра закончена.";
                        return "";
                    }
                }

                public string @PickedHero
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You picked a hero";
                        else if (lang == Language.Russian)
                            return "Вы выбрали героя";
                        return "";
                    }
                }
                public string @PickHeroError
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Error! You have been kicked from the lobby! Try to play later...";
                        else if (lang == Language.Russian)
                            return "Ошибка! Вы были исключены из лобби! Попробуйте сыграть позже...";
                        return "";
                    }
                }
                public string @StringHeroes
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Heroes";
                        else if (lang == Language.Russian)
                            return "Герои";
                        return "";
                    }
                }
                public string @PickHero
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Pick hero";
                        else if (lang == Language.Russian)
                            return "Выберите героя";
                        return "";
                    }
                }
                public string @WaitForPickOfAnotherPlayer
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Waiting for another player to pick...";
                        else if (lang == Language.Russian)
                            return "Ожидание выбора другого игрока...";
                        return "";
                    }
                }
                public string @YouDontTakePartInBattle
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You don't take a part in battles.";
                        else if (lang == Language.Russian)
                            return "Вы не участвуете в битвах.";
                        return "";
                    }
                }
                public string @PlayerLeftThisLobby
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy left this lobby.";
                        else if (lang == Language.Russian)
                            return "Противник покинул эту комнату";
                        return "";
                    }
                }

                public string @ConfirmQuestion
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Do you confirm?";
                        else if (lang == Language.Russian)
                            return "Подтверждаете?";
                        return "";
                    }
                }
                public string @IncorrectSelection
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You chose incorrect ability. Try another!";
                        else if (lang == Language.Russian)
                            return "Вы выбрали неверную способность. Попробуйте другую!";
                        return "";
                    }
                }

                public string @AccountWasDeletedString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Account has been removed.";
                        else if (lang == Language.Russian)
                            return "Учётная запись бьла удалена.";
                        return "";
                    }
                }
                public string @YouCanceledTheActionString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You canceled the action.";
                        else if (lang == Language.Russian)
                            return "Вы отменили действие.";
                        return "";
                    }
                }

                public string @NickNameIsAlreadyExists
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Nickname is already exists!";
                        else if (lang == Language.Russian)
                            return "Это имя занято!";
                        return "";
                    }
                }
                public string @NickNameSet
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The nickname is set!";
                        else if (lang == Language.Russian)
                            return "Имя установлено!";
                        return "";
                    }
                }
                public string @NeedToSetNickname
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have to set your nickname";
                        else if (lang == Language.Russian)
                            return "Вам нужно установить ваш никнейм";
                        return "";
                    }
                }
                public string @NeedToHaveNickname
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You need to have a nickname to be online.";
                        else if (lang == Language.Russian)
                            return "У вас должен быть никнейм, чтобы зайти в сеть.";
                        return "";
                    }
                }

                public string @FirstAttackNotify
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You attack first!";
                        else if (lang == Language.Russian)
                            return "Вы атакуете первым!";
                        return "";
                    }
                }
                public string @EnemyFirstAttackNotify
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Enemy attacks first!";
                        else if (lang == Language.Russian)
                            return "Враг атакует первым!";
                        return "";
                    }
                }

                public string GetAttackedMessageForAttacker(int damage)
                {
                    if (lang == Language.English)
                        return $"You dealt {damage} damage to the enemy!";
                    else if (lang == Language.Russian)
                        return $"Вы нанесли {damage} урона противнику!";
                    return "";
                }

                public string GetAttackedMessageForExcepter(int damage)
                {
                    if (lang == Language.English)
                        return $"The enemy inflicted {damage} damage to you!";
                    else if (lang == Language.Russian)
                        return $"Противник нанёс вам {damage} урона!";
                    return "";
                }
                public string @CriticalHit
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Critical hit";
                        else if (lang == Language.Russian)
                            return "Критический удар";
                        return "";
                    }
                }
                public string @StunningHit
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Stunning hit";
                        else if (lang == Language.Russian)
                            return "Оглушающий удар";
                        return "";
                    }
                }
                public string @YouMissedTheEnemy
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You missed the enemy!";
                        else if (lang == Language.Russian)
                            return "Вы промазали по противнику!";
                        return "";
                    }
                }
                public string @TheEnemyMissedYou
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy missed you!";
                        else if (lang == Language.Russian)
                            return "Противник промазал по вам!";
                        return "";
                    }
                }
                public string @TheEnemyDealtCriticalDamageToYou
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy dealt critical damage to you!";
                        else if (lang == Language.Russian)
                            return "Враг нанёс вам критический урон!";
                        return "";
                    }
                }
                public string @TheEnemyStunnedYou
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy stunned you!";
                        else if (lang == Language.Russian)
                            return "Противник оглушил вас!";
                        return "";
                    }
                }
                public string GetMessageNeedMana(int value)
                {
                    if (lang == Language.English)
                        return $"You need another {value} MP to do that.";
                    else if (lang == Language.Russian)
                        return $"Вам нужно ещё {value} маны, чтобы сделать это.";
                    return "";
                }
                public string GetMessageCountdown(int value)
                {
                    if (lang == Language.English)
                        return $"Ability will be available in {value} steps.";
                    else if (lang == Language.Russian)
                        return $"Способность будет доступна через {value} шагов.";
                    return "";
                }
                public string GetMessageHpAndMpRestored(int hp, int mp)
                {
                    if (lang == Language.English)
                        return $"You restored {hp} HP and {mp} MP.";
                    else if (lang == Language.Russian)
                        return $"Вы восстановили {hp} очков здоровья и {mp} очков маны!";
                    return "";
                }
                public string GetMessageEnemyHpAndMpRestored(int hp, int mp)
                {
                    if (lang == Language.English)
                        return $"The enemy restored {hp} HP and {mp} MP";
                    else if (lang == Language.Russian)
                        return $"Противник восстановил {hp} очков здоровья и {mp} очков маны.";
                    return "";
                }
                public string @YouLose
                {
                    get
                    {
                        return "";
                    }
                }

                public string @Winner
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Winner";
                        else if (lang == Language.Russian)
                            return "Победитель";
                        return "";
                    }
                }
                public string @Loser
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Loser";
                        else if (lang == Language.Russian)
                            return "Проигравший";
                        return "";
                    }
                }
                public string @HasWonThisBattle
                {
                    get
                    {
                        if (lang == Language.English)
                            return "has won this battle";
                        else if (lang == Language.Russian)
                            return "выиграл эту битву";
                        return "";
                    }
                }
                public string @HasLostThisBattle
                {
                    get
                    {
                        if (lang == Language.English)
                            return "has lost this battle";
                        else if (lang == Language.Russian)
                            return "проиграл эту битву";
                        return "";
                    }
                }
                public string @Result
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Result";
                        else if (lang == Language.Russian)
                            return "Результат";
                        return "";
                    }
                }
                public string @Retreat
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have retreated.";
                        else if (lang == Language.Russian)
                            return "Вы отступили.";
                        return "";
                    }
                }
                public string @RetreatEnemy
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy has retreated.";
                        else if (lang == Language.Russian)
                            return "Противник отступил.";
                        return "";
                    }
                }

                // Abilities
                public string @AttackString
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Attack";
                        else if (lang == Language.Russian)
                            return "Атака";
                        return "";
                    }
                }
                public string @Heal
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Heal";
                        else if (lang == Language.Russian)
                            return "Лечение";
                        return "";
                    }
                }
                public string @SelectAbility
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Select ability";
                        else if (lang == Language.Russian)
                            return "Выберите способность";
                        return "";
                    }
                }
                public string @WaitingForAnotherPlayerAction
                {
                    get
                    {
                        if (lang == Language.English)
                            return "Waiting for the action of another player...";
                        else if (lang == Language.Russian)
                            return "Ожидание действия другого игрока...";
                        return "";
                    }
                }

                public string GetMessageAdminCommandSuccesful(string command)
                {
                    if (lang == Language.English)
                        return $"The command\"{command}\" has been compited succesful!";
                    else if (lang == Language.Russian)
                        return $"Команда \"{command}\" была успешно выполнена.";
                    return "";
                }

                public struct InstructionText
                {
                    private Language lang;
                    public void SetLanguage(Language language)
                    {
                        lang = language;
                    }
                    public string @step1_Describe
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step2_AboutNetMode
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step3_AboutOnlineMode
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step4_AboutOfflineMode
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step5_AboutLanguage
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step6_AboutGame
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step7_AboutHeroes
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step8_AboutDonate
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step9_AboutDeveloper
                    {
                        get
                        {
                            return "";
                        }
                    }
                    public string @step10_TheEnd
                    {
                        get
                        {
                            return "";
                        }
                    }
                }
            }
        }
    }
}
