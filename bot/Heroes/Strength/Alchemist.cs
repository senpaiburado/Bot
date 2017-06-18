using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Alchemist : IHero
    {

        // Ability One : Acid Spray
        public static string AbiNameOne = "Acid Spray";
        private float AcidSprayArmorPenetrate = 17.0f;
        private float AcidSprayDamage = 45.0f;
        private int AcidSprayDuration = 9;
        private float AcidSprayManaPay = 150.0f;
        private const int AcidSprayDefaultCD = 17;
        private int AcidSprayCD = 0;

        // Ability Two : Unstable Concoction
        public string AbiNameTwo => UnstableConcoctionActivated ? "Unstable Concoction Throw" : "Unstable Concoction";
        public static string AbiNameTwoDefault = "Unstable Concoction";
        public static string AbiNameTwoActivated = "Unstable Concoction Throw";
        private bool UnstableConcoctionActivated = false;
        private float UnstableConcoctionDamage = 405.55f;
        private int UnstableConcoctionTimeToThrow = 6;
        private int UnstableConcoctionCounter = 0;
        private int UnstableConcoctionCD = 0;
        private const int UnstableConcoctionDefaultCD = 25;
        private float UnstableConcoctionManaPay = 200.0f;

        // Passive ability: GreevilsPower
        public static string AbiNamePassive = "Greevils Power";
        private int GreevilsPowerTime = 5;
        private int GreevilsPowerCounter = 0;
        private float GreevilsPowerDamage = 5.0f;

        // Ability Three : Chemical Rage
        public static string AbiNameThree = "Chemical Rage";
        private bool ChemicalRageActivated = false;
        private int ChemicalRageCounter = 0;
        private int ChemicalRageDuration = 11;
        private const int ChemicalRageDefaultCD = 30;
        private int ChemicalRageCD = 0;
        private float ChemicalRageHpRegeneration = 75.0f;
        private float ChemicalRageMpRegeneration = 25.0f;
        private float ChemicalRageManaPay = 300.0f;
        private float ChemicalRageAdditionalAttackSpeed = 2.1f;

        public Alchemist(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }

        public Alchemist(IHero hero, Sender sender)
            : base(hero, sender)
        {

        }

        private async Task<bool> UseUnstableConcoction(IHero excepter)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(UnstableConcoctionManaPay, UnstableConcoctionCD))
                return false;
            MP -= UnstableConcoctionManaPay;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameTwo));
            await excepter.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameTwo));
            UnstableConcoctionActivated = true;
            return true;
        }

        private async Task<bool> ThrowUnstableConcoction(IHero enemyHero, IHero target)
        {
            if (!await CheckSilence())
                return false;
            if(!await CheckImmuneToMagic(target) && target != this)
                return false;

            var attakerMessages = Sender.CreateMessageContainer();
            attakerMessages.Add(lang => lang.ALCHEMIST_YouHaveThrownUC);

            var enemyMessages = enemyHero.Sender.CreateMessageContainer();
            enemyMessages.Add(lang => lang.ALCHEMIST_TheEnemyHasThrownUC);

            UnstableConcoctionActivated = false;
            UnstableConcoctionCD = UnstableConcoctionDefaultCD;
            if (base.GetRandomNumber(1, 100) > 15)
            {
                target.GetDamage(target.CompileMagicDamage(UnstableConcoctionDamage));
                target.StunCounter += UnstableConcoctionCounter;

                attakerMessages.Add(lang => lang.ALCHEMIST_UC_HasExploded);
                enemyMessages.Add(lang => lang.ALCHEMIST_UC_HasExploded);

                if (target == this)
                {
                    attakerMessages.Add(lang => lang.YouStunnedYourself);
                    enemyMessages.Add(lang => lang.TheEnemyHasStunnedItself);
                }
                else
                {
                    attakerMessages.Add(lang => lang.YouStunnedEnemy);
                    enemyMessages.Add(lang => lang.TheEnemyStunnedYou);
                }
            }
            else
            {
                attakerMessages.Add(lang => lang.YouMissedTheEnemy);
                enemyMessages.Add(lang => lang.TheEnemyMissedYou);
            }

            await attakerMessages.SendAsync();
            await enemyMessages.SendAsync();
            
            UnstableConcoctionCounter = 0;
            hero_target = null;
            return true;
        }

        protected override void UpdateCounters()
        {
            UpdateUnstableConcoction();
            UpdateGreevilsPower();
            UpdateChemicalRage();
        }

        public override void UpdateCountdowns()
        {
            if (AcidSprayCD > 0)
                AcidSprayCD--;
            if (UnstableConcoctionCD > 0)
                UnstableConcoctionCD--;
            if (ChemicalRageCD > 0)
                ChemicalRageCD--;
        }

        private void UpdateGreevilsPower()
        {
            if (GreevilsPowerCounter < GreevilsPowerTime)
                GreevilsPowerCounter++;
            else
            {
                DPS += GreevilsPowerDamage;
                GreevilsPowerCounter = 0;
            }
        }

        private async void UpdateUnstableConcoction()
        {
            if (UnstableConcoctionCounter < UnstableConcoctionTimeToThrow && UnstableConcoctionActivated)
                UnstableConcoctionCounter++;
            else
            {
                if (UnstableConcoctionActivated)
                {
                    UnstableConcoctionActivated = false;
                    await ThrowUnstableConcoction(hero_target, this);
                    UnstableConcoctionCounter = 0;
                }
            }
        }

        private void UpdateChemicalRage()
        {
            if (ChemicalRageCounter < ChemicalRageDuration && ChemicalRageActivated)
                ChemicalRageCounter++;
            else
            {
                if (ChemicalRageActivated)
                {
                    ChemicalRageActivated = false;
                    ChemicalRageCounter = 0;
                    HPregen -= ChemicalRageHpRegeneration;
                    MPregen -= ChemicalRageMpRegeneration;
                    AttackSpeed -= ChemicalRageAdditionalAttackSpeed;
                    UpdateDPS();
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
            if (AcidSprayCD > 0)
                msg += $"3 - {AbiNameOne} ({AcidSprayCD}) [{AcidSprayManaPay}]\n";
            else
                msg += $"3 - {AbiNameOne} [{AcidSprayManaPay}]\n";
            if (UnstableConcoctionActivated)
                msg += $"4 - {AbiNameTwo} <<{UnstableConcoctionTimeToThrow - UnstableConcoctionCounter + 1}>>\n";
            else
            {
                if (UnstableConcoctionCD > 0)
                    msg += $"4 - {AbiNameTwo} ({UnstableConcoctionCD}) [{UnstableConcoctionManaPay}]\n";
                else
                    msg += $"4 - {AbiNameTwo} [{UnstableConcoctionManaPay}]\n";
            }
            if (ChemicalRageActivated)
                msg += $"5 - {AbiNameThree} <<{ChemicalRageDuration - ChemicalRageCounter + 1}>>\n";
            else
            {
                if (ChemicalRageCD > 0)
                    msg += $"5 - {AbiNameThree} ({ChemicalRageCD}) [{ChemicalRageManaPay}]\n";
                else
                    msg += $"5 - {AbiNameThree} [{ChemicalRageManaPay}]\n";
            }
            msg += $"{lang.SelectAbility}:";
            return msg;
        }

        public override IHero Copy(Sender sender)
        {
            return new Alchemist(this, sender);
        }

        override public async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(AcidSprayManaPay, AcidSprayCD))
                return false;
            if (!target.HasImmuneToMagic)
                target.LoosenArmor(AcidSprayArmorPenetrate, AcidSprayDuration);
            target.GetDamageByDebuffs(AcidSprayDamage - target.Armor, AcidSprayDuration);
            MP -= AcidSprayManaPay;
            AcidSprayCD = AcidSprayDefaultCD;
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }

        public async override Task<bool> UseAbilityTwo(IHero target)
        {
            if (UnstableConcoctionActivated)
                return await ThrowUnstableConcoction(target, target);
            else
            {
                hero_target = target;
                return await UseUnstableConcoction(target);
            }
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (ChemicalRageActivated)
            {
                await Sender.SendAsync(lang => lang.AbilityIsAlreadyActivated);
                return false;
            }
            if (!await CheckManaAndCD(ChemicalRageManaPay, ChemicalRageCD))
                return false;
            MP -= ChemicalRageManaPay;
            ChemicalRageCD = ChemicalRageDefaultCD;
            ChemicalRageActivated = true;
            HPregen += ChemicalRageHpRegeneration;
            MPregen += ChemicalRageMpRegeneration;
            AttackSpeed += ChemicalRageAdditionalAttackSpeed;
            UpdateDPS();
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}