using UnityEngine;

/// <summary>
/// Represents a location on a grid during a level.
/// </summary>
public class Cell : MonoBehaviour
{
    public GridPosition GridPosition;     // The position of the cell on the grid

    private Actor _content;
    public Actor Content    // The possible actor that the cell can contain
    { 
        get { return _content; }
        set // Also set the Trigger as triggered if the content tag is contained in the Trigger matching tags
        {
            _content = value;
            if (_content != null)
            {
                if (Trigger != null && !Trigger.IsTriggered && Trigger.MatchingTags.Contains(_content.tag))
                    Trigger.IsTriggered = true;
            }
            else
            {
                if (Trigger != null && !Trigger.KeepTriggered)
                    Trigger.IsTriggered = false;
            }
        }
    }

    public Trigger Trigger { get; set; }    // The possible trigger that the cell can contain
    public Door Door { get; set; }          // The possible door the cell can contain
    public EnemyPath Enemy { get; set; }    // The possible enemy the cell can contain


    /// <summary>
    /// Set the grid position of the cell.
    /// </summary>
    private void Awake() => GridPosition = new GridPosition((int)transform.localPosition.x, (int)transform.localPosition.z);
}
