using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using gc = GameController;

public class PieceController : MonoBehaviour {
    private void Awake() {
        image = GetComponent<Image>();
    }

    private void Start() {
        if(currentState == state.player) image.color = Color.cyan;
        else if(currentState == state.enemy) image.color = Color.red;
        else image.color = Color.clear;
    }

    public void SelectPiece() {
        bool hl = GetHighlight();
        if(forcedMove) {
            if(!GetHighlight()) return;

            activePiece.MoveTo(this);
            return;
        }

        foreach(var pc in highlighted) {
            pc.Highlight(false);
        }
        highlighted.Clear();

        GetTargets();

        if(activePiece == this) { // selected the same piece
            activePiece = null;
            return;
        }
        if(activePiece == null || !hl) { // highlight tiles you can move to and current tile
            activePiece = this;
            if(forcedTargets.Count > 0) {
                highlighted.AddRange(forcedTargets);
            } else {
                highlighted.AddRange(targets);
            }

            foreach(var pc in highlighted) {
                pc.Highlight(true);
            }

            Highlight(true);
            highlighted.Add(this);
            return;
        }
        activePiece.MoveTo(this);
    }

    public void GetTargets() {
        /*
        logic flow:
        as a normal piece:
            if you can capture pieces, capture one
            otherwise check if you can move forward
        as a crowned piece:
            same as a normal piece, except:
                you can stop anywhere past a single captured enemy
                you can move anywhere diagonally when not capturing
        */
        targets.Clear();
        forcedTargets.Clear();
        if(currentState == state.inactive) return;
        Debug.Log($"called by {x} - {y}");
        if(isCrowned) {
            for(int i = 0; i < 4; i++) {
                int xmod = i > 1 ? 1 : -1, ymod = i % 2 == 0 ? 1 : -1;
                Debug.Log(xmod);
                Debug.Log(ymod);
                CheckDiagonal(xmod, ymod);
            }
        } else {
            //corner enemies
            if(x > 1 && y < tableSize-2 && table[x-1, y+1].currentState != currentState
                                        && table[x-1, y+1].currentState != state.inactive
                                        && table[x-2, y+2].currentState == state.inactive) {
                forcedTargets.Add(table[x-2, y+2]); //lower left
            }
            if(x > 1 && y > 1 && table[x-1, y-1].currentState != currentState
                              && table[x-1, y-1].currentState != state.inactive
                              && table[x-2, y-2].currentState == state.inactive) {
                forcedTargets.Add(table[x-2, y-2]); // upper left
            }
            if(x < tableSize-2 && y > 1 && table[x+1, y-1].currentState != currentState
                                        && table[x+1, y-1].currentState != state.inactive
                                        && table[x+2, y-2].currentState == state.inactive) {
                forcedTargets.Add(table[x+2, y-2]); // upper right
            }
            if(x < tableSize-2 && y < tableSize-2 && table[x+1, y+1].currentState != currentState
                                                  && table[x+1, y+1].currentState != state.inactive
                                                  && table[x+2, y+2].currentState == state.inactive) {
                forcedTargets.Add(table[x+2, y+2]); // lower right
            }

            if(currentState == state.player) {
                //upper left
                if(x > 0 && table[x-1, y-1].currentState == state.inactive) {
                    targets.Add(table[x-1, y-1]);
                }
                //upper right
                if(x < tableSize-1 && table[x+1, y-1].currentState == state.inactive) {
                    targets.Add(table[x+1, y-1]);
                }
            } else if(currentState == state.enemy) {
                //lower left
                if(x > 0 && table[x-1, y+1].currentState == state.inactive) {
                    targets.Add(table[x-1, y+1]);
                }
                //lower right
                if(x < tableSize-1 && table[x+1, y+1].currentState == state.inactive) {
                    targets.Add(table[x+1, y+1]);
                }
            }
        }
    }

