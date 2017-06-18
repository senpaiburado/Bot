using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes.Agility
{
    class Ursa : IHero
    {
        // Ability One : Earthshok
        public static string AbiNameOne = "Earthshok";
        private float EarthshokDamage = 400.0f;
        private float EarthshokArmorPenetrate = 7.0f;
        private float EarthshokManaPay = 100.0f;
        private int EarthshokCD = 0;
        private const int EarthshokDefaultCD = 15;
        private int EarthshokDuration = 5;

        // Ability Two : Overpower
        public static string AbiNameTwo = "Overpower";
        private float OverpowerAddDPS = 30.0f;
        private float OverpowerAddAttackSpeed = 1.0f;
        private float OverpowerManaPay = 150.0f;
        private int OverpowerDuration = 4;
        private int OverpowerCounter = 0;
        private int OverpowerCD = 0;
        private const int OverpowerDefaultCD = 22;
        private bool OverpowerActivated = false;

        // Ability Passive : Fury Swipes
        public static string AbiNamePassive = "Fury Swipes";
        private float FurySwipesDamagePerHit = 20.0f;
        private float FurySwipesDamageLimit = 100.0f;
        private float FurySwipesCurrentAdditionalDamage = 0.0f;

        // Ability Three : Enrage
        public static string AbiNameThree = "Enrage";
        private float EnrageDamageAbsorptionPercent = 80;
        private float EnrageAddDamage = 80.0f;
        private int EnrageCD = 0;
        private const int EnrageDefaultCD = 30;
        private int EnrageDuration = 6;
        private int EnrageCounter = 0;
        private bool EnrageActivated = false;

        public Ursa(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi ,intel, feat)
        {

        }
        public Ursa(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new Ursa(this, sender);
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            string msg = $"{lang.List}:\n";
            msg += $"1 - {lang.AttackString}\n";
            if (HealCountdown > 0)
                msg += $"2 - {lang.Heal} ({HealCountdown}) [{HealPayMana}]\n";
            else
                msg += $"2 - {lang.Heal} [{HealPayMana}]\n";
            if (EarthshokCD > 0)
                msg += $"3 - {AbiNameOne} ({EarthshokCD}) [{EarthshokManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{EarthshokManaPay}]\n";
            if (OverpowerActivated)
                msg += $"4 - {AbiNameTwo} <<{OverpowerDuration - OverpowerCounter + 1}>>\n";
            else
            {
                if (OverpowerCD > 0)
                    msg += $"4 - {AbiNameTwo} ({OverpowerCD}) [{OverpowerManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{OverpowerManaPay}]\n";
            }
            if (EnrageActivated)
                msg += $"5 - {AbiNameThree} <<{EnrageDuration - EnrageCounter + 1}>>\n";
            else
            {
                if (EnrageCD > 0)
                    msg += $"5 - {AbiNameThree} ({EnrageCD})\n";
                else
                    msg += $"5 - {AbiNameThree}\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override void UpdateCountdowns()
        {
            if (EarthshokCD > 0)
                EarthshokCD--;
            if (OverpowerCD > 0)
                OverpowerCD--;
            if (EnrageCD > 0)
                EnrageCD--;
        }
        protected override void UpdateCounters()
        {
            UpdateOverpower();
            UpdateEnrage();
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(EarthshokManaPay, EarthshokCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= EarthshokManaPay;
            EarthshokCD = EarthshokDefaultCD;
            target.GetDamage(target.CompileMagicDamage(EarthshokDamage));
            target.LoosenArmor(EarthshokArmorPenetrate, EarthshokDuration);
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(OverpowerManaPay, OverpowerCD))
                return false;
            MP -= OverpowerManaPay;
            OverpowerActivated = true;
            AttackSpeed += OverpowerAddAttackSpeed;
            UpdateDPS();
            DPS += OverpowerAddDPS;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameTwo));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameTwo));
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(0.0f, EnrageCD))
                return false;
            DPS += EnrageAddDamage;
            EnrageActivated = true;
            await Sender.SendAsync(lang => lang.GetMessageYouActivated(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyActivated(AbiNameThree));
            return true;
        }
        private void UpdateOverpower()
        {
            if (OverpowerCounter < OverpowerDuration && OverpowerActivated)
                OverpowerCounter++;
            else
            {
                if (OverpowerActivated)
                {
                    OverpowerActivated = false;
                    OverpowerCounter = 0;
                    AttackSpeed -= OverpowerAddAttackSpeed;
                    UpdateDPS();
                    DPS -= OverpowerAddDPS;
                    OverpowerCD = OverpowerDefaultCD;
                }
            }
        }
        private void UpdateEnrage()
        {
            if (EnrageCounter < EnrageDuration && EnrageActivated)
                EnrageCounter++;
            else
            {
                if (EnrageActivated)
                {
                    EnrageActivated = false;
                    EnrageCD = EnrageDefaultCD;
                    DPS -= EnrageAddDamage;
                    EnrageCounter = 0;
                }
            }
        }
        public override void GetDamage(float value)
        {
            if (EnrageActivated)
                base.GetDamage(value - (value / 100.0f * EnrageDamageAbsorptionPercent));
            else
                base.GetDamage(value);
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
                if (GetRandomNumber(1, 101) <= CriticalHitChance)
                {
                    damage *= CriticalHitMultiplier;
                    attakerMessages.Add(lang => $"{lang.CriticalHit}!");
                    excepterMessages.Add(lang => lang.TheEnemyDealtCriticalDamageToYou);
                }
                if (FurySwipesCurrentAdditionalDamage + FurySwipesDamagePerHit <= FurySwipesDamageLimit)
                {
                    FurySwipesCurrentAdditionalDamage += FurySwipesDamagePerHit;
                    damage += FurySwipesCurrentAdditionalDamage;
                }
                else
                {
                    damage += 100.0f;
                    FurySwipesCurrentAdditionalDamage = 0.0f;
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
