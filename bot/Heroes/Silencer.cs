using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaTextGame.Heroes
{
    class Silencer : IHero
    {
        public Silencer(string name, int str, int agi, int intel, MainFeature feat) : base (name, str, agi, intel, feat)
        {

        }
        public Silencer(IHero hero, Sender sender) : base(hero, sender)
        {

        }
        public override void UpdateCountdowns()
        {
            base.UpdateCountdowns();
        }
        protected override void UpdateCounters()
        {
            base.UpdateCounters();
        }
        public override IHero Copy(Sender sender)
        {
            return base.Copy(sender);
        }
        public override string GetMessageAbilitesList(User.Text lang)
        {
            return base.GetMessageAbilitesList(lang);
        }
        public override Task<bool> UseAbilityOne(IHero target)
        {
            return base.UseAbilityOne(target);
        }
        public override Task<bool> UseAbilityTwo(IHero target)
        {
            return base.UseAbilityTwo(target);
        }
        public override Task<bool> UseAbilityThree(IHero target)
        {
            return base.UseAbilityThree(target);
        }
    }
}
