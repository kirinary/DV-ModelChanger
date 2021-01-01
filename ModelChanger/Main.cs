using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityModManagerNet;
using Harmony12;
using UnityEngine;

namespace ModelChanger
{
    public class Main
    {
        public static Dictionary<string, string> trainCarState = new Dictionary<string, string>();
        public static string modPath;
        static Vector2 scrollViewVector = Vector2.zero;
        public static Dictionary<TrainCarType, string> prefabMap;
        static TrainCarType trainCarSelected = TrainCarType.NotSet;
        static bool showDropdown = false;
        public static Dictionary<TrainCarType, SkinGroup> skinGroups = new Dictionary<TrainCarType, SkinGroup>();
        public static Dictionary<TrainCarType, string> selectedSkin = new Dictionary<TrainCarType, string>();
        public static Dictionary<string, GameObject> assets = new Dictionary<string, GameObject>();

        static List<string> excludedExports = new List<string>
        {
        };

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Type type = typeof(CarTypes);
            FieldInfo fieldInfo = type.GetField("prefabMap", BindingFlags.Static | BindingFlags.NonPublic);

            prefabMap = fieldInfo.GetValue(null) as Dictionary<TrainCarType, string>;

            modPath = modEntry.Path;

            LoadAssets(modPath);
            LoadHeadMarkSkins(modPath);

            foreach (TrainCarType carType in prefabMap.Keys)
            {
                selectedSkin.Add(carType, "Random");
            }

            modEntry.OnGUI = OnGUI;

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            return true;
        }

        public static void LoadAssets(string path)
        {
            Log.Write("Load AssetBundle :" + path + "Resources/sh282headmark");
            AssetBundle bundle = AssetBundle.LoadFromFile(path + "Resources/sh282headmark");
            if (bundle == null)
            {
                Log.Write("Faild to load AssetBundle");
                return;
            }
            Log.Write("Load AssetBundle Success");
            assets.Add("SH282_HeadMark", bundle.LoadAsset<GameObject>("Assets/Models/sh282headmark.prefab"));
            bundle.Unload(false);
            Log.Write("End Load Assets");
        }

