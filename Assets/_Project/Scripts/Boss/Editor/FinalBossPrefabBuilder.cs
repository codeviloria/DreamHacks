#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DreamHacks.Boss;
using System.IO;

namespace DreamHacks.Boss.EditorTools
{
    public static class FinalBossPrefabBuilder
    {
        [MenuItem("DreamHacks/Crear Prefab Final Boss Jorge")]
        public static void CreateBossPrefab()
        {
            string spritePath = "Assets/_Project/Prefabs/FINAL_BOSS_JORGE_EL_CURIOSO.png";
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

            // 1. Crear Prefab de Bola de Fuego
            string fireballPrefabPath = "Assets/_Project/Prefabs/BossFireball.prefab";
            GameObject fbObj = new GameObject("BossFireball");
            var fbSr = fbObj.AddComponent<SpriteRenderer>();
            fbSr.sortingOrder = 15;

            // Textura procedural de fuego para el proyectil
            Texture2D fireTex = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    Color c = d <= 7f ? (d < 3.5f ? Color.yellow : new Color(1f, 0.4f, 0.05f, 1f)) : Color.clear;
                    fireTex.SetPixel(x, y, c);
                }
            }
            fireTex.filterMode = FilterMode.Point;
            fireTex.Apply();

            string fireTexPath = "Assets/_Project/Prefabs/Fireball_Texture.png";
            File.WriteAllBytes(fireTexPath, fireTex.EncodeToPNG());
            AssetDatabase.ImportAsset(fireTexPath);
            var texImporter = AssetImporter.GetAtPath(fireTexPath) as TextureImporter;
            if (texImporter != null)
            {
                texImporter.textureType = TextureImporterType.Sprite;
                texImporter.spritePixelsPerUnit = 16f;
                texImporter.filterMode = FilterMode.Point;
                texImporter.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(fireTexPath, ImportAssetOptions.ForceUpdate);
            }

            var fireSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fireTexPath);
            if (fireSprite != null) fbSr.sprite = fireSprite;

            var fbScript = fbObj.AddComponent<BossFireball>();
            var savedFireballPrefab = PrefabUtility.SaveAsPrefabAsset(fbObj, fireballPrefabPath);
            Object.DestroyImmediate(fbObj);
            Debug.Log($"[FinalBoss] Prefab de proyectil guardado en {fireballPrefabPath}");

            // 2. Crear Prefab del Boss Jorge El Curioso
            string bossPrefabPath = "Assets/_Project/Prefabs/FINAL_BOSS_JORGE.prefab";
            GameObject bossObj = new GameObject("FINAL_BOSS_JORGE");
            bossObj.tag = "Untagged";

            var sr = bossObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            var rb = bossObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var boxCol = bossObj.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(2.4f, 3.2f);
            boxCol.offset = new Vector2(0f, 1.6f);

            var bossComp = bossObj.AddComponent<FinalBossJorge>();

            // Cargar sprites rebanados
            var assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            var idleList = new System.Collections.Generic.List<Sprite>();
            var walkList = new System.Collections.Generic.List<Sprite>();

            Sprite defaultSprite = null;
            Sprite firePrep = null, fireCast = null, fireRelease = null;
            Sprite jumpRise = null, jumpApex = null, jumpLand = null;
            Sprite roar = null, stun = null;

            foreach (var a in assets)
            {
                if (a is Sprite s)
                {
                    if (s.name.Contains("Idle")) idleList.Add(s);
                    else if (s.name.Contains("Walk")) walkList.Add(s);
                    else if (s.name.Contains("Fire_Prep")) firePrep = s;
                    else if (s.name.Contains("Fire_Cast")) fireCast = s;
                    else if (s.name.Contains("Fire_Release")) fireRelease = s;
                    else if (s.name.Contains("Jump_Rise")) jumpRise = s;
                    else if (s.name.Contains("Jump_Apex")) jumpApex = s;
                    else if (s.name.Contains("Jump_Land")) jumpLand = s;
                    else if (s.name.Contains("Roar")) roar = s;
                    else if (s.name.Contains("Stun")) stun = s;

                    if (defaultSprite == null) defaultSprite = s;
                }
            }

            if (idleList.Count > 0) defaultSprite = idleList[0];
            sr.sprite = defaultSprite;

            // Asignar campos serializados a través de SerializedObject
            var so = new SerializedObject(bossComp);
            SetSpriteArray(so.FindProperty("idleSprites"), idleList);
            SetSpriteArray(so.FindProperty("walkSprites"), walkList);
            SetObject(so.FindProperty("firePrepSprite"), firePrep);
            SetObject(so.FindProperty("fireCastSprite"), fireCast);
            SetObject(so.FindProperty("fireReleaseSprite"), fireRelease);
            SetObject(so.FindProperty("jumpRiseSprite"), jumpRise);
            SetObject(so.FindProperty("jumpApexSprite"), jumpApex);
            SetObject(so.FindProperty("jumpLandSprite"), jumpLand);
            SetObject(so.FindProperty("roarSprite"), roar);
            SetObject(so.FindProperty("stunSprite"), stun);
            SetObject(so.FindProperty("fireballPrefab"), savedFireballPrefab);
            so.ApplyModifiedProperties();

            // Hitbox trigger hijo
            GameObject hitboxObj = new GameObject("DamageHitbox");
            hitboxObj.transform.SetParent(bossObj.transform, false);
            var hitCol = hitboxObj.AddComponent<BoxCollider2D>();
            hitCol.isTrigger = true;
            hitCol.size = new Vector2(2.6f, 3.4f);
            hitCol.offset = new Vector2(0f, 1.6f);

            PrefabUtility.SaveAsPrefabAsset(bossObj, bossPrefabPath);
            Object.DestroyImmediate(bossObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FinalBoss] ¡Prefab del Final Boss guardado con éxito en {bossPrefabPath}!");
        }

        private static void SetSpriteArray(SerializedProperty prop, System.Collections.Generic.List<Sprite> list)
        {
            if (prop == null || list == null) return;
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }

        private static void SetObject(SerializedProperty prop, Object obj)
        {
            if (prop != null && obj != null)
            {
                prop.objectReferenceValue = obj;
            }
        }
    }
}
#endif
