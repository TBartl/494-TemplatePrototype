using UnityEngine;
using System.Collections;

public class ShowMap : MonoBehaviour {
	public int				ss = 16;
	public TextAsset		mapData;
	public GameObject		tilePrefab;
	public Vector2[]		stopPoints;
	public bool				________________;
	public int				w, h;

	// Use this for initialization
	void Start () {
		StartCoroutine( ShowMapCoRo() );
	}

	public IEnumerator ShowMapCoRo() {
		string[] lines = mapData.text.Split('\n');
		h = lines.Length;

		GameObject go;
		int tileNum;

		for (int j=0; j<h; j++) {
			string[] tiles = lines[j].Split(' ');
			w = tiles.Length;
			for (int i=0; i<w; i++) {
				foreach (Vector2 stopPoint in stopPoints) {
					if (i == stopPoint.x && j == stopPoint.y) {
						print ("Hit a stopPoint: "+i+"x"+j);
					}
				}

				// Find out which tile we're using - JB
				tileNum = int.Parse(tiles[i]);
				if (tileNum == 0) continue; // Skip tiles that we don't need. - JB

				go = Instantiate<GameObject>(tilePrefab);
				Tile t = go.GetComponent<Tile>();
				t.SetTile(i,j,tileNum);
			}

			yield return null; // Yield the coroutine to allow the screen to update. - JB
		}
	}
	
}
