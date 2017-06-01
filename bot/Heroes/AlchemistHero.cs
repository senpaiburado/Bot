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
        private int AcidSprayDuration = 9;
        private float AcidSprayManaPay = 150.0f;
        private const int AcidSprayDefaultCD = 17;
        private int AcidSprayCD = 0;

        public AlchemistHero(int str, int agi, int intel, MainFeature feat) : base("Alchemist", str, agi, intel,
            MainFeature.Str)
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
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        override public async Task<bool> UseAbilityTwo(Users.User attackerUser, Users.User targetUser, IHero target)
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
            return true;
        }
    }
}