        public static void LoadHeadMarkSkins(string path)
        {
            Log.Write("Load HeadMarks :" + path + "Resources");
            
            if (!Directory.Exists($"{path}Resources"))
            {
                Log.Write("no such resource dir");
                return;
            }

            var dir = $"{path}Resources";
            var subDirectories = Directory.GetDirectories(dir);
            if (subDirectories.Length == 0)
            {
                Log.Write("no such headmark dir");
                return;
            }

            var skinGroup = new SkinGroup(TrainCarType.LocoSteamHeavy);
            foreach (var sub in subDirectories)
            {
                var dirInfo = new DirectoryInfo(sub);
                var files = Directory.GetFiles(sub);
                var skin = new Skin(dirInfo.Name);

                foreach (var file in files)
                {
                    if (!file.Contains(".png"))
                    {
                        continue;
                    }
                    FileInfo fileInfo = new FileInfo(file);
                    string fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    byte[] fileData = File.ReadAllBytes(file);

                    var texture = new Texture2D(1024, 1024);
                    texture.name = fileName;
                    texture.LoadImage(fileData);
                    texture.Apply(true, true);
                    skin.skinTextures.Add(new SkinTexture(fileName, texture));
                    skinGroup.skins.Add(skin);
                    Log.Write($"Load Texture:{fileName}");
                }
            }
            skinGroups.Add(TrainCarType.LocoSteamHeavy, skinGroup);
            Log.Write("End Load HeadMarks");
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(2);

            GUILayout.Label("Export Textures");

            GUILayout.BeginHorizontal(GUILayout.Width(250));

            GUILayout.BeginVertical();

            if (GUILayout.Button(trainCarSelected == TrainCarType.NotSet ? "Select Train Car" : prefabMap[trainCarSelected], GUILayout.Width(220)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(350));

                foreach (var entry in prefabMap)
                {
                    if (GUILayout.Button(entry.Value, GUILayout.Width(220)))
                    {
                        showDropdown = false;
                        trainCarSelected = entry.Key;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public static Skin FindTrainCarSkin(TrainCar trainCar, string findSkinName = "")
        {
            if (!skinGroups.ContainsKey(trainCar.carType))
            {
                return null;
            }

            var skinGroup = skinGroups[trainCar.carType];

            if (skinGroup.skins.Count == 0)
            {
                return null;
            }

            Skin skin;

            if (trainCarState.ContainsKey(trainCar.logicCar.carGuid))
            {
                string skinName = trainCarState[trainCar.logicCar.carGuid];

                if (skinName == "__default")
                {
                    return null;
                }

                skin = skinGroup.GetSkin(skinName);

                if (skin == null)
                {
                    return null;
                }
            }
            else
            {
                string selectedSkin = Main.selectedSkin[trainCar.carType];

                if (findSkinName != "")
                {
                    selectedSkin = findSkinName;
                }

                if (selectedSkin == "Random" && skinGroup.skins.Count > 0)
                {
                    var range = UnityEngine.Random.Range(0, skinGroup.skins.Count);

                    // default skin if it hits out of index
                    if (range == skinGroup.skins.Count)
                    {
                        SetCarState(trainCar.logicCar.carGuid, "__default");

                        return null;
                    }

                    skin = skinGroup.skins[range];

                    SetCarState(trainCar.logicCar.carGuid, skin.name);
                }
                else if (selectedSkin == "Default")
                {
                    SetCarState(trainCar.logicCar.carGuid, "__default");

                    return null;
                }
                else
                {
                    skin = skinGroup.GetSkin(selectedSkin);

                    if (skin != null)
                    {
                        SetCarState(trainCar.logicCar.carGuid, skin.name);
                    }
                }
            }

            return skin;
        }

        public static Skin lastSteamerSkin;
        public static void ReplaceHeadMarkTexture(TrainCar trainCar)
        {
            Log.Write("ReplaceHeadMarkTexture");

            string findSkin = "";

            if (trainCar.carType == TrainCarType.Tender && lastSteamerSkin != null)
            {
                findSkin = lastSteamerSkin.name;
            }

            Skin skin = FindTrainCarSkin(trainCar, findSkin);

            if (trainCar.carType == TrainCarType.LocoSteamHeavy)
            {
                lastSteamerSkin = skin;
            }
            else
            {
                lastSteamerSkin = null;
            }

            if (skin == null)
            {
                Log.Write("Not Found Skin");
                return;
            }

            var cmps = trainCar.gameObject.GetComponentsInChildren<MeshRenderer>();
            var name = "HeadMark";
            foreach (var cmp in cmps)
            {
                if(cmp.name == name)
                {
                    if (skin.ContainsTexture(name))
                    {
                        var skinTexture = skin.GetTexture(name);

                        cmp.material.SetTexture("_MainTex", skinTexture.textureData);
                        Log.Write("SetTexture");
                    }
                }
            }
        }

        public static void SetCarState(string guid, string name)
        {
            if (trainCarState.ContainsKey(guid))
            {
                trainCarState[guid] = name;
            }
            else
            {
                trainCarState.Add(guid, name);
            }
        }
    }

    public class SkinGroup
    {
        TrainCarType trainCarType;
        public List<Skin> skins = new List<Skin>();

        public SkinGroup(TrainCarType trainCarType)
        {
            this.trainCarType = trainCarType;
        }

        public Skin GetSkin(string name)
        {
            foreach (var skin in skins)
            {
                if (skin.name == name)
                {
                    return skin;
                }
            }

            return null;
        }
    }

    public class Skin
    {
        public string name;
        public List<SkinTexture> skinTextures = new List<SkinTexture>();

        public Skin(string name)
        {
            this.name = name;
        }

        public bool ContainsTexture(string name)
        {
            foreach (var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public SkinTexture GetTexture(string name)
        {
            foreach (var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return tex;
                }
            }

            return null;
        }
    }

    public class SkinTexture
    {
        public string name;
        public Texture2D textureData;

        public SkinTexture(string name, Texture2D textureData)
        {
            this.name = name;
            this.textureData = textureData;
        }
    }

    [HarmonyPatch(typeof(UnityModManager.UI), "OnGUI")]
    class UnityModManagerUI_OnGUI_Patch
    {
        private static void Postfix(UnityModManager.UI __instance)
        {
            ModUI.DrawModUI();
        }
    }

    class ModUI
    {
        public static bool isEnabled;
        static bool showDropdown;
        static Vector2 scrollViewVector;

        public static void Init()
        {

        }

        public static void DrawModUI()
        {
            if (!isEnabled)
            {
                showDropdown = false;
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameObject carToSpawn = SingletonBehaviour<CarSpawner>.Instance.carToSpawn;
            TrainCar trainCar = carToSpawn.GetComponent<TrainCar>();
            TrainCarType carType = trainCar.carType;

            SkinGroup skinGroup = Main.skinGroups[carType];
            string selectSkin = Main.selectedSkin[carType];

            float menuHeight = 60f;

            float menuWidth = 270f;
            float buttonWidth = 240f;
            bool isMaxHeight = false;
            float maxHeight = Screen.height - 200f;

            if (showDropdown)
            {
                float totalItemHeight = skinGroup.skins.Count * 23f;

                if (totalItemHeight > maxHeight)
                {
                    totalItemHeight = maxHeight;
                    isMaxHeight = true;
                }

                menuHeight += totalItemHeight + 46f;
            }

            if (isMaxHeight)
            {
                buttonWidth -= 20f;
            }

            GUI.skin = DVGUI.skin;
            GUI.skin.label.fontSize = 11;
            GUI.skin.button.fontSize = 10;
            GUI.color = new Color32(0, 0, 0, 200);
            GUI.Box(new Rect(20f, 20f, menuWidth, menuHeight), "");
            GUILayout.BeginArea(new Rect(30f, 20f, menuWidth, menuHeight));
            GUI.color = Color.yellow;
            GUILayout.Label("Skin Manager Menu :: " + carToSpawn.name);
            GUI.color = Color.white;
            GUILayout.Space(5f);

            if (showDropdown)
            {
                if (GUILayout.Button("=== " + selectSkin + " ===", GUILayout.Width(240f)))
                {
                    showDropdown = false;
                }

                if (isMaxHeight)
                {
                    scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Width(245f), GUILayout.Height(menuHeight - 55f));
                }

                if (GUILayout.Button("Random", GUILayout.Width(buttonWidth)))
                {
                    showDropdown = false;
                    Main.selectedSkin[carType] = "Random";
                }

                if (GUILayout.Button("Default", GUILayout.Width(buttonWidth)))
                {
                    showDropdown = false;
                    Main.selectedSkin[carType] = "Default";
                }

                foreach (Skin skin in skinGroup.skins)
                {
                    if (GUILayout.Button(skin.name, GUILayout.Width(buttonWidth)))
                    {
                        showDropdown = false;
                        Main.selectedSkin[carType] = skin.name;
                    }
                }

                if (isMaxHeight)
                {
                    GUILayout.EndScrollView();
                }
            }
            else
            {
                if (GUILayout.Button("=== " + selectSkin + " ===", GUILayout.Width(240f)))
                {
                    showDropdown = true;
                }
            }

            GUILayout.EndArea();
        }
    }
}