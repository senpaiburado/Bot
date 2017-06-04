using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class IHero
    {
        public Sender Sender { get; set; }
        public enum MainFeature
        {
            Str, Agi, Intel
        }

        protected int NextSeed = 0;

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

        public int GettingDamageCounter = 0;
        public float GettingDamagePower = 0.0f;
        public bool GettingDamageActive = false;

        public int BurningCounter = 0;
        public float BurningDamage = 0.0f;
        public bool BurningActive = false;

        public int ArmorPenetratingCounter = 0;
        public float ArmorPenetrationValue = 0.0f;
        public bool ArmorPenetratingActive = false;

        // Abilities:

            // Heal
        private float HealthRestore = 500.0f;
        private float ManaRestore = 250.0f;
        public const int HealCountdownDefault = 20;
        public int HealCountdown = 0;
        public const float HealPayMana = 50.0f;


        public List<string> EffectsList = new List<string>();

        public IHero(string name, int str, int agi, int itl, MainFeature feat)
        {
            this.Name = name;
            Init(str, agi, itl, feat);
        }

        public IHero(IHero _hero, Sender sender)
        {
            this.Sender = sender;

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

            MaxHP = Strength * 20.0f + 10000.0f;
            HPregen = Strength * 0.03f;

            Armor = Agility * 0.14f;
            AttackSpeed = Agility * 0.02f;

            MaxMP = Intelligence * 4.5f;
            MPregen = Intelligence * 0.04f;

            HP = MaxHP;
            MP = MaxMP;

            UpdateDPS();

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

        protected void UpdateDPS()
        {
            float damage = 0.0f;
            if (Feature == MainFeature.Str)
                damage = Strength * 0.4f;
            else if (Feature == MainFeature.Agi)
                damage = Agility * 0.3f;
            else if (Feature == MainFeature.Intel)
                damage = Intelligence * 0.4f;
            DPS = damage + damage * AttackSpeed;
        }

        virtual public void UpdatePerStep()
        {

        }

        virtual protected void InitPassiveAbilities()
        {

        }

        public virtual IHero Copy(Sender sender)
        {
            return new IHero(this, sender);
        }

        protected string GetStringEffects()
        {
            return string.Join(", ", EffectsList.ToArray());
        }

        virtual public async Task<bool> Attack(IHero target)
        {
            float damage = 0.0f;

            var attakerMessages = Sender.CreateMessageContainer(); 
            var excepterMessages = Sender.CreateMessageContainer(); 

            if (GetRandomNumber(1, 101) >= target.MissChance)
            {
                damage += this.DPS;
                damage -= target.Armor;
                if (GetRandomNumber(1, 101) <= CriticalHitChance)
                {
                    damage *= CriticalHitMultiplier;
                    attakerMessages.Add(lang => lang.CriticalHit);
                    excepterMessages.Add(lang => lang.TheEnemyDealtCriticalDamageToYou);
                }
                if (GetRandomNumber(1, 101) <= StunHitChance)
                {
                    target.StunCounter++;
                    damage += StunDamage;
                    attakerMessages.Add(lang => $"{lang.StunningHit}!");
                    excepterMessages.Add(lang => $"{lang.TheEnemyStunnedYou}");
                }
                attakerMessages.Add(lang => lang.GetAttackedMessageForAttacker(Convert.ToInt32(damage)));
                excepterMessages.Add(lang => lang.GetAttackedMessageForExcepter(Convert.ToInt32(damage)));
                //Console.WriteLine(MessageForAttacker);
            }
            else
            {
                attakerMessages.Add(lang => lang.YouMissedTheEnemy);
                excepterMessages.Add(lang => lang.TheEnemyMissedYou);
            }
            
            target.GetDamage(damage);

            await attakerMessages.SendAsync();
            await excepterMessages.SendAsync();

            return true;
        }

        virtual public async Task<bool> Heal(IHero excepter)
        {
            if (HealCountdown > 0)
            {
                await Sender.SendAsync(lang => lang.GetMessageCountdown(HealCountdown));
                return false;
            }
            if (MP < HealPayMana)
            {
                await Sender.SendAsync(lang => lang.GetMessageNeedMana(Convert.ToInt32(HealPayMana - MP)));
                return false;
            }
            HP += HealthRestore;
            MP += ManaRestore;

            HP = HP > MaxHP ? MaxHP : HP;
            MP = MP > MaxMP ? MaxMP : MP;

            HealCountdown = HealCountdownDefault;

            await Sender.SendAsync(lang => lang.GetMessageHpAndMpRestored(Convert.ToInt32(HealthRestore), Convert.ToInt32(ManaRestore)));
            await excepter.Sender.SendAsync(lang => lang.GetMessageEnemyHpAndMpRestored(Convert.ToInt32(HealthRestore), Convert.ToInt32(ManaRestore)));
            return true;
        }

        virtual public string GetMessageAbilitesList(User.Text lang)
        {
            string[] list =
            {
                $"1 - {lang.AttackString}",
                $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]",
                $"{lang.SelectAbility}:",
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
            UpdateCounters();
            UpdateDefaultCountdowns();
            UpdateDebuffs();
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

        virtual protected void UpdateCounters()
        {

        }

        virtual public void LoosenArmor(float power, int time)
        {
            ArmorPenetratingCounter += time;
            ArmorPenetrationValue += power;
            Armor -= power;
            ArmorPenetratingActive = true;
        }

        virtual public void UpdateCountdowns()
        {
            
        }

        virtual public void UpdateStunDuration()
        {
            if (StunCounter > 0)
                StunCounter--;
        }

        virtual public void UpdateDefaultCountdowns()
        {
            if (HealCountdown > 0)
                HealCountdown--;
        }
        virtual public void UpdateDebuffs()
        {
            // Armor penetration debuff
            if (ArmorPenetratingCounter > 0)
                ArmorPenetratingCounter--;
            else
            {
                if (ArmorPenetratingActive && ArmorPenetratingCounter == 0)
                {
                    Armor += ArmorPenetrationValue;
                    ArmorPenetratingActive = false;
                    ArmorPenetrationValue = 0.0f;
                }
            }
            // Burning debuff
            if (BurningCounter > 0)
            {
                BurningCounter--;
                GetDamage(BurningDamage);
            }
            else
            {
                if (BurningActive && BurningCounter == 0)
                {
                    BurningActive = false;
                    BurningDamage = 0.0f;
                }
            }
            //Getting damage per step
            if (GettingDamageCounter > 0)
            {
                GettingDamageCounter--;
                GetDamage(GettingDamagePower);
            }
            else
            {
                if (GettingDamageActive && GettingDamageCounter == 0)
                {
                    GettingDamageActive = false;
                    GettingDamagePower = 0.0f;
                }
            }
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

        protected int GetRandomNumber(int min, int max)
        {
            Random random = new Random((int)DateTime.Now.Ticks * NextSeed);
            NextSeed += 3;
            if (NextSeed > 10000)
                NextSeed = 0;
            return random.Next(min, max);
        }

        virtual public void GetDamageByDebuffs(float power, int time)
        {
            GettingDamageCounter += time;
            GettingDamagePower += power;
            GettingDamageActive = true;
        }

        virtual public Task<bool> UseAbilityOne(IHero target)
        {
            return Task.FromResult(false);
        }
        virtual public Task<bool> UseAbilityTwo(IHero target)
        {
            return Task.FromResult(false);
        }
        virtual public Task<bool> UseAbilityThree(IHero target)
        {
            return Task.FromResult(false);
        }
    }
}
