using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class Juggernaut : IHero
    {
        // Ability One : Blade Fury
        public string AbiNameOne = "Blade Fury";
        private float BladeFuryDamage = 80.0f;
        private int BladeFuryDuration = 5;
        private int BladeFuryCounter = 0;
        private bool BladeFuryActivated = false;
        private float BladeFuryManaPay = 180.0f;
        private int BladeFuryCD = 0;
        private const int BladeFuryDefaultCD = 17;

        // Ability Two : Healing Ward
        public string AbiNameTwo = "Healing Ward";
        private float HealingWardHpRegeneration => MaxHP / 100.0f * 0.75f;
        private int HealingWardDuration = 7;
        private int HealingWardCounter = 0;
        private int HealingWardCD = 0;
        private const int HealingWardDefaultCD = 28;
        private float HealingWardManaPay = 150.0f;
        private bool HealingWardActivated = false;

        // Ability Passive : Blade Dance
        public string AbiNamePassive = "Blade Dance";
        private float BladeDanceCriticalMult = 1.2f;
        private float BladeDanceCriticalChance = 25.0f;

        // Ability Three : Omnislash
        public string AbiNameThree = "Omnislash";
        private float OmnislashDamage = 350.0f;
        private float OmnislashManaPay = 300.0f;
        private int OmnislashDuration = 5;
        private int OmnislashCD = 0;
        private int OmnislashDefaultCD = 30;

        public Juggernaut(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Juggernaut(IHero hero) : base(hero)
        {

        }

        protected override void InitPassiveAbilities()
        {
            CriticalHitMultiplier += BladeDanceCriticalMult;
            BladeDanceCriticalChance += BladeDanceCriticalChance;
        }

        public override IHero Copy()
        {
            return new Juggernaut(this);
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

        public override string GetMessageAbilitesList(Users.User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (BladeFuryActivated)
                msg += $"3 - {AbiNameOne} <<{BladeFuryDuration - BladeFuryCounter}>>\n";
            else
            {
                if (BladeFuryCD > 0)
                    msg += $"3 - {AbiNameOne} ({BladeFuryCD}) [{BladeFuryManaPay}]\n";
                else
                    msg += $"3 - {AbiNameOne} [{BladeFuryManaPay}]\n";
            }
            if (HealingWardActivated)
                msg += $"4 - {AbiNameTwo} <<{HealingWardDuration - HealingWardCounter}>>\n";
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

        public override async Task<bool> UseAbilityOne(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (BladeFuryActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.AbilityIsAlreadyActivated);
            }
            if (MP < BladeFuryManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    BladeFuryManaPay - MP)));
                return false;
            }
            if (BladeFuryCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(BladeFuryCD));
                return false;
            }
            BladeFuryActivated = true;
            BladeFuryCD = BladeFuryDefaultCD;
            MP -= BladeFuryManaPay;
            target.GetDamageByDebuffs(BladeFuryDamage, BladeFuryDuration);
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (BladeFuryActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouCantUseAbilityWhileAnotherWorks
                    (AbiNameOne));
                return false;
            }
            if (HealingWardActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (MP < HealingWardManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    HealingWardManaPay - MP)));
                return false;
            }
            if (HealingWardCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(HealingWardCD));
                return false;
            }
            HealingWardActivated = true;
            HPregen += HealingWardHpRegeneration;
            HealingWardCD = HealingWardDefaultCD;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (BladeFuryActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouCantUseAbilityWhileAnotherWorks
                    (AbiNameOne));
                return false;
            }
            if (MP < OmnislashManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    OmnislashManaPay - MP)));
                return false;
            }
            if (HealingWardCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(OmnislashCD));
                return false;
            }
            OmnislashCD = OmnislashDefaultCD;
            MP -= OmnislashManaPay;
            target.GetDamageByDebuffs(OmnislashDamage, OmnislashDuration);
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}