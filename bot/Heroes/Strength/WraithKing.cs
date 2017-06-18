using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class WraithKing : IHero
    {
        // Ability One : Wraithfire Blast
        public static string AbiNameOne = "Wraithfire Blast";
        private float WB_Damage = 300.0f;
        private float WB_DamagePerStep = 150.0f;
        private float WB_ManaPay = 200.0f;
        private int WB_Duration = 2;
        private int WB_CD = 0;
        private const int WB_DefaultCD = 18;

        // Passive Ability One : Vampiric Aura
        public static string AbiPassiveNameOne = "Vampiric Aura";
        private float VA_HpStealByDamagePercent = 10;

        // Passive Ability Two : Mortal Strike
        public static string AbiPassiveNameTwo = "Mortal Strike";
        private float MS_CriticalHitMultiplier = 0.45f;
        private float MS_CriticalHitChance = 10.0f;

        // Ability Two : Armor Fortification
        public static string AbiNameTwo = "Armor Fortification";
        private float AF_AdditionalArmor = 50.0f;
        private float AF_ManaPay = 130.0f;
        private int AF_CD = 0;
        private const int AF_DefaultCD = 17;
        private int AF_Duration = 5;
        private int AF_Counter = 0;
        private bool AF_Activated = false;

        // Ability Three : King's Luck
        public static string AbinameThree = "King's Luck";
        private float KL_ManaPay = 350.0f;
        private int KL_CD = 0;
        private const int KL_DefaultCD = 40;

        public WraithKing(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public WraithKing(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new WraithKing(this, sender);
        }
        protected override void InitPassiveAbilities()
        {
            HpStealPercent += VA_HpStealByDamagePercent;
            CriticalHitChance += MS_CriticalHitChance;
            CriticalHitMultiplier += MS_CriticalHitMultiplier;
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (WB_CD > 0)
                msg += $"3 - {AbiNameOne} ({WB_CD}) [{WB_ManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{WB_ManaPay}]\n";
            if (AF_Activated)
                msg += $"4 - {AbiNameTwo} <<{AF_Duration - AF_Counter + 1}>>\n";
            else
            {
                if (AF_CD > 0)
                    msg += $"4 - {AbiNameTwo} ({AF_CD}) [{AF_ManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{AF_ManaPay}]\n";
            }
            if (KL_CD > 0)
                msg += $"5 - {AbinameThree} ({KL_CD}) [{KL_ManaPay}]\n";
            else
                msg += $"5 - {AbinameThree} [{KL_ManaPay}]\n";
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override void UpdateCountdowns()
        {
            if (WB_CD > 0)
                WB_CD--;
            if (AF_CD > 0)
                AF_CD--;
            if (KL_CD > 0)
                KL_CD--;
        }
        protected override void UpdateCounters()
        {
            UpdateArmorFortification();
        }
        private void UpdateArmorFortification()
        {
            if (AF_Counter < AF_Duration && AF_Activated)
                AF_Counter++;
            else
            {
                if (AF_Activated)
                {
                    AF_Activated = false;
                    AF_Counter = 0;
                    AF_CD = AF_DefaultCD;
                    Armor -= AF_AdditionalArmor;
                }
            }
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            if (!await CheckManaAndCD(WB_ManaPay, WB_CD))
                return false;
            WB_CD = WB_DefaultCD;
            MP -= WB_ManaPay;
            target.GetDamage(target.CompileMagicDamage(WB_Damage));
            target.GetDamageByDebuffs(target.CompileMagicDamage(WB_DamagePerStep), WB_Duration);
            target.StunCounter += WB_Duration;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(AF_ManaPay, AF_CD))
                return false;
            AF_Activated = true;
            Armor += AF_AdditionalArmor;
            MP -= AF_ManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            if (!await CheckManaAndCD(KL_ManaPay, KL_CD))
                return false;
            MP -= KL_ManaPay;
            KL_CD = KL_DefaultCD;
            float damage = target.CompileMagicDamage(GetRandomNumber(100, 1001));
            target.GetDamage(damage);
            HP += damage;
            var H_Container = Sender.CreateMessageContainer();
            var E_Container = target.Sender.CreateMessageContainer();
            H_Container.Add(lang => lang.GetMessageYouHaveUsedAbility(AbinameThree));
            E_Container.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbinameThree));
            H_Container.Add(lang => $"{lang.DamageString}: {damage}.");
            E_Container.Add(lang => $"{lang.DamageString}: {damage}.");
            await H_Container.SendAsync();
            await E_Container.SendAsync();
            return true;
        }
    }
}
