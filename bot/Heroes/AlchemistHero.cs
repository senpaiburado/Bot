using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class AlchemistHero : IHero
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
        private int UnstableConctionTimeToThrow = 5;
        private int UnstableConctionCounter = 0;
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

        public AlchemistHero(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }

        private async Task<bool> UseUnstableConcoction(Users.User attackerUser, Users.User targetUser)
        {
            if (MP < UnstableConcoctionManaPay)
            {
                temp_playerOne = null;
                temp_playerTwo = null;
                temp_targetHero = null;
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(MP)));
                return false;
            }
            if (UnstableConcoctionCD > 0)
            {
                temp_playerOne = null;
                temp_playerTwo = null;
                temp_targetHero = null;
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(AcidSprayCD));
                return false;
            }
            UnstableConcoctionActivated = true;
            MP -= UnstableConcoctionManaPay;
            return true;
        }

        private async Task<bool> ThrowUnstableConcoction(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            string ForYouMessage = $"{attackerUser.lang.ALCHEMIST_YouHaveThrownUC}\n";
            string ForEnemyMessage = $"{targetUser.lang.ALCHEMIST_TheEnemyHasThrownUC}\n";
            UnstableConcoctionActivated = false;
            UnstableConcoctionCD = UnstableConcoctionDefaultCD;
            UnstableConctionCounter = 0;
            if (base.GetRandomNumber(1, 100) > 15)
            {
                target.GetDamage(UnstableConcoctionDamage);
                target.StunCounter += UnstableConctionCounter;

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
            if (UnstableConctionCounter < UnstableConctionTimeToThrow && UnstableConcoctionActivated)
                UnstableConctionCounter++;
            else
            {
                if (UnstableConcoctionActivated)
                {
                    UnstableConcoctionActivated = false;
                    UnstableConctionCounter = 0;
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
                }
            }
        }

        public override string GetMessageAbilitesList(Users.User user)
        {
            string[] msg =
            {
                $"1 - {user.lang.AttackString}",
                $"2 - {user.lang.Heal}",
                $"3 - {AbiNameOne}",
                $"4 - {AbiNameTwo}",
                $"5 - {AbiNameThree}"
            };
            return string.Join("\n", msg);
        }

        override public async Task<bool> UseAbilityOne(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (MP < AcidSprayManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(MP)));
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
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(MP)));
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
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}
