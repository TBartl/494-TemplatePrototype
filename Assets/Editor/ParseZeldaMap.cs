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

public class ParseZeldaMap : MonoBehaviour {
    /* Inspector Tunables */
    public int tile_dimensions = 16; // Tiles are always squares.
    public Texture2D inputMap;
    public int outputSpritesTextureSize = 1024;
    public Texture2D outputSprites;
    public int numSprites = 0;
    public List<ulong> checkSums;
    public Vector2[] stopPoints;

    /* Private Data */
    private int        	mapW;
    private Color32[] 	mapData, newData;
    private string[]    indices;
    
    // Use this for initialization
    void Start () {
        StartCoroutine( ParseMap() );
    }
    
    // Update is called once per frame
    public IEnumerator ParseMap() {
        // Pull in the original Metroid map
        mapW = inputMap.width;
        int w = inputMap.width/tile_dimensions;
        int h = inputMap.height/tile_dimensions;
        
        indices = new string[w*h];
        
        mapData = inputMap.GetPixels32(0); // This will take a long time and a LOT of memory!
        
        // Create a new texture to hold the individual sprites
        newData = new Color32[outputSpritesTextureSize * outputSpritesTextureSize];
        outputSprites = new Texture2D(outputSpritesTextureSize, outputSpritesTextureSize, TextureFormat.RGBA32, false);
        
        // Create a list of checkSums for the individual sprites
        checkSums = new List<ulong>();
        
        ulong cs;
        int found = -1;
        int ndx;
        // Parse it one 16x16-pixel section at-a-time
        for (int j=0; j<h; j++) {
            for (int i=0; i<w; i++) {
                foreach (Vector2 stopPoint in stopPoints) {
                    if (i == stopPoint.x && j == stopPoint.y) {
                        print ("Hit a stopPoint: "+i+"x"+j);
                    }
                }


                Color32[] chunk = GetChunk(i,j);
                // Convert this section to a checkSum
                cs = CheckSum(chunk);
                
                // Check to see whether the current checkSum matches an already-found one
                found = -1;
                for (int k=0; k<checkSums.Count; k++) {
                    if (cs == checkSums[k]) {
                        found = k;
                        break;
                    }
                }
                // If it doesn't, make a new checkSum and a new entry in the outputSprites Texture2D.
                if (found == -1) {
                    checkSums.Add(cs);
                    OutputChunk(chunk);
                    found = numSprites;
                    numSprites++;
                }
                ndx = i + j*w;
                indices[ndx] = found.ToString("D3");
                //                print ("i="+i+"\tj="+j+"\tSprites found:"+numSprites);
            }
            print ("j="+j+"\tSprites found:"+numSprites);
            yield return null;
        }
        
        // Generate the Texture2D from the newData
        outputSprites.SetPixels32(newData, 0);
        outputSprites.Apply(true);
        
        SaveTextureToFile(outputSprites, "spriteMap.jpg");
        
        // Output the text file 
        string[] ind2 = new string[h];
        string[] indTemp = new string[w];
        for (int i=0; i<h; i++) {
            System.Array.Copy(indices, i*w, indTemp, 0, w);
            ind2[i] = string.Join(" ", indTemp);
        }
        string str = string.Join("\n",ind2);
        
        File.WriteAllText(Application.dataPath+"/"+"spriteText.txt", str);
        print (str);
    }
    
    
    public Color32[] GetChunk(int x, int y) {
        Color32[] res = new Color32[tile_dimensions * tile_dimensions];
        x *= tile_dimensions;
        y *= tile_dimensions;
        int ndx;
        for (int j=0; j< tile_dimensions; j++) {
            for (int i=0; i< tile_dimensions; i++) {
                ndx = x+i + (y+j)*mapW;
                try {
                    res[i + j* tile_dimensions] = mapData[ ndx ];
                }
                catch (System.IndexOutOfRangeException) {
                    print ("GetChunk() Index out of range:"+ndx+"\tLength:"+mapData.Length+"\ti="+i+"\tj="+j);
                }
            }
        }
        return res;
    }
    
    public ulong CheckSum(Color32[] chunk) {
        ulong res = 0;
        
        for(int i = 0; i < chunk.Length; i++)
            res += (ulong) (chunk[i].r * (i+1) + chunk[i].g * (i+2) + chunk[i].b * (i+3));
        
        return res;
    }
    
    void OutputChunk(Color32[] chunk) {
        int spl = outputSpritesTextureSize / tile_dimensions;
        int x = numSprites % spl;
        int y = numSprites / spl;
        y = spl - y - 1;
        x *= tile_dimensions;
        y *= tile_dimensions;
        
        int ndxND, ndxC;
        for (int i=0; i< tile_dimensions; i++) {
            for (int j=0; j< tile_dimensions; j++) {
                ndxND = x+i + (y+j)*outputSpritesTextureSize;
                ndxC = i + j* tile_dimensions;
                
                try {
                    newData[ ndxND ] = chunk[ ndxC ];
                }
                catch (System.IndexOutOfRangeException) {
                    print ("OutputChunk() Index out of range:"+ndxND+"\tLengthND:"+newData.Length+"\tndxC="+ndxC+"\tLengthC"+chunk.Length+"\ti="+i+"\tj="+j);
                }
            }
        }
    }
    
    void SaveTextureToFile( Texture2D tex, string fileName) {
        byte[] bytes = tex.EncodeToJPG(100);
        File.WriteAllBytes(Application.dataPath + "/"+fileName, bytes);
    }
}