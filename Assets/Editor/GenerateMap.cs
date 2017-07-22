using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
enum TemplateGame
{
    zelda, metroid
}

//TODO "AutoGen" prefabs vs student modified prefabs
//TODO fix rooms in Metroid
//TODO move to one script

public class GenerateMap : EditorWindow
{

    TextAsset mapData;
    Texture spriteSheet;
    TextAsset tileTypes;
    TemplateGame templateGame = TemplateGame.zelda;
    TextAsset metroidRooms;

    int roomWidth = 16;
    int roomHeight = 11; // 15 for metroid

    static string prefabsFolderPath = "_GeneratedPrefabs";

    [MenuItem("494/2) Generate Map", false, 1)]
    public static void Generate()
    {
        EditorWindow.GetWindow(typeof(GenerateMap));
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Map Into Current Scene", EditorStyles.boldLabel);
        GUILayout.Label("", EditorStyles.boldLabel);
        if (GameObject.Find("Level") == null)
            mapData = (TextAsset)EditorGUILayout.ObjectField("Map Data:", mapData, typeof(TextAsset), false);
        spriteSheet = (Texture)EditorGUILayout.ObjectField("SpriteSheet:", spriteSheet, typeof(Texture), false);
        tileTypes = (TextAsset)EditorGUILayout.ObjectField("Tile Types:", tileTypes, typeof(TextAsset), false);
        templateGame = (TemplateGame)EditorGUILayout.EnumPopup("Game:", templateGame);
        if (templateGame == TemplateGame.metroid)
        {
            metroidRooms = (TextAsset)EditorGUILayout.ObjectField("Room Grouping:", metroidRooms, typeof(TextAsset), false);
        }

        if (GUILayout.Button("Generate!"))
        {
            GenerateAllTiles();
            this.Close();
        }
    }

