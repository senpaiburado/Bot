using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Slardar : IHero
    {
        // Ability One : Guardian Sprint
        public static string AbiNameOne = "Guardian Sprint";
        private float GuardianSprintAttackSpeedPercent = 75.0f;
        private float GuardianSprintLastAttackSpeed = 0.0f;
        private float GuardianSprintDeffectDamagePercent = 15.0f;
        private int GuardianSprintDuration = 8;
        private int GuardianSprintCD = 0;
        private const int GuardianSprintDefaultCD = 18;
        private int GuardianSprintCounter = 0;
        private bool GuardianSprintActivated = false;

        // Ability Two : Slithereen Crush
        public static string AbiNameTwo = "Slithereen Crush";
        private float SlithereenCrushDamage = 450.0f;
        private float SlithereenCrushManaPay = 150.0f;
        private int SlithereenCrushCD = 0;
        private const int SlithereenCrushDefaultCD = 14;
        private int SlithereenCrushStunDuration = 3;

        // Ability Passive : Bush of the Deep
        public static string AbiNamePassive = "Bush of the Deep";
        private float BotD_BushChance = 15.0f;
        private float BotD_BushDamage = 55.0f;

        // Ability Three : Corrosive Haze
        public static string AbiNameThree = "Corrosive Haze";
        private float CorrosiveHazeArmorPenetrate = 50.0f;
        private float CorrosiveHazeManaPay = 50.0f;
        private int CorrosiveHazeDuration = 15;
        private const int CorrosiveHazeDefaultCD = 33;
        private int CorrosiveHazeCD = 0;

        public Slardar(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Slardar(IHero hero, Sender sender) : base (hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new Slardar(this, sender);
        }
        protected override void InitPassiveAbilities()
        {
            StunHitChance += BotD_BushChance;
            StunDamage += BotD_BushDamage;
        }
        public override void GetDamage(float value)
        {
            if (GuardianSprintActivated)
                base.GetDamage(value + (value / 100 * GuardianSprintDeffectDamagePercent));
            else
                base.GetDamage(value);
        }
        public override void UpdateCountdowns()
        {
            if (GuardianSprintCD > 0)
                GuardianSprintCD--;
            if (SlithereenCrushCD > 0)
                SlithereenCrushCD--;
            if (CorrosiveHazeCD > 0)
                CorrosiveHazeCD--;
        }
        protected override void UpdateCounters()
        {
            UpdateGuardianSprint();
        }
        private void UpdateGuardianSprint()
        {
            if (GuardianSprintCounter < GuardianSprintDuration && GuardianSprintActivated)
                GuardianSprintCounter++;
            else
            {
                if (GuardianSprintActivated)
                {
                    GuardianSprintActivated = false;
                    AttackSpeed = GuardianSprintLastAttackSpeed;
                    UpdateDPS();
                    GuardianSprintCounter = 0;
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
            if (GuardianSprintActivated)
                msg += $"3 - {AbiNameOne} <<{GuardianSprintDuration - GuardianSprintCounter + 1}>>\n";
            else
            {
                if (GuardianSprintCD > 0)
                    msg += $"3 - {AbiNameOne} ({GuardianSprintCD})\n";
                else
                    msg += $"3 - {AbiNameOne}\n";
            }
            if (SlithereenCrushCD > 0)
                msg += $"4 - {AbiNameTwo} ({SlithereenCrushCD}) [{SlithereenCrushManaPay}]\n";
            else
                msg += $"4 - {AbiNameTwo} [{SlithereenCrushManaPay}]\n";
            if (CorrosiveHazeCD > 0)
                msg += $"5 - {AbiNameThree} ({CorrosiveHazeCD}) [{CorrosiveHazeManaPay}]\n";
            else
                msg += $"5 - {AbiNameThree} [{CorrosiveHazeManaPay}]\n";
            msg += $"{lang.SelectAbility}:";
            return msg;
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(0.0f, GuardianSprintCD))
                return false;
            GuardianSprintCD = GuardianSprintDefaultCD;
            GuardianSprintActivated = true;
            GuardianSprintLastAttackSpeed = AttackSpeed;
            AttackSpeed += AttackSpeed / 100.0f * GuardianSprintAttackSpeedPercent;
            UpdateDPS();
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameOne));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameOne));
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            if (!await CheckSilence())
                return false;
            if (!await CheckManaAndCD(SlithereenCrushManaPay, SlithereenCrushCD))
                return false;
            if (!await CheckImmuneToMagic(target))
                return false;
            MP -= SlithereenCrushManaPay;
            SlithereenCrushCD = SlithereenCrushDefaultCD;
            target.GetDamage(SlithereenCrushDamage - target.Armor);
            target.StunCounter += SlithereenCrushStunDuration;
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
            if (!await CheckManaAndCD(CorrosiveHazeManaPay, CorrosiveHazeCD))
                return false;
            MP -= CorrosiveHazeManaPay;
            CorrosiveHazeCD = CorrosiveHazeDefaultCD;
            target.LoosenArmor(CorrosiveHazeArmorPenetrate, CorrosiveHazeDuration);
            await Sender.SendAsync(lang => lang.GetMessageYouHaveUsedAbility(AbiNameThree));
            await target.Sender.SendAsync(lang => lang.GetMessageEnemyHasUsedAbility(AbiNameThree));
            return true;
        }
    }
}
