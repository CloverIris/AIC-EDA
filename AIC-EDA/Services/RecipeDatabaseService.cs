using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIC_EDA.Services
{
    /// <summary>
    /// 配方数据库服务 - 管理所有配方和物品数据
    /// </summary>
    public class RecipeDatabaseService
    {
        private static RecipeDatabaseService? _instance;
        public static RecipeDatabaseService Instance => _instance ??= new RecipeDatabaseService();

        private List<Recipe> _recipes = new();
        private List<Item> _items = new();
        private bool _loaded = false;

        public IReadOnlyList<Recipe> Recipes => _recipes.AsReadOnly();
        public IReadOnlyList<Item> Items => _items.AsReadOnly();

        private RecipeDatabaseService() { }

        /// <summary>
        /// 从JSON文件加载配方数据库
        /// </summary>
        public async Task LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // 使用内置默认数据
                LoadDefaultData();
                return;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var data = JsonSerializer.Deserialize<RecipeDatabaseJson>(json, options);

            if (data != null)
            {
                _items = data.Items?.ToList() ?? new List<Item>();
                _recipes = data.Recipes?.ToList() ?? new List<Recipe>();
            }

            _loaded = true;
        }

        /// <summary>
        /// 加载内置默认数据
        /// </summary>
        public void LoadDefaultData()
        {
            // 物品库
            _items = new List<Item>
            {
                // 原材料
                new("originium_ore", "源石矿", "Originium Ore", ItemCategory.RawMaterial),
                new("iron_ore", "铁矿石", "Iron Ore", ItemCategory.RawMaterial),
                new("amethyst_ore", "紫水晶矿", "Amethyst Ore", ItemCategory.RawMaterial),
                new("copper_ore", "赤铜矿", "Copper Ore", ItemCategory.RawMaterial),
                new("blue_iron_ore", "蓝铁矿", "Blue Iron Ore", ItemCategory.RawMaterial),
                new("xiranite", "夕然石", "Xiranite", ItemCategory.RawMaterial),
                new("buckflower", "荞花", "Buckflower", ItemCategory.RawMaterial),
                new("citrome", "柠果", "Citrome", ItemCategory.RawMaterial),
                new("aketine", "阿 ketine", "Aketine", ItemCategory.RawMaterial),
                new("sandleaf", "砂叶", "Sandleaf", ItemCategory.RawMaterial),
                new("yazhen", "亚珍", "Yazhen", ItemCategory.RawMaterial),
                new("jincao", "金草", "Jincao", ItemCategory.RawMaterial),

                // 基础产物
                new("origocrust", "源石粗矿", "Origocrust", ItemCategory.Intermediate),
                new("steel", "钢材", "Steel", ItemCategory.Intermediate),
                new("ferrium", "铁材", "Ferrium", ItemCategory.Intermediate),
                new("amethyst_fiber", "紫水晶纤维", "Amethyst Fiber", ItemCategory.Intermediate),
                new("cryston_fiber", "晶石纤维", "Cryston Fiber", ItemCategory.Intermediate),
                new("cuprium", "赤铜", "Cuprium", ItemCategory.Intermediate),
                new("clean_water", "洁净水", "Clean Water", ItemCategory.Fluid),

                // 零件
                new("ferrium_part", "铁材零件", "Ferrium Part", ItemCategory.Intermediate),
                new("steel_part", "钢材零件", "Steel Part", ItemCategory.Intermediate),
                new("amethyst_part", "紫水晶零件", "Amethyst Part", ItemCategory.Intermediate),
                new("cryston_part", "晶石零件", "Cryston Part", ItemCategory.Intermediate),
                new("cuprium_part", "赤铜零件", "Cuprium Part", ItemCategory.Intermediate),

                // 组件
                new("amethyst_component", "紫水晶组件", "Amethyst Component", ItemCategory.Intermediate),
                new("ferrium_component", "铁材组件", "Ferrium Component", ItemCategory.Intermediate),
                new("cryston_component", "晶石组件", "Cryston Component", ItemCategory.Intermediate),
                new("xiranite_component", "夕然石组件", "Xiranite Component", ItemCategory.Intermediate),
                new("cuprium_component", "赤铜组件", "Cuprium Component", ItemCategory.Intermediate),

                // 瓶/容器类（可回收）
                new("ferrium_bottle", "铁材瓶", "Ferrium Bottle", ItemCategory.Intermediate),
                new("amethyst_bottle", "紫水晶瓶", "Amethyst Bottle", ItemCategory.Intermediate),

                // 液体内容物
                new("jincao_solution", "金草溶液", "Jincao Solution", ItemCategory.Fluid),
                new("yazhen_solution", "亚珍溶液", "Yazhen Solution", ItemCategory.Fluid),
                new("liquid_xiranite", "液态夕然石", "Liquid Xiranite", ItemCategory.Fluid),

                // 包装物
                new("packed_origocrust", "包装源石粗矿", "Packed Origocrust", ItemCategory.Intermediate),

                // 种子
                new("buckflower_seed", "荞花种子", "Buckflower Seed", ItemCategory.Intermediate),
                new("citrome_seed", "柠果种子", "Citrome Seed", ItemCategory.Intermediate),
                new("aketine_seed", "Aketine种子", "Aketine Seed", ItemCategory.Intermediate),
                new("yazhen_seed", "亚珍种子", "Yazhen Seed", ItemCategory.Intermediate),
                new("jincao_seed", "金草种子", "Jincao Seed", ItemCategory.Intermediate),
                new("sandleaf_seed", "砂叶种子", "Sandleaf Seed", ItemCategory.Intermediate),

                // 最终产品
                new("buck_capsule_c", "荞愈胶囊[C]", "Buck Capsule [C]", ItemCategory.FinalProduct),
                new("yazhen_syringe_a", "亚珍注射剂[A]", "Yazhen Syringe [A]", ItemCategory.FinalProduct),
                new("yazhen_syringe_c", "亚珍注射剂[C]", "Yazhen Syringe [C]", ItemCategory.FinalProduct),
                new("jincao_drink", "金草饮料", "Jincao Drink", ItemCategory.FinalProduct),
                new("industrial_explosives", "工业炸药", "Industrial Explosives", ItemCategory.FinalProduct),
                new("medium_battery", "中型电池", "Medium Battery", ItemCategory.FinalProduct),
                new("sc_wuling_battery", "SC武陵电池", "SC Wuling Battery", ItemCategory.FinalProduct),
                new("equipment_original", "装备原件", "Equipment Original", ItemCategory.FinalProduct),
            };

            // 配方库
            _recipes = new List<Recipe>
            {
                // 采矿机产出（作为基础配方，duration=1s表示持续产出）
                new Recipe { Id = "mine_originium", Name = "开采源石矿", Machine = MachineType.MiningRig, Duration = 1, PowerConsumption = 5, Outputs = { ["originium_ore"] = 1 } },
                new Recipe { Id = "mine_iron", Name = "开采铁矿石", Machine = MachineType.MiningRig, Duration = 1, PowerConsumption = 5, Outputs = { ["iron_ore"] = 1 } },
                new Recipe { Id = "mine_amethyst", Name = "开采紫水晶", Machine = MachineType.MiningRigMk2, Duration = 1, PowerConsumption = 8, Outputs = { ["amethyst_ore"] = 1 } },
                new Recipe { Id = "mine_copper", Name = "开采赤铜", Machine = MachineType.HydraulicMiningRig, Duration = 1, PowerConsumption = 10, Outputs = { ["copper_ore"] = 1 } },
                new Recipe { Id = "pump_water", Name = "抽取净水", Machine = MachineType.FluidPump, Duration = 1, PowerConsumption = 3, Outputs = { ["clean_water"] = 1 } },

                // 精炼
                new Recipe { Id = "refine_origocrust", Name = "精炼源石粗矿", Machine = MachineType.RefiningUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["originium_ore"] = 1 }, Outputs = { ["origocrust"] = 1 } },
                new Recipe { Id = "refine_steel", Name = "精炼钢材", Machine = MachineType.RefiningUnit, Duration = 2, PowerConsumption = 12, Inputs = { ["iron_ore"] = 1 }, Outputs = { ["steel"] = 1 } },
                new Recipe { Id = "refine_ferrium", Name = "精炼铁材", Machine = MachineType.RefiningUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["iron_ore"] = 1 }, Outputs = { ["ferrium"] = 1 } },
                new Recipe { Id = "refine_amethyst_fiber", Name = "精炼紫水晶纤维", Machine = MachineType.RefiningUnit, Duration = 2, PowerConsumption = 12, Inputs = { ["amethyst_ore"] = 1 }, Outputs = { ["amethyst_fiber"] = 1 } },
                new Recipe { Id = "refine_cuprium", Name = "精炼赤铜", Machine = MachineType.RefiningUnit, Duration = 2, PowerConsumption = 12, Inputs = { ["copper_ore"] = 1 }, Outputs = { ["cuprium"] = 1 } },

                // 粉碎
                new Recipe { Id = "shred_buckflower", Name = "粉碎荞花", Machine = MachineType.ShreddingUnit, Duration = 2, PowerConsumption = 8, Inputs = { ["buckflower"] = 1 }, Outputs = { ["buckflower"] = 1 } }, // 简化为同物品，实际可能是粉末
                new Recipe { Id = "shred_aketine", Name = "粉碎Aketine", Machine = MachineType.ShreddingUnit, Duration = 2, PowerConsumption = 8, Inputs = { ["aketine"] = 1 }, Outputs = { ["aketine_powder"] = 1 } },

                // 装配零件
                new Recipe { Id = "fit_ferrium_part", Name = "装配铁材零件", Machine = MachineType.FittingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["ferrium"] = 1 }, Outputs = { ["ferrium_part"] = 1 } },
                new Recipe { Id = "fit_steel_part", Name = "装配钢材零件", Machine = MachineType.FittingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["steel"] = 1 }, Outputs = { ["steel_part"] = 1 } },
                new Recipe { Id = "fit_amethyst_part", Name = "装配紫水晶零件", Machine = MachineType.FittingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["amethyst_fiber"] = 1 }, Outputs = { ["amethyst_part"] = 1 } },
                new Recipe { Id = "fit_cryston_part", Name = "装配晶石零件", Machine = MachineType.FittingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["cryston_fiber"] = 1 }, Outputs = { ["cryston_part"] = 1 } },

                // 齿轮单元 - 组件制造
                new Recipe { Id = "gear_amethyst_component", Name = "制造紫水晶组件", Machine = MachineType.GearingUnit, Duration = 10, PowerConsumption = 25, Inputs = { ["origocrust"] = 5, ["amethyst_fiber"] = 5 }, Outputs = { ["amethyst_component"] = 1 } },
                new Recipe { Id = "gear_ferrium_component", Name = "制造铁材组件", Machine = MachineType.GearingUnit, Duration = 10, PowerConsumption = 25, Inputs = { ["origocrust"] = 10, ["ferrium"] = 10 }, Outputs = { ["ferrium_component"] = 1 } },
                new Recipe { Id = "gear_cryston_component", Name = "制造晶石组件", Machine = MachineType.GearingUnit, Duration = 10, PowerConsumption = 25, Inputs = { ["packed_origocrust"] = 10, ["cryston_fiber"] = 10 }, Outputs = { ["cryston_component"] = 1 } },
                new Recipe { Id = "gear_xiranite_component", Name = "制造夕然石组件", Machine = MachineType.GearingUnit, Duration = 10, PowerConsumption = 30, Inputs = { ["xiranite"] = 10, ["packed_origocrust"] = 10 }, Outputs = { ["xiranite_component"] = 1 } },

                // 塑形机 - 瓶子制造
                new Recipe { Id = "mould_ferrium_bottle", Name = "塑形铁材瓶", Machine = MachineType.MouldingUnit, Duration = 4, PowerConsumption = 15, Inputs = { ["ferrium"] = 1 }, Outputs = { ["ferrium_bottle"] = 1 } },
                new Recipe { Id = "mould_amethyst_bottle", Name = "塑形紫水晶瓶", Machine = MachineType.MouldingUnit, Duration = 4, PowerConsumption = 15, Inputs = { ["amethyst_fiber"] = 1 }, Outputs = { ["amethyst_bottle"] = 1 } },

                // 灌装
                new Recipe { Id = "fill_jincao_solution", Name = "灌装金草溶液", Machine = MachineType.FillingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["jincao"] = 1, ["ferrium_bottle"] = 1 }, Outputs = { ["jincao_solution"] = 1, ["ferrium_bottle"] = 1 } },
                new Recipe { Id = "fill_yazhen_solution", Name = "灌装亚珍溶液", Machine = MachineType.FillingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["yazhen"] = 1, ["ferrium_bottle"] = 1 }, Outputs = { ["yazhen_solution"] = 1, ["ferrium_bottle"] = 1 } },
                new Recipe { Id = "fill_liquid_xiranite", Name = "灌装液态夕然石", Machine = MachineType.FillingUnit, Duration = 2, PowerConsumption = 12, Inputs = { ["xiranite"] = 1, ["ferrium_bottle"] = 1 }, Outputs = { ["liquid_xiranite"] = 1, ["ferrium_bottle"] = 1 } },

                // 分离（瓶子回收）
                new Recipe { Id = "separate_jincao_bottle", Name = "分离金草瓶", Machine = MachineType.SeparatingUnit, Duration = 2, PowerConsumption = 8, Inputs = { ["ferrium_bottle_jincao"] = 1 }, Outputs = { ["jincao_solution"] = 1, ["ferrium_bottle"] = 1 } },

                // 包装
                new Recipe { Id = "pack_origocrust", Name = "包装源石粗矿", Machine = MachineType.PackagingUnit, Duration = 2, PowerConsumption = 10, Inputs = { ["origocrust"] = 1 }, Outputs = { ["packed_origocrust"] = 1 } },

                // 种植
                new Recipe { Id = "plant_buckflower", Name = "种植荞花", Machine = MachineType.PlantingUnit, Duration = 2, PowerConsumption = 3, Inputs = { ["buckflower_seed"] = 1 }, Outputs = { ["buckflower"] = 1 } },
                new Recipe { Id = "plant_yazhen", Name = "种植亚珍", Machine = MachineType.PlantingUnit, Duration = 2, PowerConsumption = 3, Inputs = { ["yazhen_seed"] = 1, ["clean_water"] = 1 }, Outputs = { ["yazhen"] = 2 } },
                new Recipe { Id = "plant_jincao", Name = "种植金草", Machine = MachineType.PlantingUnit, Duration = 2, PowerConsumption = 3, Inputs = { ["jincao_seed"] = 1, ["clean_water"] = 1 }, Outputs = { ["jincao"] = 2 } },

                // 选种
                new Recipe { Id = "seedpick_buckflower", Name = "选种荞花", Machine = MachineType.SeedPickingUnit, Duration = 2, PowerConsumption = 5, Inputs = { ["buckflower"] = 1 }, Outputs = { ["buckflower_seed"] = 1 } },
                new Recipe { Id = "seedpick_yazhen", Name = "选种亚珍", Machine = MachineType.SeedPickingUnit, Duration = 2, PowerConsumption = 5, Inputs = { ["yazhen"] = 1 }, Outputs = { ["yazhen_seed"] = 1 } },
                new Recipe { Id = "seedpick_jincao", Name = "选种金草", Machine = MachineType.SeedPickingUnit, Duration = 2, PowerConsumption = 5, Inputs = { ["jincao"] = 1 }, Outputs = { ["jincao_seed"] = 1 } },

                // 最终产品 - 荞愈胶囊[C]
                new Recipe { Id = "make_buck_capsule_c", Name = "制造荞愈胶囊[C]", Machine = MachineType.PackagingUnit, Duration = 4, PowerConsumption = 15, 
                    Inputs = { ["buckflower"] = 1, ["amethyst_bottle"] = 1 }, Outputs = { ["buck_capsule_c"] = 1, ["amethyst_bottle"] = 1 } },

                // 工业炸药
                new Recipe { Id = "make_industrial_explosives", Name = "制造工业炸药", Machine = MachineType.PackagingUnit, Duration = 4, PowerConsumption = 20,
                    Inputs = { ["amethyst_component"] = 5, ["aketine_powder"] = 1 }, Outputs = { ["industrial_explosives"] = 1 } },

                // 热能银行（燃烧源石发电）
                new Recipe { Id = "burn_thermal", Name = "热能发电", Machine = MachineType.ThermalBank, Duration = 1, PowerConsumption = -50, Inputs = { ["originium_ore"] = 1 } },
            };

            _loaded = true;
        }

        /// <summary>获取物品</summary>
        public Item? GetItem(string id)
        {
            return _items.FirstOrDefault(i => i.Id == id);
        }

        /// <summary>获取配方的所有产出物品</summary>
        public List<Item> GetRecipeOutputs(Recipe recipe)
        {
            return recipe.Outputs.Keys.Select(GetItem).Where(i => i != null).ToList()!;
        }

        /// <summary>查找能生产指定物品的所有配方</summary>
        public List<Recipe> FindRecipesByOutput(string itemId)
        {
            return _recipes.Where(r => r.Outputs.ContainsKey(itemId)).ToList();
        }

        /// <summary>查找使用指定物品作为输入的所有配方</summary>
        public List<Recipe> FindRecipesByInput(string itemId)
        {
            return _recipes.Where(r => r.Inputs.ContainsKey(itemId)).ToList();
        }

        /// <summary>查找指定设备的默认配方</summary>
        public Recipe? FindDefaultRecipe(MachineType machineType, string? preferredOutput = null)
        {
            var recipes = _recipes.Where(r => r.Machine == machineType).ToList();
            if (recipes.Count == 0) return null;
            if (preferredOutput != null)
                return recipes.FirstOrDefault(r => r.Outputs.ContainsKey(preferredOutput)) ?? recipes[0];
            return recipes[0];
        }

        /// <summary>保存到JSON文件</summary>
        public async Task SaveToFileAsync(string filePath)
        {
            var data = new RecipeDatabaseJson { Items = _items, Recipes = _recipes };
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public bool IsLoaded => _loaded;
    }

    public class RecipeDatabaseJson
    {
        public List<Item> Items { get; set; } = new();
        public List<Recipe> Recipes { get; set; } = new();
    }
}
