using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revcom_bot
{
    class IHero
    {
        public enum MainFeature
        {
            Str, Agi, Intel
        }

        protected int Strength { get; set; }
        protected int Agility { get; set; }
        protected int Intelligence { get; set; }
        protected MainFeature Feature { get; set; }

        public string Name { get; set; }
        public float HP { get; set; }
        public float MP { get; set; }
        public float MaxHP { get; set; }
        public float MaxMP { get; set; }
        public float HPregen { get; set; }
        public float MPregen { get; set; }
        public float DPS { get; set; }
        public float Armor { get; set; }

        //////////////////

        protected float AttackSpeed { get; set; }
        protected float CriticalShotChance { get; set; }
        protected float CriticalShotMultiplier { get; set; }
        protected float HpStealPercent { get; set; }

        public IHero(string name, int str, int agi, int itl, MainFeature feat)
        {
            this.Name = name;
            Init(str, agi, itl, feat);
        }

        public IHero(IHero _hero)
        {
            string name = _hero.Name;
            int STR = _hero.Strength;
            int AGI = _hero.Agility;
            int INT = _hero.Intelligence;
            MainFeature feat = _hero.Feature;
            this.Name = name;
            Init(STR, AGI, INT, feat);
        }

        virtual public void Init(int str, int agi, int itl, MainFeature feat)
        {
            Strength = str;
            Agility = agi;
            Intelligence = itl;
            Feature = feat;

            MaxHP = Strength * 20.0f;
            HPregen = Strength * 0.03f;

            Armor = Agility * 0.14f;
            AttackSpeed = Agility * 0.02f;

            MaxMP = Intelligence * 12.0f;
            MPregen = Intelligence * 0.04f;

            HP = MaxHP;
            MP = MaxMP;

            float damage = 0.0f;
            if (Feature == MainFeature.Str)
                damage = Strength * 1.0f;
            else if (Feature == MainFeature.Agi)
                damage = Agility * 1.0f;
            else if (Feature == MainFeature.Intel)
                damage = Intelligence * 1.0f;
            DPS = damage + damage * AttackSpeed;

            CriticalShotChance = 20.0f;
            CriticalShotMultiplier = 1.5f;
            HpStealPercent = 5.0f;

            InitAdditional();
            InitPassiveAbilities();
        }

        virtual protected void InitAdditional()
        {

        }

        virtual protected void InitPassiveAbilities()
        {

        }

        virtual public void Attack(IHero target)
        {
            Random random = new Random();


        }

        virtual public void Update()
        {
            Regeneration();
            UpdateCountdowns();
        }

        virtual public void UpdateCountdowns()
        {

        }

        virtual public void GetDamage(float value)
        {
            HP -= value;
        }
        virtual protected void Regeneration()
        {
            HP += HPregen;
            MP += MPregen;
        }
    }
}
