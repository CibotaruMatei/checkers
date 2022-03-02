using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour {
    void Start() {
        Vector2 screen = transform.gameObject.GetComponent<RectTransform>().sizeDelta;
        float tileScale = screen.y / (PieceController.tableSize + 1);
        Vector2 tileSize = new Vector2(tileScale, tileScale);
        float left, top = screen.y;

        Color[] colors = new Color[] {Color.black, Color.white};

        for (int i = 0; i < PieceController.tableSize; i++) {
            left = (screen.x - tileScale * (PieceController.tableSize - 1)) / 2;
            top -= tileScale;
            for(int j = 0; j < PieceController.tableSize; j++) {
                GameObject newTile = Instantiate(tile, new Vector3(left, top), Quaternion.identity, transform);
                newTile.GetComponent<RectTransform>().sizeDelta = tileSize;
                foreach(var tf in newTile.transform.GetComponentsInChildren<RectTransform>()) {
                    tf.sizeDelta = tileSize;
                }
                newTile.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = tileSize;
                newTile.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = tileSize;
                newTile.GetComponent<Image>().color = colors[(i + j) % 2];
                newTile.name = $"{j}-{i}";

                left += tileScale;

                PieceController pc = newTile.transform.GetChild(1).GetComponent<PieceController>();                
                PieceController.table[j, i] = pc;
                if((i + j) % 2 == 1) {
                    pc.gameObject.SetActive(false);
                    continue; //only place pieces on black tiles
                }
                
                pc.x = j;
                pc.y = i;
                if(i < PieceController.tableSize / 2 - 1) {
                    pc.Switch(PieceController.state.enemy);
                } else if(i > PieceController.tableSize / 2) {
                    pc.Switch(PieceController.state.player);
                }
            }
        }
        /*
        //load targets
        for(int i = 0; i < PieceController.tableSize; i++) {
            for(int j = 0; j < PieceController.tableSize; j++) {
                if(PieceController.table[j, i].currentState != PieceController.state.inactive) {
                    PieceController.table[j, i].GetTargets();
                }
            }
        }
        */
    }

    public GameObject tile;
    public static bool playerTurn = true;
    public static int score = 0;
}
