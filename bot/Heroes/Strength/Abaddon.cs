using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Abaddon : IHero
    {

        // Ability One : Mist Coil
        public static string AbiNameOne = "Mist Coil";
        private float MistCoilPower = 300.0f;
        private float MistCoilManaPay = 95.0f;
        private int MistCoilCD = 0;
        private const int MistCoilDefaultCD = 11;

        // Ability Two : Aphotic Shield
        public static string AbiNameTwo = "Aphotic Shield";
        private float AphoticShieldDamageAbsorption = 1000.0f;
        private float AphoticShieldManaPay = 230.0f;
        private int AphoticShieldCD = 0;
        private int AphoticShieldDefaultCD = 20;
        private bool AphoticShieldActivated = false;
        private int AphoticShieldDuration = 8;
        private int AphoticShieldCounter = 0;

        // Ability Passive : Curse of Avernus
        public static string AbiNamePassive = "Curse of Avernus";
        private float CoA_AddArmor = 35.0f;
        private float CoA_DpsPerAttack = 20.0f;
        private int CoA_Counter = 0;
        private int CoA_Limit = 6;

        // Ability Three : Borrowed Time
        public static string AbiNameThree = "Borrowed Time";
        private bool BorrowedTimeActivated = false;
        private int BorrowedTimeCD = 0;
        private int BorrowedTimeDefaultCD = 35;
        private int BorrowedTimeDuration = 6;
        private int BorrowedTimeCounter = 0;

        public Abaddon(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Abaddon(IHero hero, Sender sender)
            : base(hero, sender)
        {

        }

        public override IHero Copy(Sender sender)
        {
            return new Abaddon(this, sender);
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (MistCoilCD > 0)
                msg += $"3 - {AbiNameOne} ({MistCoilCD}) [{MistCoilManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{MistCoilManaPay}]\n";
            if (AphoticShieldActivated)
                msg += $"4 - {AbiNameTwo} <<{AphoticShieldDuration - AphoticShieldCounter + 1}>>\n";
            else
            {
                if (AphoticShieldCD > 0)
                    msg += $"4 - {AbiNameTwo} ({AphoticShieldCD}) [{AphoticShieldManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{AphoticShieldManaPay}]\n";
            }
            if (BorrowedTimeActivated)
                msg += $"5 - {AbiNameThree} <<{BorrowedTimeDuration - BorrowedTimeCounter + 1}>>\n";
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
            {
                if (AphoticShieldDamageAbsorption > value)
                    AphoticShieldDamageAbsorption -= value;
                else
                {
                    float temp = value - AphoticShieldDamageAbsorption;
                    AphoticShieldDamageAbsorption = -1f;
                    base.GetDamage(temp);
                }
            }
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
                    BorrowedTimeCD = BorrowedTimeDefaultCD;
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
            if (AphoticShieldCounter < AphoticShieldDuration && AphoticShieldActivated && Convert.ToInt32(
                AphoticShieldDamageAbsorption) > 0)
                AphoticShieldCounter++;
            else
            {
                if (AphoticShieldActivated)
                {
                    AphoticShieldCounter = 0;
                    if (AphoticShieldDamageAbsorption > 0.0f && !hero_target.HasImmuneToMagic)
                        hero_target.GetDamage(AphoticShieldDamageAbsorption);
                    AphoticShieldActivated = false;
                    AphoticShieldDamageAbsorption = 1000.0f;
                    await Sender.SendAsync(lang => lang.ABADDON_AS_HasExploded);
                    await hero_target.Sender.SendAsync(lang => lang.ABADDON_AS_HasExploded);
                }
            }
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(MistCoilManaPay, MistCoilCD))
                return false;
            float power = MistCoilPower - target.Armor;
            target.GetDamage(power);
            HP += power;
            MistCoilCD = MistCoilDefaultCD;
            MP -= MistCoilManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (AphoticShieldActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(AphoticShieldManaPay, AphoticShieldCD))
                return false;
            AphoticShieldActivated = true;
            if (hero_target == null)
                hero_target = target;
            AphoticShieldCD = AphoticShieldDefaultCD;
            MP -= AphoticShieldManaPay;
            AphoticShieldDamageAbsorption = 1000.0f;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (BorrowedTimeActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(0, BorrowedTimeCD))
                return false;
            BorrowedTimeActivated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}
