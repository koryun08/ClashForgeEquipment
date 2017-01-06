using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using LitJson;
using System.ComponentModel;
using System.Reflection;

namespace MakeSomethingGood
{
    using MT = MaterialType;
    public enum EquipmentType { Weapon, Armor, Boots, Helmet, LegArmor, Ring }
    [TypeConverter(typeof(MaterialTypeConverter))]
    public enum MaterialType {Maple,Rhodonite,Bone,Fabric,Garnet,Bronze,Herb,Fur,Jade,Cobalt,Horn,Amber }
    

    public class Program
    {
        public static List<Equipment> equips;
        public static List<Stack<Equipment>> eqStacks;
        static Stack<Equipment> eqs;
        static int __max = -30;

        static void Main(string[] args)
        {            
            equips = GetEquipmnets();
            eqStacks = new List<Stack<Equipment>>();
            var filename = Console.ReadLine();
            var configuration = ReadMaterialsFromFile(filename);
            var materials = configuration.Materials;     
            using (var w = new StreamWriter(@"C:\cok\EQ-" + Guid.NewGuid().ToString().ToUpper() + ".txt"))
            {
                w.WriteLine("FOR " + filename);
                eqs = new Stack<Equipment>();
                CheckForEq2(w, materials, configuration.MinLvl, configuration.MaxLvl, configuration.NeedEq);
            }            
            //Console.WriteLine(eqStacks.Count);            
            Console.WriteLine("Program executed.");
            //Console.ReadLine();
            
        }
        

        static Configuration ReadMaterialsFromFile(string filename)
        {
            var _conf = new Configuration();
            var jText = string.Empty;
            var fName = string.Empty;

            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException();
            }

            using (var tr = new StreamReader(filename)){jText = tr.ReadToEnd();}
            var _a = JsonMapper.ToObject(jText);
            var _config = _a["Configuration"];
            _conf.MinLvl = int.Parse(_config["Min"].ToString());
            _conf.MaxLvl = int.Parse(_config["Max"].ToString());
            _conf.NeedEq = _config.Keys.Contains("NeedEq") ? _config["NeedEq"].ToString() : "";
            _conf.Materials = new Dictionary<MT, int>();
            var _mats =  _a["Materials"] ;
            foreach (var key in _mats.Keys)
            {
                _conf.Materials.Add((MT)Enum.Parse(typeof(MT), key) , (int) _mats[key]);
            }
            return _conf;
            //return JsonMapper.ToObject<Dictionary<string, int>>(jText).ToDictionary(item => (MT)Enum.Parse(typeof(MT), item.Key), item => item.Value);
        }

        public static bool CheckForEq(StreamWriter sw, Dictionary<MT, int> materials, int lvl, int maxLvl = 40)
        {
            foreach (var eq in equips.Where(x => x.Level == lvl))
            {
                var canForge = true;                
                foreach (var m in eq.Materials)
                {
                    canForge = canForge && materials.ContainsKey(m.Key) && materials[m.Key] >= m.Value;
                }
                if (canForge && lvl < maxLvl)
                {
                    //sw.WriteLine(string.Format("{0}{1} => {2} => {3}", "+-".PadLeft(eq.Level == 1 ? 0 : 2 * eq.Level / 5 + 2, ' '), eq.EType, eq.Level, eq.Name));
                    CheckForEq(sw, Sub(materials, eq), lvl == 1 ? 5 : lvl + 5, maxLvl);
                }
            }
            //sw.WriteLine(max);
            return false;
        }

