﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace D_Ezreal
{
    using static ObjectManager;

    internal class Program
    {
        private const string ChampionName = "Ezreal";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static SpellSlot _igniteSlot;

        private static Vector2 _pingLocation;

        private static int _lastPingT = 0;

        private static int _lastTick;

        private static Items.Item _youmuu, _blade, _bilge, _hextech, _archangel;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Ping(Vector2 position)
        {
            if (Environment.TickCount - _lastPingT < 30 * 1000) return;
            _lastPingT = Environment.TickCount;
            _pingLocation = position;
            SimplePing();
            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
        }

        private static void SimplePing()
        {
            Game.ShowPing(PingCategory.Fallback, _pingLocation, true);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (ObjectManager.Player.ChampionName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 1180);
            _w = new Spell(SpellSlot.W, 950);
            _e = new Spell(SpellSlot.E, 475);
            _r = new Spell(SpellSlot.R, 3000);

            _q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            _w.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            _r.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            _archangel = Utility.Map.GetMap().Type == Utility.Map.MapType.TwistedTreeline ||
                         Utility.Map.GetMap().Type == Utility.Map.MapType.CrystalScar
                ? new Items.Item(3048, float.MaxValue)
                : new Items.Item(3040, float.MaxValue);

            _hextech = new Items.Item(3146, 700);
            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            //D Ezreal
            _config = new Menu("D-Ezreal", "D-Ezreal", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            //Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite")).SetValue(true);
            _config.SubMenu("Combo")
               .AddItem(new MenuItem("ignitehp", "use ignite if Enemy HP%<").SetValue(new Slider(30, 1, 100)));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRE", "Use R if  hit x Enemys")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("MinTargets", "Auto R if Hit X Enemys").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("Minrange", "Min R range to Use").SetValue(new Slider(800, 0, 1500)));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("Maxrange", "Max R range to Use").SetValue(new Slider(2500, 1500, 3000)));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseWH", "Use W")).SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "AutoHarass (toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Farm
            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(new MenuItem("Lanemana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("ActiveLane", "Farm key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lasthit", "Lasthit"));
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(new MenuItem("lastmana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(
                    new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("JungleClear", "JungleClear"));
            _config.SubMenu("Farm").SubMenu("JungleClear").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("JungleClear")
                .AddItem(new MenuItem("Junglemana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("JungleClear")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle key").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            //items
            _config.AddSubMenu(new Menu("items", "items"));
            _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextech", "Hextech Gunblade"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("HextechEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextechmyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));

            _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
            _config.SubMenu("items").SubMenu("Deffensive").AddSubMenu(new Menu("Cleanse", "Cleanse"));
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("useqss", "Use QSS/Mercurial Scimitar/Dervish Blade"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("blind", "Blind"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("charm", "Charm"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("fear", "Fear"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("flee", "Flee"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("snare", "Snare"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("taunt", "Taunt"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("suppression", "Suppression"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("stun", "Stun"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("polymorph", "Polymorph"))
                .SetValue(false);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("silence", "Silence"))
                .SetValue(false);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("zedultexecute", "Zed Ult"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .SubMenu("Cleanse")
                .AddItem(new MenuItem("Cleansemode", "Use Cleanse"))
                .SetValue(new StringList(new string[2] { "Always", "In Combo" }));
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .AddItem(new MenuItem("Archangel", "Seraph's Embrace"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Deffensive")
                .AddItem(new MenuItem("Archangelmyhp", "If My HP% <").SetValue(new Slider(85, 1, 100)));

            //potions
            _config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usehppotions", "Use Healt potion/Refillable/Hunters/Corrupting/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionhp", "If Health % <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usemppotions", "Use Hunters/Corrupting/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionmp", "If Mana % <").SetValue(new Slider(35, 1, 100)));


            //Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc")
                .AddItem(new MenuItem("pingulti", "Ping If R Dmg>Enemy Health").SetValue(false));
            _config.SubMenu("Misc").AddItem(new MenuItem("useQK", "Use Q KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useWK", "Use W KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("UseRM", "Use R KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useEK", "Use (E-Q) or (E-W) KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQdash", "Auto Q dashing")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQimmo", "Auto Q Immobile")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQstun", "Auto Q Taunt/Fear/Charm/Snare")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("Gap_E", "Use E to Gapcloser")).SetValue(true);
            

            //Drawings
            _config.AddSubMenu(new Menu("Drawings", "Drawings"));
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(false);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(false);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(false);
            _config.SubMenu("Drawings").AddItem(new MenuItem("Drawharass", "Draw AutoHarass")).SetValue(true);


            _config.AddToMainMenu();
            Game.PrintChat("<font color='#881df2'>D-Ezreal by Diabaths</font> Loaded.");
            Game.PrintChat(
                 "<font color='#f2f21d'>Do you like it???  </font> <font color='#ff1900'>Drop 1 Upvote in Database </font>");
            Game.PrintChat(
                "<font color='#f2f21d'>Buy me cigars </font> <font color='#ff1900'>ssssssssssmith@hotmail.com</font> (10) S");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;
            _r.Range = _config.Item("Maxrange").GetValue<Slider>().Value;
            if (_config.Item("pingulti").GetValue<bool>())
            {
                foreach (var enemy in
                    Get<Obj_AI_Hero>()
                        .Where(
                            hero =>
                            Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready && hero.IsValidTarget(30000)
                            && _player.GetSpellDamage(hero, SpellSlot.R) * 0.9 > hero.Health
                            && Player.Distance(hero) > 1000))
                {

                    Ping(enemy.Position.To2D());
                }
            }

            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var qpred = _q.GetPrediction(target);
            var manacheck = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost
                            + _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            if (target.IsValidTarget(_q.Range) && !_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                if (_player.Mana >= manacheck && qpred.CollisionObjects.Count == 0 && _q.IsReady()
                    && _config.Item("useQimmo").GetValue<bool>())
                {
                    _q.CastIfHitchanceEquals(target, HitChance.Immobile, true);
                }
                if (_player.Mana >= manacheck && qpred.CollisionObjects.Count == 0 && _q.IsReady()
                    && _config.Item("useQdash").GetValue<bool>())
                {
                    _q.CastIfHitchanceEquals(target, HitChance.Dashing, true);
                }
                if (_player.Mana >= manacheck && qpred.CollisionObjects.Count == 0 && _q.IsReady()
                    && _config.Item("useQstun").GetValue<bool>()
                    && (target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Charm)
                        || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Taunt)))
                {
                    _q.CastIfHitchanceEquals(target, HitChance.High, true);
                }
            }
            _player = Player;
            _orbwalker.SetAttack(true);
            if (_config.Item("ActiveJungle").GetValue<KeyBind>().Active
                && 100 * (_player.Mana / _player.MaxMana) > _config.Item("Junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if ((_config.Item("ActiveHarass").GetValue<KeyBind>().Active
                 || _config.Item("harasstoggle").GetValue<KeyBind>().Active)
                && 100 * (_player.Mana / _player.MaxMana) > _config.Item("Harrasmana").GetValue<Slider>().Value
                && !_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Harass();
            }

            if (!_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                KillSteal();
            }

            Usepotion();
            Usecleanse();
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_e.IsReady() && gapcloser.Sender.Distance(_player.ServerPosition) <= 250 &&
              _config.Item("Gap_E").GetValue<bool>())
            {
                _e.Cast(Player.Position.Extend(gapcloser.Sender.Position, -_e.Range));
            }
        }
        
        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            var useQH = _config.Item("UseQH").GetValue<bool>();
            var useWH = _config.Item("UseWH").GetValue<bool>();
            var useqlast = _config.Item("UseQLH").GetValue<bool>();
            var useqlane = _config.Item("UseQL").GetValue<bool>();
            var lastmana = (100 * (_player.Mana / _player.MaxMana)) > _config.Item("lastmana").GetValue<Slider>().Value;
            var lanemana = (100 * (_player.Mana / _player.MaxMana)) > _config.Item("Lanemana").GetValue<Slider>().Value
                           || (100 * (_player.Mana / _player.MaxMana))
                           > _config.Item("Junglemana").GetValue<Slider>().Value;
            var harassmana = (100 * (_player.Mana / _player.MaxMana))
                             > _config.Item("Harrasmana").GetValue<Slider>().Value;
            var combo = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo;
            var harass = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed
                         || _config.Item("harasstoggle").GetValue<KeyBind>().Active;
            var lastHit = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit;
            var laneClear = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear;
            var t = target as Obj_AI_Hero;
            /*if (combo && target is Obj_AI_Hero) //unit.IsMe)
            {
                if (useQ && _q.IsReady() && t.IsValidTarget(_q.Range))
                {
                    var qpred = _q.GetPrediction(t);
                    if ( qpred.CollisionObjects.Count == 0 && _q.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _q.Cast(t);
                    }
                }
                if (useW && _w.IsReady() && t.IsValidTarget(_w.Range))
                {
                   if ( _w.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _w.Cast(t, false, true);
                    }
                }
            }
            if (harass &&  target is Obj_AI_Hero && harassmana && !combo)
            {
                if (useQH && _q.IsReady())
                {
                    var qpred = _q.GetPrediction(t);
                    if (t.IsValidTarget(_q.Range) && qpred.CollisionObjects.Count == 0 && _q.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _q.Cast(t);
                    }
                }
                if (useWH && _w.IsReady())
                {
                    if (t.IsValidTarget(_w.Range) && _w.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _w.Cast(t, false, true);
                    }
                }
            }*/

            //Creditc FlapperDoodle
            if (_q.IsReady() && ((lastHit && lastmana && useqlast) || (laneClear && lanemana && useqlane)))
            {
                int countMinions = 0;
                foreach (var minionDie in
                    MinionManager.GetMinions(_q.Range)
                        .Where(
                            minion => target.NetworkId != minion.NetworkId && minion.IsEnemy /*&&
                                    HealthPrediction.GetHealthPrediction(minion,
                                        (int) ((_player.AttackDelay*600)*2.65f + Game.Ping/1.5), 0) <= 0*/
                                      && (_q.GetDamage(minion) >= minion.Health
                                          || minion.Health < Player.GetAutoAttackDamage(minion)
                                          || minion.Health < _q.GetDamage(minion) + Player.GetAutoAttackDamage(minion)))
                    )
                {
                    countMinions++;

                    if (countMinions >= 1 && _q.GetPrediction(minionDie).Hitchance >= HitChance.High
                        && _q.GetPrediction(minionDie).CollisionObjects.Count == 0) _q.Cast(minionDie);
                }
            }
        }


        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var ignitecombo = _config.Item("UseIgnitecombo").GetValue<bool>();
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            UseItemes();

            if (target.IsValidTarget(600) && _igniteSlot != SpellSlot.Unknown && ignitecombo
                && _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (target.HealthPercent<= _config.Item("ignitehp").GetValue<Slider>().Value)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }

            }
            if (_q.IsReady() && useQ)
            {
                var tq = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
                if (tq.IsValidTarget(_q.Range) && _q.GetPrediction(tq).CollisionObjects.Count == 0
                    && _q.GetPrediction(tq).Hitchance >= HitChance.High)
                {
                    _q.Cast(tq);
                }
            }
            if (_w.IsReady() && useW)
            {
                var tw = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget(_w.Range) && _w.GetPrediction(tw).Hitchance >= HitChance.High)
                {
                    _w.Cast(tw, false, true);
                }
            }

            UseRcombo();
        }

        private static void Harass()
        {
            var useQ = _config.Item("UseQH").GetValue<bool>();
            var useW = _config.Item("UseWH").GetValue<bool>();
            if (_q.IsReady() && useQ)
            {
                var tq = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
                if (tq.IsValidTarget(_q.Range) &&
                    _q.GetPrediction(tq).CollisionObjects.Count == 0 && _q.GetPrediction(tq).Hitchance >= HitChance.High)
                {
                    _q.Cast(tq);
                }
            }

            if (_w.IsReady() && useW)
            {
                var tw = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget(_w.Range) && _w.GetPrediction(tw).Hitchance >= HitChance.High)
                {
                    _w.Cast(tw, false, true);
                }
            }
        }
        
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = _config.Item("UseQJ").GetValue<bool>();
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (useQ && _q.IsReady())
            {
                _q.Cast(mob);
            }
        }

        private static void Usepotion()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var iusehppotion = _config.Item("usehppotions").GetValue<bool>();
            var iusepotionhp = _player.Health <=
                               (_player.MaxHealth * (_config.Item("usepotionhp").GetValue<Slider>().Value) / 100);
            var iusemppotion = _config.Item("usemppotions").GetValue<bool>();
            var iusepotionmp = _player.Mana <=
                               (_player.MaxMana * (_config.Item("usepotionmp").GetValue<Slider>().Value) / 100);
            if (_player.InFountain() || Player.HasBuff("Recall")) return;

            if (Utility.CountEnemiesInRange(800) > 0 ||
                (mobs.Count > 0 && _config.Item("ActiveJungle").GetValue<KeyBind>().Active))
            {
                if (iusepotionhp && iusehppotion &&
                    !(Player.HasBuff("RegenerationPotion") ||
                      Player.HasBuff("ItemMiniRegenPotion")
                      || Player.HasBuff("ItemCrystalFlask") ||
                      Player.HasBuff("ItemCrystalFlaskJungle")
                      || Player.HasBuff("ItemDarkCrystalFlask")))
                {

                    if (Items.HasItem(2010) && Items.CanUseItem(2010))
                    {
                        Items.UseItem(2010);
                    }
                    if (Items.HasItem(2003) && Items.CanUseItem(2003))
                    {
                        Items.UseItem(2003);
                    }
                    if (Items.HasItem(2031) && Items.CanUseItem(2031))
                    {
                        Items.UseItem(2031);
                    }
                    if (Items.HasItem(2032) && Items.CanUseItem(2032))
                    {
                        Items.UseItem(2032);
                    }
                    if (Items.HasItem(2033) && Items.CanUseItem(2033))
                    {
                        Items.UseItem(2033);
                    }
                }
                if (iusepotionmp && iusemppotion &&
                    !(Player.HasBuff("ItemDarkCrystalFlask") ||
                      Player.HasBuff("ItemMiniRegenPotion") ||
                      Player.HasBuff("ItemCrystalFlaskJungle") ||
                      Player.HasBuff("ItemCrystalFlask")))
                {
                    if (Items.HasItem(2041) && Items.CanUseItem(2041))
                    {
                        Items.UseItem(2041);
                    }
                    if (Items.HasItem(2010) && Items.CanUseItem(2010))
                    {
                        Items.UseItem(2010);
                    }
                    if (Items.HasItem(2032) && Items.CanUseItem(2032))
                    {
                        Items.UseItem(2032);
                    }
                    if (Items.HasItem(2033) && Items.CanUseItem(2033))
                    {
                        Items.UseItem(2033);
                    }
                }
            }
        }

        private static void UseItemes()
        {
            foreach (var hero in Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var iBilge = _config.Item("Bilge").GetValue<bool>();
                var iBilgeEnemyhp = hero.Health <=
                                    (hero.MaxHealth*(_config.Item("BilgeEnemyhp").GetValue<Slider>().Value)/100);
                var iBilgemyhp = _player.Health <=
                                 (_player.MaxHealth*(_config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
                var iBlade = _config.Item("Blade").GetValue<bool>();
                var iBladeEnemyhp = hero.Health <=
                                    (hero.MaxHealth*(_config.Item("BladeEnemyhp").GetValue<Slider>().Value)/100);
                var iBlademyhp = _player.Health <=
                                 (_player.MaxHealth*(_config.Item("Blademyhp").GetValue<Slider>().Value)/100);
                var iYoumuu = _config.Item("Youmuu").GetValue<bool>();
                var iHextech = _config.Item("Hextech").GetValue<bool>();
                var iHextechEnemyhp = hero.Health <=
                                      (hero.MaxHealth*(_config.Item("HextechEnemyhp").GetValue<Slider>().Value)/100);
                var iHextechmyhp = _player.Health <=
                                   (_player.MaxHealth*(_config.Item("Hextechmyhp").GetValue<Slider>().Value)/100);

                var iArchange = _config.Item("Archangel").GetValue<bool>();
                var iArchangelmyhp = _player.Health <=
                                     (_player.MaxHealth*(_config.Item("Archangelmyhp").GetValue<Slider>().Value)/100);

                if (hero.IsValidTarget(450) && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
                {
                    _bilge.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
                {

                    _blade.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iYoumuu && _youmuu.IsReady())
                {
                    _youmuu.Cast();
                }
                if (hero.IsValidTarget(700) && iHextech && (iHextechEnemyhp || iHextechmyhp) && _hextech.IsReady())
                {
                    _hextech.Cast(hero);
                }
                if (iArchange && iArchangelmyhp && _archangel.IsReady() && Utility.CountEnemiesInRange(800) > 0)
                {
                    _archangel.Cast();
                }
            }
        }

        private static void KillSteal()
        {
            foreach (var hero in Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var useq = _config.Item("useQK").GetValue<bool>();
                var usew = _config.Item("useWK").GetValue<bool>();
                var usee = _config.Item("useEK").GetValue<bool>();
                var user = _config.Item("UseRM").GetValue<bool>();
                var whDmg = _player.GetSpellDamage(hero, SpellSlot.W);
                var qhDmg = _player.GetSpellDamage(hero, SpellSlot.Q);
                var rhDmg = _player.GetSpellDamage(hero, SpellSlot.R);
                var emana = _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
                var wmana = _player.Spellbook.GetSpell(SpellSlot.W).ManaCost;
                var qmana = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                var rmana = _player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
                var minrange = _config.Item("Minrange").GetValue<Slider>().Value;
                if (usew && _w.IsReady() && whDmg - 20 > hero.Health && !hero.IsInvulnerable)
                {
                    if (hero.IsValidTarget(_w.Range) && _player.Mana > wmana + emana)
                    {
                        _w.CastIfHitchanceEquals(hero, HitChance.High);
                    }
                    if (usee && !hero.IsValidTarget(_w.Range) && hero.IsValidTarget(_w.Range + _e.Range)
                        && _player.Mana > wmana + emana && _e.IsReady())
                    {
                        _e.Cast(hero.Position);
                        _w.CastIfHitchanceEquals(hero, HitChance.High);
                    }
                }
                if (useq && _q.IsReady() && qhDmg - 20 > hero.Health && !hero.IsInvulnerable)
                {
                    if (_player.Mana > qmana && hero.IsValidTarget(_q.Range)
                        && _q.GetPrediction(hero).CollisionObjects.Count == 0)
                    {
                        _q.CastIfHitchanceEquals(hero, HitChance.High);
                    }
                    if (_e.IsReady() && usee && !hero.IsValidTarget(_q.Range) && _player.Mana > qmana + emana
                        && hero.IsValidTarget(_q.Range + _e.Range)
                        && _q.GetPrediction(hero).Hitchance >= HitChance.VeryHigh)
                    {
                        _e.Cast(hero.Position);
                        _q.Cast(hero);
                    }
                }
                if (user && rhDmg - 20 > hero.Health && hero.IsValidTarget(_r.Range))
                {
                    if (!hero.IsInvulnerable && _player.Distance(hero) >= minrange)
                    {
                        if (_q.IsReady() && _w.IsReady()
                            && hero.Health
                            <= _player.GetSpellDamage(hero, SpellSlot.Q) + _player.GetSpellDamage(hero, SpellSlot.W)
                            && hero.IsValidTarget(_q.Range)) return;
                        if (_q.IsReady() && hero.Health <= _player.GetSpellDamage(hero, SpellSlot.Q)
                            && hero.IsValidTarget(_q.Range)) return;
                        if (_w.IsReady() && hero.Health <= _player.GetSpellDamage(hero, SpellSlot.W)
                            && hero.IsValidTarget(_w.Range)) return;
                        if (_r.IsReady()) _r.CastIfHitchanceEquals(hero, HitChance.High, true);
                    }
                }
            }
        }

        private static void UseRcombo()
        {
            foreach (var hero in Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                if (!_r.IsReady()) return;
                var minrange = _config.Item("Minrange").GetValue<Slider>().Value;
                var rsolo = _config.Item("UseRC").GetValue<bool>();
                var autoR = _config.Item("UseRE").GetValue<bool>();
                var rDmg = _player.GetSpellDamage(hero, SpellSlot.R) * 0.9;
                if (hero.IsInvulnerable) return;
                if (_player.Distance(hero) >= minrange && hero.IsValidTarget(_r.Range) && rDmg > hero.Health)
                {
                    if (rsolo)
                    {
                        if (_q.IsReady() && _w.IsReady()
                            && hero.Health
                            <= _player.GetSpellDamage(hero, SpellSlot.Q) + _player.GetSpellDamage(hero, SpellSlot.W)
                            && hero.IsValidTarget(_q.Range)) return;
                        if (_q.IsReady() && hero.Health <= _player.GetSpellDamage(hero, SpellSlot.Q)
                            && hero.IsValidTarget(_q.Range)) return;
                        if (_w.IsReady() && hero.Health <= _player.GetSpellDamage(hero, SpellSlot.W)
                            && hero.IsValidTarget(_w.Range)) return;
                        if (!hero.IsDead) _r.CastIfHitchanceEquals(hero, HitChance.VeryHigh);
                    }
                }
                if (autoR)
                {
                    var fuckr = _r.GetPrediction(hero, true);
                    if (fuckr.AoeTargetsHitCount >= _config.Item("MinTargets").GetValue<Slider>().Value)
                    {
                        _r.CastIfHitchanceEquals(hero, HitChance.High);
                    }
                }
            }
        }




        private static void Usecleanse()
        {
            if (_player.IsDead ||
                (_config.Item("Cleansemode").GetValue<StringList>().SelectedIndex == 1 &&
                 !_config.Item("ActiveCombo").GetValue<KeyBind>().Active)) return;
            if (Cleanse(_player) && _config.Item("useqss").GetValue<bool>())
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3140));
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3139));
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137))
                        Utility.DelayAction.Add(1000, () => Items.UseItem(3137));
                }
                else
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140)) Items.UseItem(3140);
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139)) Items.UseItem(3139);
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137)) Items.UseItem(3137);
                }
            }
        }

        private static bool Cleanse(Obj_AI_Hero hero)
        {
            var cc = false;
            if (_config.Item("blind").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Blind))
                {
                    cc = true;
                }
            }
            if (_config.Item("charm").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Charm))
                {
                    cc = true;
                }
            }
            if (_config.Item("fear").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Fear))
                {
                    cc = true;
                }
            }
            if (_config.Item("flee").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Flee))
                {
                    cc = true;
                }
            }
            if (_config.Item("snare").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Snare))
                {
                    cc = true;
                }
            }
            if (_config.Item("taunt").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Taunt))
                {
                    cc = true;
                }
            }
            if (_config.Item("suppression").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Suppression))
                {
                    cc = true;
                }
            }
            if (_config.Item("stun").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Stun))
                {
                    cc = true;
                }
            }
            if (_config.Item("polymorph").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Polymorph))
                {
                    cc = true;
                }
            }
            if (_config.Item("silence").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Silence))
                {
                    cc = true;
                }
            }
            if (_config.Item("zedultexecute").GetValue<bool>())
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    cc = true;
                }
            }
            return cc;
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            var harass = (_config.Item("harasstoggle").GetValue<KeyBind>().Active);

            if (_config.Item("Drawharass").GetValue<bool>())
            {
                if (harass)
                {
                    Drawing.DrawText(Drawing.Width*0.02f, Drawing.Height*0.92f, System.Drawing.Color.GreenYellow,
                        "Auto harass Enabled");
                }
                else
                    Drawing.DrawText(Drawing.Width*0.02f, Drawing.Height*0.92f, System.Drawing.Color.OrangeRed,
                        "Auto harass Disabled");
            }
            if (_config.Item("DrawQ").GetValue<bool>() && _q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, _q.Range, _q.IsReady() ? System.Drawing.Color.GreenYellow : System.Drawing.Color.OrangeRed);
            }
            if (_config.Item("DrawW").GetValue<bool>() && _w.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, _w.Range, System.Drawing.Color.GreenYellow);
            }
            if (_config.Item("DrawE").GetValue<bool>() && _e.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, _e.Range, System.Drawing.Color.GreenYellow);
            }

            if (_config.Item("DrawR").GetValue<bool>() && _r.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, _r.Range, System.Drawing.Color.GreenYellow);
            }
        }
    }
}



