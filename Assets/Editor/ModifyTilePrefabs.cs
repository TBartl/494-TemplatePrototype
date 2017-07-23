using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class ModifyTilePrefabs : EditorWindow
{

    TextAsset tilePrefabsByTileIndex;

    List<string> prefabTypesNeeded;

    [MenuItem("494/3) Modify Tile Prefabs", false, 2)]
    public static void Generate()
    {
        EditorWindow.GetWindow(typeof(ModifyTilePrefabs));
    }

    void OnGUI()
    {
        // Check for errors
        GameObject rootGO = GameObject.Find("Level");
        if (rootGO == null)
        {
            GUILayout.Label("There is no Level GameObject in this scene. The script can not continue.", EditorStyles.boldLabel);
        }
        else
        {
            GUILayout.Label("Change tile prefabs in current scene", EditorStyles.boldLabel);
            GUILayout.Label("", EditorStyles.boldLabel);
            tilePrefabsByTileIndex = (TextAsset)EditorGUILayout.ObjectField("Tile Prefabs by Tile Index:", tilePrefabsByTileIndex, typeof(TextAsset), false);
            if (tilePrefabsByTileIndex == null)
            {
                GUILayout.Label("Tile Prefab data not provided!\nAdd Tile prefab file to continue..", EditorStyles.boldLabel);
            }
            else if (GUILayout.Button("Check Updates."))
            {
                Check(rootGO.transform);
                //this.Close();
            }
            if (prefabTypesNeeded != null)
            {
                string toBeGeneratedMessage = "";
                if (prefabTypesNeeded.Count == 0)
                {
                    toBeGeneratedMessage = "No new tile prefabs will be generated";
                }
                else
                {
                    toBeGeneratedMessage = "The following new prefabs will be generated: ";
                    foreach (string prefabType in prefabTypesNeeded)
                    {
                        toBeGeneratedMessage += "\n - " + EditorUtilityFunctions.tilePrefix + prefabType;
                    }
                }
                GUILayout.Label(toBeGeneratedMessage, EditorStyles.label);

            }
        }
    }

    void Check(Transform root)
    {
        // Load all tile prefab assets into a dict
        Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
        List<GameObject> prefabs = EditorUtilityFunctions.GetTilePrefabs();
        foreach (GameObject prefab in prefabs)
        {
            string prefabKey = prefab.name.Substring(EditorUtilityFunctions.tilePrefix.Length); // Get the part after Tile_, like WALL
            prefabDict.Add(prefabKey, prefab);
        }

        // Parse input txt file
        string[] prefabByIndexArray = tilePrefabsByTileIndex.text.Split(new char[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Check to see if any new prefabs are needed
        prefabTypesNeeded = new List<string>();
        foreach (string prefabType in prefabByIndexArray)
        {
            if (prefabType == "NULL" || prefabDict.ContainsKey(prefabType) || prefabTypesNeeded.Contains(prefabType))
                continue;
            prefabTypesNeeded.Add(prefabType);
        }

    }


    //    for (int i = 0; i<prefabByIndexArray.Length; i++)
    //        {
    //            string thisPrefabType = prefabByIndexArray[i];
    //            if (thisPrefabType == "NULL") // Don't generate NULL GameObject
    //                continue;
    //            if (prefabDict.ContainsKey(thisPrefabType)) // Only need to generate one prefab, will skip most tile numbers
    //                continue;
    //            string prefabName = EditorUtilityFunctions.tilePrefix + thisPrefabType;
    //    string prefabPath = "Assets/" + prefabName + ".prefab";
    //    GameObject tile = new GameObject();
    //    SpriteRenderer tileSR = tile.AddComponent<SpriteRenderer>();
    //    tileSR.sprite = prefabByIndexArray[i];
    //            GameObject newPrefab = PrefabUtility.CreatePrefab(prefabPath, tile);
    //    prefabDict.Add(thisPrefabType, newPrefab);
    //            DestroyImmediate(tile);
    //}

    //    foreach (Transform child in root)
    //    {
    //        if (!child.name.StartsWith(EditorUtilityFunctions.tilePrefix)) // Only worrying about tiles
    //            continue;

    //        GameObject childGO = root.gameObject;
    //        GameObject currentPrefab = (GameObject)PrefabUtility.GetPrefabParent(childGO);
    //        Transform originalParent = child.parent;

    //        int x = Mathf.RoundToInt(child.transform.position.x);
    //        int y = Mathf.RoundToInt(child.transform.position.y);
    //        int typeNum = map[x, y];
    //        string type = prefabArray[typeNum];
    //        GameObject targetPrefab = prefabDict[type];

    //        if (currentPrefab != targetPrefab)
    //        { // Need to remake the tile if the prefab changes
    //            GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefabDict[type]);
    //            tile.transform.position = new Vector3(x, y);
    //            tile.transform.parent = originalParent;
    //            tile.GetComponent<SpriteRenderer>().sprite = spriteArray[typeNum];

    //            DestroyImmediate(childGO);
    //            Debug.Log("Replaced");
    //        }
    //    }
    //}
}
