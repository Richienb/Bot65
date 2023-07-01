using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Piece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    bool _player = false;
    public bool player {
        get { return _player; }
        set {
            if(image == null)
                image = GetComponent<Image>();
            image.color = value ? Color.red : Color.white;
            _player = value;
        }
    }

    public RectTransform canvasRT;
    RectTransform rt;

    public Slot slot;

    public bool isOut;
    public bool isCaptured;

    public bool inBase {
        get {
            if(isOut)
                return true;

            if(slot == null)
                return false;

            if(player)
                return slot.index <= 6;
            else
                return slot.index >= 18;
        }
    }

    Image image;
    void Start() {
        image = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        canvasRT = this.transform.root.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
            return;

        Manager.instance.ClearHighlights();
        Debug.Log(LegalMoves().Count);
        foreach(Slot s in LegalMoves()) {
            s.Highlight(true);
        }
        
        slot.pieces.Remove(this);
        this.transform.parent = Manager.instance.topLayerParent;
    }

    public void OnDrag(PointerEventData data)
    {
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
            return;

        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRT, data.position, data.pressEventCamera, out globalMousePos))
        {
            rt.position = globalMousePos;
            rt.rotation = canvasRT.rotation;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(Manager.instance.whoseTurn != player) // can't move pieces if it's not our turn
            return;
        
        Slot closestSlot = slot;
        float closestDist = Mathf.Infinity;
        foreach (Slot s in Manager.instance.slots)
        {
            float dist = Vector2.Distance(s.transform.position, this.transform.position);
            if(dist < closestDist) {
                closestDist = dist;
                closestSlot = s;
            }
        }

        Debug.Log(closestSlot.index);

        if(LegalMoves().Contains(closestSlot)) { // if moved to a legal slot, move
            // see which dice was used and set it as used
            int diceNum = Mathf.Abs(closestSlot.index - slot.index); // difference between start and end slot
            // ^ this will cause issues when i add getting pieces out, because e.g. if you roll a 4 but get out the piece at 3
            Manager.instance.diceRolls.Remove(diceNum); // will remove the first instance of num in the dice list
            
            if(closestSlot.pieces.Count == 1) { // check for capturable pieces
                if(closestSlot.pieces[0].player != this.player) {
                    Manager.instance.CapturePiece(closestSlot.pieces[0]);
                }
            }

            if(isCaptured) {
                isCaptured = false;
                Manager.instance.player1Captured.Remove(this);
                Manager.instance.player2Captured.Remove(this);
            }

            closestSlot.AddPiece(this);
        }
        
        // if not a legal move, go back to starting slot
        slot.AddPiece(this);
        
        Debug.Log(slot.index);

        Manager.instance.HighlightLegalMoves();

        if(Manager.instance.diceRolls.Count == 0 && Manager.instance.whoseTurn == player) {
            Manager.instance.NextTurn();
        }
    }

    public List<Slot> LegalMoves() {
        List<Slot> moves = new List<Slot>();

        List<Piece> capturedCheck = this.player ? Manager.instance.player2Captured : Manager.instance.player1Captured;
        if(capturedCheck.Count > 0 && !isCaptured) // if this player has a piece captured and it's not this one, we can't move
            return moves;

        if(isOut) // out pieces are done, no moving
            return moves;

        List<int> diceRolls = new List<int>(Manager.instance.diceRolls);
        if(player) { // since red moves counter-clockwise, moves need to be going down in index rather than up
            for (int i = 0; i < diceRolls.Count; i++)
            {
                diceRolls[i] = -diceRolls[i];
            }
        }

        // check dice moves
        foreach (int diceRoll in diceRolls)
        {
            if(!(slot.index + diceRoll > 23) && !(slot.index + diceRoll < 0)) {
                Slot s = Manager.instance.slots[slot.index + diceRoll];
                if(s.pieces.Count > 0) { // check for other player's pieces on that slot
                    if(s.pieces[0].player != this.player) {
                        if(s.pieces.Count <= 1) { // if there's more than 1, we can't move there
                            moves.Add(s); // we can move to this slot and capture
                        }
                    }
                    else {
                        moves.Add(s); // there's pieces on there but they belong to us so we can move there
                    }
                }
                else {
                    moves.Add(s); // it's a free slot we can move to
                }
            }
        }

        return moves;
    }

}
