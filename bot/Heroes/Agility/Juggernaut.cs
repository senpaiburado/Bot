using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Juggernaut : IHero
    {
        // Ability One : Blade Fury
        public static string AbiNameOne = "Blade Fury";
        private float BladeFuryDamage = 80.0f;
        private int BladeFuryDuration = 5;
        private int BladeFuryCounter = 0;
        private bool BladeFuryActivated = false;
        private float BladeFuryManaPay = 180.0f;
        private int BladeFuryCD = 0;
        private const int BladeFuryDefaultCD = 17;

        // Ability Two : Healing Ward
        public static string AbiNameTwo = "Healing Ward";
        private float HealingWardHpRegeneration => MaxHP / 100.0f * 0.75f;
        private int HealingWardDuration = 7;
        private int HealingWardCounter = 0;
        private int HealingWardCD = 0;
        private const int HealingWardDefaultCD = 28;
        private float HealingWardManaPay = 150.0f;
        private bool HealingWardActivated = false;

        // Ability Passive : Blade Dance
        public static string AbiNamePassive = "Blade Dance";
        private float BladeDanceCriticalMult = 1.1f;
        private float BladeDanceCriticalChance = 15.0f;

        // Ability Three : Omnislash
        public static string AbiNameThree = "Omnislash";
        private float OmnislashDamage = 350.0f;
        private float OmnislashManaPay = 300.0f;
        private int OmnislashDuration = 5;
        private int OmnislashCD = 0;
        private int OmnislashDefaultCD = 30;

        public Juggernaut(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Juggernaut(IHero hero, Sender sender)
            : base(hero, sender)
        {

        }

        protected override void InitPassiveAbilities()
        {
            CriticalHitMultiplier += BladeDanceCriticalMult;
            CriticalHitChance += BladeDanceCriticalChance;
        }

        public override IHero Copy(Sender sender)
        {
            return new Juggernaut(this, sender);
        }

        private void UpdateBladeFury()
        {
            if (BladeFuryCounter < BladeFuryDuration && BladeFuryActivated)
                BladeFuryCounter++;
            else
            {
                if (BladeFuryActivated)
                {
                    BladeFuryActivated = false;
                    BladeFuryCounter = 0;
                }
            }
        }

        private void UpdateHealingWard()
        {
            if (HealingWardCounter < HealingWardDuration && HealingWardActivated)
                HealingWardCounter++;
            else
            {
                if (HealingWardActivated)
                {
                    HealingWardActivated = false;
                    HealingWardCounter = 0;
                    HPregen -= HealingWardHpRegeneration;
                }
            }
        }

        public override void UpdateCountdowns()
        {
            if (BladeFuryCD > 0)
                BladeFuryCD--;
            if (HealingWardCD > 0)
                HealingWardCD--;
            if (OmnislashCD > 0)
                OmnislashCD--;
        }

        protected override void UpdateCounters()
        {
            UpdateBladeFury();
            UpdateHealingWard();
        }

        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (BladeFuryActivated)
                msg += $"3 - {AbiNameOne} <<{BladeFuryDuration - BladeFuryCounter + 1}>>\n";
            else
            {
                if (BladeFuryCD > 0)
                    msg += $"3 - {AbiNameOne} ({BladeFuryCD}) [{BladeFuryManaPay}]\n";
                else
                    msg += $"3 - {AbiNameOne} [{BladeFuryManaPay}]\n";
            }
            if (HealingWardActivated)
                msg += $"4 - {AbiNameTwo} <<{HealingWardDuration - HealingWardCounter + 1}>>\n";
            else
            {
                if (HealingWardCD > 0)
                    msg += $"4 - {AbiNameTwo} ({HealingWardCD}) [{HealingWardManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{HealingWardManaPay}]\n";
            }
            if (OmnislashCD > 0)
                msg += $"5 - {AbiNameThree} ({OmnislashCD}) [{OmnislashManaPay}]\n";
            else
                msg += $"5 - {AbiNameThree} [{OmnislashManaPay}]\n";
            msg += $"{lang.SelectAbility}:";
            return msg;
        }

        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (BladeFuryActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(BladeFuryManaPay, BladeFuryCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            BladeFuryActivated = true;
            BladeFuryCD = BladeFuryDefaultCD;
            MP -= BladeFuryManaPay;
            target.GetDamageByDebuffs(target.CompileMagicDamage(BladeFuryDamage), BladeFuryDuration);
            AddImmuneToMagic(BladeFuryDuration);
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne) + "\n" + lang.YouHaveImmuneToMagic);
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne) +
                "\n" + lang.EnemyHasImmuneToMagic);
            return true;
        }

        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (BladeFuryActivated)
            {
                await Sender.SendAsync(lang => lang.GetMessageYouCantUseAbilityWhileAnotherWorks
                    (AbiNameOne));
                return false;
            }
            if (HealingWardActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(HealingWardManaPay, HealingWardCD))
                return false;
            HealingWardActivated = true;
            HPregen += HealingWardHpRegeneration;
            HealingWardCD = HealingWardDefaultCD;
            MP -= HealingWardManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (BladeFuryActivated)
            {
                await Sender.SendAsync(lang => lang.GetMessageYouCantUseAbilityWhileAnotherWorks
                    (AbiNameOne));
                return false;
            }
            if (!await CheckManaAndCD(OmnislashManaPay, OmnislashCD))
                return false;
            OmnislashCD = OmnislashDefaultCD;
            MP -= OmnislashManaPay;
            target.GetDamageByDebuffs(OmnislashDamage - target.Armor, OmnislashDuration);
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}