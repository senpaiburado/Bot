using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class FacelessVoid : IHero
    {
        private float PreviousHP = 0.0f;
        // Ability One : Time Walk
        public static string AbiNameOne = "Time Walk";
        private float TimeWalkDamage = 150.0f;
        private int TimeWalkCD = 0;
        private const int TimeWalkDefaultCD = 12;
        private float TimeWalkManaPay = 100.0f;

        // Ability Two : Acceleration of Time
        public static string AbiNameTwo = "Acceleration of time";
        private float AoT_ManaPay = 130.0f;
        private float AoT_AttackSpeed = 1.9f;
        private int AoT_CD = 0;
        private const int AoT_DefaultCD = 20;
        private int AoT_Counter = 0;
        private int AoT_Duration = 5;
        private bool AoT_Activated = false;

        // Ability Passive : Time Lock
        public static string AbiNamePassive = "Time lock";
        private float TimeLockAddDamage = 25.0f;
        private float TimeLockAddStunChance = 15.0f;

        // Ability Three : Chronosphere
        public static string AbiNameThree = "Chronosphere";
        private int ChronosphereDuration = 5;
        private int ChronosphereCD = 0;
        private const int ChronosphereDefaultCD = 40;
        private float ChronosphereManaPay = 200.0f;
        private int ChronosphereCounter = 0;
        private bool ChronosphereActivated = false;

        public FacelessVoid(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public FacelessVoid(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new FacelessVoid(this, sender);
        }
        protected override void InitPassiveAbilities()
        {
            StunDamage += TimeLockAddDamage;
            StunHitChance += TimeLockAddStunChance;
        }
        private void UpdateAoT()
        {
            if (AoT_Counter < AoT_Duration && AoT_Activated)
                AoT_Counter++;
            else
            {
                if (AoT_Activated)
                {
                    AoT_Counter = 0;
                    AoT_Activated = false;
                    AttackSpeed -= AoT_AttackSpeed;
                    UpdateDPS();
                    AoT_CD = AoT_DefaultCD;
                }
            }
        }
        private void UpdateChronosphere()
        {
            if (ChronosphereCounter < ChronosphereDuration && ChronosphereActivated)
                ChronosphereCounter++;
            else
            {
                if (ChronosphereActivated)
                {
                    ChronosphereActivated = false;
                    ChronosphereCD = ChronosphereDefaultCD;
                    ChronosphereCounter = 0;
                }
            }
        }
        public override void UpdateCountdowns()
        {
            if (TimeWalkCD > 0)
                TimeWalkCD--;
            if (AoT_CD > 0)
                AoT_CD--;
            if (ChronosphereCD > 0)
                ChronosphereCD--;
        }
        protected override void UpdateCounters()
        {
            UpdateAoT();
            UpdateChronosphere();
        }
        public override void UpdatePerStep()
        {
            PreviousHP = HP;
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (TimeWalkCD > 0)
                msg += $"3 - {AbiNameOne} ({TimeWalkCD}) [{TimeWalkManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{TimeWalkManaPay}]\n";
            if (AoT_Activated)
                msg += $"4 - {AbiNameTwo} <<{AoT_Duration - AoT_Counter}>>\n";
            else
            {
                if (AoT_CD > 0)
                    msg += $"4 - {AbiNameTwo} ({AoT_CD}) [{AoT_ManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{AoT_ManaPay}]\n";
            }
            if (ChronosphereActivated)
                msg += $"5 - {AbiNameThree} <<{ChronosphereDuration - ChronosphereCounter + 1}>>\n";
            else
            {
                if (ChronosphereCD > 0)
                    msg += $"5 - {AbiNameThree} ({ChronosphereCD}) [{ChronosphereManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{ChronosphereManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(TimeWalkManaPay, TimeWalkCD))
                return false;
            HP = PreviousHP;
            MP -= TimeWalkManaPay;
            TimeWalkCD = TimeWalkDefaultCD;
            if (!target.HasImmuneToMagic)
                target.GetDamage(target.CompileMagicDamage(TimeWalkDamage));
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (AoT_Activated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(AoT_ManaPay, AoT_CD))
                return false;
            AoT_Activated = true;
            AttackSpeed += AoT_AttackSpeed;
            UpdateDPS();
            MP -= AoT_ManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(ChronosphereManaPay, ChronosphereCD))
                return false;
            MP -= ChronosphereManaPay;
            DisableFull(ChronosphereDuration, target);
            ChronosphereActivated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}
