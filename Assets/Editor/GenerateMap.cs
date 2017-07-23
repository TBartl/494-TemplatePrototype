using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[System.Serializable]
enum TemplateGame
{
    zelda, metroid
}

public class GenerateMap : EditorWindow
{

    TextAsset mapAsTileIndexes;
    Texture spriteSheet;
    TextAsset tileTypes;
    TemplateGame templateGame = TemplateGame.zelda;
    TextAsset metroidRooms;

    int roomWidth = 16;
    int roomHeight = 11; // 15 for metroid

    [MenuItem("494/2) Generate Map", false, 1)]
    public static void Generate()
    {
        EditorWindow.GetWindow(typeof(GenerateMap));
    }

    void OnGUI()
    {
        // Check for errors
        if (EditorUtilityFunctions.GetRoomPrefab() != null)// If room prefab exists
        {
            GUILayout.Label("Room prefab already exists!\nScript will not rerun in order to ensure this prefabs isn't overwritten.", EditorStyles.boldLabel);
        }
        else if (EditorUtilityFunctions.GetTilePrefabs().Count > 0)// If any Tiles exist
        {
            GUILayout.Label("Tile prefabs already exist!\nScript will not rerun in order to ensure these prefabs aren't overwritten.", EditorStyles.boldLabel);
        }
        else if (GameObject.Find("Level") != null)
        {
            GUILayout.Label("This scene already has a Level GameObject!\nThe script will not continue.", EditorStyles.boldLabel);
        }
        else
        {
            GUILayout.Label("Generate Map Into Current Scene", EditorStyles.boldLabel);
            GUILayout.Label("", EditorStyles.boldLabel);
            mapAsTileIndexes = (TextAsset)EditorGUILayout.ObjectField("Map as Tile Indexes:", mapAsTileIndexes, typeof(TextAsset), false);
            spriteSheet = (Texture)EditorGUILayout.ObjectField("Tile Sprite Sheet:", spriteSheet, typeof(Texture), false);
            tileTypes = (TextAsset)EditorGUILayout.ObjectField("Tile Prefabs by Tile Index:", tileTypes, typeof(TextAsset), false);
            templateGame = (TemplateGame)EditorGUILayout.EnumPopup("Game:", templateGame);
            if (templateGame == TemplateGame.metroid)
            {
                metroidRooms = (TextAsset)EditorGUILayout.ObjectField("Room Grouping:", metroidRooms, typeof(TextAsset), false);
            }

            if (mapAsTileIndexes == null)
            {
                GUILayout.Label("Map data not provided!\nAdd map data to continue.", EditorStyles.boldLabel);
            }
            else if (spriteSheet == null)
            {
                GUILayout.Label("Sprite Sheet not provided!\nAdd a sprite sheet to continue.", EditorStyles.boldLabel);
            }
            else if (tileTypes == null)
            {
                GUILayout.Label("Tile Prefab data not provided!\nAdd Tile prefab file to continue..", EditorStyles.boldLabel);
            }
            else if (templateGame == TemplateGame.metroid && metroidRooms == null)
            {
                GUILayout.Label("Room groupings not provided!\nAdd room groupings to continue.", EditorStyles.boldLabel);
            }
            else if (GUILayout.Button("Generate!"))
            {
                GenerateAllTiles();
                this.Close();
            }
        }
    }