    private void CheckDiagonal(int xmod, int ymod) {
        PieceController pc;

        for(int i = 1; -1<x+i*xmod && x+i*xmod<8 &&
                       -1<y+i*ymod && y+i*ymod<8; i++) {
            
            pc = table[x+i, y+i];
            if(pc.currentState == state.inactive) {
                targets.Add(pc);
            } else if(pc.currentState == currentState) {
                break;
            } else {
                i++;
                for(; -1<x+i*xmod && x+i*xmod<tableSize &&
                      -1<y+i*ymod && y+i*ymod<tableSize    ; i++) {
                    
                    pc = table[x+i, y+i];
                    
                    if(pc.currentState == state.inactive) {
                        forcedTargets.Add(pc);
                    } else break;
                }
                
                break;
            }
        }
    }

    private void MoveTo(PieceController target) {
        if((gc.playerTurn && currentState == state.enemy) || 
           (!gc.playerTurn && currentState == state.player)    ) return;
        
        int xmod = target.x > x ? 1 : -1;
        int ymod = target.y > y ? 1 : -1;
        int distance = (target.x - x) * xmod;
        int newx = x, newy = y;
        PieceController captured = null;
        for(int i = 0; i < distance - 1; i++) { // search for captured piece
            newx += xmod;
            newy += ymod;
            if(table[newx, newy].currentState != currentState && table[newx, newy].currentState != state.inactive) {
                captured = table[newx, newy];
                captured.Switch(state.inactive);
                break;
            }
        }

        activePiece = target;
        target.Switch(currentState);
        target.Crown(isCrowned);
        Switch(state.inactive);
        Crown(false);
        if(captured) {
            captured.Switch(state.inactive);
            captured.Crown(false);
        }
        
        //UpdateMovementTargets(target, captured, xmod, ymod);

        if((target.y == 0         && target.currentState == state.player) ||
           (target.y == tableSize && target.currentState == state.enemy )    ) {
            target.Crown(true);
        }

        // check if player should be forced to make a move
        target.GetTargets();
        if(forcedTargets.Count > 0) {
            forcedMove = true;
            foreach(var pc in highlighted) {
                pc.Highlight(false);
            }
            highlighted.Clear();

            highlighted.AddRange(forcedTargets);

            foreach(var pc in highlighted) {
                Debug.Log($"highlighted tile {pc.x}-{pc.y} with status {pc.currentState} targeted by {target.x}-{target.y} with status {target.currentState}");
                pc.Highlight(true);
            }
        } else {
            activePiece = null;
            gc.playerTurn = !gc.playerTurn;
        }
    }
    /*
    private void UpdateMovementTargets(PieceController target, PieceController captured, int xmod, int ymod) {
        // update piece targets where applicable
        // neighbors of origin
        GetTargets();
        if(0 <= x - xmod && x - xmod < tableSize && 0 <= y - ymod && y - ymod < tableSize) {
            table[x - xmod, y - ymod].GetTargets();
        }
        if(0 <= x - xmod && x - xmod < tableSize && 0 <= y + ymod && y + ymod < tableSize) {
            table[x - xmod, y + ymod].GetTargets();
        }
        if(0 <= x + xmod && x + xmod < tableSize && 0 <= y - ymod && y - ymod < tableSize) {
            table[x + xmod, y - ymod].GetTargets();
        }
        //indirect neighbors of origin
        if(0 <= x - 2*xmod && x - 2*xmod < tableSize && 0 <= y - 2*ymod && y - 2*ymod < tableSize) {
            table[x - 2*xmod, y - 2*ymod].GetTargets();
        }
        if(0 <= x - 2*xmod && x - 2*xmod < tableSize && 0 <= y + 2*ymod && y + 2*ymod < tableSize) {
            table[x - 2*xmod, y + 2*ymod].GetTargets();
        }
        if(0 <= x + 2*xmod && x + 2*xmod < tableSize && 0 <= y - 2*ymod && y - 2*ymod < tableSize) {
            table[x + 2*xmod, y - 2*ymod].GetTargets();
        }
        //neighbors of target
        target.GetTargets();
        if(0 <= target.x + 2*xmod && target.x + 2*xmod < tableSize && 0 <= target.y + 2*ymod && target.y + 2*ymod < tableSize) {
            table[target.x + 2*xmod, target.y + 2*ymod].GetTargets();
        }
        if(0 <= target.x + 2*xmod && target.x + 2*xmod < tableSize && 0 <= target.y - 2*ymod && target.y - 2*ymod < tableSize) {
            table[target.x + 2*xmod, target.y - 2*ymod].GetTargets();
        }
        if(0 <= target.x - 2*xmod && target.x - 2*xmod < tableSize && 0 <= target.y + 2*ymod && target.y + 2*ymod < tableSize) {
            table[target.x - 2*xmod, target.y + 2*ymod].GetTargets();
        }
        //indirect neighbors of target
        if(0 <= target.x + xmod && target.x + xmod < tableSize && 0 <= target.y + ymod && target.y + ymod < tableSize) {
            table[target.x + xmod, target.y + ymod].GetTargets();
        }
        if(0 <= target.x + xmod && target.x + xmod < tableSize && 0 <= target.y - ymod && target.y - ymod < tableSize) {
            table[target.x + xmod, target.y - ymod].GetTargets();
        }
        if(0 <= target.x - xmod && target.x - xmod < tableSize && 0 <= target.y + ymod && target.y + ymod < tableSize) {
            table[target.x - xmod, target.y + ymod].GetTargets();
        }
        //neighbors of captured piece
        if(captured == null) return;
        captured.GetTargets();
        if(0 <= captured.x - xmod && captured.x - xmod < tableSize && 0 <= captured.y - ymod && captured.y - ymod < tableSize) {
            table[captured.x - xmod, captured.y - ymod].GetTargets();
        }
        if(0 <= captured.x - xmod && captured.x - xmod < tableSize && 0 <= captured.y - ymod && captured.y - ymod < tableSize) {
            table[captured.x - xmod, captured.y - ymod].GetTargets();
        }
        //indirect neighbors of captured piece
        if(0 <= captured.x - 2*xmod && captured.x - 2*xmod < tableSize && 0 <= captured.y - 2*ymod && captured.y - 2*ymod < tableSize) {
            table[captured.x - 2*xmod, captured.y - 2*ymod].GetTargets();
        }
        if(0 <= captured.x - 2*xmod && captured.x - 2*xmod < tableSize && 0 <= captured.y - 2*ymod && captured.y - 2*ymod < tableSize) {
            table[captured.x - 2*xmod, captured.y - 2*ymod].GetTargets();
        }
    }*/

