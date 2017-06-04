using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot.Heroes
{
    class Lifestealer : IHero
    {
        // Ability One : Rage
        public string AbiNameOne = "Rage";
        private int RageDuration = 5;
        private int RageCounter = 0;
        private float RageAttackSpeed = 1.5f;
        private float RageManaPay = 130.0f;
        private int RageCD = 0;
        private const int RageDefaultCD = 9;

        // Ability Passive : Feast
        public string AbiNamePassive = "Feast";
        private float FeastHpStealPercent = 1.0f;

        // Ability Two : Open Wounds
        public string AbiNameTwo = "Open Wounds";

        public Lifestealer(string name, int str, int agi, int intel, MainFeature feat) : base(name, str, agi, intel, feat)
        {

        }
        public Lifestealer(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override IHero Copy(Sender sender)
        {
            return new Lifestealer(this, sender);
        }
        public override void UpdateCountdowns()
        {
            base.UpdateCountdowns();
        }
        protected override void UpdateCounters()
        {
            base.UpdateCounters();
        }
        public override async Task<bool> UseAbilityOne(IHero target)
        {
            return true;
        }
        public override async Task<bool> UseAbilityTwo(IHero target)
        {
            return true;
        }
        public override async Task<bool> UseAbilityThree(IHero target)
        {
            return true;
        }
        public override async Task<bool> Attack(IHero target)
        {
            HpStealAdditional = target.HP / 100 * FeastHpStealPercent;
            return await base.Attack(target);
        }
    }
}
