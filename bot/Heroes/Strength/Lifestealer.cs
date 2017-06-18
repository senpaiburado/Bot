using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Lifestealer : IHero
    {
        // Ability One : Rage
        public static string AbiNameOne = "Rage";
        private int RageDuration = 5;
        private int RageCounter = 0;
        private float RageAttackSpeed = 1.5f;
        private float RageManaPay = 130.0f;
        private int RageCD = 0;
        private const int RageDefaultCD = 9;
        private bool RageActivated = false;

        // Ability Passive : Feast
        public static string AbiNamePassive = "Feast";
        private float FeastHpStealPercent = 2.0f;

        // Ability Two : Open Wounds
        public static string AbiNameTwo = "Open Wounds";
        private float OW_HpStealPercentPrev = 0.0f;
        private float OW_HpStealPercentAdditional = 5f;
        private float OW_ManaPay = 155.0f;
        private int OW_CD = 0;
        private const int OW_DefaultCD = 15;
        private int OW_Counter = 0;
        private int OW_Duration = 5;
        private bool OW_Activated = false;

        // Hit of Monster
        public static string AbiNameThree = "Hit of Monster";
        private float HoM_Damage = 600.0f;
        private float HoM_CriticalChance = 50.0f;
        private float HoM_ManaPay = 220.0f;
        private int HoM_CD = 0;
        private const int HoM_DefaultCD = 23; 

        public Lifestealer(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Lifestealer(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        protected override void InitAdditional()
        {
            OW_HpStealPercentPrev = HpStealPercent;
        }
        public override IHero Copy(Sender sender)
        {
            return new Lifestealer(this, sender);
        }
        public override void UpdateCountdowns()
        {
            if (RageCD > 0)
                RageCD--;
            if (OW_CD > 0)
                OW_CD--;
            if (HoM_CD > 0)
                HoM_CD--;
        }
        protected override void UpdateCounters()
        {
            UpdateRage();
            UpdateOW();
        }
        private void UpdateRage()
        {
            if (RageCounter < RageDuration)
                RageCounter++;
            else
            {
                if (RageActivated)
                {
                    RageActivated = false;
                    AttackSpeed -= RageAttackSpeed;
                    UpdateDPS();
                    RageCD = RageDefaultCD;
                    RageCounter = 0;
                }
            }
        }
        private void UpdateOW()
        {
            if (OW_Counter < OW_Duration && OW_Activated)
                OW_Counter++;
            else
            {
                if (OW_Activated)
                {
                    OW_CD = OW_DefaultCD;
                    OW_Activated = false;
                    OW_Counter = 0;
                    HpStealPercent = OW_HpStealPercentPrev;
                }
            }
        }

        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (RageActivated)
                msg += $"3 - {AbiNameOne} <<{RageDuration - RageCounter + 1}>>\n";
            else
            {
                if (RageCD > 0)
                    msg += $"3 - {AbiNameOne} ({RageCD}) [{RageManaPay}]\n";
                else
                    msg += $"3 - {AbiNameOne} [{RageManaPay}]\n";
            }
            if (OW_Activated)
                msg += $"4 - {AbiNameTwo} <<{OW_Duration - OW_Counter + 1}>>\n";
            else
            {
                if (OW_CD > 0)
                    msg += $"4 - {AbiNameTwo} ({OW_CD}) [{OW_ManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{OW_ManaPay}]\n";
            }
            if (HoM_CD > 0)
                msg += $"5 - {AbiNameThree} ({HoM_CD}) [{HoM_ManaPay}]\n";
            else
                msg += $"5 - {AbiNameThree} [{HoM_ManaPay}]\n";
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (RageActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(RageManaPay, RageCD))
                return false;
            MP -= RageManaPay;
            AddImmuneToMagic(RageDuration);
            AttackSpeed += RageAttackSpeed;
            UpdateDPS();
            RageActivated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameOne) + "\n" + lang.YouHaveImmuneToMagic);
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameOne) + "\n" + lang.EnemyHasImmuneToMagic);
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (OW_Activated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(OW_ManaPay, OW_CD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= OW_ManaPay;
            HpStealPercent += OW_HpStealPercentAdditional;
            OW_Activated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(HoM_ManaPay, HoM_CD))
                return false;

            MP -= HoM_ManaPay;
            HoM_CD = HoM_DefaultCD;
            var HeroContainer = Sender.CreateMessageContainer();
            var EnemyContainer = target.Sender.CreateMessageContainer();
            if (GetRandomNumber(1, 100) > HoM_CriticalChance)
            {
                HeroContainer.Add(lang => lang.CriticalHit);
                EnemyContainer.Add(lang => lang.TheEnemyDealtCriticalDamageToYou);
                target.GetDamage(HoM_Damage * 2.0f);
            }
            else
                target.GetDamage(HoM_Damage);
            HeroContainer.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            EnemyContainer.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            await HeroContainer.SendAsync();
            await EnemyContainer.SendAsync();
            return true;
        }
        public override async Task<bool> Attack(IHero target)
        {
            if (!target.HasImmuneToMagic)
                HpStealAdditional = target.HP / 100 * FeastHpStealPercent;
            return await base.Attack(target);
        }
    }
}
