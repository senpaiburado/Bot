using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame
{
    class IHero
    {
        public Sender Sender { get; set; }
        public enum MainFeature
        {
            Str, Agi, Intel
        }

        protected int NextSeed = 0;

        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Intelligence { get; set; }
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
        protected float HpStealAdditional { get; set; }
        protected float StunHitChance { get; set; }
        public float MissChance { get; set; }
        public float StunDamage { get; set; }

        protected float AdditionalDamage = 0.0f;

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

        public bool HasImmuneToMagic = false;
        protected int HasImmuneToMagicCounter = 0;

        public bool IsSilenced = false;
        protected int SilenceCounter = 0;

        public bool IsFullDisabled = false;
        protected int FullDisableCounter = 0;

        protected float MagicResistance = 25.0f;

        public bool AttackWeakening = false;
        protected int AttackWeakeningCounter = 0;
        protected float AttackWeakeningPower = 0.0f;

        public bool StealingDPS = false;

        // Abilities:

        // Heal
        private float HealthRestore = 500.0f;
        private float ManaRestore = 250.0f;
        public const int HealCountdownDefault = 20;
        public int HealCountdown = 0;
        public const float HealPayMana = 50.0f;


        public List<string> EffectsList = new List<string>();

        protected IHero hero_target = null;

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

            MaxHP = Strength * 20.0f + 5000.0f;
            HPregen = Strength * 0.07f;

            Armor = Agility * 0.14f;
            AttackSpeed = Agility * 0.02f;

            MaxMP = Intelligence * 4.5f;
            MPregen = Intelligence * 0.04f;

            HP = MaxHP;
            MP = MaxMP;

            UpdateDPS();

            CriticalHitChance = 15.0f;
            CriticalHitMultiplier = 1.5f;
            HpStealPercent = 0.2f;
            HpStealAdditional = 0.0f;
            MissChance = 8.0f;
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
                damage = Strength * 0.25f;
            else if (Feature == MainFeature.Agi)
                damage = Agility * 0.25f;
            else if (Feature == MainFeature.Intel)
                damage = Intelligence * 0.25f;
            DPS = damage + damage * AttackSpeed;
        }

        virtual public void UpdatePerStep()
        {

        }

        virtual protected void InitPassiveAbilities()
        {

        }

        protected void WeakAttack(int time, float power, IHero target)
        {
            target.AttackWeakening = true;
            target.AttackWeakeningCounter += time;
            target.AttackWeakeningPower += power;
            target.DPS -= power;
        }

        protected void UpdateAttackWeakening()
        {
            if (AttackWeakeningCounter > 0)
                AttackWeakeningCounter--;
            else
            {
                if (AttackWeakening)
                {
                    AttackWeakening = false;
                    AttackWeakeningCounter = 0;
                    DPS += AttackWeakeningPower;
                    AttackWeakeningPower = 0.0f;
                }
            }
        }

        protected void UpdateImmuneToMagic()
        {
            if (HasImmuneToMagicCounter > 0)
                HasImmuneToMagicCounter--;
            else
            {
                if (HasImmuneToMagic)
                {
                    HasImmuneToMagicCounter = 0;
                    HasImmuneToMagic = false;
                }
            }
        }

        public string GetEffects(User.Text lang)
        {
            EffectsList.Clear();
            if (StunCounter > 0)
                EffectsList.Add($"{lang.Stun}({StunCounter})");
            if (IsSilenced)
                EffectsList.Add($"{lang.Silence}({SilenceCounter + 1})");
            if (HasImmuneToMagic)
                EffectsList.Add($"{lang.ImmuneToMagic}({HasImmuneToMagicCounter + 1})");
            if (ArmorPenetratingActive)
                EffectsList.Add($"{lang.ArmorDecreasing}({ArmorPenetratingCounter + 1})");
            if (IsFullDisabled)
                EffectsList.Add($"{lang.Disable}({FullDisableCounter + 1})");
            if (AttackWeakening)
                EffectsList.Add($"{lang.AttackWeakening}({AttackWeakeningCounter + 1})");

            if (EffectsList.Count > 1)
                return $"{lang.Effects}: {string.Join(", ", EffectsList.ToArray())}.";
            else if (EffectsList.Count == 1)
                return $"{lang.Effect}: {string.Join(", ", EffectsList.ToArray())}.";
            else
                return "";
        }

        public void AddImmuneToMagic(int time)
        {
            HasImmuneToMagicCounter += time;
            HasImmuneToMagic = true;
        }

        public void Silence(int time, IHero target)
        {
            target.IsSilenced = true;
            target.SilenceCounter += time;
        }

        protected void UpdateSilence()
        {
            if (SilenceCounter > 0 && IsSilenced)
                SilenceCounter--;
            else
            {
                if (IsSilenced)
                {
                    IsSilenced = false;
                    SilenceCounter = 0;
                }
            }
        }

        protected async Task<bool> CheckSilence()
        {
            if (IsSilenced)
            {
                await Sender.SendAsync(lang => lang.YouCantPronounceAbilities);
                return false;
            }
            return true;
        }

        protected async Task<bool> CheckImmuneToMagic(IHero target)
        {
            if (target.HasImmuneToMagic)
            {
                await Sender.SendAsync(lang => lang.EnemyHasImmuneToMagic);
                return false;
            }
            return true;
        }

        protected void UpdateFullDisable()
        {
            if (FullDisableCounter > 0 && IsFullDisabled)
                FullDisableCounter--;
            else
            {
                if (IsFullDisabled)
                {
                    IsFullDisabled = false;
                    FullDisableCounter = 0;
                }
            }
        }

        protected void DisableFull(int time, IHero target)
        {
            target.IsFullDisabled = true;
            target.FullDisableCounter += time;
        }

        public virtual IHero Copy(Sender sender)
        {
            return new IHero(this, sender);
        }

        protected string GetStringEffects()
        {
            return string.Join(", ", EffectsList.ToArray());
        }

        public float CompileMagicDamage(float damage)
        {
            return damage - (damage / 100 * MagicResistance);
        }

        virtual public async Task<bool> Attack(IHero target)
        {
            float damage = 0.0f;

            var attakerMessages = Sender.CreateMessageContainer(); 
            var excepterMessages = target.Sender.CreateMessageContainer(); 

            if (GetRandomNumber(1, 101) >= target.MissChance)
            {
                damage += this.DPS + this.AdditionalDamage;
                damage -= target.Armor;
                if (GetRandomNumber(1, 101) <= StunHitChance)
                {
                    target.StunCounter++;
                    damage += StunDamage;
                    attakerMessages.Add(lang => $"{lang.StunningHit}!");
                    excepterMessages.Add(lang => $"{lang.TheEnemyStunnedYou}");
                }
                if (GetRandomNumber(1, 101) <= CriticalHitChance)
                {
                    damage *= CriticalHitMultiplier;
                    attakerMessages.Add(lang => $"{lang.CriticalHit}!");
                    excepterMessages.Add(lang => lang.TheEnemyDealtCriticalDamageToYou);
                }
                attakerMessages.Add(lang => lang.GetAttackedMessageForAttacker(Convert.ToInt32(damage)));
                excepterMessages.Add(lang => lang.GetAttackedMessageForExcepter(Convert.ToInt32(damage)));
            }
            else
            {
                attakerMessages.Add(lang => lang.YouMissedTheEnemy);
                excepterMessages.Add(lang => lang.TheEnemyMissedYou);
            }
            
            target.GetDamage(damage);
            HP += (damage / 100 * HpStealPercent) + HpStealAdditional;

            await attakerMessages.SendAsync();
            await excepterMessages.SendAsync();

            return true;
        }

        virtual public async Task<bool> Heal(IHero excepter)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(HealPayMana, HealCountdown))
                return false;
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

        protected async Task<bool> CheckManaAndCD(float NeedMP, int CD)
        {
            if (MP < NeedMP)
            {
                await Sender.SendAsync(lang => lang.GetMessageNeedMana(Convert.ToInt32(NeedMP - MP)));
                return false;
            }
            if (CD > 0)
            {
                await Sender.SendAsync(lang => lang.GetMessageCountdown(CD));
                return false;
            }
            return true;
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
            UpdateEffects();
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

        virtual public void UpdateStunAndDisableDuration()
        {
            if (StunCounter > 0)
                StunCounter--;
            UpdateFullDisable();
        }

        virtual public void UpdateDefaultCountdowns()
        {
            if (HealCountdown > 0)
                HealCountdown--;
        }
        virtual public void UpdateEffects()
        {
            UpdateImmuneToMagic();
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
            UpdateSilence();
            UpdateAttackWeakening();
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
