using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes.Agility
{
    class Razor : IHero
    {
        // Ability One : Plasma Field
        public static string AbiNameOne = "Plasma Field";
        private float PlasmaFieldDamage = 650.0f;
        private float PlasmaFieldManaPay = 150.0f;
        private int PlasmaFieldCD = 0;
        private const int PlasmaFieldDefaultCD = 13;

        // Ability Two : Static Link
        public static string AbiNameTwo = "Static Link";
        private float StaticLinkStealDpsPercent = 35;
        private float StaticLinkLastDPS = 0.0f;
        private int StaticLinkDuration = 5;
        private int StaticLinkCounter = 0;
        private float StaticLinkManaPay = 180.0f;
        private int StaticLinkCD = 0;
        private const int StaticLinkDefaultCD = 20;
        private bool StaticLinkActivated = false;

        // Ability Passive : Electrical Hit
        public static string AbiNamePassive = "Electrical Hit";
        private float ElectricalHitDamage = 200.0f;
        private float ElectricalHitChance = 15.0f;

        // Ability Three : Eye of the Storm
        public static string AbiNameThree = "Eye of the Storm";
        private float EotS_DamagePerHit = 85.0f;
        private float EotS_FirstDamage = 250.0f;
        private float EotS_ArmorPenetratePerHit = 1.0f;
        private float EotS_ArmorPenetrateFirst = 5.0f;
        private float EotS_ManaPay = 300.0f;
        private int EotS_CD = 0;
        private const int EotS_DefaultCD = 35;
        private int EotS_Duration = 7;
        private int EotS_Counter = 0;
        private bool EotS_Activated = false;

        public Razor(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi ,intel, feat)
        {

        }
        public Razor(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new Razor(this, sender);
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (PlasmaFieldCD > 0)
                msg += $"3 - {AbiNameOne} ({PlasmaFieldCD}) [{PlasmaFieldManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{PlasmaFieldManaPay}]\n";
            if (StaticLinkActivated)
                msg += $"4 - {AbiNameTwo} <<{StaticLinkDuration - StaticLinkCounter + 1}>>\n";
            else
            {
                if (StaticLinkCD > 0)
                    msg += $"4 - {AbiNameTwo} ({StaticLinkCD}) [{StaticLinkManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{StaticLinkManaPay}]\n";
            }
            if (EotS_Activated)
                msg += $"5 - {AbiNameThree} <<{EotS_Duration - EotS_Counter + 1}>>\n";
            else
            {
                if (EotS_CD > 0)
                    msg += $"5 - {AbiNameThree} ({EotS_CD}) [{EotS_ManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{EotS_ManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override void UpdateCountdowns()
        {
            if (PlasmaFieldCD > 0)
                PlasmaFieldCD--;
            if (StaticLinkCD > 0)
                StaticLinkCD--;
            if (EotS_CD > 0)
                EotS_CD--;
        }
        protected override void UpdateCounters()
        {
            UpdateStaticLink();
            UpdateEotS();
        }
        private void UpdateEotS()
        {
            if (EotS_Counter < EotS_Duration && EotS_Activated)
            {
                EotS_Counter++;
                hero_target.GetDamage(EotS_DamagePerHit);
                if (!hero_target.HasImmuneToMagic)
                    hero_target.LoosenArmor(EotS_ArmorPenetratePerHit, 1);
            }
            else
            {
                if (EotS_Activated)
                {
                    EotS_Activated = false;
                    EotS_Counter = 0;
                    EotS_CD = EotS_DefaultCD;
                }
            }
        }
        private void UpdateStaticLink()
        {
            if (StaticLinkCounter < StaticLinkDuration && StaticLinkActivated)
                StaticLinkCounter++;
            else
            {
                if (StaticLinkActivated)
                {
                    StaticLinkCD = StaticLinkDefaultCD;
                    DPS = StaticLinkLastDPS;
                    StaticLinkActivated = false;
                    StaticLinkCounter = 0;
                }
            }
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(PlasmaFieldManaPay, PlasmaFieldCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= PlasmaFieldManaPay;
            PlasmaFieldCD = PlasmaFieldDefaultCD;
            target.GetDamage(target.CompileMagicDamage(PlasmaFieldDamage));
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(StaticLinkManaPay, StaticLinkCD))
                return false;
            MP -= StaticLinkManaPay;
            StaticLinkActivated = true;
            StaticLinkLastDPS = DPS;
            float stealed_damage = target.DPS / 100.0f * StaticLinkStealDpsPercent;
            DPS += stealed_damage;
            WeakAttack(StaticLinkDuration, stealed_damage, target);
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameTwo) + "\n" + lang.YouHaveWeakenedTheEnemy);
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameTwo)
                + "\n" + lang.TheEnemyHasWeakenedYou);
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(EotS_ManaPay, EotS_CD))
                return false;
            MP -= EotS_ManaPay;
            EotS_Activated = true;
            target.GetDamage(EotS_FirstDamage - target.Armor);
            target.LoosenArmor(EotS_ArmorPenetrateFirst, 1);
            hero_target = target;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameThree));
            return true;
        }
        public override async Task<bool> Attack(IHero target)
        {
            float damage = 0.0f;

            var attakerMessages = Sender.CreateMessageContainer();
            var excepterMessages = target.Sender.CreateMessageContainer();

            if (GetRandomNumber(1, 101) >= target.MissChance)
            {
                damage += this.DPS + this.AdditionalDamage;
                damage -= target.Armor;
                if (GetRandomNumber(1, 101) <= StunHitChance)
                {
                    target.StunCounter++;
                    damage += StunDamage;
                    attakerMessages.Add(lang => $"{lang.StunningHit}!");
                    excepterMessages.Add(lang => $"{lang.TheEnemyStunnedYou}");
                }
                if (GetRandomNumber(1, 101) <= ElectricalHitChance)
                {
                    damage += target.CompileMagicDamage(ElectricalHitDamage);
                    attakerMessages.Add(lang => $"{AbiNamePassive}!");
                    excepterMessages.Add(lang => $"{AbiNamePassive}!");
                }
                if (GetRandomNumber(1, 101) <= CriticalHitChance)
                {
                    damage *= CriticalHitMultiplier;
                    attakerMessages.Add(lang => $"{lang.CriticalHit}!");
                    excepterMessages.Add(lang => lang.TheEnemyDealtCriticalDamageToYou);
                }
                attakerMessages.Add(lang => lang.GetAttackedMessageForAttacker(Convert.ToInt32(damage)));
                excepterMessages.Add(lang => lang.GetAttackedMessageForExcepter(Convert.ToInt32(damage)));
            }
            else
            {
                attakerMessages.Add(lang => lang.YouMissedTheEnemy);
                excepterMessages.Add(lang => lang.TheEnemyMissedYou);
            }

            target.GetDamage(damage);
            HP += (damage / 100 * HpStealPercent) + HpStealAdditional;

            await attakerMessages.SendAsync();
            await excepterMessages.SendAsync();

            return true;
        }
    }
}