    public void Highlight(bool active) {
        Transform target = transform.parent.GetChild(0);
        target.gameObject.SetActive(active);
    }

    public bool GetHighlight() {
        return transform.parent.GetChild(0).gameObject.activeSelf;
    }

    public void SetColor(Color c) {
        gameObject.GetComponent<Image>().color = c;
    }

    public void Switch(state s) {
        switch(s) {
            case state.inactive:
                if(isCrowned) gc.score += currentState == state.player ? 3 : -3;
                else gc.score += currentState == state.enemy ? 1 : -1;
                
                image.color = Color.clear;
                currentState = s;
                break;
            case state.player:
                if(isCrowned) gc.score += 3;
                else gc.score += 1;

                image.color = Color.cyan;
                currentState = s;
                break;
            case state.enemy:
                if(isCrowned) gc.score += -3;
                else gc.score += -1;

                image.color = Color.red;
                currentState = s;
                break;
        }
    }
    
    private void Crown(bool active) {
        isCrowned = active;
        transform.GetChild(0).gameObject.SetActive(active);
    }

    public int x, y;
    public enum state {
        inactive, player, enemy
    }
    public state currentState = state.inactive;
    public List<PieceController> targets = new List<PieceController>(), 
                                 forcedTargets = new List<PieceController>();
    public bool isCrowned = false;
    private static bool forcedMove = false;
    private Image image;

    public static int tableSize = 10;
    public static PieceController[,] table = new PieceController[tableSize, tableSize];
    public static PieceController activePiece = null;
    public static List<PieceController> players = new List<PieceController>(), 
                                        enemies = new List<PieceController>(), 
                                        highlighted = new List<PieceController>();
}
