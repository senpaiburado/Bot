using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Silencer : IHero
    {
        // Ability One : Arcane Curse
        public static string AbiNameOne = "Arcane Curse";
        private float ArcaneCurseDamage = 60.0f;
        private float ArcaneCurseSilenceChance = 35.0f;
        private int ArcaneCurseSilenceDuration = 4;
        private int ArcaneCurseDuration = 10;
        private int ArcaneCurseCD = 0;
        private const int ArcaneCurseDefaultCD = 22;
        private float ArcaneCurseManaPay = 180.0f;

        // Ability Two : Last Word
        public static string AbiNameTwo = "Last Word";
        private float LastWordDamage = 900.0f;
        private float LastWordManaPay = 300.0f;
        private int LastWordSilenceDuration = 6;
        private int LastWordCD = 0;
        private const int LastWordDefaultCD = 20;

        // Ability Passive: Glaives of Wisdom
        public static string AbiNamePassive = "Glaives of Wisdom";
        private float GoW_Percent = 35;

        // Ability Three - Global Silence
        public static string AbiNameThree = "Global Silence";
        private int GlobalSilenceCD = 0;
        private const int GlobalSilenceDefaultCD = 30;
        private float GlobalSilenceManaPay = 390.0f;
        private int GlobalSilenceDuration = 10;
        private int GlobalSilenceCounter = 0;
        private bool GlobalSilenceActivated = false;

        public Silencer(string name, int str, int agi, int intel, MainFeature feat) : base (name, str, agi, intel, feat)
        {

        }
        public Silencer(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override void UpdateCountdowns()
        {
            if (ArcaneCurseCD > 0)
                ArcaneCurseCD--;
            if (LastWordCD > 0)
                LastWordCD--;
            if (GlobalSilenceCD > 0)
                GlobalSilenceCD--;
        }
        protected override void UpdateCounters()
        {
            if (GlobalSilenceCounter < GlobalSilenceDuration && GlobalSilenceActivated)
                GlobalSilenceCounter++;
            else
            {
                if (GlobalSilenceActivated)
                {
                    GlobalSilenceActivated = false;
                    GlobalSilenceCounter = 0;
                    GlobalSilenceCD = GlobalSilenceDefaultCD;
                }
            }
        }
        protected override void InitPassiveAbilities()
        {
            AdditionalDamage = Intelligence / 100.0f * GoW_Percent;
        }
        public override IHero Copy(Sender sender)
        {
            return new Silencer(this, sender);
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (ArcaneCurseCD > 0)
                msg += $"3 - {AbiNameOne} ({ArcaneCurseCD}) [{ArcaneCurseManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{ArcaneCurseManaPay}]\n";
            if (LastWordCD > 0)
                msg += $"4 - {AbiNameTwo} ({LastWordCD}) [{LastWordManaPay}]\n";
            else
                msg += $"4 - {AbiNameTwo} [{LastWordManaPay}]\n";
            if (GlobalSilenceActivated)
                msg += $"5 - {AbiNameThree} <<{GlobalSilenceDuration - GlobalSilenceCounter + 1}>>\n";
            else
            {
                if (GlobalSilenceCD > 0)
                    msg += $"5 - {AbiNameThree} ({GlobalSilenceCD}) [{GlobalSilenceManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{GlobalSilenceManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(ArcaneCurseManaPay, ArcaneCurseCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= ArcaneCurseManaPay;
            ArcaneCurseCD = ArcaneCurseDefaultCD;
            target.GetDamageByDebuffs(target.CompileMagicDamage(ArcaneCurseDamage), ArcaneCurseDuration);
            var HeroContainer = Sender.CreateMessageContainer();
            var EnemyContainter = target.Sender.CreateMessageContainer();
            if (GetRandomNumber(1, 100) <= ArcaneCurseSilenceChance)
            {
                Silence(ArcaneCurseSilenceDuration, target);
                HeroContainer.Add(lang => lang.EnemyIsSilenced);
                EnemyContainter.Add(lang => lang.YouAreSilenced);
            }
            HeroContainer.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            EnemyContainter.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            await HeroContainer.SendAsync();
            await EnemyContainter.SendAsync();
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(LastWordManaPay, LastWordCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= LastWordManaPay;
            LastWordCD = LastWordDefaultCD;
            target.GetDamage(target.CompileMagicDamage(LastWordDamage));
            Silence(LastWordSilenceDuration, target);
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(GlobalSilenceManaPay, GlobalSilenceCD))
                return false;
            MP -= GlobalSilenceDuration;
            Silence(GlobalSilenceDuration, target);
            GlobalSilenceActivated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }

        public override async Task<bool> Attack(IHero target)
        {
            if (!target.HasImmuneToMagic)
            {
                AdditionalDamage = target.CompileMagicDamage(AdditionalDamage);
                InitPassiveAbilities();
            }
            else
                AdditionalDamage = 0.0f;
            return await base.Attack(target);
        }
    }
}
