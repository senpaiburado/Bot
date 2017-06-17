using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MySql.Data.MySqlClient;

namespace DotaTextGame
{
    //Пока дублирует похожий класс в Game
    class Sender
    {
        
        private Telegram.Bot.TelegramBotClient bot;
        private long userID;
        public User.Text lang;

        public Sender(long userID, User.Text lang, Telegram.Bot.TelegramBotClient bot)
        {
            this.userID = userID;
            this.bot = bot;
            this.lang = lang;
            
        }

        public async Task SendAsync(Func<User.Text, string> getText, Telegram.Bot.Types.ReplyMarkups.IReplyMarkup replyMarkup = null)
        {
            await SendAsync(getText(lang), replyMarkup);
        }

        public async Task SendAsync(string text, Telegram.Bot.Types.ReplyMarkups.IReplyMarkup replyMarkup = null)
        {
            await bot.SendTextMessageAsync(userID, text, replyMarkup: replyMarkup);
        }

        public async Task<Telegram.Bot.Types.Message> SendPhotoWithText(Func<User.Text, string> getText, string photo_path)
        {
            return await bot.SendPhotoAsync(userID, photo_path, getText(lang));
        }

        internal SenderContainer CreateMessageContainer()
        {
            return new SenderContainer(this);
        }
    }

    class SenderContainer
    {
        private List<string> lines = new List<string>();
        private Sender sender;

        public SenderContainer(Sender sender)
        {
            this.sender = sender;
        }

        public void Add(Func<User.Text, string> getText)
        {
            lines.Add(getText(sender.lang));
        }

        public async Task SendAsync(Telegram.Bot.Types.ReplyMarkups.IReplyMarkup replyMarkup = null)
        {
            var message = string.Join("\n", lines);
            lines.Clear();

            await sender.SendAsync(message, replyMarkup);
        }
    }

    class Users
    {
        private Dictionary<long, User> users = new Dictionary<long, User>();

        public static long AdminID = 295568848L;



        public async Task Init(Telegram.Bot.TelegramBotClient sender)
        {
            await InitializeFromFiles();
            foreach (var user in users)
            {
                if (user.Value.Sender == null)
                    user.Value.InitSender(sender);
            }

        }

        public Dictionary<long, User> GetUserList()
        {
            return users;
        }

        public bool Contains(long _Id)
        {
            return users.Keys.Contains(_Id);
        }

        public ICollection<long> GetIDs()
        {
            return users.Keys;
        }

        public bool Contains(string name)
        {
            return GetUserByName(name) != null;
        }

        public string[] GetNames()
        {
            return users.Values.Select(x => x.Name).ToArray();
        }

        public User GetUserByName(string name)
        {
            name = name.ToLower();
            return users.Values.SingleOrDefault(x => x.Name.ToLower() == name);
        }

        public long GetIdByName(string name)
        {
            return GetUserByName(name)?.ID ?? -1L;
        }

        public bool NicknameExists(string nick)
        {
            return users.Values.Any(x => x.Name == nick);
        }

