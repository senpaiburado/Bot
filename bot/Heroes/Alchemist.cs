using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class Alchemist : IHero
    {
        Users.User temp_playerOne = null;
        Users.User temp_playerTwo = null;
        IHero temp_targetHero = null;

        // Ability One : Acid Spray
        public string AbiNameOne = "Acid Spray";
        private float AcidSprayArmorPenetrate = 16.5f;
        private float AcidSprayDamage = 15.8f;
        private int AcidSprayDuration = 9;
        private float AcidSprayManaPay = 150.0f;
        private const int AcidSprayDefaultCD = 17;
        private int AcidSprayCD = 0;

        // Ability Two : Unstable Concoction
        public string AbiNameTwo => UnstableConcoctionActivated ? "Unstable Concoction Throw" : "Unstable Concoction";
        private bool UnstableConcoctionActivated = false;
        private float UnstableConcoctionDamage = 405.55f;
        private int UnstableConcoctionTimeToThrow = 7;
        private int UnstableConcoctionCounter = 0;
        private int UnstableConcoctionCD = 0;
        private const int UnstableConcoctionDefaultCD = 25;
        private float UnstableConcoctionManaPay = 200.0f;

        // Passive ability: GreevilsPower
        public string AbiNamePassive = "Greevils Power";
        private int GreevilsPowerTime = 5;
        private int GreevilsPowerCounter = 0;
        private float GreevilsPowerDamage = 5.0f;

        // Ability Three : Chemical Rage
        public string AbiNameThree = "Chemical Rage";
        private bool ChemicalRageActivated = false;
        private int ChemicalRageCounter = 0;
        private int ChemicalRageDuration = 15;
        private const int ChemicalRageDefaultCD = 30;
        private int ChemicalRageCD = 0;
        private float ChemicalRageHpRegeneration = 25.0f;
        private float ChemicalRageMpRegeneration = 10.0f;
        private float ChemicalRageManaPay = 300.0f;
        private float ChemicalRageAdditionalAttackSpeed = 2.5f;

        public Alchemist(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }

        public Alchemist(IHero hero) : base(hero)
        {

        }

        private async Task<bool> UseUnstableConcoction(Users.User attackerUser, Users.User targetUser)
        {
            if (MP < UnstableConcoctionManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    UnstableConcoctionManaPay - MP)));
                return false;
            }
            if (UnstableConcoctionCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(UnstableConcoctionCD));
                return false;
            }
            MP -= UnstableConcoctionManaPay;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            UnstableConcoctionActivated = true;
            return true;
        }

        private async Task<bool> ThrowUnstableConcoction(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            string ForYouMessage = $"{attackerUser.lang.ALCHEMIST_YouHaveThrownUC}\n";
            string ForEnemyMessage = $"{targetUser.lang.ALCHEMIST_TheEnemyHasThrownUC}\n";
            UnstableConcoctionActivated = false;
            UnstableConcoctionCD = UnstableConcoctionDefaultCD;
            if (base.GetRandomNumber(1, 100) > 15)
            {
                target.GetDamage(UnstableConcoctionDamage);
                target.StunCounter += UnstableConcoctionCounter;

                ForYouMessage += $"{attackerUser.lang.ALCHEMIST_UC_HasExploded}\n";
                ForEnemyMessage += $"{targetUser.lang.ALCHEMIST_UC_HasExploded}\n";

                if (target == this)
                {
                    ForYouMessage += $"{attackerUser.lang.YouStunnedYourself}";
                    ForEnemyMessage += $"{targetUser.lang.TheEnemyHasStunnedItself}";
                }
                else
                {
                    ForYouMessage += $"{attackerUser.lang.YouStunnedEnemy}";
                    ForEnemyMessage += $"{targetUser.lang.TheEnemyStunnedYou}";
                }
            }
            else
            {
                ForYouMessage += attackerUser.lang.YouMissedTheEnemy;
                ForEnemyMessage += targetUser.lang.TheEnemyMissedYou;
            }

            await bot.SendTextMessageAsync(attackerUser.ID, ForYouMessage);
            await bot.SendTextMessageAsync(targetUser.ID, ForEnemyMessage);
            UnstableConcoctionCounter = 0;
            temp_playerOne = null;
            temp_playerTwo = null;
            temp_targetHero = null;
            return true;
        }

        protected override void UpdateCounters()
        {
            UpdateUnstableConcoction();
            UpdateGreevilsPower();
            UpdateChemicalRage();
        }

        public override void UpdateCountdowns()
        {
            if (AcidSprayCD > 0)
                AcidSprayCD--;
            if (UnstableConcoctionCD > 0)
                UnstableConcoctionCD--;
            if (ChemicalRageCD > 0)
                ChemicalRageCD--;
        }

        private void UpdateGreevilsPower()
        {
            if (GreevilsPowerCounter < GreevilsPowerTime)
                GreevilsPowerCounter++;
            else
            {
                DPS += GreevilsPowerDamage;
                GreevilsPowerCounter = 0;
            }
        }

        private async void UpdateUnstableConcoction()
        {
            if (UnstableConcoctionCounter < UnstableConcoctionTimeToThrow && UnstableConcoctionActivated)
                UnstableConcoctionCounter++;
            else
            {
                if (UnstableConcoctionActivated)
                {
                    UnstableConcoctionActivated = false;
                    UnstableConcoctionCounter = 0;
                    await ThrowUnstableConcoction(temp_playerOne, temp_playerTwo, this);
                }
            }
        }

        private void UpdateChemicalRage()
        {
            if (ChemicalRageCounter < ChemicalRageDuration && ChemicalRageActivated)
                ChemicalRageCounter++;
            else
            {
                if (ChemicalRageActivated)
                {
                    ChemicalRageActivated = false;
                    ChemicalRageCounter = 0;
                    HPregen -= ChemicalRageHpRegeneration;
                    MPregen -= ChemicalRageMpRegeneration;
                    AttackSpeed -= ChemicalRageAdditionalAttackSpeed;
                    UpdateDPS();
                }
            }
        }

        public override string GetMessageAbilitesList(Users.User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (AcidSprayCD > 0)
                msg += $"3 - {AbiNameOne} ({AcidSprayCD}) [{AcidSprayManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{AcidSprayManaPay}]\n";
            if (UnstableConcoctionActivated)
                msg += $"4 - {AbiNameTwo} <<{UnstableConcoctionTimeToThrow - UnstableConcoctionCounter}>>\n";
            else
            {
                if (UnstableConcoctionCD > 0)
                    msg += $"4 - {AbiNameTwo} ({UnstableConcoctionCD}) [{UnstableConcoctionManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{UnstableConcoctionManaPay}]\n";
            }
            if (ChemicalRageActivated)
                msg += $"5 - {AbiNameThree} <<{ChemicalRageDuration - ChemicalRageCounter}>>\n";
            else
            {
                if (ChemicalRageCD > 0)
                    msg += $"5 - {AbiNameThree} ({ChemicalRageCD}) [{ChemicalRageManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{ChemicalRageManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }

        public override IHero Copy()
        {
            return new Alchemist(this);
        }

        override public async Task<bool> UseAbilityOne(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (MP < AcidSprayManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    AcidSprayManaPay - MP)));
                return false;
            }
            if (AcidSprayCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(AcidSprayCD));
                return false;
            }
            target.LoosenArmor(AcidSprayArmorPenetrate, AcidSprayDuration);
            target.GetDamageByDebuffs(AcidSprayDamage, AcidSprayDuration);
            MP -= AcidSprayManaPay;
            AcidSprayCD = AcidSprayDefaultCD;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        override public async Task<bool> UseAbilityTwo(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (UnstableConcoctionActivated)
                return await ThrowUnstableConcoction(attackerUser, targetUser, target);
            else
            {
                temp_playerOne = attackerUser;
                temp_playerTwo = targetUser;
                temp_targetHero = target;
                return await UseUnstableConcoction(attackerUser, targetUser);
            }
        }
        public override async Task<bool> UseAbilityThree(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (MP < ChemicalRageManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    ChemicalRageManaPay - MP)));
                return false;
            }
            if (ChemicalRageCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(ChemicalRageCD));
                return false;
            }
            if (ChemicalRageActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.AbilityIsAlreadyActivated);
                return false;
            }
            MP -= ChemicalRageManaPay;
            ChemicalRageCD = ChemicalRageDefaultCD;
            ChemicalRageActivated = true;
            HPregen += ChemicalRageHpRegeneration;
            MPregen += ChemicalRageMpRegeneration;
            AttackSpeed += ChemicalRageAdditionalAttackSpeed;
            UpdateDPS();
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}