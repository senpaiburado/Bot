using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class AlchemistHero : IHero
    {
        // Ability One : Acid Spray
        public string AbiNameOne = "Acid Spray";
        private float AcidSprayArmorPenetrate = 16.5f;
        private float AcidSprayDamage = 15.8f;
        private int AcidSprayDuration = 9;
        private float AcidSprayManaPay = 150.0f;
        private const int AcidSprayDefaultCD = 17;
        private int AcidSprayCD = 0;

        // Ability Two : Unstable Concoction
        public string AbiNameTwo => UnstableConcotionActivated ? "Unstable Concoction Throw" : "Unstable Concoction";
        private bool UnstableConcotionActivated = false;
        private bool UnstableConcoctionThrowed = false;
        private float UnstableConcoctionDamage = 405.55f;
        private int UnstableConctionTimeToThrow = 5;
        private int UnstableConctionCounter = 0;
        private int UnstableConcoctionCD = 0;
        private const int UnstableConcoctionDefaultCD = 25;
        private float UnstableConcoctionManaPay = 200.0f;

        public AlchemistHero(int str, int agi, int intel, MainFeature feat) : base("Alchemist", str, agi, intel,
            MainFeature.Str)
        {

        }

        private async Task<bool> UseUnstableConcoction(Users.User attackerUser, Users.User targetUser)
        {
            if (MP < UnstableConcoctionManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(MP)));
                return false;
            }
            if (UnstableConcoctionCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(AcidSprayCD));
                return false;
            }
            UnstableConcotionActivated = true;
            MP -= UnstableConcoctionManaPay;
            return true;
        }

        private async Task<bool> ThrowUnstableConcoction(Users.User attackerUser, Users.User targetUser, IHero target)
        {

            return true;
        }

        protected override void UpdateCounters()
        {

        }

        public override void UpdateCountdowns()
        {
            
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
            if (UnstableConcotionActivated)
                return await ThrowUnstableConcoction(attackerUser, targetUser, target);
            else
            {
                return await UseUnstableConcoction(attackerUser, targetUser);
            }
        }
    }
}