        private async Task InitializeFromFiles()
        {
            List<User> list = new List<User>();
            using (User.con)
            {
                MySql.Data.MySqlClient.MySqlCommand com = new MySql.Data.MySqlClient.MySqlCommand();
                com.CommandText = "SELECT id, name, language, wins, loses, rating from user";
                com.Connection = User.con;
                await User.con.OpenAsync();

                MySqlDataReader reader = com.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        User user = new User
                        {
                            Name = reader.GetString("name"),
                            ID = reader.GetInt64("id"),
                            wins = reader.GetInt32("wins"),
                            loses = reader.GetInt32("loses"),
                            rate = reader.GetInt32("rating")
                        };
                        user.lang.lang = (User.Text.Language)Enum.Parse(typeof(User.Text.Language), reader.GetString("language"));
                        list.Add(user);
                    }
                }
                await User.con.CloseAsync();
                foreach (var x in list)
                {
                    await x.Init();
                    AddUser(x);
                }
            }
        }

        public async Task<bool> AddUser(long _Id)
        {
            if (users.ContainsKey(_Id))
                return false;

            User user = new User
            {
                Name = "",
                ID = _Id
            };

            await user.Init();
            await user.SaveToFile();

            users[_Id] = user;

            //IDs.Sort(); Зачем это?
            return true;
        }

        public bool AddUser(User user)
        {
            if (users.ContainsKey(user.ID))
                return false;

            users[user.ID] = user;
            return true;
        }

        public async Task<bool> DeleteUser(long UserID)
        {
            if (users.ContainsKey(UserID))
            {
                MySqlCommand com = new MySqlCommand($"DELETE FROM user WHERE id = {UserID};", User.con);
                await User.con.OpenAsync();
                await com.ExecuteNonQueryAsync();
                await User.con.CloseAsync();
                users.Remove(UserID);
                return true;
            }

            return false;
        }

        public User getUserByID(long _Id)
        {
            if (users.ContainsKey(_Id))
                return users[_Id];

            return null;
        }
    }

  
    public class User
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public Status status { get; set; }
        public NetworkStatus net_status { get; set; }
        public Text lang = new Text();

        internal Sender Sender;

        public long ActiveGameID = 0L;
        public ulong LastMoveTime = 0L;
        public short HeroListPage = 0;
        public string HeroName = "";

        public int wins = 0;
        public int loses = 0;
        public float winrate => loses + wins == 0 ? 0.0f : (wins * 100.0f) / (wins + loses);

        public int rate = 1000;

        static string connection = "server=127.0.0.1;database=user;uid=root;password=xjkfr2017";
        public static MySql.Data.MySqlClient.MySqlConnection con = new MySql.Data.MySqlClient.MySqlConnection(connection);
        MySql.Data.MySqlClient.MySqlCommand command = new MySql.Data.MySqlClient.MySqlCommand("", con);
        bool Made = false;

        public async void AddWin()
        {
            wins++;
            rate += 25;
            await SaveToFile();
        }

        public async void AddLose()
        {
            loses++;
            rate -= 25;
            if (rate < 0)
                rate = 0;
            await SaveToFile();
        }

        public string GetStatisctisMessage()
        {
            string[] lines =
            {
                    $"{lang.NameMessage}: {Name}",
                    $"{lang.GamesCountString}: {wins+loses}",
                    $"{lang.WinsCountString}: {wins}",
                    $"{lang.LosesCountString}: {loses}",
                    $"{lang.WinrateString}: {winrate.ToString("#.##")}%",
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

        public async Task Init()
        {
            await con.OpenAsync();
            status = Status.Default;
            net_status = NetworkStatus.Offline;

            command.CommandText = $"SELECT 1 FROM user WHERE id = {ID} limit 1;";
            await command.ExecuteNonQueryAsync();
            if (await command.ExecuteScalarAsync() != DBNull.Value && await command.ExecuteScalarAsync() != null)
            {
                Made = true;
            }
            else
                Made = false;
            await con.CloseAsync();
        }

        public void InitSender(Telegram.Bot.TelegramBotClient Bot)
        {
            Sender = new Sender(ID, lang, Bot);
        }

        public void LanguageSet(Text.Language language)
        {
            lang.lang = language;
            Sender.lang.lang = language;
        }

        public async Task SaveToFile()
        {
            await con.OpenAsync();
            if (Made)
            {
                command.CommandText = $"UPDATE user SET name='{Name}', language='{lang.lang.ToString()}', wins={wins}, loses={loses}, rating={rate} WHERE id={ID};";
                await command.ExecuteNonQueryAsync();
            }
            else
            {
                Made = true;
                command.CommandText = $"INSERT INTO user VALUES ({ID}, '{Name}', '{lang.lang.ToString()}', {wins}, {loses}, {rate});";
                await command.ExecuteNonQueryAsync();
            }
            await con.CloseAsync();
        }

        public class Text
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

            public string GetAds()
            {
                if (lang == Language.English)
                {
                    return "Advertisement\n" +
                        "Would you like to make money? BestChange will help you! - http://bit.ly/2sxvDaK";
                }
                else if (lang == Language.Russian)
                {
                    return "Реклама\n" + "Хотите заработать денег? BestChange Вам поможет! - http://bit.ly/2sxvDaK";
                }
                return "";
            }

            public string @ChangeLanguage
            {
                get
                {
                    if (lang == Language.English)
                        return "Available languages: English | Русский. Select one: ";
                    else if (lang == Language.Russian)
                        return "Доступные языки: English | Русский. Выберите: ";
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

            public string @List
            {
                get
                {
                    if (lang == Language.English)
                        return "List";
                    else if (lang == Language.Russian)
                        return "Список";
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

            public string GetMessageYouHaveUsedAbility(string ability_name)
            {
                if (lang == Language.English)
                    return $"You have used ability {ability_name}!";
                else if (lang == Language.Russian)
                    return $"Вы использовали способность {ability_name}!";
                return "";
            }

            public string GetMessageEnemyHasUsedAbility(string ability_name)
            {
                if (lang == Language.English)
                    return $"The enemy has used ability {ability_name}!";
                else if (lang == Language.Russian)
                    return $"Противник использовал способность {ability_name}!";
                return "";
            }

            public string @AbilityIsAlreadyActivated
            {
                get
                {
                    if (lang == Language.English)
                        return "This ability is already activated.";
                    else if (lang == Language.Russian)
                        return "Эта способность уже активирована.";
                    return "";
                }
            }

                public string @YouStunnedYourself
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have stunned yourself.";
                        else if (lang == Language.Russian)
                            return "Вы оглушили себя.";
                        return "";
                    }
                }
                public string @YouStunnedEnemy
                {
                    get
                    {
                        if (lang == Language.English)
                            return "You have stunned the enemy.";
                        else if (lang == Language.Russian)
                            return "Вы оглушили противника.";
                        return "";
                    }
                }
                public string @TheEnemyHasStunnedItself
                {
                    get
                    {
                        if (lang == Language.English)
                            return "The enemy has stunned itself.";
                        else if (lang == Language.Russian)
                            return "Враг оглушил себя.";
                        return "";
                    }
                }

            public string @YouHaveImmuneToMagic
            {
                get
                {
                    if (lang == Language.English)
                        return "You have immune to magic.";
                    else if (lang == Language.Russian)
                        return "Вы неуязвимы к магии.";
                    return "";
                }
            }
            public string @EnemyHasImmuneToMagic
            {
                get
                {
                    if (lang == Language.English)
                        return "The enemy has immune to magic.";
                    else if (lang == Language.Russian)
                        return "Противник неуязвим к магии.";
                    return "";
                }
            }

            public string GetMessageYouActivated(string abi_name)
            {
                if (lang == Language.English)
                    return $"You have actived {abi_name}!";
                else if (lang == Language.Russian)
                    return $"Вы активировали {abi_name}";
                return "";
            }
            public string GetMessageEnemyActivated(string abi_name)
            {
                if (lang == Language.English)
                    return $"The enemy has activated {abi_name}!";
                else if (lang == Language.Russian)
                    return $"Противник активировал {abi_name}!";
                return "";
            }

            public string @YouCantPronounceAbilities
            {
                get
                {
                    if (lang == Language.English)
                        return "You can't pronounce abilities.";
                    else if (lang == Language.Russian)
                        return "Вы не можете произносить заклинания.";
                    return "";
                }
            }

            public string @YouAreSilenced
            {
                get
                {
                    if (lang == Language.English)
                        return "You are silenced!";
                    else if (lang == Language.Russian)
                        return "Вы замолчали!";
                    return "";
                }
            }

            public string @EnemyIsSilenced
            {
                get
                {
                    if (lang == Language.English)
                        return "The enemy is silenced!";
                    else if (lang == Language.Russian)
                        return "Враг замолчал!";
                    return "";
                }
            }

            public string @DamageString
            {
                get
                {
                    if (lang == Language.English)
                        return "Damage";
                    else if (lang == Language.Russian)
                        return "Урон";
                    return "";
                }
            }

            public string @LengthOfNicknameError
            {
                get
                {
                    if (lang == Language.English)
                        return "Nickname should not contain more than 10 characters!";
                    else if (lang == Language.Russian)
                        return "Ник не должен содержать более 10 символов!";
                    return "";
                }
            }

            public string GetMessageYouCantUseAbilityWhileAnotherWorks(string abi_name)
                {
                    if (lang == Language.English)
                        return $"You can use \"Attack\" only while {abi_name} works!";
                    else if (lang == Language.Russian)
                        return $"Вы можете использовать только \"Атака\", пока работает способоность {abi_name}!";
                    return "";
                }

            public string @ALCHEMIST_YouHaveThrownUC
            {
                get
                {
                    if (lang == Language.English)
                        return "You have thrown Unstable Concoction at the enemy!";
                    else if (lang == Language.Russian)
                        return "Вы кинули Unstable Concoction у врага!";
                    return "";
                }
            }
            public string @ALCHEMIST_TheEnemyHasThrownUC
            {
                get
                {
                    if (lang == Language.English)
                        return "The enemy has thrown Unstable Concoction at you!\nYou are stunned.";
                    else if (lang == Language.Russian)
                        return "Противник бросил Unstable Concoction на вас!\nВы оглушены.";
                    return "";
                }
            }
            public string @ALCHEMIST_UC_HasExploded
            {
                get
                {
                    if (lang == Language.English)
                        return "Unstable Concocton has exploded!";
                    else if (lang == Language.Russian)
                        return "Unstable Concoction взорвался!";
                    return "";
                }
            }

            public string @ABADDON_AS_HasExploded
            {
                get
                {
                    if (lang == Language.English)
                        return "Aphotic Shield has exploded!";
                    else if (lang == Language.Russian)
                        return "Aphotic Shield взорвался!";
                    return "";
                }
            }

            public string @CountOfHits
            {
                get
                {
                    if (lang == Language.English)
                        return "Count of hits";
                    else if (lang == Language.Russian)
                        return "Количество попаданий";
                    return "";
                }
            }

            public string @YouHaveWeakenedTheEnemy
            {
                get
                {
                    if (lang == Language.English)
                        return "You have weakened the enemy.";
                    else if (lang == Language.Russian)
                        return "Вы обессилели врага.";
                    return "";
                }
            }
            public string @TheEnemyHasWeakenedYou
            {
                get
                {
                    if (lang == Language.English)
                        return "The enemy has weakened you.";
                    else if (lang == Language.Russian)
                        return "Враг обессилел вас.";
                    return "";
                }
            }

            public string @Stun
            {
                get
                {
                    if (lang == Language.English)
                        return "Stun";
                    else if (lang == Language.Russian)
                        return "Оглушение";
                    return "";
                }
            }
            public string @Silence
            {
                get
                {
                    if (lang == Language.English)
                        return "Silence";
                    else if (lang == Language.Russian)
                        return "Молчание";
                    return "";
                }
            }
            public string @ArmorDecreasing
            {
                get
                {
                    if (lang == Language.English)
                        return "Armor Decreasing";
                    else if (lang == Language.Russian)
                        return "Ослабление брони";
                    return "";
                }
            }
            public string @ImmuneToMagic
            {
                get
                {
                    if (lang == Language.English)
                        return "Immune to magic";
                    else if (lang == Language.Russian)
                        return "Невосприимчивость к магии";
                    return "";
                }
            }
            public string @Disable
            {
                get
                {
                    if (lang == Language.English)
                        return "Disable";
                    else if (lang == Language.Russian)
                        return "Бездействие";
                    return "";
                }
            }
            public string @Effects
            {
                get
                {
                    if (lang == Language.English)
                        return "Effects";
                    else if (lang == Language.Russian)
                        return "Эффекты";
                    return "";
                }
            }
            public string @Effect
            {
                get
                {
                    if (lang == Language.English)
                        return "Effect";
                    else if (lang == Language.Russian)
                        return "Эффект";
                    return "";
                }
            }
            public string @AttackWeakening
            {
                get
                {
                    if (lang == Language.English)
                        return "Attack Weakening";
                    else if (lang == Language.Russian)
                        return "Ослабление атаки";
                    return "";
                }
            }

            public string @TimeLeftYou
            {
                get
                {
                    if (lang == Language.English)
                        return "You weren't active for 5 minutes and were excluded from the game.";
                    else if (lang == Language.Russian)
                        return "Вы не были активны в течение 5 минут и были исключены из игры.";
                    return "";
                }
            }
            public string @TimeLeftEnemy
            {
                get
                {
                    if (lang == Language.English)
                        return "The enemy is expelled from the game for inaction.";
                    else if (lang == Language.Russian)
                        return "Противник исключён из игры за бездействие.";
                    return "";
                }
            }
            public string @GetMessageCantReportNow(int time_left)
            {
                if (lang == Language.English)
                    return $"You can't report the enemy now! Time left: {time_left} seconds.";
                else if (lang == Language.Russian)
                    return $"Вы не можете пожаловаться на противника сейчас. Осталось: {time_left} секунд.";
                return "";
            }

            public string @ErrorSearchingIncorrectCommand
            {
                get
                {
                    if (lang == Language.English)
                        return "Incorrect command! Use /stopsearching if you want to stop searching.";
                    else if (lang == Language.Russian)
                        return "Неизвестная команда! Используйте /stopsearching, если Вы хотите остановить поиск.";
                    return "";
                }
            }
            public string @ErrorPickingIncorrectCommand
            {
                get
                {
                    if (lang == Language.English)
                        return "Incorrect command! Use /stopsearching if you want to leave lobby.";
                    else if (lang == Language.Russian)
                        return "Неизвестная команда! Используйте /stopsearching, если Вы хотите покинуть комнату.";
                    return "";
                }
            }

            public string @JUGGERNAUT_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Juggernaut is a hero whose main characteristic is agility.\n";
                        msg += "Juggernaut's features:\n";
                        msg += "Strength - 215\n";
                        msg += "Agility - 240\n";
                        msg += "Intelligence - 145\n";
                        msg += "Juggernaut's abilities:\n";
                        msg += $"1 - {Heroes.Juggernaut.AbiNameOne}\nJuggernaut deals 80 magic damage each step for 5 steps, gets immune to magic and cannot use other abilities.\n";
                        msg += $"2 - {Heroes.Juggernaut.AbiNameTwo}\nJuggernaut calls Ward, which recovers 0.75% health from the maximum score every step for 7 steps.\n";
                        msg += $"3 - {Heroes.Juggernaut.AbiNamePassive} (Passive)\nJuggernaut gets an additional 15% chance to critically hit, and also raises the critical strike multiplier by 1.1.\n";
                        msg += $"4 - {Heroes.Juggernaut.AbiNameThree}\nJuggernaut deals 420 physical damage each step for 5 steps.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Juggernaut - герой, основная характеристика которого - ловкость.\n";
                        msg += "Характеристики Juggernaut:\n";
                        msg += "Сила - 215\n";
                        msg += "Ловкость - 240\n";
                        msg += "Интеллект - 145\n";
                        msg += "Способности Juggernaut:\n";
                        msg += $"1 - {Heroes.Juggernaut.AbiNameOne}\nJuggernaut наносит 80 магического урона каждый шаг на протяжении 5 шагов, получает неуязвимость к магии и не может использовать другие способности.\n";
                        msg += $"2 - {Heroes.Juggernaut.AbiNameTwo}\nJuggernaut вызывает Ward, который восстанавливает 0.75% здоровья от максимального показателя каждый шаг в течении 7 шагов.\n";
                        msg += $"3 - {Heroes.Juggernaut.AbiNamePassive} (Пассивная)\nJuggernaut получает допольнительные 15% шанса нанести критический удар, а также повышает множитель критического удара на 1.1.\n";
                        msg += $"4 - {Heroes.Juggernaut.AbiNameThree}\nJuggernaut наносит 450 физического урона противнику каждый шаг на протяжении 5 шагов.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @FACELESSVOID_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @ALCHEMIST_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @ABADDON_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @LIFESTEALER_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @SILENCER_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @WRAITHKING_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @SNIPER_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @DRAGONKNIGHT_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @SLARDAR_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @RAZOR_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }
            public string @URSA_DESCRIBTION
            {
                get
                {
                    return "";
                }
            }

            public string @Donate
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "";
                        msg += "Qiwi - 380666122552 (RUB)\n";
                        msg += "PrivatBank - 5168757311202479 (UAH)\n";
                        msg += "Webmoney WMU - U202606251553 (UAH)\n";
                        msg += "Webmoney WMZ - Z378442228645 (USD)\n";
                        msg += "Webmoney WME - E090510764182 (EURO)\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "";
                        msg += "Qiwi - 380666122552 (Рубли)\n";
                        msg += "ПриватБанк - 5168757311202479 (Гривны)\n";
                        msg += "Webmoney WMU - U202606251553 (Гривы)\n";
                        msg += "Webmoney WMZ - Z378442228645 (Доллары)\n";
                        msg += "Webmoney WME - E090510764182 (Евро)\n";
                        return msg;
                    }
                    return "";
                }
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
                        if (lang == Language.English)
                        {
                            string msg = "\tGame\n";
                            msg += "DotA Text - (free to play) step-by-step text multiplayer game, where 2 players battle each ";
                            msg += "other using heroes from the DotA 2 universe.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tИгра\n";
                            msg += "DotA Text - (free to play) пошаговая текстовая мультиплеерная игра, где 2 игрока сражаются между собой ";
                            msg += "с помощью героев из вселенной DotA 2.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step2_AboutNetMode
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tNetwork Modes\n";
                            msg += "There are 2 network modes in the game - offline and online.\n";
                            msg += "In offline mode you cannot be called to a duel, but you also can't use many functions.\n";
                            msg += "In online mode you can use all functions and can be called to a duel.";
                            msg += "Use /online to be online.\n";
                            msg += "Use /offline to be offine.\n";
                            msg += "Use /netstatus to get your current network mode.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tСетевые режимы\n";
                            msg += "В игре есть два сетевых режима - автономный режим и режим 'В сети'.\n";
                            msg += "В автономном режиме Вас не могут позвать на дуэли (в разработке), но и вы не можете использовать большиство функций.\n";
                            msg += "В режиме 'В сети' Вам доступны все функции, и Вас могут позвать на дуэль (в разработке).\n";
                            msg += "Используйте /online, чтобы быть в сети.\n";
                            msg += "Используйте /offline, чтобы перейти в автономный режим.\n";
                            msg += "Используйте /netstatus, чтобы получить текущий сетевой режим.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step3_AboutLanguage
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tLanguages\n";
                            msg += "There are 2 languages in the game - English and Russian.\n";
                            msg += "If you want to change language, use /language and select language you want.";
                            return msg; ;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tЯзыки\n";
                            msg += "В игре есть 2 языка - Английский и Русский\n";
                            msg += "Если Вы хотите изменить язык, используйте /language и выберите который хотите.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step4_AboutBattle
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tBattle\n";
                            msg += "To start the game, use /startgame and wait for the game to pick up your enemy.";
                            msg += "Use /stopsearching to stop searching enemy.\n";
                            msg += "Then accept the game by clicking the button or writing 'Yes', or if you change your mind to play - 'No'.\n";
                            msg += "If the enemy doesn't accept or reject the game, write /stopsearching, and you will leave the room.\n";
                            msg += "Select the hero you want to play. You can do this by writing the name of the hero or clicking on the button with his name.\n";
                            msg += "If the enemy doesn't choose a hero or you change your mind, use /stopsearching.\n";
                            msg += "The game has begun. Now you are fighting the enemy hero. At your disposal are different abilities and ordinary attacks.\n";
                            msg += "More details about the possibilities and the ordinary attacks can be found below.\n";
                            msg += "The battle goes on step by step. However, stun and disables can give the attacking hero additional steps.\n";
                            msg += "The winner is the one who kills the enemy first. To do this, you must lower the enemy's health to zero.\n";
                            msg += "For the victory you will get 25 points of the rating, for the defeat - will lose 25 points.\n";
                            msg += "Use /leavegame if you want to leave the game. In this case, you will lose, and the enemy will win.\n";
                            msg += "Use /report if the opponent is not active for more than 5 minutes. You will win, and the enemy will be defeated.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tБитва\n";
                            msg += "Чтобы начать игру, используйте /startgame и ждите, пока игра подберёт Вам противника.\n";
                            msg += "Используйте /stopsearching, чтобы остановить поиск противника.\n";
                            msg += "Затем примите игру, нажав кнопку или написав 'Да', или если Вы передумали играть - 'Нет'\n";
                            msg += "Если противник не принимает и не отклоняет игру, то напишите /stopsearching, и вы выйдете из комнаты.\n";
                            msg += "Выберите героя, на котором хотите сыграть. Сделать это можно написав имя героя или нажав на кнопку с его именем.\n";
                            msg += "Если противник не выбирает героя или Вы передумали играть, используйте /stopsearching.";
                            msg += "Игра началась. Теперь Вы сражаетесь против героя противника. В Вашем распоряжении разные способности и обычные удары.\n";
                            msg += "Более подробно о способностях и ударах можно узнать ниже.";
                            msg += "Битва идёт пошагово. Однако оглушения и обездвиживания могут дать атакующему герою дополнительные шаги.\n";
                            msg += "Выиграет тот, кто первый убьёт противника. Для этого нужно опустить здоровье противника к нулю.\n";
                            msg += "За победу Вам начисляется 25 очков рейтинга, за поражение - отбирается 25.";
                            msg += "Используйте /leavegame, если Вы хотите покинуть игру. В таком случае Вам засчитается поражение, а противнику - победа.\n";
                            msg += "Используйте /report, если противник не активен более 5 минут. Вам засчитается победа, а ему - поражение.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step5_AboutHeroes
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tHeroes\n";
                            msg += "Heroes are creatures fighting in battles. Each hero has unique abilities that differ from the abilities of other heroes.\n";
                            msg += "Heroes have 3 characteristics: strength, agility and intelligence. And one of them is basic.\n";
                            msg += "Strength adds 20 health and 0.07 health regeneration per point.\n";
                            msg += "Agility adds 0.14 armor and 0.02 attack speed per point.\n";
                            msg += "Intelligence adds 4.5 mana and 0.04 mana regeneration per point.\n";
                            msg += "The hero's damage is equal to the product of the main characteristic by 0.25 and the attack speed.\n";
                            msg += "Mana is needed to use the abilities of the hero.\n";
                            msg += "Armor blocks a certain amount of damage from the enemy.\n";
                            msg += "To get information about the hero and his abilities, use the '/' + hero name. For example: /juggernaut.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tГерои\n";
                            msg += "Герои - существа, которые сражаются в битвах. Каждый герой имеет уникальные способности, которые разнятся от способностей других героев.\n";
                            msg += "У героев есть 3 характеристики: сила, ловкость и интеллект. И одна из них - основная.\n";
                            msg += "Сила прибавляет 20 единиц здоровья и 0.07 регенерации здоровья за каждое очко.\n";
                            msg += "Ловкость прибавляет 0.14 брони и 0.02 скорости атаки за каждое очко.\n";
                            msg += "Интеллект прибавляет 4.5 маны и 0.04 регенерации маны за каждое очко.\n";
                            msg += "Урон героя равен произведению основной характеристики на 0.25 и на скорость атаки.\n";
                            msg += "Мана нужна для использования способностей героя.\n";
                            msg += "Броня блокирует определённое количество урона от противника.\n";
                            msg += "Чтобы получить информацию о герое и его способностях, используйте '/' + имя героя. Например: /juggernaut.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step6_AboutAbilities
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tAbilities\n";
                            msg += "Each hero has 5 abilities. Two of them are common, and three are unique.\n";
                            msg += "There are also three types of damage: physical, magical and pure.\n";
                            msg += "Physical damage is the damage done to the target, given its armor (unique indicators).\n";
                            msg += "Magical damage is the damage done to the target, given its magical resistance (usually 25%).";
                            msg += "Pure damage is the damage done to target, ignoring all its protective properties.\n";
                            msg += "Abilities can deal any of the three types of damage, and some abilities deal several types of damage at the same time.\n";
                            msg += "They can also have other functions. For example, to strengthen the armor, heal health, improve the regeneration of health, increase the damage to the hero, etc.\n";
                            msg += "The first general ability - Attack - deals the enemy physical damage depending on the damage of the hero.\n";
                            msg += "The second general ability - Heal - restores 500 health and 250 mana.\n";
                            msg += "In addition, the abilities can be of two types: active and passive.\n";
                            msg += "Active abilities are abilities that need to be activated by the player.\n";
                            msg += "Passive abilities are abilities that work independently of the player's actions.\n";
                            msg += "In parentheses () you can see the countdown of ability, in square [] - the needed amount of mana.\n";
                            msg += "To select a method, click or write the corresponding number. More information about the unique abilities can be found in the description of the hero who uses it.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tСпособности\n";
                            msg += "У каждого героя есть 5 способностей. Две из которых общие, и три - уникальные.\n";
                            msg += "Так же есть три типа урона: физический, магический и чистый.\n";
                            msg += "Физический урон - урон, который наносится цели, учитывая её броню (уникальные показатели).\n";
                            msg += "Магический урон - урон, который наносится цели, учитывая её магическое сопротивление (обычно - 25%).\n";
                            msg += "Чистый урон - урон, который наносится цели, игнорируя все защитные свойства.\n";
                            msg += "Способности могут наносить любой из трёх типов урона, причём некоторые способности могут наносить несколько типов урона одновременно.\n";
                            msg += "Также способности могут иметь и другие функции. К примеру, укреплять броню, лечить здоровье, повышать регенерацию здоровья, повышать урон героя и т.д.\n";
                            msg += "Первая общая способность - Атака - наносит врагу физический урон, зависящий от урона героя.\n";
                            msg += "Вторая общая способность - Лечение - восстанавливает 500 здоровья и 250 маны.\n";
                            msg += "Также способности могут быть двух видов: Активная и Пассивная.";
                            msg += "Активные способности - способности, которые нуждаются в активировании игроком.\n";
                            msg += "Пассивные способности - способности, которые работают независимо от действий игрока.";
                            msg += "В круглых скобках () можно увидеть откат способности, в квадратных [] - нужное количество маны.\n";
                            msg += "Чтобы выбрать способность, нажмите или напишите соотвутствующую цифру. Подробнее об уникальных способностях можно узнать в описании героя, который её использует.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step7_AboutDonate
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tDonate\n";
                            msg += "At the moment there is no donat in the game. There is only an opportunity to donate money to develop a project.\n";
                            msg += "To learn more about donate, use /donate\n";
                            msg += "You can also help the project by clicking on ads.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tДонат\n";
                            msg += "На данный момент в игре нет доната. Есть только возможность пожертвовать деньги на развитие проекта.\n";
                            msg += "Чтобы подробнее узнать о донате, используйте /donate\n";
                            msg += "Также Вы можете помочь проекту, кликая на рекламу.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step8_AboutDeveloper
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tDevelopers\n";
                            msg += "In the development involved 2 people: Vladislav Cholak and Roman Rusnakov.\n";
                            msg += "Vladislav Cholak - @BuradoSenpai\n";
                            msg += "Roman Rusnakov - @Mblkolo";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tРазработчики\n";
                            msg += "В разработке участвовали 2 человека: Владислав Чолак и Роман Руснаков.\n";
                            msg += "Владислав Чолак - @BuradoSenpai\n";
                            msg += "Роман Руснаков - @Mblkolo";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step9_TheEnd
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tSupport and Feedbacks\n";
                            msg += "If you want to suggest something or complain about the game's failure, write dotatextgame@gmail.com\n";
                            msg += "In addition, if you like the game, do not forget to call your friends! :)";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tПоддержка и отзывы\n";
                            msg += "Если хотите что-то предложить или пожаловаться на недоработку игры, пишите на dotatextgame@gmail.com\n";
                            msg += "Также, если Вам понравилась игра, не забудьте позвать друзей! :)";
                            return msg;
                        }
                        return "";
                    }
                }
            }
        }
    }
}