    void GenerateAllTiles()
    {
        // Check for errors
        if (mapData == null)
        {
            Debug.LogError("Map data not provided!");
            return;
        }
        if (spriteSheet == null)
        {
            Debug.LogError("Sprite Sheet not provided.");
            return;
        }
        if (tileTypes == null)
        {
            Debug.LogError("Tile Types not provided!");
            return;
        }
        if (templateGame == TemplateGame.metroid && metroidRooms == null)
        {
            Debug.LogError("Room groupings not provided!");
            return;
        }
        Debug.Log("Tile Generation Script is now running!");

        if (templateGame == TemplateGame.metroid)
            roomHeight = 15;

        // Load Sprites
        Sprite[] spriteArray = Resources.LoadAll<Sprite>(spriteSheet.name);

        // Create prefabs for possible tile types
        bool prefabsFolderExists = AssetDatabase.IsValidFolder("Assets/" + prefabsFolderPath);
        if (prefabsFolderExists)
        {
            Debug.Log("Prefab folder already exists. Will use preexisting prefabs when possible.");
        }
        else
        {
            Debug.Log("Prefab folder does not already exist. Generating first time prefabs.");
            AssetDatabase.CreateFolder("Assets", prefabsFolderPath);
        }
        string[] prefabArray = tileTypes.text.Split(new char[] { ' ', '\n' });
        Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
        for (int i = 0; i < prefabArray.Length; i++)
        {
            string thisPrefabType = prefabArray[i];
            if (thisPrefabType == "NULL") // Don't generate NULL GameObject
                continue;
            if (prefabDict.ContainsKey(thisPrefabType)) // Only need to generate one prefab, will skip most tile numbers
                continue;
            string prefabName = "Tile_" + thisPrefabType;
            string prefabPath = "Assets/" + prefabsFolderPath + "/" + prefabName + ".prefab";
            GameObject oldPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            if (oldPrefab)
            {
                prefabDict.Add(thisPrefabType, oldPrefab);
            }
            else
            {
                GameObject tile = new GameObject();
                SpriteRenderer tileSR = tile.AddComponent<SpriteRenderer>();
                tileSR.sprite = spriteArray[i];
                GameObject newPrefab = PrefabUtility.CreatePrefab(prefabPath, tile);
                prefabDict.Add(thisPrefabType, newPrefab);
                DestroyImmediate(tile);
            }
        }

        // Read in the map data
        string[] mapLines = mapData.text.Split('\n');
        int height = mapLines.Length;
        string[] tileNums = mapLines[0].Split(' ');
        int width = tileNums.Length;

        // Place the map data into a 2D Array to make it faster to access
        int[,] map = new int[width, height];
        for (int y = 0; y < height; y++)
        {
            tileNums = mapLines[y].Split(' ');
            for (int x = 0; x < width; x++)
            {
                map[x, y] = int.Parse(tileNums[x]);
            }
        }

        // Root parent to store rooms and tiles
        // Need to create if this is the first time running the script on the scene
        // Otherwise use existing and modify tiles
        Transform root = null;
        GameObject rootGO = GameObject.Find("Level");
        bool levelAlreadySetup = (rootGO != null);
        if (levelAlreadySetup)
        { // Level already setup
            root = rootGO.transform;
        }
        else
        {
            root = new GameObject("Level").transform;
        }

        // If the level isn't already set up, go through and create all new tiles and rooms
        if (!levelAlreadySetup)
        {
            Debug.Log("Generating Level");

            // Rooms for organization (optional)
            Transform[,] rooms = null;
            int numRoomsX = width / roomWidth;
            int numRoomsY = height / roomHeight;
            rooms = new Transform[numRoomsX, numRoomsY];
            string roomPrefabPath = "Assets/" + prefabsFolderPath + "/Room.prefab";
            GameObject roomPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(roomPrefabPath, typeof(GameObject));
            if (roomPrefab == null)
            {
                Debug.Log("Creating Room Prefab");
                GameObject room = new GameObject("Room");
                roomPrefab = PrefabUtility.CreatePrefab(roomPrefabPath, room);
                DestroyImmediate(room);
            }

            //Metroid rooms are grouped together in halls
            Dictionary<char, GameObject> hallsDict = new Dictionary<char, GameObject>();
            string[] hallsMatrix = null;
            if (templateGame == TemplateGame.metroid)
            {
                hallsMatrix = metroidRooms.text.Split('\n');
                //if (hallsMatrix.Length != numRoomsY || hallsMatrix[0].Length != numRoomsX)
                //{
                //    Debug.LogError("Room grouping size does not equal the size of the map");
                //    return;
                //}
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
                        char thisRoomChar = hallsMatrix[y][x];
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
                    if (rooms[x, y].childCount == 0)
                        DestroyImmediate(rooms[x, y].gameObject);
                }
            }
        }

        // If the level is setup, go through the tiles and see if they need to be updated
        //TODO update to work off of what is currently in the scene 
        else
        {
            Debug.Log("Updating tiles with new prefabs");

            foreach (Transform child in root)
            {
                if (!child.name.StartsWith("Tile_")) // Only worrying about tiles
                    continue;

                GameObject childGO = root.gameObject;
                GameObject currentPrefab = (GameObject)PrefabUtility.GetPrefabParent(childGO);
                Transform originalParent = child.parent;

                int x = Mathf.RoundToInt(child.transform.position.x);
                int y = Mathf.RoundToInt(child.transform.position.y);
                int typeNum = map[x, y];
                string type = prefabArray[typeNum];
                GameObject targetPrefab = prefabDict[type];

                if (currentPrefab != targetPrefab)
                { // Need to remake the tile if the prefab changes
                    GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefabDict[type]);
                    tile.transform.position = new Vector3(x, y);
                    tile.transform.parent = originalParent;
                    tile.GetComponent<SpriteRenderer>().sprite = spriteArray[typeNum];

                    DestroyImmediate(childGO);
                    Debug.Log("Replaced");
                }
            }
        }
    }
}