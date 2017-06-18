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
                        msg += "Juggernaut's abilities:\n\n";
                        msg += $"1 - {Heroes.Juggernaut.AbiNameOne}\nJuggernaut deals 80 magic damage each step for 5 steps, gets immune to magic and cannot use other abilities. Cannot use if the enemy has immune to magic.\n\n";
                        msg += $"2 - {Heroes.Juggernaut.AbiNameTwo}\nJuggernaut calls Ward, which recovers 0.75% health from the maximum score every step for 7 steps.\n\n";
                        msg += $"3 - {Heroes.Juggernaut.AbiNamePassive} (Passive)\nJuggernaut gets an additional 15% chance to critically hit, and also raises the critical strike multiplier by 1.1.\n\n";
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
                        msg += "Способности Juggernaut:\n\n";
                        msg += $"1 - {Heroes.Juggernaut.AbiNameOne}\nJuggernaut наносит 80 магического урона каждый шаг на протяжении 5 шагов, получает неуязвимость к магии и не может использовать другие способности. Невозможно использовать, если противник невосприимчив к магии.\n\n";
                        msg += $"2 - {Heroes.Juggernaut.AbiNameTwo}\nJuggernaut вызывает Ward, который восстанавливает 0.75% здоровья от максимального показателя каждый шаг в течении 7 шагов.\n\n";
                        msg += $"3 - {Heroes.Juggernaut.AbiNamePassive} (Пассивная)\nJuggernaut получает допольнительные 15% шанса нанести критический удар, а также повышает множитель критического удара на 1.1.\n\n";
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
                    if (lang == Language.English)
                    {
                        string msg = "Faceless Void is a hero whose main characteristic is agility.\n";
                        msg += "Strength - 230\n";
                        msg += "Agility - 250\n";
                        msg += "Intelligence - 150\n";
                        msg += "Abilities of Faceless Void:\n\n";
                        msg += $"1 - {Heroes.FacelessVoid.AbiNameOne}\nFaceless Void restores itself a level of health that was a step back and deals 150 magical damage.\n\n";
                        msg += $"2 - {Heroes.FacelessVoid.AbiNameTwo}\nFaceless Void accelerates its attack speed by 1.9 for 5 steps.\n\n";
                        msg += $"3 - {Heroes.FacelessVoid.AbiNamePassive} (Passive)\nFaceless Void gets an additional 15% chance of stun and an additional 25 damage to it.\n\n";
                        msg += $"4 - {Heroes.FacelessVoid.AbiNameThree}\nFaceless Void sets the Chronosphere, which imposes a 5-step disable on the target.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Faceless Void - герой, основная характеристика которого - ловкость.\n";
                        msg += "Сила - 230\n";
                        msg += "Ловкость - 250\n";
                        msg += "Интеллект - 150\n";
                        msg += "Способности Faceless Void:\n\n";
                        msg += $"1 - {Heroes.FacelessVoid.AbiNameOne}\nFaceless Void возвращает себе уровень здоровья, который был шаг назад, и наносит цели 150 магического урона.\n\n";
                        msg += $"2 - {Heroes.FacelessVoid.AbiNameTwo}\nFaceless Void ускоряет свою скорость атаки на 1.9 на 5 шагов.\n\n";
                        msg += $"3 - {Heroes.FacelessVoid.AbiNamePassive} (Пассивная)\nFaceless Void получает дополнительные 15% шанса олушения и дополнительно 25 урона к нему.\n\n";
                        msg += $"4 - {Heroes.FacelessVoid.AbiNameThree}\nFaceless Void ставит Chronosphere, которая накладывает на цель обездвиживание на 5 шагов.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @ALCHEMIST_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Alchemist is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 270\n";
                        msg += "Agility - 110\n";
                        msg += "Intelligence - 200\n";
                        msg += "Alchemist's abilities:\n\n";
                        msg += $"1 - {Heroes.Alchemist.AbiNameOne}\nAlchemist pours on the enemy an acid that deals 45 physical damage to him, and if the target is susceptible to magic, reduces armor by 17, for 9 steps.\n\n";
                        msg += $"2 - {Heroes.Alchemist.AbiNameTwoDefault}\nAlchemist activates an unstable explosive concoction that he can throw at the enemy. The application of the ability turns it into the ability {Heroes.Alchemist.AbiNameTwoActivated}.\n\n";
                        msg += $"3 - {Heroes.Alchemist.AbiNameTwoActivated}\nAlchemist can throw the concoction at the enemy for 6 steps. The enemy will be stunned as many steps as the concoction was prepared. If Alchemist does not throw the concoction for 6 steps, it will blow itself up.\n\n";
                        msg += $"4 - {Heroes.Alchemist.AbiNamePassive} (Passive)\nAlchemist raises its damage by 5 points every 5 steps.\n\n";
                        msg += $"5 - {Heroes.Alchemist.AbiNameThree}\nAlchemist drinks a potion that makes it stronger! The hero receives +75 health regeneration, +25 mana regeneration and 2.1 attack speed for 11 steps.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Alchemist - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 270\n";
                        msg += "Ловкость - 110\n";
                        msg += "Интеллект - 200\n";
                        msg += "Способности Alchemist:\n\n";
                        msg += $"1 - {Heroes.Alchemist.AbiNameOne}\nAlchemist выливает на противника кислоту, которая наносит ему 45 физического урона, и, если цель восприимчива к магии, снижает броню на 17 единиц, и действует 9 шагов.\n\n";
                        msg += $"2 - {Heroes.Alchemist.AbiNameTwoDefault}\nAlchemist активирует нестабильную взрывную смесь, которую может кинуть у врага. Применение способности превращает её в способность {Heroes.Alchemist.AbiNameTwoActivated}.\n\n";
                        msg += $"3 - {Heroes.Alchemist.AbiNameTwoActivated}\nAlchemist на протяжении 6 шагов может кинуть смесь у противника. Враг будет оглушён на столько шагов, сколько готовилась смесь. Если на протяжении 6 шагов Alchemist не кинет смесь, он взорвёт сам себя.\n\n";
                        msg += $"4 - {Heroes.Alchemist.AbiNamePassive} (Пассивная)\nAlchemist повышает свой урон на 5 единиц каждые 5 шагов.\n\n";
                        msg += $"5 - {Heroes.Alchemist.AbiNameThree}\nAlchemist выпивает зелье, которое делает его сильнее! Герой получает +75 регенерации здоровья, +25 регенерации маны и 2.1 к скорости атаки на 11 шагов.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @ABADDON_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Abaddon is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 250\n";
                        msg += "Agility - 170\n";
                        msg += "Intelligence - 210\n";
                        msg += "Abaddon's abilities:\n\n";
                        msg += $"1 - {Heroes.Abaddon.AbiNameOne}\nAbaddon deals 300 damage to the target, and restores his health, depending on the damage dealt.\n\n";
                        msg += $"2 - {Heroes.Abaddon.AbiNameTwo}\nAbaddon creates a shield around himself that blocks 1000 damage to the hero, and also explodes if it is not broken, and then inflicts magical damage to the enemy, depending on the remaining shield strength.\n\n";
                        msg += $"3 - {Heroes.Abaddon.AbiNamePassive} (Passive)\nAbaddon receives an additional 35 armor, and each attack increases damage by 20. After the 6th attack, damage is reset, and everything goes first.\n\n";
                        msg += $"4 - {Heroes.Abaddon.AbiNameThree}\nFor 6 steps, Abaddon turns the received damage to health, healing himself this way.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Abaddon - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 250\n";
                        msg += "Ловкость - 110\n";
                        msg += "Интеллект - 200\n";
                        msg += "Способности Abaddon:\n\n";
                        msg += $"1 - {Heroes.Abaddon.AbiNameOne}\nAbaddon наносит цели 300 физического урона, а также восстанавливает ему здоровье зависимо от нанесённого урона.\n\n";
                        msg += $"2 - {Heroes.Abaddon.AbiNameTwo}\nAbaddon создаёт вокруг себя щит, который блокирует 1000 урона по герою, а также взрывается, если его не сломать, после чего наносит магический урон врагу зависимо от оставшиеся прочности щита.\n\n";
                        msg += $"3 - {Heroes.Abaddon.AbiNamePassive} (Пассивная)\nAbaddon получает дополнительные 35 брони, и каждую атаку увеличивает урон на 20. После 6-й атаки урон сбрасывается, и всё идёт сначала.\n\n";
                        msg += $"4 - {Heroes.Abaddon.AbiNameThree}\nНа протяжении 6 шагов Abaddon превращает получаемый урон в здоровье, исцеляя себя таким образом.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @LIFESTEALER_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Lifestealer is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 250\n";
                        msg += "Agility - 180\n";
                        msg += "Intelligence - 150\n";
                        msg += "Lifestealer's abilities:\n\n";
                        msg += $"1 - {Heroes.Lifestealer.AbiNameOne}\nLifestealer abuses, becomes immune to magic and gets 1.5 to attack speed for 5 steps.\n\n";
                        msg += $"2 - {Heroes.Lifestealer.AbiNamePassive} (Пассивная)\nAttacking, Lifestealer restores health - 2% of the current health of the enemy.\n\n";
                        msg += $"3 - {Heroes.Lifestealer.AbiNameTwo}\nLifestealer every hit for 5 steps restores health - + 5% of the damage dealt.\n\n";
                        msg += $"4 - {Heroes.Lifestealer.AbiNameThree}\nLifestealer rips the enemy, causing him 600 pure damage, and doubling it with a 50% chance\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Lifestealer - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 250\n";
                        msg += "Ловкость - 180\n";
                        msg += "Интеллект - 150\n";
                        msg += "Способности Lifestealer:\n\n";
                        msg += $"1 - {Heroes.Lifestealer.AbiNameOne}\nLifestealer озверевает, становится невосприимчивым к магии и получает 1.5 к скорости атаки на 5 шагов.\n\n";
                        msg += $"2 - {Heroes.Lifestealer.AbiNamePassive} (Пассивная)\nАтакуя, Lifestealer восстанавливает здоровье - 2% от текущего здоровья противника.\n\n";
                        msg += $"3 - {Heroes.Lifestealer.AbiNameTwo}\nLifestealer каждый удар на протяжении 5 шагов восстанавливает здоровье - +5% от нанесённого урона.\n\n";
                        msg += $"4 - {Heroes.Lifestealer.AbiNameThree}\nLifestealer разрывает противника, нанося ему 600 чистого урона, и с шансом 50% удвоит его.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @SILENCER_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Silencer is a hero whose main characteristic is intelligence.\n";
                        msg += "Strength - 195\n";
                        msg += "Agility - 225\n";
                        msg += "Intelligence - 230\n";
                        msg += "Abilities of Silencer:\n\n";
                        msg += $"1 - {Heroes.Silencer.AbiNameOne}\nSilencer curses the enemy, inflicting 60 magical damage every step for 10 steps. Also with a 35% chance, you can silence your enemy for 4 steps.\n\n";
                        msg += $"2 - {Heroes.Silencer.AbiNameTwo}\nNSilencer steals from the enemy voice, deals him 900 magical damage and silences him for 6 steps.\n\n";
                        msg += $"3 - {Heroes.Silencer.AbiNamePassive} (Passive)\nIf the opponent is susceptible to magic, Silencer deals him (+35% of intelligence) additional damage.\n\n";
                        msg += $"4 - {Heroes.Silencer.AbiNameThree}\nSilencer closes his mouth to the enemy, the same imposes a silence on him for 10 steps.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Silencer - герой, основая характеристика которого - интеллект.\n";
                        msg += "Сила - 195\n";
                        msg += "Ловкость - 225\n";
                        msg += "Интеллект - 230\n";
                        msg += "Способности Silencer:\n\n";
                        msg += $"1 - {Heroes.Silencer.AbiNameOne}\nSilencer проклинает противника, нанося ему 60 магического урона каждый шаг на протяжении 10 шагов. Также с шансом 35% может заставить противника замолчать на 4 шага.\n\n";
                        msg += $"2 - {Heroes.Silencer.AbiNameTwo}\nSilencer ворует у противника голос, наносит ему 900 магического урона и заставляет его замолчать на 6 шагов.\n\n";
                        msg += $"3 - {Heroes.Silencer.AbiNamePassive} (Пассивная)\nЕсли противник восприимчив к магии, Silencer наносит ему дополнительно (+35% от интеллекта) урона с атаки.\n\n";
                        msg += $"4 - {Heroes.Silencer.AbiNameThree}\nSilencer закрывает рот врагу, тем же накладывая на него молчание на 10 шагов.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @WRAITHKING_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Wraith King is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 250\n";
                        msg += "Agility - 180\n";
                        msg += "Intelligence - 180\n";
                        msg += "Abilities of Wraith King:\n\n";
                        msg += $"1 - {Heroes.WraithKing.AbiNameOne}\nWraith King stuns the enemy for 2 steps, inflicting 300 magical damage.\n\n";
                        msg += $"2 - {Heroes.WraithKing.AbiPassiveNameOne} (Passive)\nПризрачный король получает еще 10% кражи здоровья.\n\n";
                        msg += $"3 - {Heroes.WraithKing.AbiPassiveNameTwo} (Passive)\nWright King adds a 10% chance of critical strike and adds 0.45 to the critical hit multiplier.\n\n";
                        msg += $"4 - {Heroes.WraithKing.AbiNameTwo}\nWraith King strengthens its armor by 50 units for 5 steps.\n\n";
                        msg += $"5 - {Heroes.WraithKing.AbinameThree}\nNWraith King deals its strongest blow, inflicting 100 to 1000 magic damage to the enemy, and restoring damage done to the enemy as health.";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Wraith King - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 250\n";
                        msg += "Ловкость - 180\n";
                        msg += "Интеллект - 180\n";
                        msg += "Способности Wraith King:\n\n";
                        msg += $"1 - {Heroes.WraithKing.AbiNameOne}\nWraith King оглушает противника на 2 шага, нанося ему 300 магического урона.\n\n";
                        msg += $"2 - {Heroes.WraithKing.AbiPassiveNameOne} (Пассивная)\nWraith King получает дополнительно 10% кражи здоровья.\n\n";
                        msg += $"3 - {Heroes.WraithKing.AbiPassiveNameTwo} (Пассивная)\nWraith King получает дополнительно 10% шанса критического удара и прибавляет 0,45 к множителю критического удара.\n\n";
                        msg += $"4 - {Heroes.WraithKing.AbiNameTwo}\nWraith King укрепляет свою броню на 50 единиц на 5 шагов.\n\n";
                        msg += $"5 - {Heroes.WraithKing.AbinameThree}\nWraith King наносит свой сильнейший удар, нанося противнику от 100 до 1000 магического урона, и восстанавливая себе нанесённый урон как здоровье.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @SNIPER_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Sniper is a hero whose main characteristic is agility.\n";
                        msg += "Strength - 170\n";
                        msg += "Agility - 235\n";
                        msg += "Intelligence - 155\n";
                        msg += "Sniper's abilities:\n\n";
                        msg += $"1 - {Heroes.Sniper.AbiNameOne}\nSniper releases a charge of shrapnel, inflicting 100 magical damage each step for 8 steps, and also weakening the enemy by 75 for 8 steps.\n\n";
                        msg += $"2 - {Heroes.Sniper.AbiNamePassive}\nThe sniper gets the opportunity to hit the enemy's head, disabling him for 1-2 steps.\n\n";
                        msg += $"3 - {Heroes.Sniper.AbiNameTwo}\nSniper makes 5 shots, each shot with a 65% chance hits the enemy, causing him 350 physical damage.\n\n";
                        msg += $"4 - {Heroes.Sniper.AbiNameThree}\nSniper takes the strongest charge and accurately targets the enemy, inflicting 1000 magical damage to him.";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Sniper - герой, основая характеристика которого - ловкость.\n";
                        msg += "Сила - 170\n";
                        msg += "Ловкость - 235\n";
                        msg += "Интеллект - 155\n";
                        msg += "Способности Sniper:\n\n";
                        msg += $"1 - {Heroes.Sniper.AbiNameOne}\nSniper выпускает заряд шрапнели, нанося врагу 100 магического урона в шаг на протяжении 8 шагов, а также ослабляя врага на 75 единиц на 8 шагов.\n\n";
                        msg += $"2 - {Heroes.Sniper.AbiNamePassive}\nSniper получает возможность попасть в голову противника, обездвиживая его на 1-2 шага.\n\n";
                        msg += $"3 - {Heroes.Sniper.AbiNameTwo}\nSniper делает 5 выстрелов, каждый выстрел с шансом 65% попадает в противника, нанося ему 350 физического урона.\n\n";
                        msg += $"4 - {Heroes.Sniper.AbiNameThree}\nSniper берёт самый сильный заряд и точно целится у врага, нанося ему 1000 магического урона.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @DRAGONKNIGHT_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Dragon Knight is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 210\n";
                        msg += "Agility - 190\n";
                        msg += "Intelligence - 150\n";
                        msg += "Abilities of Dragon Knight:\n\n";
                        msg += $"1 - {Heroes.DragonKnight.AbiNameOne}\nDragon Knight emits a flame into the enemy, inflicting 400 magical damage to it, and weakening his attack by 35% for 8 steps.\n\n";
                        msg += $"2 - {Heroes.DragonKnight.AbiNameTwo}\nDragon Knight stuns the enemy for 3 steps and deals 500 physical damage to him.\n\n";
                        msg += $"3 - {Heroes.DragonKnight.AbiNamePassive}\nDragon Knight gets +30 health regeneration and +25 armor.\n\n";
                        msg += $"4 - {Heroes.DragonKnight.AbiNameThree}\nDragon Knight enters into dragon's fury, increasing its attack speed by 45%, increasing health regeneration by 20, increasing armor by 25, and adding 50 to damage. It works 6 steps.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Dragon Knight - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 210\n";
                        msg += "Ловкость - 190\n";
                        msg += "Интеллект - 150\n";
                        msg += "Способности Dragon Knight:\n\n";
                        msg += $"1 - {Heroes.DragonKnight.AbiNameOne}\nDragon Knight испускает в противника пламя, нанося ему 400 магического урона, а также ослабляя его атаку на 35% на 8 шагов.\n\n";
                        msg += $"2 - {Heroes.DragonKnight.AbiNameTwo}\nDragon Knight оглушает противника на 3 шага и наносит ему 500 физического урона.\n\n";
                        msg += $"3 - {Heroes.DragonKnight.AbiNamePassive}\nDragon Knight получает +30 к регенерации здоровья и +25 к броне.\n\n";
                        msg += $"4 - {Heroes.DragonKnight.AbiNameThree}\nDragon Knight входит в ярость дракона, повышая свою скорость атаки на 45%, регенерацию здоровья на 20, защиту на 25, а также добавляет 50 к урону. Действует 6 шагов.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @SLARDAR_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Slardar is a hero whose main characteristic is strength.\n";
                        msg += "Strength - 245\n";
                        msg += "Agility - 170\n";
                        msg += "Intelligence - 150\n";
                        msg += "Slardar's abilities:\n\n";
                        msg += $"1 - {Heroes.Slardar.AbiNameOne}\nSlardar increases attack speed by 75%, but also receives 15% more damage. It works 8 steps.\n\n";
                        msg += $"2 - {Heroes.Slardar.AbiNameTwo}\nSlardar deals a powerful blow to the enemy, stunning him for 3 steps and causing 450 physical damage.\n\n";
                        msg += $"3 - {Heroes.Slardar.AbiNamePassive} (Passive)\nSlardar gets an additional 15% chance of stunning and 55 damage to stun.\n\n";
                        msg += $"4 - {Heroes.Slardar.AbiNameThree}\nSlardar breaks the enemy's armor, thereby reducing it by 50 units.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Slardar - герой, основая характеристика которого - сила.\n";
                        msg += "Сила - 245\n";
                        msg += "Ловкость - 170\n";
                        msg += "Интеллект - 150\n";
                        msg += "Способности Slardar:\n\n";
                        msg += $"1 - {Heroes.Slardar.AbiNameOne}\nSlardar повышает скорость атаки на 75%, но при этом получает на 15% больше урона. Действует 8 шагов.\n\n";
                        msg += $"2 - {Heroes.Slardar.AbiNameTwo}\nSlardar наносит врагу сильный удар, оглушая его на 3 шага и нанося 450 физического урона.\n\n";
                        msg += $"3 - {Heroes.Slardar.AbiNamePassive} (Пассивная)\nSlardar получает дополнительные 15% к шансу оглушения и 55 к урону от оглушения.\n\n";
                        msg += $"4 - {Heroes.Slardar.AbiNameThree}\nSlardar разбивает броню противника, тем самым снижая её на 50 единиц.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @RAZOR_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Razor is a hero whose main characteristic is agility.\n";
                        msg += "Strength - 210\n";
                        msg += "Agility - 240\n";
                        msg += "Intelligence - 210\n";
                        msg += "Razor's abilities:\n\n";
                        msg += $"1 - {Heroes.Agility.Ursa.AbiNameOne}\nRazor creates a plasma field around itself, which deals 650 magic damage to the enemy.\n\n";
                        msg += $"2 - {Heroes.Agility.Ursa.AbiNameTwo}\nRazor steals from the enemy 35% damage for 5 steps.\n\n";
                        msg += $"3 - {Heroes.Agility.Ursa.AbiNamePassive} (Passive)\nRazor during an attack with a probability of 15% can deal an enemy electrical hit - 200 magic damage.\n\n";
                        msg += $"4 - {Heroes.Agility.Ursa.AbiNameThree}\nRazor causes a storm that, when activated, deals 250 physical damage to the enemy and reduces armor by 5. Each step for 7 steps the enemy receives 85 damage and loses 1 armor.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Razor - герой, основая характеристика которого - ловкость.\n";
                        msg += "Сила - 210\n";
                        msg += "Ловкость - 240\n";
                        msg += "Интеллект - 210\n";
                        msg += "Способности Razor:\n\n";
                        msg += $"1 - {Heroes.Agility.Razor.AbiNameOne}\nRazor создаёт вокруг себя плазменное поле, которое наносит противнику 650 магического урона.\n\n";
                        msg += $"2 - {Heroes.Agility.Razor.AbiNameTwo}\nRazor ворует у противника 35% урона на 5 шагов.\n\n";
                        msg += $"3 - {Heroes.Agility.Razor.AbiNamePassive} (Пассивная)\nRazor во время атаки с шансом 15% может нанести противнику электрический удар - 200 магического урона.\n\n";
                        msg += $"4 - {Heroes.Agility.Razor.AbiNameThree}\nRazor вызывает шторм, который при активации наносит врагу 250 физического урона и снижает броню на 5 единиц. Каждый шаг на протяжении 7 шагов противник получает 85 чистого урона и теряет 1 единицу брони.\n";
                        return msg;
                    }
                    return "";
                }
            }
            public string @URSA_DESCRIBTION
            {
                get
                {
                    if (lang == Language.English)
                    {
                        string msg = "Ursa is a hero whose main characteristic is agility.\n";
                        msg += "Strength - 230\n";
                        msg += "Agility - 200\n";
                        msg += "Intelligence - 160\n";
                        msg += "Abilities of Ursa:\n\n";
                        msg += $"1 - {Heroes.Agility.Ursa.AbiNameOne}\nUrsa creates an earthquake that inflicts 400 magic damage to the enemy and reduces armor by 7 for 5 steps.\n\n";
                        msg += $"2 - {Heroes.Agility.Ursa.AbiNameTwo}\nUrsa receives 30 points to damage and 1 to attack speed for 4 steps.\n\n";
                        msg += $"3 - {Heroes.Agility.Ursa.AbiNamePassive} (Passive)\nUrsa deals 0/20/40/60/80/100 additional damage. It grows with every blow. After a threshold of 100 units, the additional damage is reset to zero.\n\n";
                        msg += $"4 - {Heroes.Agility.Ursa.AbiNameThree}\nUrsa runs into enrage, takes an additional 80 damage and reduces damage taken by 80%.\n";
                        return msg;
                    }
                    else if (lang == Language.Russian)
                    {
                        string msg = "Ursa - герой, основая характеристика которого - ловкость.\n";
                        msg += "Сила - 230\n";
                        msg += "Ловкость - 200\n";
                        msg += "Интеллект - 160\n";
                        msg += "Способности Ursa:\n\n";
                        msg += $"1 - {Heroes.Agility.Ursa.AbiNameOne}\nUrsa создаёт землетрясение, которое наносит противнику 400 магического урона и снижает броню на 7 единиц на 5 шагов.\n\n";
                        msg += $"2 - {Heroes.Agility.Ursa.AbiNameTwo}\nUrsa получает 30 единиц к урону и 1 к скорости атаки на 4 шага.\n\n";
                        msg += $"3 - {Heroes.Agility.Ursa.AbiNamePassive} (Пассивная)\nUrsa наносит 0/20/40/60/80/100 дополнительного урона. Растёт с каждыи ударом. После порога в 100 единиц дополнительный урон сбрасывается к нулю.\n\n";
                        msg += $"4 - {Heroes.Agility.Ursa.AbiNameThree}\nUrsa впадает в бешенство, получает дополнительные 80 урона и снижает получаемый урон на 80%.\n";
                        return msg;
                    }
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
                            msg += "In online mode you can use all functions and can be called to a duel.\n";
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
                            msg += "To start the game, use /startgame and wait for the game to pick up your enemy.\n";
                            msg += "Use /stopsearching to stop searching enemy.\n";
                            msg += "Then accept the game by clicking the button or writing 'Yes', or if you change your mind to play - 'No'.\n";
                            msg += "If the enemy doesn't accept or reject the game, write /stopsearching, and you will leave the room.\n";
                            msg += "Select the hero you want to play. You can do this by writing the name of the hero or clicking on the button with his name.\n";
                            msg += "If the enemy doesn't choose a hero or you change your mind, use /stopsearching.\n";
                            msg += "The game has begun. Now you are fighting the enemy hero. At your disposal are different abilities and ordinary attacks.\n";
                            msg += "More details about the abilities and the ordinary attacks can be found below.\n";
                            msg += "The battle goes on step by step. However, stun and disables can give the attacking hero additional steps.\n";
                            msg += "The winner is the one who kills the enemy first. To do this, you must lower the enemy's health to zero.\n";
                            msg += "For the victory you will get 25 points of the rating, for the defeat - will lose 25 points.\n";
                            msg += "Use /leavegame if you want to leave the game. In this case, you will lose, and the enemy will win.\n";
                            msg += "Use /report if the enemy is not active for more than 5 minutes. You will win, and the enemy will be defeated.";
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
                            msg += "To get information about the hero and his abilities, use the '/' + hero name. For example: /juggernaut.\n";
                            msg += "List of heroes:\n";
                            foreach (var hero in Game.hero_list)
                                msg += $"{hero.Name}\n";
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
                            msg += "Чтобы получить информацию о герое и его способностях, используйте '/' + имя героя. Например: /juggernaut.\n";
                            msg += "Список героев:\n";
                            foreach (var hero in Game.hero_list)
                                msg += $"{hero.Name}\n";
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
                            msg += "Magical damage is the damage done to the target, given its magical resistance (usually 25%).\n";
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
                public string @step7_AboutBuffstAndModifiers
                {
                    get
                    {
                        if (lang == Language.English)
                        {
                            string msg = "\tEffects and Modifiers\n";
                            msg += "Foldable effects are effects that increase their strength and duration, applying the same ability, or the other, with the same effect.\n";
                            msg += "Critical hit is a blow that deals increased damage to the enemy (usually x1.5) with a certain chance (usually 15%).\n";
                            msg += "Stun is a (folding) effect that prohibits the purpose of doing anything, and therefore the goal skips its step. This effect can be applied to both abilities and normal attacks. At the same time, the probability of stunning is usually 10%, and 15% of the hero's damage is added to the strike.\n";
                            msg += "Immobilization is a (foldable) analog of stun, it can only be inflicted on abilities and does not cause additional damage.\n";
                            msg += "Penetration of armor is a (folding) effect that reduces the target's armor by X units for Y steps.\n";
                            msg += "Attack weakening is a (folding) effect, in which the target loses X points of damage for Y steps.\n";
                            msg += "Immune to magic - (folding) effect, in which the hero does not receive magical damage, and also can not be affected by certain abilities.\n";
                            msg += "Silence is a (folding) effect, in which the hero cannot use any abilities, except for the attack.";
                            return msg;
                        }
                        else if (lang == Language.Russian)
                        {
                            string msg = "\tЭффекты и модификаторы\n";
                            msg += "Складываемые эффекты - эффекты, которые увеличивают свою силу и длительность за счёт применения такой же способности, или другой, с таким же эффектом.\n";
                            msg += "Критический удар - удар, который наносит врагу увеличенный урон (обычно - х1.5) с определённым шансом (обычно - 15%).\n";
                            msg += "Оглушение - (складываемый) эффект, который запрещает цели совершать какие-либо действия, и поэтому цель пропускает свой шаг. Этот эффект может быть нанесён как от способностей, так и от обычной атаки. При этом шанс оглушения, обычно, 10%, и к удару прибавляется 15% от урона героя.\n";
                            msg += "Обездвиживание - (складываемый) аналог оглушения, только может быть нанесён только от способностей и не наносит дополнительного урона.\n";
                            msg += "Пробивание брони - (складываемый) эффект, который снижает броню цели на X единиц на Y шагов.\n";
                            msg += "Ослабление атаки - (складываемый) эффект, при котором цель теряет X единиц урона на Y шагов.\n";
                            msg += "Невосприимчивость к магии - (складываемый) эффект, при котором герой не получает магический урон, а также не может быть подвержена некоторым способностям.\n";
                            msg += "Молчание - (складываемый) эффект, при котором герой не может использовать любые способности, за исключением атаки.";
                            return msg;
                        }
                        return "";
                    }
                }
                public string @step8_AboutDonate
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
                public string @step9_AboutDeveloper
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
                public string @step10_TheEnd
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