        public static bool CheckForEq2(StreamWriter sw, Dictionary<MT, int> materials, int lvl, int maxLvl = 40, string needEq = "")
        {
            foreach (var eq in equips.Where(x => x.Level == lvl))
            {
                var canForge = true;
                foreach (var m in eq.Materials)
                {
                    canForge = canForge && materials.ContainsKey(m.Key) && materials[m.Key] >= m.Value;
                }
                if (canForge && lvl < maxLvl)
                {
                    eqs.Push(eq);
                    CheckForEq2(sw, Sub(materials, eq), lvl == 1 ? 5 : lvl + 5, maxLvl, needEq);
                    eqs.Pop();
                }
                else if (lvl == maxLvl)
                {
                    if (needEq == eq.Name)
                    {
                        eqs.Push(eq);
                        var _max = Sub(materials, eq).Where(x => x.Value < 1).Sum(y => y.Value);
                        if (_max >= __max)
                        {
                            __max = _max;
                            sw.WriteLine("{0} >> {1} >> {2}", __max, string.Join(" > ", eqs.Select(x => x.Name).ToArray()), GetInsuffMaterialsString(Sub(materials, eq)));
                        }
                        eqs.Pop();
                    }
                }
                //else if (!canForge && lvl == maxLvl)
                //{
                //    //if(need == eq.Name)
                //    //sw.WriteLine(string.Format("{0} insuff {1} => {2} => {3} |=> {4}", "+-".PadLeft(eq.Level == 1 ? 0 : 2 * eq.Level / 5 + 2, ' '), eq.EType, eq.Level, eq.Name,GetInsuffMaterialsString(Sub(materials, eq))));
                //}
                else if (!canForge && lvl < maxLvl)
                {
                    //sw.WriteLine(string.Format("{0} insuff {1} => {2} => {3} |=> {4}", "+-".PadLeft(eq.Level == 1 ? 0 : 2 * eq.Level / 5 + 2, ' '), eq.EType, eq.Level, eq.Name,GetInsuffMaterialsString(Sub(materials, eq))));
                    eqs.Push(eq);
                    CheckForEq2(sw, Sub(materials, eq), lvl == 1 ? 5 : lvl + 5, maxLvl, needEq);
                    eqs.Pop();
                }
            }
            //sw.WriteLine(max);
            return false;
        }

        public static Dictionary<MT, int> Sub(Dictionary<MT, int> target, Equipment eq)
        {
            var _m = new Dictionary<MT, int>(target);
            foreach (var m in eq.Materials)
                _m[m.Key]-=m.Value;
            return _m;
        }

        public static Dictionary<MT, int> Add(Dictionary<MT, int> target, Equipment eq)
        {
            foreach (var m in eq.Materials)
                target[m.Key]+= m.Value;
            return target;
        }

        public static string GetMaterialsString(Dictionary<MT, int> m)
        {
            return string.Join("; ",m.Select(x => x.Key.ToString().Substring(0,2)+ ":" + x.Value).ToArray());
        }

        public static string GetInsuffMaterialsString(Dictionary<MT, int> m)
        {
            return string.Join("; ", m.Where(y=>y.Value<0).Select(x => x.Key.ToString().Substring(0, 2) + ":" + x.Value).ToArray());
        }

