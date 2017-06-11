using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Sniper : IHero
    {
        // Ability One : Shrapnel
        public static string AbiNameOne = "Shrapnel";
        protected float ShrapnelDamagePerStep = 100.0f;
        protected float ShrapnelAttackWeakening = 75.0f;
        protected float ShrapnelManaPay = 95.0f;
        protected int ShrapnelDuration = 8;
        protected int ShrapnelCD = 0;
        protected const int ShrapnelDefaultCD = 20;

        // Ability Passive : Headshot
        public static string AbiNamePassive = "Headshot";
        protected float HeadshotDamage = 90.0f;
        protected int HeadshotDisableDuration = 1;
        protected float HeadshotDisableChance = 25;

        // Ability Two : Machine Gun
        public static string AbiNameTwo = "Machine Gun";
        protected float MG_Damage = 350.0f;
        protected float MG_Chance = 65.0f;
        protected float MG_ManaPay = 220.0f;
        protected int MG_CD = 0;
        protected const int MG_DefaultCD = 23;

        // Ability Three : Assassinate
        public static string AbiNameThree = "Assassinate";
        protected float AssassinateDamage = 1000.0f;
        protected float AssassinateManaPay = 300.0f;
        protected int AssassinateCD = 0;
        protected const int AssassinateDefaultCD = 27;

        public Sniper(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Sniper(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new Sniper(this, sender);
        }
        public override void UpdateCountdowns()
        {
            if (ShrapnelCD > 0)
                ShrapnelCD--;
            if (MG_CD > 0)
                MG_CD--;
            if (AssassinateCD > 0)
                AssassinateCD--;
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (ShrapnelCD > 0)
                msg += $"3 - {AbiNameOne} ({ShrapnelCD}) [{ShrapnelManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{ShrapnelManaPay}]\n";
            if (MG_CD > 0)
                msg += $"4 - {AbiNameTwo} ({MG_CD}) [{MG_ManaPay}]\n";
            else
                msg += $"4 - {AbiNameTwo} [{MG_ManaPay}]\n";
            if (AssassinateCD > 0)
                msg += $"5 - {AbiNameThree} ({AssassinateCD}) [{AssassinateManaPay}]\n";
            else
                msg += $"5 - {AbiNameThree} [{AssassinateManaPay}]\n";
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            if (!await CheckManaAndCD(ShrapnelManaPay, ShrapnelCD))
                return false;
            hero_target = target;
            MP -= ShrapnelManaPay;
            ShrapnelCD = ShrapnelDefaultCD;
            target.GetDamageByDebuffs(target.CompileMagicDamage(ShrapnelDamagePerStep), ShrapnelDuration);
            WeakAttack(ShrapnelDuration, ShrapnelAttackWeakening, target);
            var hCon = Sender.CreateMessageContainer();
            var eCon = target.Sender.CreateMessageContainer();
            hCon.Add(lang => lang.YouHaveWeakenedTheEnemy);
            eCon.Add(lang => lang.TheEnemyHasWeakenedYou);
            hCon.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            eCon.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            await hCon.SendAsync();
            await eCon.SendAsync();
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(MG_ManaPay, MG_CD))
                return false;
            int count = 0;
            float damage = 0.0f;
            for (int i = 0; i < 5; i++)
            {
                if (GetRandomNumber(1, 101) <= MG_Chance)
                {
                    damage += MG_Damage - target.Armor;
                    count++;
                }
            }
            target.GetDamage(damage);
            MG_CD = MG_DefaultCD;
            MP -= MG_ManaPay;
            var hCon = Sender.CreateMessageContainer();
            var eCon = target.Sender.CreateMessageContainer();
            hCon.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            eCon.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            hCon.Add(lang => $"{lang.CountOfHits}: {count}.");
            eCon.Add(lang => $"{lang.CountOfHits}: {count}.");
            hCon.Add(lang => $"{lang.DamageString}: {damage}.");
            eCon.Add(lang => $"{lang.DamageString}: {damage}.");
            await hCon.SendAsync();
            await eCon.SendAsync();
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            if (!await CheckManaAndCD(AssassinateManaPay, AssassinateCD))
                return false;
            target.GetDamage(target.CompileMagicDamage(AssassinateDamage));
            MP -= AssassinateManaPay;
            AssassinateCD = AssassinateDefaultCD;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
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
                if (GetRandomNumber(1, 101) <= HeadshotDisableChance)
                {
                    damage += HeadshotDamage;
                    DisableFull(HeadshotDisableDuration, target);
                    attakerMessages.Add(x => $"{AbiNamePassive}!");
                    excepterMessages.Add(x => $"{AbiNamePassive}!");
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
