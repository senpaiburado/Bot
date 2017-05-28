using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class IHero
    {
        public static Telegram.Bot.TelegramBotClient bot;
        public enum MainFeature
        {
            Str, Agi, Intel
        }

        protected int Strength { get; set; }
        protected int Agility { get; set; }
        protected int Intelligence { get; set; }
        protected MainFeature Feature { get; set; }

        public string Name { get; set; }
        public float HP { get; set; }
        public float MP { get; set; }
        public float MaxHP { get; set; }
        public float MaxMP { get; set; }
        public float HPregen { get; set; }
        public float MPregen { get; set; }
        public float DPS { get; set; }
        public float Armor { get; set; }

        //////////////////

        protected float AttackSpeed { get; set; }
        protected float CriticalHitChance { get; set; }
        protected float CriticalHitMultiplier { get; set; }
        protected float HpStealPercent { get; set; }
        protected float StunHitChance { get; set; }
        public float MissChance { get; set; }
        public float StunDamage { get; set; }

        public int StunCounter = 0;
        public int BurningCounter = 0;
        public float BurningDamage = 0.0f;
        public int ArmorPenetratingCounter = 0;
        public float ArmorPenetrationValue = 0.0f;

        // Abilities:

            // Heal
        private float HealthRestore = 500.0f;
        private float ManaRestore = 250.0f;
        public const int HealCountdownDefault = 20;
        public int HealCountdown = 0;
        public const float HealPayMana = 50.0f;

        public IHero(string name, int str, int agi, int itl, MainFeature feat)
        {
            this.Name = name;
            Init(str, agi, itl, feat);
        }

        public IHero(IHero _hero)
        {
            string name = _hero.Name;
            int STR = _hero.Strength;
            int AGI = _hero.Agility;
            int INT = _hero.Intelligence;
            MainFeature feat = _hero.Feature;
            this.Name = name;
            Init(STR, AGI, INT, feat);
        }

        virtual public void Init(int str, int agi, int itl, MainFeature feat)
        {
            Strength = str;
            Agility = agi;
            Intelligence = itl;
            Feature = feat;

            MaxHP = Strength * 20.0f;
            HPregen = Strength * 0.03f;

            Armor = Agility * 0.14f;
            AttackSpeed = Agility * 0.02f;

            MaxMP = Intelligence * 12.0f;
            MPregen = Intelligence * 0.04f;

            HP = MaxHP;
            MP = MaxMP;

            float damage = 0.0f;
            if (Feature == MainFeature.Str)
                damage = Strength * 0.4f;
            else if (Feature == MainFeature.Agi)
                damage = Agility * 0.3f;
            else if (Feature == MainFeature.Intel)
                damage = Intelligence * 0.4f;
            DPS = damage + damage * AttackSpeed;

            CriticalHitChance = 15.0f;
            CriticalHitMultiplier = 1.5f;
            HpStealPercent = 5.0f;
            MissChance = 10.0f;
            StunHitChance = 10.0f;
            StunDamage = DPS / 100 * 15;

            InitAdditional();
            InitPassiveAbilities();
        }

        virtual protected void InitAdditional()
        {

        }

        virtual protected void InitPassiveAbilities()
        {

        }

        virtual public async Task<bool> Attack(IHero target, Users.User attacker_user, Users.User target_user)
        {
            Random temp_rand = new Random();
            Random random = new Random(temp_rand.Next(1000000));
            float damage = 0.0f;

            string MessageForAttacker = "";
            string MessageForExcepter = "";

            if (random.Next(1, 101) >= target.MissChance)
            {
                damage += this.DPS;
                if (random.Next(1, 101) <= CriticalHitChance)
                {
                    damage *= CriticalHitMultiplier;
                    MessageForAttacker += $"{attacker_user.lang.CriticalHit}!\n";
                    MessageForExcepter += $"{target_user.lang.TheEnemyDealtCriticalDamageToYou}\n";
                }
                if (random.Next(1,101) <= StunHitChance)
                {
                    target.StunCounter++;
                    MessageForAttacker += $"{attacker_user.lang.StunningHit}!\n";
                    MessageForExcepter += $"{target_user.lang.TheEnemyStunnedYou}\n";
                }
                MessageForAttacker += attacker_user.lang.GetAttackedMessageForAttacker(Convert.ToInt32(damage));
                MessageForExcepter += target_user.lang.GetAttackedMessageForExcepter(Convert.ToInt32(damage));
                Console.WriteLine(MessageForAttacker);
            }
            else
            {
                MessageForAttacker += attacker_user.lang.YouMissedTheEnemy;
                MessageForExcepter += target_user.lang.TheEnemyMissedYou;
            }

            target.GetDamage(damage-target.Armor);

            await bot.SendTextMessageAsync(attacker_user.ID, MessageForAttacker);
            await bot.SendTextMessageAsync(target_user.ID, MessageForExcepter);

            return true;
        }

        virtual public async Task<bool> Heal(Users.User attacker, Users.User excepter)
        {
            if (HealCountdown > 0)
            {
                await bot.SendTextMessageAsync(attacker.ID, attacker.lang.GetMessageCountdown(HealCountdown));
                return false;
            }
            if (MP < HealPayMana)
            {
                await bot.SendTextMessageAsync(attacker.ID, attacker.lang.GetMessageNeedMana(Convert.ToInt32(
                    HealPayMana - MP)));
                return false;
            }
            HP += HealthRestore;
            MP += ManaRestore;

            HP = HP > MaxHP ? MaxHP : HP;
            MP = MP > MaxMP ? MaxMP : MP;

            HealCountdown = HealCountdownDefault;

            await bot.SendTextMessageAsync(attacker.ID, attacker.lang.GetMessageHpAndMpRestored(
                Convert.ToInt32(HealthRestore), Convert.ToInt32(ManaRestore)));
            await bot.SendTextMessageAsync(excepter.ID, excepter.lang.GetMessageEnemyHpAndMpRestored(
                Convert.ToInt32(HealthRestore), Convert.ToInt32(ManaRestore)));
            return true;
        }

        virtual public string GetMessageAbiliesList(Users.User user)
        {
            string[] list =
            {
                $"1 - {user.lang.AttackString}",
                $"2 - {user.lang.Heal} ({HealCountdown}) [{HealPayMana}]",
                $"{user.lang.SelectAbility}:",
            };
            return string.Join("\n", list);
        }

        virtual public void Update()
        {
            if (Math.Floor(HP) <= 0.0f)
            {
                HP = 0;
                return;
            }
            Regeneration();
            UpdateCountdowns();
            UpdateDefaultCountdowns();
            if (Math.Ceiling(HP) >= MaxHP)
                HP = MaxHP;
            if (Math.Ceiling(MP) >= MaxMP || Math.Floor(MP) < 0.0f)
            {
                if (Math.Ceiling(MP) >= MaxMP)
                    MP = MaxMP;
                else
                    MP = 0.0f;
            }
        }

        virtual public void UpdateCountdowns()
        {
            
        }

        virtual public void UpdateDefaultCountdowns()
        {
            if (HealCountdown > 0)
                HealCountdown--;
        }

        virtual public void GetDamage(float value)
        {
            HP -= value;
        }
        virtual protected void Regeneration()
        {
            HP += HPregen;
            MP += MPregen;
        }
    }
}
