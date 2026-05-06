using System;
using System.IO;
using Project.Config;
using Project.Domain.Cards;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class CardSpriteDatabaseAutoBuilder
    {
        private const string OutputFolderPath = "Assets/_Project/Configs";
        private const string OutputAssetPath = OutputFolderPath + "/CardSpriteDatabase_Standard.asset";

        [MenuItem("Project/Card Game/Create Standard Card Sprite Database From Selected Folder")]
        public static void CreateFromSelectedFolder()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;

            if (selectedObject == null)
            {
                Debug.LogError("Select the 'Standard 52 Cards/Standard Cards' folder first.");
                return;
            }

            string selectedPath = AssetDatabase.GetAssetPath(selectedObject);

            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                Debug.LogError("Selected object is not a folder.");
                return;
            }

            EnsureFolderExists(OutputFolderPath);

            CardSpriteDatabase database = AssetDatabase.LoadAssetAtPath<CardSpriteDatabase>(OutputAssetPath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<CardSpriteDatabase>();
                AssetDatabase.CreateAsset(database, OutputAssetPath);
            }

            SerializedObject serializedDatabase = new SerializedObject(database);

            SerializedProperty cardBackSpriteProperty = serializedDatabase.FindProperty("cardBackSprite");
            SerializedProperty entriesProperty = serializedDatabase.FindProperty("entries");

            entriesProperty.ClearArray();

            string[] pngFiles = Directory.GetFiles(selectedPath, "*.png", SearchOption.AllDirectories);

            int entryCount = 0;

            foreach (string pngFile in pngFiles)
            {
                string assetPath = NormalizePath(pngFile);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (sprite == null)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                if (fileName == "card_back")
                {
                    cardBackSpriteProperty.objectReferenceValue = sprite;
                    continue;
                }

                if (!TryParseStandardCardFileName(fileName, out Rank rank, out Suit suit))
                    continue;

                entriesProperty.InsertArrayElementAtIndex(entryCount);

                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(entryCount);
                entryProperty.FindPropertyRelative("rank").enumValueIndex = (int)rank - 2;
                entryProperty.FindPropertyRelative("suit").enumValueIndex = (int)suit;
                entryProperty.FindPropertyRelative("sprite").objectReferenceValue = sprite;

                entryCount++;
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created/Updated {OutputAssetPath} | Entries: {entryCount}");

            if (entryCount != 52)
                Debug.LogWarning($"Expected 52 card sprites, but found {entryCount}. Check folder/file names.");
        }

        private static bool TryParseStandardCardFileName(string fileName, out Rank rank, out Suit suit)
        {
            rank = default;
            suit = default;

            string[] parts = fileName.Split('_');

            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int rankNumber))
                return false;

            rank = rankNumber switch
            {
                1 => Rank.Ace,
                2 => Rank.Two,
                3 => Rank.Three,
                4 => Rank.Four,
                5 => Rank.Five,
                6 => Rank.Six,
                7 => Rank.Seven,
                8 => Rank.Eight,
                9 => Rank.Nine,
                10 => Rank.Ten,
                11 => Rank.Jack,
                12 => Rank.Queen,
                13 => Rank.King,
                _ => default
            };

            if (rank == default)
                return false;

            suit = parts[1] switch
            {
                "club" => Suit.Clubs,
                "diamond" => Suit.Diamonds,
                "heart" => Suit.Hearts,
                "spade" => Suit.Spades,
                _ => default
            };

            return parts[1] is "club" or "diamond" or "heart" or "spade";
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');

            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);

                currentPath = nextPath;
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}