    void GenerateAllTiles()
    {
        // Game specific setup
        if (templateGame == TemplateGame.zelda)
            roomHeight = 11;
        else
            roomHeight = 15;

        // Load Sprites
        var spriteSheetPath = AssetDatabase.GetAssetPath(spriteSheet);
        Sprite[] spriteArray = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>().ToArray();

        // Generate new tile prefabs
        string[] prefabArray = tileTypes.text.Split(new char[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
        for (int i = 0; i < prefabArray.Length; i++)
        {
            string thisPrefabType = prefabArray[i];
            if (thisPrefabType == "NULL") // Don't generate NULL GameObject
                continue;
            if (prefabDict.ContainsKey(thisPrefabType)) // Only need to generate one prefab, will skip most tile numbers
                continue;
            GameObject newPrefab = EditorUtilityFunctions.GenerateNewTilePrefab(thisPrefabType, spriteArray[i]);
            prefabDict.Add(thisPrefabType, newPrefab);
        }

        // Read in the map data
        string[] mapLines = mapAsTileIndexes.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        int height = mapLines.Length;
        string[] tileNums = mapLines[0].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        int width = tileNums.Length;

        // Place the map data into a 2D Array to make it faster to access
        int[,] map = new int[width, height];
        for (int y = 0; y < height; y++)
        {
            tileNums = mapLines[y].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < width; x++)
            {
                map[x, y] = int.Parse(tileNums[x]);
            }
        }

        // Root parent to store rooms and tiles
        Transform root = new GameObject("Level").transform;

        // Rooms for organization
        Transform[,] rooms = null;
        int numRoomsX = width / roomWidth;
        int numRoomsY = height / roomHeight;
        rooms = new Transform[numRoomsX, numRoomsY];
        string roomPrefabPath = "Assets/Room.prefab";
        GameObject roomInstance = new GameObject("Room");
        GameObject roomPrefab = PrefabUtility.CreatePrefab(roomPrefabPath, roomInstance);
        DestroyImmediate(roomInstance);

        //Metroid rooms are grouped together in halls
        Dictionary<char, GameObject> hallsDict = new Dictionary<char, GameObject>();
        string[] hallsMatrix = null;
        if (templateGame == TemplateGame.metroid)
        {
            hallsMatrix = metroidRooms.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }



        // Now generate all of the rooms
        for (int y = 0; y < numRoomsY; y++)
        {
            for (int x = 0; x < numRoomsX; x++)
            {
                // In zelda, every room is the same size
                if (templateGame == TemplateGame.zelda)
                {
                    GameObject roomGO = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
                    roomGO.transform.position = new Vector2(x * roomWidth, y * roomHeight);
                    roomGO.name = "Room (" + x + "," + y + ")";
                    roomGO.transform.parent = root;
                    rooms[x, y] = roomGO.transform;
                }
                //In metroid, rooms are connected
                else
                {
                    char thisRoomChar = hallsMatrix[numRoomsY - y - 1][x];
                    if (!hallsDict.ContainsKey(thisRoomChar))
                    {
                        GameObject roomGO = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
                        roomGO.transform.position = new Vector2(x * roomWidth, y * roomHeight);
                        roomGO.name = "Room " + thisRoomChar;
                        roomGO.transform.parent = root;
                        rooms[x, y] = roomGO.transform;
                        hallsDict.Add(thisRoomChar, roomGO);
                    }
                    else
                    {
                        rooms[x, y] = hallsDict[thisRoomChar].transform;
                    }
                }

            }
        }

        // Place tiles on the map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int typeNum = map[x, y];
                string type = prefabArray[typeNum];
                if (type == "NULL")
                    continue;

                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefabDict[type]);
                tile.transform.position = new Vector3(x, y);
                tile.transform.parent = rooms[x / roomWidth, y / roomHeight];
                tile.GetComponent<SpriteRenderer>().sprite = spriteArray[typeNum];
            }
        }

        //Delete Empty rooms
        for (int y = 0; y < numRoomsY; y++)
        {
            for (int x = 0; x < numRoomsX; x++)
            {
                if (rooms[x, y] && rooms[x, y].childCount == 0)
                    DestroyImmediate(rooms[x, y].gameObject);
            }
        }

        // Sort metroid rooms
        if (templateGame == TemplateGame.metroid)
        {
            SortChildrenByName(root);
        }
    }

    void SortChildrenByName(Transform root)
    {
        List<Transform> children = new List<Transform>();
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            children.Add(child);
            child.parent = null;
        }
        children.Sort((Transform t1, Transform t2) => { return t1.name.CompareTo(t2.name); });
        foreach (Transform child in children)
        {
            child.parent = root.transform;
        }
    }
}