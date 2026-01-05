using UnityEngine;
using UnityEditor;

public class CreateEnemyStatsAsset
{
    [MenuItem("Assets/Create Enemy Stats Asset (Manual)")]
    public static void CreateAsset()
    {
        EnemyStats asset = ScriptableObject.CreateInstance<EnemyStats>();

        string path = "Assets/BadKnightStats.asset";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        UnityEngine.Debug.Log("Created EnemyStats asset at: " + path);
    }
}