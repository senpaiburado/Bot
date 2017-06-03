using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class Abaddon : IHero
    {
        private Users.User player_this = null;
        private Users.User player_enemy = null;
        private IHero hero_target = null;

        // Ability One : Mist Coil
        public string AbiNameOne = "Mist Coil";
        private float MistCoilPower = 300.0f;
        private float MistCoilManaPay = 95.0f;
        private int MistCoilCD = 0;
        private const int MistCoilDefaultCD = 11;

        // Ability Two : Aphotic Shield
        public string AbiNameTwo = "Aphotic Shield";
        private float AphoticShieldDamageAbsorption = 1000.0f;
        private float AphoticShieldManaPay = 230.0f;
        private int AphoticShieldCD = 0;
        private int AphoticShieldDefaultCD = 20;
        private bool AphoticShieldActivated = false;
        private int AphoticShieldDuration = 8;
        private int AphoticShieldCounter = 0;

        // Ability Passive : Curse of Avernus
        public string AbiNamePassive = "Curse of Avernus";
        private float CoA_AddArmor = 35.0f;
        private float CoA_DpsPerAttack = 20.0f;
        private int CoA_Counter = 0;
        private int CoA_Limit = 6;
        private float CoA_AdditionalDPS = 0.0f;

        // Ability Three : Borrowed Time
        public string AbiNameThree = "Borrowed Time";
        private bool BorrowedTimeActivated = false;
        private int BorrowedTimeCD = 0;
        private int BorrowedTimeDefaultCD = 35;
        private int BorrowedTimeDuration = 6;
        private int BorrowedTimeCounter = 0;

        public Abaddon(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Abaddon(IHero hero) : base(hero)
        {

        }

        public override IHero Copy()
        {
            return new Abaddon(this);
        }
        public override string GetMessageAbilitesList(Users.User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            msg += $"2 - {lang.Heal}\n";
            if (MistCoilCD > 0)
                msg += $"3 - {AbiNameOne} ({MistCoilCD}) [{MistCoilManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{MistCoilManaPay}]\n";
            if (AphoticShieldActivated)
                msg += $"4 - {AbiNameTwo} <<{AphoticShieldDuration - AphoticShieldCounter}>>\n";
            else
            {
                if (AphoticShieldCD > 0)
                    msg += $"4 - {AbiNameTwo} ({AphoticShieldCD}) [{AphoticShieldManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{AphoticShieldManaPay}]\n";
            }
            if (BorrowedTimeActivated)
                msg += $"5 - {AbiNameThree} <<{AphoticShieldDuration - AphoticShieldCounter}>>\n";
            else
            {
                if (BorrowedTimeCD > 0)
                    msg += $"5 - {AbiNameThree} ({BorrowedTimeCD})\n";
                else
                    msg += $"5 - {AbiNameThree}\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override void GetDamage(float value)
        {
            if (AphoticShieldActivated)
                AphoticShieldDamageAbsorption -= value;
            else if (BorrowedTimeActivated)
                HP += value;
            else
                base.GetDamage(value);
        }
        protected override void InitPassiveAbilities()
        {
            Armor += CoA_AddArmor;
        }
        protected override void UpdateCounters()
        {
            UpdateAphoticShield();
            UpdateCoA();
            UpdateBorrowedTime();
        }
        private void UpdateBorrowedTime()
        {
            if (BorrowedTimeCounter < BorrowedTimeDuration && BorrowedTimeActivated)
                BorrowedTimeCounter++;
            else
            {
                if (BorrowedTimeActivated)
                {
                    BorrowedTimeActivated = false;
                    BorrowedTimeCounter = 0;
                }
            }
        }
        private void UpdateCoA()
        {
            if (CoA_Counter < CoA_Limit)
            {
                DPS -= CoA_Counter * CoA_DpsPerAttack;
                CoA_Counter++;
                DPS += CoA_Counter * CoA_DpsPerAttack;
            }
            else
            {
                DPS -= CoA_Counter * CoA_DpsPerAttack;
                CoA_Counter = 0;
            }
        }
        public override void UpdateCountdowns()
        {
            if (MistCoilCD > 0)
                MistCoilCD--;
            if (AphoticShieldCD > 0)
                AphoticShieldCD--;
            if (BorrowedTimeCD > 0)
                BorrowedTimeCD--;
        }
        private async void UpdateAphoticShield()
        {
            if (AphoticShieldCounter < AphoticShieldDuration && AphoticShieldActivated)
                AphoticShieldCounter++;
            else
            {
                if (AphoticShieldActivated)
                {
                    AphoticShieldActivated = false;
                    AphoticShieldCounter = 0;
                    hero_target.GetDamage(AphoticShieldDamageAbsorption);
                    AphoticShieldDamageAbsorption = 1000.0f;
                    await bot.SendTextMessageAsync(player_this.ID, player_this.lang.ABADDON_AS_HasExploded);
                    await bot.SendTextMessageAsync(player_enemy.ID, player_enemy.lang.ABADDON_AS_HasExploded);
                }
                else
                {
                    AphoticShieldCounter = 0;
                    AphoticShieldDamageAbsorption = 1000.0f;
                    await bot.SendTextMessageAsync(player_this.ID, player_this.lang.ABADDON_AS_HasExploded);
                    await bot.SendTextMessageAsync(player_enemy.ID, player_enemy.lang.ABADDON_AS_HasExploded);
                }
            }
        }
        public override async Task<bool> UseAbilityOne(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (MP < MistCoilManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    MistCoilManaPay - MP)));
                return false;
            }
            if (MistCoilCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(MistCoilCD));
                return false;
            }
            float power = DPS - target.Armor;
            target.GetDamage(power);
            HP += power;
            MistCoilCD = MistCoilDefaultCD;
            MP -= MistCoilManaPay;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (AphoticShieldActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (MP < AphoticShieldManaPay)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageNeedMana(Convert.ToInt32(
                    AphoticShieldManaPay - MP)));
                return false;
            }
            if (AphoticShieldCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(AphoticShieldCD));
                return false;
            }
            AphoticShieldActivated = true;
            player_this = attackerUser;
            player_enemy = targetUser;
            hero_target = target;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouActivated(AbiNameTwo));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyActivated(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(Users.User attackerUser, Users.User targetUser, IHero target)
        {
            if (BorrowedTimeActivated)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (BorrowedTimeCD > 0)
            {
                await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageCountdown(BorrowedTimeCD));
                return false;
            }
            BorrowedTimeCD = BorrowedTimeDefaultCD;
            BorrowedTimeActivated = true;
            await bot.SendTextMessageAsync(attackerUser.ID, attackerUser.lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await bot.SendTextMessageAsync(targetUser.ID, targetUser.lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}
