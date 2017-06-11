using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class DragonKnight : IHero
    {
        // Ability one : Breathe Fire
        public static string AbiNameOne = "Breathe Fire";
        private float BreatheFireDamage = 400.0f;
        private float BreatheFireLoseDamagePercent = 35;
        private float BreatheFireManaPay = 200.0f;
        private int BreatheFireLoseDamageDuration = 8;
        private int BreatheFireCD = 0;
        private const int BreatheFireDefaultCD = 19;

        // Ability Two : Dragon Tail
        public static string AbiNameTwo = "Dragon Tail";
        private float DragonTailDamage = 500.0f;
        private float DragonTailManaPay = 230.0f;
        private int DragonTailStunDuration = 3;
        private int DragonTailCD = 0;
        private const int DragonTailDefaultCD = 16;

        // Ability Passive : Dragon Blood
        public static string AbiNamePassive = "Dragon Blood";
        private float DragonBloodHpRegeneration = 30.0f;
        private float DragonBloodAddArmor = 25.0f;

        // Ability Three : Dragon Fury
        public static string AbiNameThree = "Dragon Fury";
        private float DragonFuryAddDamage = 50.0f;
        private float DragonFuryAddAttackSpeedPercent = 45;
        private float DragonFuryLastAttackSpeed = 0.0f;
        private float DragonFuryHpRegeneration = 20.0f;
        private float DragonFuryAddArmor = 25.0f;
        private float DragonFuryManaPay = 300.0f;
        private int DragonFuryDuration = 6;
        private int DragonFuryCD = 0;
        private const int DragonFuryDefaultCD = 35;
        private int DragonFuryCounter = 0;
        private bool DragonFuryActivated = false;

        public DragonKnight(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public DragonKnight(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new DragonKnight(this, sender);
        }
        protected override void InitPassiveAbilities()
        {
            HPregen += DragonBloodHpRegeneration;
            Armor += DragonBloodAddArmor;
        }
        protected override void UpdateCounters()
        {
            UpdateDragonFury();
        }
        public override void UpdateCountdowns()
        {
            if (BreatheFireCD > 0)
                BreatheFireCD--;
            if (DragonTailCD > 0)
                DragonTailCD--;
            if (DragonFuryCD > 0)
                DragonFuryCD--;
        }
        private void UpdateDragonFury()
        {
            if (DragonFuryCounter < DragonFuryDuration && DragonFuryActivated)
                DragonFuryCounter++;
            else
            {
                if (DragonFuryActivated)
                {
                    DragonFuryActivated = false;
                    HPregen -= DragonFuryHpRegeneration;
                    Armor -= DragonFuryAddArmor;
                    DPS -= DragonFuryAddDamage;
                    AttackSpeed = DragonFuryLastAttackSpeed;
                    UpdateDPS();
                    DragonFuryCD = DragonFuryDefaultCD;
                    DragonFuryCounter = 0;
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
            if (BreatheFireCD > 0)
                msg += $"3 - {AbiNameOne} ({BreatheFireCD}) [{BreatheFireManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{BreatheFireManaPay}]\n";
            if (DragonTailCD > 0)
                msg += $"4 - {AbiNameTwo} ({DragonTailCD}) [{DragonTailManaPay}]\n";
            else
                msg += $"4 - {AbiNameTwo} [{DragonTailManaPay}]\n";
            if (DragonFuryActivated)
                msg += $"5 - {AbiNameThree} <<{DragonFuryDuration - DragonFuryCounter + 1}>>\n";
            else
            {
                if (DragonFuryCD > 0)
                    msg += $"5 - {AbiNameThree} ({DragonFuryCD}) [{DragonFuryManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{DragonFuryManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(BreatheFireManaPay, BreatheFireCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= BreatheFireManaPay;
            BreatheFireCD = BreatheFireDefaultCD;
            target.GetDamage(target.CompileMagicDamage(BreatheFireDamage));
            WeakAttack(BreatheFireLoseDamageDuration, (target.DPS / 100.0f * BreatheFireLoseDamagePercent), target);
            var hCon = Sender.CreateMessageContainer();
            var eCon = target.Sender.CreateMessageContainer();
            hCon.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            eCon.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            hCon.Add(lang => lang.YouHaveWeakenedTheEnemy);
            eCon.Add(lang => lang.TheEnemyHasWeakenedYou);
            await hCon.SendAsync();
            await eCon.SendAsync();
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(DragonTailManaPay, DragonTailCD))
                return false;
            MP -= DragonTailManaPay;
            DragonTailCD = DragonTailDefaultCD;
            if (!target.HasImmuneToMagic)
                target.GetDamage(DragonTailDamage - target.Armor);
            target.StunCounter += DragonTailStunDuration;
            var hCon = Sender.CreateMessageContainer();
            var eCon = target.Sender.CreateMessageContainer();
            hCon.Add(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            eCon.Add(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            hCon.Add(lang => lang.YouStunnedEnemy);
            eCon.Add(lang => lang.TheEnemyStunnedYou);
            await hCon.SendAsync();
            await eCon.SendAsync();
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(DragonFuryManaPay, DragonFuryCD))
                return false;
            DragonFuryActivated = true;
            HPregen += DragonFuryHpRegeneration;
            Armor += DragonFuryAddArmor;
            DPS += DragonFuryAddDamage;
            DragonFuryLastAttackSpeed = AttackSpeed;
            AttackSpeed += AttackSpeed / 100.0f * DragonFuryAddAttackSpeedPercent;
            UpdateDPS();
            MP -= DragonFuryManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameThree));
            return true;
        }
    }
}