        static List<Equipment> GetEquipmnets()
        {
            var eqs = new List<Equipment>
            {
                #region Weapons
                new Weapon(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Amber, 1}}, 1, "Apprentice Sword"),
                new Weapon(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Cobalt, 1}}, 5, "Hunting Bow"),
                new Weapon(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Bronze, 1}, { MT.Fabric, 1}}, 10, "Glaive"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Cobalt, 2}}, 10, "Hammer"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bronze, 2}, { MT.Maple, 1}}, 15, "Longbow"),
                new Weapon(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Amber, 1}, { MT.Garnet, 1}}, 15, "Battle Axe"),
                new Weapon(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Amber, 1}, { MT.Horn, 1}}, 15, "Great Sword"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Amber, 1}, { MT.Horn, 1}}, 20, "Lance"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Cobalt, 1}, { MT.Horn, 1}}, 20, "Assasin's Dagger"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Fabric, 1}, { MT.Garnet, 1}}, 20, "Crossbow"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Bronze, 1}, { MT.Amber, 2}}, 25, "Knight Sword"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Bronze, 1}, { MT.Garnet, 1}, { MT.Maple, 1}}, 25, "Halberd"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Bronze, 1}, { MT.Cobalt, 2}}, 25, "Wrestle Axe"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Bronze, 1}, { MT.Horn, 2}}, 30, "Flaming Sword"),
                new Weapon(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Fabric, 1}, { MT.Jade, 1}, { MT.Bone, 1}}, 30, "Meteor Hammer"),
                new Weapon(new Dictionary<MT, int> {{ MT.Bone, 2}, { MT.Bronze, 1}, { MT.Maple, 1}}, 30, "Fauchard"),
                new Weapon(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Jade, 1}, { MT.Garnet, 1}, { MT.Rhodonite, 1}}, 35, "Prophecy Blade"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Jade, 1}, { MT.Horn, 2}}, 35, "Compound Bow"),
                new Weapon(new Dictionary<MT, int> {{ MT.Amber, 2}, { MT.Fabric, 1}, { MT.Bronze, 1}}, 35, "Bucktooth Battle Axe"),
                new Weapon(new Dictionary<MT, int> {{ MT.Cobalt, 2}, { MT.Fabric, 1}, { MT.Horn, 1}}, 40, "Elven Longbow"),
                new Weapon(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Jade, 1}, { MT.Bronze, 2}}, 40, "Axe of Mars"),
                new Weapon(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Garnet, 2}, { MT.Maple, 1}}, 40, "Wolf Tooth Hammer"),
                #endregion

                #region Armor
                new Armor(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Fur, 1}}, 1, "Apprentice Coat"),
                new Armor(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Fur, 1}}, 5, "Fur Gilet"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Jade, 1}, { MT.Bronze, 1}}, 10, "Scholar Coat"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Horn, 1}, { MT.Fur, 1}}, 10, "Linen Shirt"),
                new Armor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Horn, 1}, { MT.Cobalt, 1}}, 15, "Coat of Nobility"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Fabric, 1}, { MT.Amber, 1}}, 15, "Iron Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Horn, 1}, { MT.Bronze, 1}}, 15, "Soft Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Bronze, 1}, { MT.Fur, 1}}, 20, "Padded Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Fabric, 1}, { MT.Jade, 1}}, 20, "Chainmail"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Jade, 2}}, 20, "Assasin Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Jade, 2}, { MT.Bronze, 1}}, 25, "Lord Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Cobalt, 1}, { MT.Bronze, 1}, { MT.Amber, 1}}, 25, "Plate Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Fabric, 2}, { MT.Amber, 1}}, 25, "Defender Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 2}, { MT.Amber, 1}}, 30, "Leather Hunter Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Fabric, 1}, { MT.Horn, 1}, { MT.Herb, 1}}, 30, "Oath Robe"),
                new Armor(new Dictionary<MT, int> {{ MT.Bronze, 1}, { MT.Fabric, 1}, { MT.Jade, 1}, { MT.Herb, 1}}, 30, "Wild Army Coat"),
                new Armor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Fabric, 1}, { MT.Jade, 1}, { MT.Herb, 1}}, 35, "Scales of Forgiveness"),
                new Armor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Jade, 2}, { MT.Bronze, 1}}, 35, "Coat of Calmness"),
                new Armor(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Jade, 1}, { MT.Bronze, 1}, { MT.Horn, 1}}, 35, "Evil Wolf Plate Armor"),
                new Armor(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Jade, 1}, { MT.Fabric, 2}, { MT.Horn, 1}}, 40, "Rock Cuirass"),
                new Armor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Bronze, 1}, { MT.Horn, 1}, { MT.Herb, 1}}, 40, "Knight's Tunic"),
                new Armor(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bronze, 1}, { MT.Amber, 1}, { MT.Herb, 1}}, 40, "Meteor Armor"),
                #endregion
                
                #region Boots
                new Boots(new Dictionary<MT, int> {{ MT.Fabric, 1}, { MT.Maple, 1}}, 1, "Apprentice Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Jade, 1}, { MT.Fur, 1}}, 5, "Light Leather Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Fabric, 1}, { MT.Garnet, 1}}, 10, "Iron Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 1}, { MT.Garnet, 1}}, 10, "Thick Leather Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Jade, 1}, { MT.Garnet, 1}}, 15, "Silver Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fabric, 1}, { MT.Maple, 1}, { MT.Fur, 1 } }, 15, "Lord Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Jade, 1}, { MT.Fur, 1}}, 15, "Boots of Wizardry"),
                new Boots(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}}, 20, "Desert Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Jade, 2}, { MT.Garnet, 1}}, 20, "Spur Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Fabric, 1}, { MT.Jade, 1}}, 20, "Adventure Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Fabric, 2},{ MT.Garnet, 1}}, 25, "Heavy Leather Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Fabric, 1},{ MT.Maple, 1}, { MT.Bone, 1}}, 25, "Guard Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Jade, 1}, { MT.Garnet, 1}, { MT.Rhodonite, 1}}, 25, "Plunder Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Fabric, 1},{ MT.Jade, 1}, { MT.Bone, 1}}, 30, "Evil Leather Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Fabric, 1},{ MT.Garnet, 1}, { MT.Rhodonite, 1}}, 30, "Mystery Knight Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 2}, { MT.Rhodonite, 1}}, 30, "Speedy Shin Run"),
                new Boots(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Fabric, 1}, { MT.Maple, 1}, { MT.Herb, 1}}, 35, "Knight Heavy Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Bone, 2}, { MT.Jade, 1}, { MT.Herb, 1}}, 35, "Oblivion Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}, { MT.Bone, 1}}, 35, "Lucky Shin Guard"),
                new Boots(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Fabric, 2}, { MT.Jade, 1}}, 40, "Shadow Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 3}}, 40, "Rose Boots"),
                new Boots(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Rhodonite, 1}, { MT.Fur, 1}, { MT.Bone, 1 }}, 40, "Swift Boots"),
                #endregion
                
                #region Helmet
                new Helmet(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bone, 1}}, 1, "Apprentice Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Garnet, 1}}, 5, "Horned Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Bronze, 1}, { MT.Amber, 1}}, 10, "Battle Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Horn, 1}, { MT.Cobalt, 1}}, 10, "Copper Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Bronze, 1}, { MT.Garnet, 1}}, 15, "Lord Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Jade, 1}, { MT.Amber, 1}}, 15, "Knight Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Horn, 1}, { MT.Garnet, 1}}, 15, "Heavy Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Jade, 2}, { MT.Bone, 1}}, 20, "Leather Hat"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Amber, 1}, { MT.Cobalt, 1}}, 20, "Sorcer Hat"),
                new Helmet(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bronze, 1}, { MT.Amber, 1}}, 20, "Viking Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Fabric, 1}, { MT.Bronze, 1}, { MT.Bone, 1}}, 25, "Phoenix Tail Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Bronze, 1}, { MT.Horn, 1}, { MT.Bone, 1}}, 25, "Raptor Battle Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bronze, 1}, { MT.Amber, 1}, { MT.Garnet, 1}}, 25, "Scouter Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Fabric, 1}, { MT.Bronze, 1}, { MT.Cobalt, 1}}, 30, "Golden Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Jade, 2}, { MT.Bronze, 1}}, 30, "Ancient headdress"),
                new Helmet(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Fabric, 1}, { MT.Cobalt, 1}, { MT.Bone, 1}}, 30, "Blessing Headwear"),
                new Helmet(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Bronze, 1}, { MT.Horn, 1}, { MT.Garnet, 1}}, 35, "Warrior Mask"),
                new Helmet(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Fabric, 1}, { MT.Jade, 1}, { MT.Bone, 1}}, 35, "Barbute Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 2}, { MT.Jade, 1}, { MT.Bronze, 1}}, 35, "Stylet-lata Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Amber, 1}, { MT.Jade, 2}, { MT.Bronze, 1}}, 40, "Light-Feathered Helmet"),
                new Helmet(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Amber, 1}, { MT.Horn, 1}, { MT.Bone, 1} }, 40, "Knight's Vizor"),
                new Helmet(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Fabric, 1}, { MT.Cobalt, 1}}, 40, "Helmet of Savagery"),
                #endregion
                
                #region LegArmor
                new LegArmor(new Dictionary<MT, int> {{ MT.Jade, 1}, { MT.Maple, 1}}, 1, "Apprentice Shorts"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Fur, 1}}, 5, "Light Leather Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Jade, 1}, { MT.Garnet, 1}}, 10, "Thick Leather Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Fabric, 1}, { MT.Maple, 1}}, 10, "Dexterity Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Garnet, 1}, { MT.Maple, 1}}, 15, "Noble Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Bone, 1}, { MT.Maple, 1}, { MT.Fur, 1}}, 15, "Pants of Wizardry"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}}, 15, "Lord Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Fabric, 1}, { MT.Fur, 1}}, 20, "Pelt Breeches"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Maple, 2}}, 20, "Heavy Greaves"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}}, 20, "Warrior Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Fabric, 1}, { MT.Jade, 1}, { MT.Bone, 1}}, 25, "Leather Truss Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Garnet, 1}, { MT.Maple, 1}, { MT.Herb, 1}}, 25, "Scale Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Bone, 2}, { MT.Jade, 1}, { MT.Herb, 1}}, 25, "Exquisite Silk Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Fabric, 1}, { MT.Maple, 1}, { MT.Herb, 1}}, 30, "Scaly Leg Armor"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 1}, { MT.Garnet, 1}, { MT.Herb, 1}}, 30, "Leather Leg Guard"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Jade, 1}, { MT.Maple, 1}, { MT.Bone, 1}}, 30, "Praise Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}, { MT.Bone, 1}}, 35, "Glory Leg Guard"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Maple, 1}, { MT.Fabric, 1}, { MT.Garnet, 1}, { MT.Fur, 1}}, 35, "Blackstone Leg Armor"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Fabric, 2}, { MT.Jade, 1}}, 35, "Matador Pants"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Jade, 2}, { MT.Garnet, 1}}, 40, "Obsidian Galter"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Rhodonite, 1}, { MT.Fur, 1}, { MT.Bone, 1}}, 40, "Amazon Galter"),
                new LegArmor(new Dictionary<MT, int> {{ MT.Bone, 2}, { MT.Maple, 1}, { MT.Rhodonite, 1}}, 40, "Hermes' Pants"),
                #endregion
                
                #region Ring
                new Ring(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Rhodonite, 1}}, 1, "Apprentice Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 2}}, 5, "Ring of Precision"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 2}, { MT.Herb, 1}}, 10, "Crystal Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Amber, 1}, { MT.Horn, 1}}, 10, "Copper Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Maple, 1}, { MT.Rhodonite, 1}}, 15, "Gold Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 2}, { MT.Fur, 1}}, 15, "Ring of Sovereignty"),
                new Ring(new Dictionary<MT, int> {{ MT.Garnet, 2}, { MT.Rhodonite, 1}}, 15, "Ring of Wizardry"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Horn, 1}, { MT.Cobalt, 1}}, 20, "Ring of the Serpent"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Maple, 1}, { MT.Fur, 1}}, 20, "Pupil Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Rhodonite, 1}, { MT.Garnet, 1}, { MT.Maple, 1}}, 20, "Elemenal Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Garnet, 2}, { MT.Amber, 1}, { MT.Horn, 1}}, 25, "Fearless Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bronze, 1}, { MT.Amber, 1}, { MT.Herb, 1}}, 25, "Angel Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Fur, 1}, { MT.Bronze, 1}, { MT.Maple, 1}, { MT.Herb, 1}}, 25, "Scholar Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Garnet, 1}, { MT.Fur, 1}, { MT.Bone, 1}}, 30, "Guard Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Fur, 2}, { MT.Bronze, 1}, { MT.Rhodonite, 1}}, 30, "Faith Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Horn, 1}, { MT.Maple, 2}}, 30, "Iron Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Horn, 1}, { MT.Bronze, 1}, { MT.Amber, 1}, { MT.Garnet, 1}}, 35, "Ring of Cruelty"),
                new Ring(new Dictionary<MT, int> {{ MT.Garnet, 1}, { MT.Amber, 1}, { MT.Cobalt, 1}, { MT.Herb, 1}}, 35, "Ring of deception"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Maple, 1}, { MT.Fur, 1}, { MT.Bone, 1}}, 35, "Ring of Balance"),
                new Ring(new Dictionary<MT, int> {{ MT.Cobalt, 1}, { MT.Amber, 1}, { MT.Horn, 1}, { MT.Garnet, 1}}, 40, "Sacred Ring"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 1}, { MT.Rhodonite, 2}, { MT.Bone, 1}}, 40, "Ring of Life"),
                new Ring(new Dictionary<MT, int> {{ MT.Herb, 2}, { MT.Rhodonite, 1}, { MT.Fur, 1}}, 40, "Giant's Ring")
                #endregion
                
            };

            return eqs;
        }
    }

    struct Configuration
    {
        public int MinLvl { get; set; }
        public int MaxLvl { get; set; }
        public string NeedEq { get; set; }
        public Dictionary<MT, int> Materials { get; set; }
    }

    public class Equipment
    {
        public EquipmentType EType { get; set; }

        public Dictionary<MT, int> Materials { get; set; }

        public string Name { get; set; }

        public int Level { get; set; }

        public Equipment()
        {
            Materials = new Dictionary<MT, int>();
        }

        public Equipment(EquipmentType t) 
            :this()
        {
            EType = t;
        }

        public Equipment(EquipmentType t, Dictionary<MT, int> m)
            : this(t)
        {
            this.Materials = m;
        }

        public Equipment(EquipmentType t, Dictionary<MT, int> m, int l, string name)
            : this(t, m)
        {
            this.Name = name; //.PadRight(20);
            this.Level = l;
        }
    }

    public class Armor : Equipment
    {
        public Armor() :
            base(EquipmentType.Armor)
        { }

        public Armor(Dictionary<MT, int> m)
            : base(EquipmentType.Armor, m)
        {}

        public Armor(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.Armor, m, l, n)
        { }
    }

    public class Weapon : Equipment
    {
        public Weapon() :
            base(EquipmentType.Weapon)
        { }

        public Weapon(Dictionary<MT, int> m)
            : base(EquipmentType.Weapon, m)
        { }

        public Weapon(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.Weapon, m, l, n)
        { }
    }

    public class Boots : Equipment
    {
        public Boots() :
            base(EquipmentType.Boots)
        { }

        public Boots(Dictionary<MT, int> m)
            : base(EquipmentType.Boots, m)
        { }

        public Boots(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.Boots, m, l, n)
        { }
    }

    public class Helmet : Equipment
    {
        public Helmet() :
            base(EquipmentType.Helmet)
        { }

        public Helmet(Dictionary<MT, int> m)
            : base(EquipmentType.Helmet, m)
        { }

        public Helmet(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.Helmet, m, l, n)
        { }
    }

    public class LegArmor : Equipment
    {
        public LegArmor() :
            base(EquipmentType.Helmet)
        { }

        public LegArmor(Dictionary<MT, int> m)
            : base(EquipmentType.Helmet, m)
        { }

        public LegArmor(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.LegArmor, m, l, n)
        { }
    }

    public class Ring : Equipment
    {
        public Ring() :
            base(EquipmentType.Ring)
        { }

        public Ring(Dictionary<MT, int> m)
            : base(EquipmentType.Ring, m)
        { }

        public Ring(Dictionary<MT, int> m, int l, string n)
            : base(EquipmentType.Ring, m, l, n)
        { }
    }

    public class MaterialTypeConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                return (MaterialType) Enum.Parse(typeof(MaterialType), (string) value);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}

/*
Maple: Building Speed
Rhodonite: Science Research Speed
Bone: Farm Income
Fabric: Troop Load
Garnet: Trap Attack
Bronze: Infantry Attack
Amber: Cavalry Attack
Horn: Archer Attack
Cobalt Ore: Chariot Attack
Jade: Marching Speed
Fur: Hospital Capacity
Herb: Wounded Recovery Speed aka Healing Speed 
 */

/*
Saw Mill: Maple, Fur, Horn
Farm: Grass, Bone, Cloth
Iron Mine: Bronze, Cobalt, Amber
Mythril, Jade, Garnet, Crystal
 */