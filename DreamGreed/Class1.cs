using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.Linq;
using System.IO;
using System.Security.Permissions;
using System.Security;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Text.RegularExpressions;
using System.Collections;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace DreamGreed
{
    [BepInPlugin("com.myname.DreamGreed", "DreamGreed", "1.0.0")]
    public class Base : BaseUnityPlugin
    {
        public void Awake()
        {
            /*On.Chest.Interactive += Chest_Interactive;
            On.ChestSpawner.SetRoom += ChestSpawner_SetRoom;
            On.Chest.CoinCoroutine += Chest_CoinCoroutine;*/
            On.StatusUtility.GetDaisyStatus += StatusUtility_GetDaisyStatus;
            On.StatusUtility.GetRandomStatus += StatusUtility_GetRandomStatus;
            On.StatusUtility.GetAnvilLowStatusCandidates += StatusUtility_GetAnvilLowStatusCandidates;
            On.StatusUtility.GetAnvilStatusCandidates += StatusUtility_GetAnvilStatusCandidates;
            On.StatusUtility.GetAnvilStatus += StatusUtility_GetAnvilStatus;
            On.StatusUtility.GetRandomExoticStatus += StatusUtility_GetRandomExoticStatus;
            On.StatusUtility.GetBossStatus += StatusUtility_GetBossStatus;
            On.Anvil.BuildItemEffects += Anvil_BuildItemEffects;
            On.DungeonNPC_Food.GenerateFoodRandomly += DungeonNPC_Food_GenerateFoodRandomly;
            On.DungeonNPC_Shop.GetShopItems += DungeonNPC_Shop_GetShopItems;
            On.DungeonNPC_Shop.PickShopItems += DungeonNPC_Shop_PickShopItems;
            On.MyFood.ctor_MyFoodData += MyFood_ctor_MyFoodData;
            On.MyFood.AmplifyRandom += MyFood_AmplifyRandom;
            On.Morpheus.Heal += Morpheus_Heal;
            On.PlagueDoctor.GetItem += PlagueDoctor_GetItem;
            On.DaisyStatue.Awake += DaisyStatue_Awake;
        }

        private void MyFood_ctor_MyFoodData(On.MyFood.orig_ctor_MyFoodData orig, MyFood self, MyFoodData data)
        {
            self.baseData = data;
            if (data.randomPowerMin != 0f || data.randomPowerMax != 0f)
            {
                self.powerEffect = new StatusModule_Power(Mathf.RoundToInt(data.randomPowerMax));
            }
            else
            {
                self.powerEffect = null;
            }
            self.convertedEffects = StatusUtility.FindAll(data.buffEffects);
            self.healRatio = 1f;
        }

        private void MyFood_AmplifyRandom(On.MyFood.orig_AmplifyRandom orig, MyFood self)
        {
            if (!self.baseData.canAmplify)
            {
                return;
            }
            for (int i = 0; i < self.convertedEffects.Length; i++)
            {
                if (self.convertedEffects[i] != null)
                {
                    if (self.convertedEffects[i].Amplified)
                    {
                        self.convertedEffects[i].ClearAmplifyEffect();
                    }
                    self.convertedEffects[i].AmplifyEffect(UnityEngine.Random.Range(2f, 2f));
                }
            }
        }

        private void DaisyStatue_Awake(On.DaisyStatue.orig_Awake orig, DaisyStatue self)
        {
            /*(self as InteractiveObject).Awake();*/
            self._miniMapObject = base.GetComponent<MiniMapObject>();
            self._mapObject = base.GetComponent<MapObject>();
            if (StageManager.Instance != null && StageManager.Instance.current != null)
            {
                UnityEngine.Random.InitState(StageManager.Instance.current.GeneratedSeed);
            }
            if (UnityEngine.Random.Range(1f, 1f) <= 0.5f)
            {
                self.currentRingData = self.gerberaRingData;
                self.CurrentCost = self.gerberaRingCost;
            }
            else
            {
                self.currentRingData = self.daisyRingData;
                self.CurrentCost = self.daisyRingCost;
            }
            if (self.currentRingData)
            {
                self.ringRenderer.sprite = self.currentRingData.icon;
                self.preview = new ItemOwnInfo(self.currentRingData.id, ItemOwnInfo.OwnType.NORMAL, ItemOwnInfo.BitrhType.None);
            }
            UnityEngine.UI.Text text = self.soulText;
            DaisyStatue.Cost currentCost = self.CurrentCost;
            text.text = currentCost.soul.ToString();
            UnityEngine.UI.Text text2 = self.goldText;
            currentCost = self.CurrentCost;
            text2.text = currentCost.money.ToString();
        }

        private void PlagueDoctor_GetItem(On.PlagueDoctor.orig_GetItem orig, PlagueDoctor self)
        {
            SoundManager.PlayFX(self.healClip, 1f);
            self.Use();
            GameManager.Instance.currentPlayer._creature.ApplyDamage(null, 20f, UnityEngine.Color.red, "RandomEncounter_PlagueDoctor_Name", false, false, false);
            UIManager.Instance.sparkle.Spark(1f, 1f, 0f, 0f);
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(self.perkData.resourcePrefab, Vector3.zero, Quaternion.identity);
            if (gameObject)
            {
                Accessory_StrangeCure component = gameObject.GetComponent<Accessory_StrangeCure>();
                if (component)
                {
                    if (component.attributePlayer)
                    {
                        component.transform.SetParent(GameManager.Instance.currentPlayer.transform);
                    }
                    component.name = "[RANDOM_ENCOUNTER_ACCESSORY]" + component.name;
                    component.heal_MaxValue = UnityEngine.Random.Range(self.heal_MaxValue, self.heal_MaxValue);
                    component.heal_Value = self.heal_Value;
                    component.heal_Interval = self.heal_Interval;
                    component.SetPlayer(GameManager.Instance.currentPlayer);
                    Player.PerkData data = new Player.PerkData(self.perkData.id, gameObject);
                    GameManager.Instance.currentPlayer.AddPerk(data);
                }
            }
            AnalyticsManager.CustomEvent("RandomEvent_PlagueDoctorUsed", new Dictionary<string, object>
    {
        {
            "floor",
            AnalyticsManager.GetCurrentFloor()
        }
    });
            self.SaveState();
            if (StageManager.Instance)
            {
                StageManager.Instance.SaveCurrentDungeon();
            }
        }

        private void Morpheus_Heal(On.Morpheus.orig_Heal orig, Morpheus self)
        {
            self.Use();
            self.SaveState();
            if (self.propSeed)
            {
                UnityEngine.Random.InitState(self.propSeed.Seed);
            }
            bool flag = UnityEngine.Random.Range(1f, 1f) >= 0.34f;
            int num = UnityEngine.Random.Range(5, 5) * 2;
            if (!flag)
            {
                num++;
            }
            UIManager.Instance.Narration(I._("RandomEncounter_Morpheus_Effect_" + num.ToString()), 2.5f);
            Player currentPlayer = GameManager.Instance.currentPlayer;
            string text = "";
            switch (num)
            {
                case 0:
                    text = currentPlayer._creature.HealPercent(0.5f).ToString("F0");
                    break;
                case 1:
                    text = currentPlayer._creature.HealPercent(-0.25f).ToString("F0");
                    break;
                case 2:
                    if (!GameManager.Instance.currentPlayer._creature.status.lockHP)
                    {
                        float curHPRatio = currentPlayer._creature.status.GetCurHPRatio();
                        currentPlayer.AddSavableStatus(new Status
                        {
                            bonusHPRatio = 0.2f
                        });
                        currentPlayer._creature.status.hp = currentPlayer._creature.status.GetMAXHP() * curHPRatio;
                    }
                    text = "20%";
                    break;
                case 3:
                    if (!GameManager.Instance.currentPlayer._creature.status.lockHP)
                    {
                        float curHPRatio2 = currentPlayer._creature.status.GetCurHPRatio();
                        currentPlayer.AddSavableStatus(new Status
                        {
                            bonusHPRatio = -0.1f
                        });
                        currentPlayer._creature.status.hp = currentPlayer._creature.status.GetMAXHP() * curHPRatio2;
                    }
                    text = "-10%";
                    break;
                case 4:
                    {
                        Status status = new Status();
                        status.power += 20;
                        currentPlayer.AddSavableStatus(status);
                        text = "20";
                        break;
                    }
                case 5:
                    {
                        Status status2 = new Status();
                        status2.power -= 10;
                        currentPlayer.AddSavableStatus(status2);
                        text = "-10";
                        break;
                    }
                case 6:
                    {
                        Status status3 = new Status();
                        status3.defense += 10f;
                        currentPlayer.AddSavableStatus(status3);
                        text = "10";
                        break;
                    }
                case 7:
                    {
                        Status status4 = new Status();
                        status4.defense -= 5f;
                        currentPlayer.AddSavableStatus(status4);
                        text = "-5";
                        break;
                    }
                case 8:
                    currentPlayer.AddSavableAtkSpeedBonus(10f);
                    currentPlayer.AddSavableReloadSpeedBonus(10f);
                    text = "10";
                    break;
                case 9:
                    currentPlayer.AddSavableAtkSpeedBonus(-5f);
                    currentPlayer.AddSavableReloadSpeedBonus(-5f);
                    text = "-5";
                    break;
                case 10:
                    currentPlayer.UseSatiety(40);
                    text = "40";
                    break;
                case 11:
                    currentPlayer.AddSatiety(20);
                    text = "-20";
                    break;
            }
            UnityEngine.Color color = flag ? UnityEngine.Color.green : UnityEngine.Color.red;
            if (flag)
            {
                SoundManager.PlayFX(self.positiveSound, 1f);
            }
            else
            {
                SoundManager.PlayFX(self.negativeSound, 1f);
            }
            if (text != "")
            {
                WorldUIManager.Instance.DamageText(currentPlayer._creature.CenterPosition, text, false, color, color);
            }
            AnalyticsManager.CustomEvent("RandomEvent_MorpheusUsed", new Dictionary<string, object>
            {
                {
                    "floor",
                    AnalyticsManager.GetCurrentFloor()
                },
                {
                    "id",
                    "effect" + num.ToString()
                },
                {
                    "value",
                    text
                }
            });
            if (StageManager.Instance)
            {
                StageManager.Instance.SaveCurrentDungeon();
            }
        }

        private ShopItemInfo[] DungeonNPC_Shop_PickShopItems(On.DungeonNPC_Shop.orig_PickShopItems orig, DungeonNPC_Shop self)
        {
            if (StageManager.Instance != null && StageManager.Instance.current != null)
            {
                float floorProgress = StageManager.Instance.current.FloorProgress;
            }
            List<MyItemData> shopItems = self.GetShopItems();
            bool flag = false;
            if (StageManager.Instance && StageManager.Instance.current != null && StageManager.Instance.current.FloorProgress >= 0.6f && UnityEngine.Random.Range(0, 10) == 0)
            {
                flag = true;
                UnityEngine.Debug.Log("SET");
            }
            if (flag)
            {
                int index = UnityEngine.Random.Range(0, shopItems.Count);
                MyItemData myItemData = null;
                if (GameManager.Instance)
                {
                    List<int> list = new List<int>();
                    Dictionary<MySetEffectData, int> dictionary = new Dictionary<MySetEffectData, int>();
                    List<ItemOwnInfo> allItems = GameManager.Instance.currentPlayer.AllItems;
                    foreach (ItemOwnInfo itemOwnInfo in allItems)
                    {
                        if (!list.Contains(itemOwnInfo.itemCode))
                        {
                            list.Add(itemOwnInfo.itemCode);
                            List<MySetEffectData> containedSet = MySetEffectManager.Instance.GetContainedSet(itemOwnInfo.itemCode);
                            for (int i = 0; i < containedSet.Count; i++)
                            {
                                bool flag2 = true;
                                foreach (MyItemData item in containedSet[i].AllItems)
                                {
                                    if (!MyItemManager.Instance.CheckAvailableItem(item))
                                    {
                                        flag2 = false;
                                        break;
                                    }
                                }
                                if (!flag2)
                                {
                                    containedSet.RemoveAt(i--);
                                }
                            }
                            foreach (MySetEffectData mySetEffectData in containedSet)
                            {
                                if (!dictionary.ContainsKey(mySetEffectData))
                                {
                                    dictionary.Add(mySetEffectData, 1);
                                }
                                else
                                {
                                    Dictionary<MySetEffectData, int> dictionary2 = dictionary;
                                    MySetEffectData key = mySetEffectData;
                                    int num = dictionary2[key];
                                    dictionary2[key] = num + 1;
                                    if (dictionary[mySetEffectData] >= mySetEffectData.ItemCount)
                                    {
                                        dictionary[mySetEffectData] = 0;
                                    }
                                }
                            }
                        }
                    }
                    List<MySetEffectData> list2 = new List<MySetEffectData>();
                    foreach (MySetEffectData mySetEffectData2 in dictionary.Keys)
                    {
                        if (dictionary[mySetEffectData2] > 0)
                        {
                            list2.Add(mySetEffectData2);
                        }
                    }
                    if (list2.Count > 0)
                    {
                        List<MyItemData> allItems2 = list2[UnityEngine.Random.Range(0, list2.Count)].AllItems;
                        foreach (ItemOwnInfo itemOwnInfo2 in allItems)
                        {
                            MyItemData item2 = MyItemManager.Instance.GetItem(itemOwnInfo2.itemCode);
                            if (allItems2.Contains(item2))
                            {
                                allItems2.Remove(item2);
                            }
                        }
                        if (allItems2.Count > 0)
                        {
                            myItemData = allItems2[UnityEngine.Random.Range(0, allItems2.Count)];
                        }
                    }
                }
                if (myItemData)
                {
                    shopItems[index] = myItemData;
                }
            }
            return self.CreateItemInfos(shopItems);
        }

        private List<MyItemData> DungeonNPC_Shop_GetShopItems(On.DungeonNPC_Shop.orig_GetShopItems orig, DungeonNPC_Shop self)
        {
            List<MyItemData> list = new List<MyItemData>();
            int num = UnityEngine.Random.Range(7, 7);
            if (StageManager.Instance && StageManager.Instance.current.FloorProgress >= 0.5f)
            {
                num++;
            }
            if (num > 6)
            {
                num = 6;
            }
            float uncommon = 0.35f;
            float rare = 0.35f;
            float legend = 0.20f;
            if (StageManager.Instance)
            {
                uncommon = Mathf.Lerp(0.35f, 0.35f, StageManager.Instance.current.FloorProgress);
                rare = Mathf.Lerp(0.35f, 0.35f, StageManager.Instance.current.FloorProgress);
                legend = Mathf.Lerp(0.20f, 0.20f, StageManager.Instance.current.FloorProgress);
            }
            for (int i = 0; i < num; i++)
            {
                MyItemData randomItemAvailableWithCriteria = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(uncommon, rare, legend, true);
                if (randomItemAvailableWithCriteria && (randomItemAvailableWithCriteria.id == 1 || !randomItemAvailableWithCriteria.sellAtShop || list.Contains(randomItemAvailableWithCriteria)))
                {
                    i--;
                }
                else
                {
                    list.Add(randomItemAvailableWithCriteria);
                }
            }
            return list;
        }

        private MyFood[] DungeonNPC_Food_GenerateFoodRandomly(On.DungeonNPC_Food.orig_GenerateFoodRandomly orig, DungeonNPC_Food self)
        {
            List<MyFoodData> list = new List<MyFoodData>();
            int num = UnityEngine.Random.Range(6, 6);
            MyFoodData[] foodsWithType = MyFoodManager.Instance.GetFoodsWithType(MyFoodData.FoodType.NORMAL);
            for (int i = 0; i < num; i++)
            {
                MyFoodData item = foodsWithType[UnityEngine.Random.Range(0, foodsWithType.Length)];
                if (list.Contains(item))
                {
                    i--;
                }
                else
                {
                    list.Add(item);
                }
            }
            int num2 = 0;
            float num3 = UnityEngine.Random.Range(0f, 0f);
            if (num3 <= 0.01f)
            {
                num2 = 2;
            }
            else if (num3 <= 0.15f)
            {
                num2 = 1;
            }
            MyFoodData[] foodsWithType2 = MyFoodManager.Instance.GetFoodsWithType(MyFoodData.FoodType.SATIETY_DECREASE);
            for (int j = 0; j < num2; j++)
            {
                MyFoodData item2 = foodsWithType2[UnityEngine.Random.Range(0, foodsWithType2.Length)];
                if (list.Contains(item2))
                {
                    j--;
                }
                else
                {
                    list.Add(item2);
                }
            }
            int num4 = 0;
            float num5 = UnityEngine.Random.Range(0f, 0f);
            if (num5 <= 0.08f)
            {
                num4 = 2;
            }
            else if (num5 <= 0.36f)
            {
                num4 = 1;
            }
            MyFoodData[] foodsWithType3 = MyFoodManager.Instance.GetFoodsWithType(MyFoodData.FoodType.SPECIALTY);
            if (GameManager.Instance.currentPlayer.foodAlreadyViewed.Count >= foodsWithType3.Length)
            {
                UnityEngine.Debug.Log("볼 수 있는 특식을 다 봤음");
                num4 = 0;
            }
            else if (GameManager.Instance.currentPlayer.foodAlreadyViewed.Count >= foodsWithType3.Length - num4)
            {
                UnityEngine.Debug.Log("특식 " + num4.ToString() + "개 떠야하는데 볼 수 있는 특식 갯수가 그보다 적음");
                num4 = foodsWithType3.Length - GameManager.Instance.currentPlayer.foodAlreadyViewed.Count;
            }
            for (int k = 0; k < num4; k++)
            {
                MyFoodData item3 = foodsWithType3[UnityEngine.Random.Range(0, foodsWithType3.Length)];
                if (list.Contains(item3))
                {
                    k--;
                }
                else if (GameManager.Instance.currentPlayer.foodAlreadyViewed.Contains(item3))
                {
                    k--;
                }
                else
                {
                    GameManager.Instance.currentPlayer.foodAlreadyViewed.Add(item3);
                    list.Add(item3);
                }
            }
            int num6 = 1;
            if (StageManager.Instance != null && StageManager.Instance.current != null)
            {
                num6 = StageManager.Instance.currentFloor;
            }
            MyFood[] array = new MyFood[list.Count];
            for (int l = 0; l < list.Count; l++)
            {
                array[l] = new MyFood(list[l]);
                if (self.propSeed != null)
                {
                    UnityEngine.Random.InitState(self.propSeed.Seed + self.rerollCount + list[l].id);
                }
                array[l].AmplifyRandom();
                array[l].additionalSatiety = UnityEngine.Random.Range(0, 0);
                if (num6 <= 5)
                {
                    array[l].additionalPrice = Mathf.RoundToInt((float)array[l].BaseData.basePrice * UnityEngine.Random.Range(-0.25f, -0.25f) / 10f) * 10;
                }
                else if (num6 <= 10)
                {
                    array[l].additionalPrice = Mathf.RoundToInt((float)array[l].BaseData.basePrice * UnityEngine.Random.Range(0f, 0f) / 10f) * 10;
                }
                else
                {
                    array[l].additionalPrice = Mathf.RoundToInt((float)array[l].BaseData.basePrice * UnityEngine.Random.Range(0.25f, 0.25f) / 10f) * 10;
                }
            }
            Array.Sort<MyFood>(array, delegate (MyFood lhs, MyFood rhs)
            {
                if (lhs.BaseData.foodType == rhs.BaseData.foodType)
                {
                    return lhs.BaseData.id.CompareTo(rhs.BaseData.id);
                }
                int num7 = (int)lhs.BaseData.foodType;
                return num7.CompareTo((int)rhs.BaseData.foodType);
            });
            return array;
        }

        private Anvil.EnhancementPreview Anvil_BuildItemEffects(On.Anvil.orig_BuildItemEffects orig, Anvil self, List<string> candidates, ItemOwnInfo item, int seed)
        {
            Anvil.EnhancementPreview enhancementPreview = new Anvil.EnhancementPreview();
            MyItemData item2 = MyItemManager.Instance.GetItem(item.itemCode);
            UnityEngine.Random.InitState(seed);
            float[] array = new float[]
            {
                0.15f,
                0.2f,
                0.25f
            };
            float value = UnityEngine.Random.value;
            int num = 0;
            ItemRarityTier powerRarity = ItemRarityTier.UNCOMMON;
            if (value > 0.33f)
            {
                num = 1;
                powerRarity = ItemRarityTier.RARE;
            }
            else if (value > 0.7f)
            {
                num = 2;
                powerRarity = ItemRarityTier.LEGEND;
            }
            enhancementPreview.power = array[num];
            if (item2.GetItemType() == ItemType.ACCESSORY)
            {
                enhancementPreview.power = 0f;
            }
            else if (item2.GetItemType() == ItemType.WEAPON)
            {
                MyWeaponData myWeaponData = item2 as MyWeaponData;
                if (myWeaponData && Mathf.Abs(myWeaponData.damage) <= 1E-45f && Mathf.Abs(myWeaponData.maxDamage) <= 1E-45f && Mathf.Abs(myWeaponData.defense) <= 1E-45f)
                {
                    enhancementPreview.power = 0f;
                }
            }
            enhancementPreview.powerRarity = powerRarity;
            List<string> list = new List<string>();
            int index = Mathf.RoundToInt(UnityEngine.Random.value * 10000f) % candidates.Count;
            list.Add(candidates[index]);
            candidates.RemoveAt(index);
            float num2 = UnityEngine.Random.value;
            if (num == 0)
            {
                num2 += 0.35f;
            }
            if (num != 2 && num2 > 0.7f)
            {
                List<string> anvilLowStatusCandidates = StatusUtility.GetAnvilLowStatusCandidates(item2);
                int index2 = Mathf.RoundToInt(UnityEngine.Random.value * 10000f) % anvilLowStatusCandidates.Count;
                list.Add(anvilLowStatusCandidates[index2]);
            }
            enhancementPreview.status = list.ToArray();
            return enhancementPreview;
        }

        private string StatusUtility_GetBossStatus(On.StatusUtility.orig_GetBossStatus orig, MyItemData item)
        {
            List<string> list = new List<string>();
            if (item)
            {
                if (item.GetItemType() == ItemType.WEAPON)
                {
                    MyWeaponData myWeaponData = (MyWeaponData)item;
                    if (myWeaponData)
                    {
                        if (myWeaponData.maxShots > 0)
                        {
                            if (myWeaponData.reloadTime >= 4f)
                            {
                                list.Add("RELOAD_SPEED/33");
                            }
                            else
                            {
                                list.Add("RELOAD_SPEED/15");
                            }
                            list.Add("LAST_RELOAD");
                            list.Add("PAIN_DETECTION/" + UnityEngine.Random.Range(myWeaponData.maxShots / 4, myWeaponData.maxShots / 4).ToString());
                        }
                    }
                }
            }
            if (list.Count < 3) { list.Add("POWER/20"); }
            if (list.Count < 3) { list.Add("ATTACK_SPEED/20"); }
            if (list.Count < 3) { list.Add("BOSS_ENEMY_BONUS/" + 20.ToString()); }
            if (list.Count > 0)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }
            return "";
        }

        private string StatusUtility_GetRandomExoticStatus(On.StatusUtility.orig_GetRandomExoticStatus orig, float ratio, MyItemData item)
        {
            List<string> list = new List<string>();
            list.Add(StatusUtility.GetAnvilStatus(item));
            if (item && item.GetItemType() == ItemType.WEAPON)
            {
                MyWeaponData myWeaponData = (MyWeaponData)item;
                if (myWeaponData)
                {
                    if (myWeaponData.maxShots > 0)
                    {
                        list.Add("LAST_RELOAD");
                        list.Add("PAIN_DETECTION/" + UnityEngine.Random.Range(1, myWeaponData.maxShots / 4).ToString());
                    }
                    if (myWeaponData.attackType == WeaponAttackType.BULLET)
                    {
                        list.Add("EXPLOSIVE_BULLET/" + ((int)(myWeaponData.maxDamage / 2f)).ToString());
                    }
                }
            }
            if (list.Count < 3) { list.Add("IGNORE_DEFENSE/15"); }
            if (list.Count < 3) { list.Add("NORMAL_ENEMY_BONUS/15"); }
            if (list.Count < 3) { list.Add("BOSS_ENEMY_BONUS/15"); }
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        private string StatusUtility_GetAnvilStatus(On.StatusUtility.orig_GetAnvilStatus orig, MyItemData item)
        {
            List<string> anvilStatusCandidates = StatusUtility.GetAnvilStatusCandidates(item, UnityEngine.Random.Range(0, 100000));
            if (anvilStatusCandidates.Count > 0)
            {
                return anvilStatusCandidates[UnityEngine.Random.Range(0, anvilStatusCandidates.Count)];
            }
            return "";
        }

        private List<string> StatusUtility_GetAnvilStatusCandidates(On.StatusUtility.orig_GetAnvilStatusCandidates orig, MyItemData item, int seed)
        {
            UnityEngine.Random.InitState(seed);
            List<string> list = new List<string>();
            if (item)
            {
                if (item.GetItemType() == ItemType.WEAPON)
                {
                    MyWeaponData myWeaponData = (MyWeaponData)item;
                    if (myWeaponData.attackType == WeaponAttackType.BULLET)
                    {
                        list.Add("EXPLOSIVE_BULLET/1");
                    }
                    if (myWeaponData.handType != WeaponHandType.OFF_HANDED)
                    {
                        list.Add("GOLD_DAMAGE/2");
                        if (myWeaponData.attackIntervalTime < 0.33f)
                        {
                            list.Add("TRUE_DAMAGE/1");
                        }
                    }
                    if (myWeaponData.maxShots > 0)
                    {
                        if (item.rarity == ItemRarityTier.COMMON || item.rarity == ItemRarityTier.UNCOMMON)
                        {
                            list.Add("INFINITY_SHOT");
                        }
                        if (list.Count < 3) { list.Add("RELOAD_SPEED/50"); }
                    }
                    if (myWeaponData.attackType == WeaponAttackType.MELEE)
                    {
                        if (myWeaponData.handType != WeaponHandType.OFF_HANDED && item.rarity != ItemRarityTier.LEGEND)
                        {
                            list.Add("FULL_SWING/33");
                        }
                        if (list.Count < 3) { list.Add("DASH_ATTACK_DAMAGE/25"); }
                    }
                    
                    if (myWeaponData && myWeaponData.inputType == WeaponInputType.CHARGE)
                    {
                        list.Add("FAST_CHARGE/60");
                    }
                    else if (list.Count < 3)
                    {
                        list.Add("ATTACK_SPEED/10");
                    }
                }
                else if (item.GetItemType() == ItemType.ACCESSORY)
                {
                    if (item.rarity == ItemRarityTier.COMMON || item.rarity == ItemRarityTier.UNCOMMON)
                    {
                        list.Add("POWER_WEAPONSWING/12");
                        list.Add("TRUE_DAMAGE/1");
                    }
                    list.Add("TOUGHNESS/2");
                }
                if (list.Count < 3) { list.Add("IGNORE_DEFENSE/20"); }
                if (list.Count < 3) { list.Add("POWER/10"); }
                if (list.Count < 3) { list.Add("DEFENSE/6"); }
            }
            return list;
        }

        private List<string> StatusUtility_GetAnvilLowStatusCandidates(On.StatusUtility.orig_GetAnvilLowStatusCandidates orig, MyItemData item)
        {
            return new List<string>
            {
                "CRITICAL_DAMAGE_RATE/5",
            };
        }

        private string StatusUtility_GetRandomStatus(On.StatusUtility.orig_GetRandomStatus orig, float floorProgress, ItemRarityTier rarity)
        {
            List<string> list = new List<string>();
            list.Add("POWER/" + Mathf.RoundToInt(UnityEngine.Random.Range(Mathf.Lerp(floorProgress, 10f, 15f), Mathf.Lerp(floorProgress, 10f, 15f))).ToString());
            if (list.Count > 0)
            {
                return list[UnityEngine.Random.Range(0, list.Count)];
            }
            return "";
        }

        private string StatusUtility_GetDaisyStatus(On.StatusUtility.orig_GetDaisyStatus orig)
        {
            List<string> list = new List<string>();
            list.Add("INFINITY_SHOT");
            list.Add("FULL_SWING/40");
            list.Add("POWER/30");
            list.Add("DASH_ATTACK_DAMAGE/45");
            return list.GetRandomElement<string>();
        }

        private void ChestSpawner_SetRoom(On.ChestSpawner.orig_SetRoom orig, ChestSpawner self, IRoom room)
        {
            self.room = room;
            UnityEngine.Random.InitState(room.ProceduralDungeon.Seed);
            self._spriteRenderer = base.GetComponent<SpriteRenderer>();
            if (self._spriteRenderer)
            {
                self._spriteRenderer.enabled = false;
            }
            if (self.isRandom)
            {
                float num = UnityEngine.Random.Range(0f, 0f);
                float num2 = Mathf.Clamp01(Mathf.Lerp(0.1f, 0.13f, StageManager.Instance.current.FloorProgress));
                float num3 = Mathf.Clamp01(Mathf.Lerp(0.2f, 0.3f, StageManager.Instance.current.FloorProgress));
                float num4 = Mathf.Clamp01(Mathf.Lerp(0.35f, 0.45f, StageManager.Instance.current.FloorProgress));
                if (num <= num2)
                {
                    self.chestType = Chest.Type.ONLY_LEGEND;
                }
                else if (num <= num2 + num3)
                {
                    self.chestType = Chest.Type.ONLY_RARE;
                }
                else if (num <= num2 + num3 + num4)
                {
                    self.chestType = Chest.Type.ONLY_UNCOMMON;
                }
                else
                {
                    self.chestType = Chest.Type.ONLY_COMMON;
                }
            }
            GameObject gameObject = null;
            switch (self.chestType)
            {
                case Chest.Type.TUTORIAL:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/TutorialChest"), base.transform);
                    break;
                case Chest.Type.COMMON:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/Old/Chest"), base.transform);
                    break;
                case Chest.Type.BOSS:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/Chest_Boss"), base.transform);
                    break;
                case Chest.Type.UNCOMMON:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/Old/Chest_Uncommon"), base.transform);
                    break;
                case Chest.Type.RARE:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/Old/Chest_Rare"), base.transform);
                    break;
                case Chest.Type.CURSE:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/Chest_Cursed"), base.transform);
                    break;
                case Chest.Type.ONLY_COMMON:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/NormalChest_Common"), base.transform);
                    break;
                case Chest.Type.ONLY_UNCOMMON:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/NormalChest_Uncommon"), base.transform);
                    break;
                case Chest.Type.ONLY_RARE:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/NormalChest_Rare"), base.transform);
                    break;
                case Chest.Type.ONLY_LEGEND:
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Chest/NormalChest_Legend"), base.transform);
                    break;
            }
            if (gameObject)
            {
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localPosition = Vector2.zero;
                PropSeed propSeed = gameObject.AddComponent<PropSeed>();
                propSeed.SetSeed(UnityEngine.Random.Range(0, 100000000));
                Chest component = gameObject.GetComponent<Chest>();
                if (component)
                {
                    component.parentSpawner = self;
                    if (self.isRandom)
                    {
                        component.containExoticStatus = (UnityEngine.Random.Range(0, 0) < 2);
                    }
                    if (room.ProceduralDungeon.GetChestState(propSeed.Seed.ToString()) < 0)
                    {
                        MapObject component2 = component.GetComponent<MapObject>();
                        if (component2)
                        {
                            component2.Initialize(room.ProceduralDungeon);
                        }
                    }
                }
            }
        }

        private void Chest_Interactive(On.Chest.orig_Interactive orig, Chest self, GameObject actor)
        {
            if (GameManager.Instance.currentPlayer && GameManager.Instance.currentPlayer._creature.IsOnBattle)
            {
                UIManager.Instance.Narration(I._("UI_Narration_CantOpenBattle"), 2f);
                return;
            }
            self.Interactive(actor);
            if (self.propSeed != null)
            {
                UnityEngine.Random.InitState(self.propSeed.Seed);
            }
            self.actualUnlocked.Clear();
            Vector3 position = base.transform.position + new Vector3(0f, 1f);
            if (self.openFXPrefab)
            {
                UnityEngine.Object.Instantiate<GameObject>(self.openFXPrefab, position, Quaternion.identity);
            }
            SoundManager.PlayFX(self.openClip, 1f);
            SoundManager.PlayFX(self.additionalOpenClip, 1f);
            for (int i = 0; i < self.unlockBossItem.Count; i++)
            {
                if (self.unlockBossItem[i] && MyItemManager.Instance.GetItemAvailable(self.unlockBossItem[i].id) == null && !self.unlockBossItem[i].disable)
                {
                    MyItemManager.Instance.Unlock(self.unlockBossItem[i].id);
                    self.actualUnlocked.Add(self.unlockBossItem[i]);
                }
            }
            if (self.actualUnlocked.Count > 0)
            {
                base.Invoke("OpenUnlockItemPanel", 1.5f);
            }
            if (self.type == Chest.Type.BOSS || self.type == Chest.Type.GOLEM)
            {
                if (self.type == Chest.Type.BOSS)
                {
                    GameCamera.Instance.bobber.Bob(0.5f, 0.2f);
                    UIManager.Instance.sparkle.Spark(1f, 1f, 1f, 1f);
                    self.CreateSouls();
                    if (GameManager.Instance.lasleyHiddenBoss)
                    {
                        Status status = new Status();
                        status.maxHP = 10f;
                        Creature creature = GameManager.Instance.currentPlayer._creature;
                        float num = ((creature.status.hp < 1f) ? 1f : creature.status.hp) / ((creature.status.GetMAXHP() < 1f) ? 1f : creature.status.GetMAXHP());
                        if (num > 1f)
                        {
                            num = 1f;
                        }
                        GameManager.Instance.currentPlayer.AddSavableStatus(status);
                        creature.status.hp = creature.status.GetMAXHP() * num;
                    }
                }
                else if (self.type == Chest.Type.GOLEM)
                {
                    GameCamera.Instance.bobber.Bob(0.3f, 0.3f);
                    self.CreateSouls();
                }
                self.CreateRewardBubble();
                self.Open();
                if (StageManager.Instance != null && StageManager.Instance.current != null && self.propSeed != null)
                {
                    IProceduralDungeon current = StageManager.Instance.current.GetCurrent();
                    current.AddObjectData(self.saveKeyBase + "_Open", true.ToString());
                    if (current.GetChestState(self.propSeed.Seed.ToString()) == 0)
                    {
                        current.SetChestState(self.propSeed.Seed.ToString(), 1);
                    }
                }
                if (StageManager.Instance)
                {
                    StageManager.Instance.SaveCurrentDungeon();
                }
                return;
            }
            self.Open();
            int id;
            switch (self.type)
            {
                case Chest.Type.TUTORIAL:
                    id = 2001;
                    goto IL_55A;
                case Chest.Type.COMMON:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0.25f, 0.05f, 0f).id;
                    goto IL_55A;
                case Chest.Type.BOSS:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0.7f, 0.3f).id;
                    goto IL_55A;
                case Chest.Type.UNCOMMON:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0.5f, 0.2f, 0.1f).id;
                    goto IL_55A;
                case Chest.Type.RARE:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0.5f, 0.3f, 0.15f).id;
                    goto IL_55A;
                case Chest.Type.GOLEM:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0.5f, 0.5f).id;
                    goto IL_55A;
                case Chest.Type.ONLY_COMMON:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0f, 0f).id;
                    goto IL_55A;
                case Chest.Type.ONLY_UNCOMMON:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(1f, 0f, 0f).id;
                    goto IL_55A;
                case Chest.Type.ONLY_RARE:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 1f, 0f).id;
                    goto IL_55A;
                case Chest.Type.ONLY_LEGEND:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0f, 1f).id;
                    goto IL_55A;
                case Chest.Type.ELITE:
                    id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0.5f, 0.35f, 0.15f).id;
                    goto IL_55A;
                case Chest.Type.MONEY:
                    id = 0;
                    goto IL_55A;
            }
            id = MyItemManager.Instance.GetRandomItemAvailable(ItemRarityTier.LEGEND).id;
        IL_55A:
            if (self.type == Chest.Type.CURSE)
            {
                id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0.7f, 0.3f).id;
                float num2 = UnityEngine.Random.Range(0f, 0f);
                int num3;
                if (num2 <= 0.03f)
                {
                    num3 = -1;
                }
                else if (num2 <= 0.18f)
                {
                    num3 = 0;
                }
                else if (num2 <= 0.77f)
                {
                    num3 = 1;
                }
                else if (num2 <= 0.97f)
                {
                    num3 = 2;
                }
                else
                {
                    num3 = 3;
                }
                switch (num3)
                {
                    case -1:
                        id = MyItemManager.Instance.GetRandomItemAvailableWithCriteria(0f, 0f, 1f).id;
                        UIManager.Instance.Narration(I._("UI_Narration_Curse_Fairy"), 2.5f);
                        break;
                    case 0:
                        UIManager.Instance.Narration(I._("UI_Narration_Curse_NothingHappened"), 2.5f);
                        break;
                    case 1:
                        if (self.parentSpawner && self.parentSpawner.cursedEnemyZones.Length != 0)
                        {
                            self.parentSpawner.cursedEnemyZones[UnityEngine.Random.Range(0, self.parentSpawner.cursedEnemyZones.Length)].gameObject.SetActive(true);
                            UIManager.Instance.Narration(I._("UI_Narration_Curse_Monsters"), 2f);
                        }
                        break;
                    case 2:
                        {
                            int num4 = UnityEngine.Random.Range(20, 40);
                            GameManager.Instance.currentPlayer._creature.status.hp -= (float)num4;
                            if (GameManager.Instance.currentPlayer._creature.status.hp <= 1f)
                            {
                                GameManager.Instance.currentPlayer._creature.status.hp = 1f;
                            }
                            UIManager.Instance.Narration(I._("UI_Narration_Curse_HP"), 2.5f);
                            break;
                        }
                    case 3:
                        {
                            int num5 = UnityEngine.Random.Range(5, 10);
                            if (!GameManager.Instance.currentPlayer._creature.status.lockHP)
                            {
                                float num6 = GameManager.Instance.currentPlayer._creature.status.maxHP - (float)num5;
                                if (num6 <= 1f)
                                {
                                    num5 = (int)((float)num5 - (1f - num6));
                                }
                                Status status2 = new Status();
                                status2.maxHP = (float)(-(float)num5);
                                GameManager.Instance.currentPlayer.AddSavableStatus(status2);
                                if (GameManager.Instance.currentPlayer._creature.status.hp > GameManager.Instance.currentPlayer._creature.status.GetMAXHP())
                                {
                                    GameManager.Instance.currentPlayer._creature.status.hp = GameManager.Instance.currentPlayer._creature.status.GetMAXHP();
                                }
                            }
                            UIManager.Instance.Narration(I._("UI_Narration_Curse_MaxHP"), 2.5f);
                            break;
                        }
                }
            }
            self.CreateItem(id);
            if (StageManager.Instance != null && StageManager.Instance.current != null && self.propSeed != null)
            {
                IProceduralDungeon current2 = StageManager.Instance.current.GetCurrent();
                current2.AddObjectData(self.saveKeyBase + "_ItemID", id.ToString());
                current2.AddObjectData(self.saveKeyBase + "_Open", true.ToString());
                if (current2.GetChestState(self.propSeed.Seed.ToString()) == 0)
                {
                    current2.SetChestState(self.propSeed.Seed.ToString(), 1);
                }
            }
            if (StageManager.Instance)
            {
                StageManager.Instance.SaveCurrentDungeon();
            }
            if (self.type != Chest.Type.TUTORIAL)
            {
                base.StartCoroutine(self.CoinCoroutine());
            }
        }

        private System.Collections.IEnumerator Chest_CoinCoroutine(On.Chest.orig_CoinCoroutine orig, Chest self)
        {
            int gold = UnityEngine.Random.Range(50, 50);
            if (!self.isUseRandomGold)
            {
                gold = self.customGold;
            }
            else
            {
                if (GameManager.Instance && GameManager.Instance.currentPlayer)
                {
                    gold = Mathf.RoundToInt((float)gold * GameManager.Instance.currentPlayer._creature.dropGoldRatio);
                }
                if (self.type == Chest.Type.MONEY)
                {
                    gold = (int)((float)gold * 0.75f);
                }
            }
            while (gold > 0)
            {
                if (gold >= 10)
                {
                    FxPool.Instance.CreateBullion(base.transform.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.7f));
                    gold -= 10;
                }
                else
                {
                    FxPool.Instance.CreateCoin(base.transform.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.7f));
                    gold--;
                }
                float seconds = UnityEngine.Random.Range(0.01f, 0.07f);
                yield return new WaitForSeconds(seconds);
            }
            yield break;
        }
    }
}