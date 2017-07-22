/* A component for parsing a Zelda map */

/* A word on the generation of game maps
 * 
 * Once upon a time, EECS 494 students were required to assemble maps from classic games
 * tile-by-tile, by hand. Clicking...dragging...clicking...dragging. What a waste of time.
 * 
 * Jeremy Bond sought to automate this process with this special script. This component
 * consumes png images of maps (like the one in your project-- Resources/dungeon.png),
 * and creates two essential things-- (1) A grid of codes representing map tiles (Resources/map_sprites_data.txt),
 * and (2) a code-key of images assigning a tile image to each code in the map (Resources/map_tile_sprites.png).
 * 
 * With these two files, we can automate the process of reconstructing the background of an entire level by
 * iterating through the grid text file, and applying the corresponding images from the code-key. 
 * The ShowMapOnCamera component does this for you based on the position of the camera. 
 * No more manual clicking and dragging!
 * 
 * But while this may make everything LOOK accurate, what if you need to add functionality to certain tiles?
 * For instance, what if you need to designate certain tile types as "Solid", like walls and statues?
 * For that, take a peak at the Tile.cs script, and the Resources/Collision.txt and Resources/Destructible.txt
 * files.
 * 
 * It behooves you to spend some time studying this script, as you may need to customize it when you create
 * your custom levels.
 * - AY
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class ParseMap : EditorWindow
{
    public Texture2D inputMap;

    int tileDimensions = 16; // Tiles are always squares.
    int outputSpritesTextureSize = 256;

    string textureName = "tileSpriteSheet.png";
    string textName = "mapAsTileIndexes.txt";

    [MenuItem("494/1) Parse Map", false, 0)]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(ParseMap));
    }

    void OnGUI()
    {
        GUILayout.Label("Parse map into output spritesheet and text file", EditorStyles.boldLabel);
        inputMap = (Texture2D)EditorGUILayout.ObjectField("Input Map:", inputMap, typeof(Texture2D), false);
        if (GUILayout.Button("Run Script!"))
        {
            Parse();
            this.Close();
        }
    }

    // Update is called once per frame
    public void Parse()
    {
        if (File.Exists(Application.dataPath + "/" + textureName))
        {
            Debug.LogError("Texture already generated! Delete or rename " + textureName + " to run the script!");
            return;
        }
        if (File.Exists(Application.dataPath + "/" + textName))
        {
            Debug.LogError("Text file already generated! Delete or rename " + textName + " to run the script!");
            return;
        }

        // Pull in the original Metroid map
        Color32[] mapData = inputMap.GetPixels32(0); // This will take a long time and a LOT of memory!

        // Create a new texture to hold the individual sprites
        Color32[] newData = new Color32[outputSpritesTextureSize * outputSpritesTextureSize];
        Texture2D outputSprites = new Texture2D(outputSpritesTextureSize, outputSpritesTextureSize, TextureFormat.RGBA32, false);

        int mapTilesX = inputMap.width / tileDimensions;
        int mapTilesY = inputMap.height / tileDimensions;
        string[,] indices = new string[mapTilesX, mapTilesY];




        // Create a list of checkSums for the individual sprites
        // CheckSums are used to distinguish two tiles from each other
        List<ulong> checkSums = new List<ulong>();


        // Parse it one 16x16-pixel section at-a-time
        for (int y = 0; y < mapTilesY; y++)
        {
            for (int x = 0; x < mapTilesX; x++)
            {
                Color32[] chunk = GetChunk(x, y, mapData);

                // Convert this section to a checkSum
                ulong checkSum = CheckSum(chunk);

                // Check to see whether the current checkSum matches an already-found one
                int checkSumIndex = checkSums.IndexOf(checkSum);

                // If it doesn't, make a new checkSum and a new entry in the outputSprites Texture2D.
                if (checkSumIndex == -1)
                {
                    checkSums.Add(checkSum);
                    checkSumIndex = checkSums.Count - 1;


                    OutputChunk(chunk, newData, checkSumIndex);
                }
                indices[x, y] = checkSumIndex.ToString("D3");
            }
        }

        // Generate and output the text file 
        string outputText = "";
        for (int y = 0; y < mapTilesY; y++)
        {
            for (int x = 0; x < mapTilesX; x++)
            {
                outputText += indices[x, y] + ' ';
            }
            outputText += '\n';
        }

        File.WriteAllText(Application.dataPath + "/" + textName, outputText);

        // Generate and output the Texture2D from the newData
        outputSprites.SetPixels32(newData, 0);
        outputSprites.Apply(true);
        SaveTextureToFile(outputSprites, textureName);

        // Update sprite sheet of texture
        int numTiles = checkSums.Count;
        int tileCountPerSide = outputSpritesTextureSize / tileDimensions;
        List<SpriteMetaData> newSpriteMetaData = new List<SpriteMetaData>();
        for (int i = 0; i < numTiles; i++)
        {
            int x = i % tileCountPerSide;
            int y = i / tileCountPerSide;
            SpriteMetaData smd = new SpriteMetaData();
            smd.pivot = new Vector2(0.5f, 0.5f);
            smd.alignment = 9;
            smd.name = "t_" + i.ToString("D3");
            smd.rect = new Rect(x * tileDimensions, (tileCountPerSide - y - 1) * tileDimensions, tileDimensions, tileDimensions);
            newSpriteMetaData.Add(smd);
        }
        AssetDatabase.Refresh();

        Texture2D outputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + textureName);
        string path = AssetDatabase.GetAssetPath(outputTexture);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.isReadable = true;
        ti.spritePixelsPerUnit = 16;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritesheet = newSpriteMetaData.ToArray();
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();

    }


    public Color32[] GetChunk(int x, int y, Color32[] mapData)
    {
        Color32[] res = new Color32[tileDimensions * tileDimensions];
        x *= tileDimensions;
        y *= tileDimensions;
        int ndx;
        for (int j = 0; j < tileDimensions; j++)
        {
            for (int i = 0; i < tileDimensions; i++)
            {
                ndx = x + i + (y + j) * inputMap.width;
                try
                {
                    res[i + j * tileDimensions] = mapData[ndx];
                }
                catch (System.IndexOutOfRangeException)
                {
                    Debug.Log("GetChunk() Index out of range:" + ndx + "\tLength:" + mapData.Length + "\ti=" + i + "\tj=" + j);
                }
            }
        }
        return res;
    }

    public ulong CheckSum(Color32[] chunk)
    {
        ulong res = 0;

        for (int i = 0; i < chunk.Length; i++)
            res += (ulong)(chunk[i].r * (i + 1) + chunk[i].g * (i + 2) + chunk[i].b * (i + 3));

        return res;
    }

    void OutputChunk(Color32[] chunk, Color32[] toData, int spriteIndex)
    {
        int spl = outputSpritesTextureSize / tileDimensions;
        int x = spriteIndex % spl;
        int y = spriteIndex / spl;
        y = spl - y - 1;
        x *= tileDimensions;
        y *= tileDimensions;

        int ndxND, ndxC;
        for (int i = 0; i < tileDimensions; i++)
        {
            for (int j = 0; j < tileDimensions; j++)
            {
                ndxND = x + i + (y + j) * outputSpritesTextureSize;
                ndxC = i + j * tileDimensions;

                try
                {
                    toData[ndxND] = chunk[ndxC];
                }
                catch (System.IndexOutOfRangeException)
                {
                    Debug.Log("OutputChunk() Index out of range:" + ndxND + "\tLengthND:" + toData.Length + "\tndxC=" + ndxC + "\tLengthC" + chunk.Length + "\ti=" + i + "\tj=" + j);
                }
            }
        }
    }

    void SaveTextureToFile(Texture2D tex, string fileName)
    {
        byte[] bytes = tex.EncodeToJPG(100);
        File.WriteAllBytes(Application.dataPath + "/" + fileName, bytes);
    }
}