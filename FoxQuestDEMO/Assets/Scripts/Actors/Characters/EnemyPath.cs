using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy that follows a given path (a set of cells).
/// </summary>
public class EnemyPath : DynamicActor
{
    [Header("Enemy path")]
    [SerializeField][Tooltip("All the locations of the camera.")]
    private List<Cell> _pathList;

    [SerializeField][Tooltip("True : when arrived at the end of the path, start again at the first element of the list.")]
    private bool _loop = false;

    private LinkedList<Cell> _path;
    private LinkedListNode<Cell> _currentCell;
    private bool _forward;

    #region Delegates

    // Indicates that the enemy has attacked and his current cell
    public delegate void EnemyPAttacked(Cell currentCell);
    public static event EnemyPAttacked EnemyPAttackedEvent;

    /// <summary>
    /// Add needed delegates.
    /// </summary>
    private void OnEnable()
    {
        Player.PlayerStartMovingEvent += MakeMove;
        Player.PlayerFinishedMovingEvent += OnPlayerFinishedMoving;
    }

    /// <summary>
    /// Remove used delegates.
    /// </summary>
    private void OnDisable()
    {
        Player.PlayerStartMovingEvent -= MakeMove;
        Player.PlayerFinishedMovingEvent -= OnPlayerFinishedMoving;
    }

    #endregion

    /// <summary>
    /// Initialises the path linked list and the first cell of the enemy.
    /// </summary>
    private void Start()
    {
        // Initialisation of the path
        _path = new LinkedList<Cell>();
        foreach (Cell cell in _pathList)
            _path.AddLast(cell);

        // Initialisation of the first cell
        _currentCell = _path.First;
        Cell = _currentCell.Value;
        _forward = true;
    }

    /// <summary>
    /// Calculate the direction to the next path position and make the enemy move to it.
    /// </summary>
    /// <param name="nextPlayerCell">The cell the player is going to.</param>
    private void MakeMove(Cell nextPlayerCell)
    {
        _currentCell = GetNextCell();

        // Calculate the next direction
        int xDirection = _currentCell.Value.GridPosition.x - Cell.GridPosition.x;
        int yDirection = _currentCell.Value.GridPosition.y - Cell.GridPosition.y;

        // Set the new rotation
        LookInDiretion(xDirection, yDirection);

        // Do the attack animation if the player is on the next cell
        if (nextPlayerCell == _currentCell.Value && AnimationController != null)
        {
            AnimationController.SetBool("isMoving", false);
            StartCoroutine(AttackMovement(_currentCell.Value));
        }
        else // Move to the next position
            MoveToGridPosition(GetDirection(xDirection, yDirection));
    }

    /// <summary>
    /// Determine the next cell to go to (next or previous cell in the list).
    /// </summary>
    /// <returns>The next cell to go to (next or previous cell in the list).</returns>
    private LinkedListNode<Cell> GetNextCell()
    {
        if (_loop)
        {
            if (_currentCell.Next == null)
                return _path.First;
            return _currentCell.Next;
        }
        else
        {
            if (_forward)
            {
                if (_currentCell.Next == null)
                    _forward = false;
            }
            else
            {
                if (_currentCell.Previous == null)
                    _forward = true;
            }
            return (_forward) ? _currentCell.Next : _currentCell.Previous;
        }
    }

    public Cell PlayerGetNextCell()
    {
        bool nextIsForward = _forward;
        if (_loop)
        {
            if (_currentCell.Next == null)
                return _path.First.Value;
            return _currentCell.Next.Value;
        }
        else
        {
            if (_forward)
            {
                if (_currentCell.Next == null)
                    nextIsForward = false;
            }
            else
            {
                if (_currentCell.Previous == null)
                    nextIsForward = true;
            }
            return (nextIsForward) ? _currentCell.Next.Value : _currentCell.Previous.Value;
        }
    }

    /// <summary>
    /// Free the content of the actual cell and set the new cell of this enemy.
    /// </summary>
    /// <param name="newCell">The new cell to assign to the enemy.</param>
    protected override void ChangeCell(Cell newCell)
    {
        Cell previousCell = Cell;
        Cell = newCell;
        previousCell.Content = null;

        // Set the enemy for the new cell and free for the ancient one
        Cell.Enemy = this;
        previousCell.Enemy = null;
    }

    /// <summary>
    /// Interpolate the position of the enemy to the given destination cell position.
    /// </summary>
    /// <param name="destinationCell">The destination cell</param>
    /// <returns>Corountine</returns>
    protected override IEnumerator MoveToCell(Cell destinationCell)
    {
        Vector3 finalDestination = new Vector3(GetXDestination(destinationCell), GetYDestination(destinationCell), destinationCell.transform.position.z);
        Vector3 startPosition = transform.position;

        inMovement = true;
        if (AnimationController != null)
            AnimationController.SetBool("isMoving", true);

        bool cellChanged = false;

        float timeElapsed = 0f;
        while (timeElapsed < _cellTransitionDuration)
        {
            transform.position = Vector3.Lerp(startPosition, finalDestination, timeElapsed / _cellTransitionDuration);

            // Cell changing
            if (Vector3.Distance(transform.position, finalDestination) < 0.5f && !cellChanged)
            {
                cellChanged = true;
                ChangeCell(destinationCell);
            }

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = finalDestination;
        inMovement = false;
        if (AnimationController != null)
            AnimationController.SetBool("isMoving", false);
    }

    #region Attack animation management

    /// <summary>
    /// Stop the attack animation and do the victory animation if the player has been attacked (means on the same cell).
    /// </summary>
    /// <param name="playerCell">The current cell of the player.</param>
    private void OnPlayerFinishedMoving(Cell playerCell)
    {
        // Do the attack animation if the player is on the next cell
        if (playerCell == _currentCell.Value && AnimationController != null)
        {
            AnimationController.SetBool("isAttacking", true);
            StartVictory();
            EnemyPAttackedEvent?.Invoke(Cell);
        }
    }

    /// <summary>
    /// Start the victory animation.
    /// </summary>
    private void StartVictory()
    {
        AnimationController.SetBool("victory", true);
        Invoke("StopVictory", 2.2f);
    }

    /// <summary>
    /// Stop the victory animation.
    /// </summary>
    private void StopVictory()
    {
        AnimationController.SetBool("victory", false);
        AnimationController.SetBool("isAttacking", false);
    }

    /// <summary>
    /// Increase the forward position to half a cell.
    /// </summary>
    /// <returns>Coroutine</returns>
    private IEnumerator AttackMovement(Cell destinationCell)
    {
        float finalDestinationX = transform.position.x;
        float finalDestinationZ = transform.position.z;
        if (destinationCell.GridPosition.x == Cell.GridPosition.x)
        {
            if (destinationCell.GridPosition.y > Cell.GridPosition.y)
                finalDestinationZ += 0.5f;
            else
                finalDestinationZ -= 0.5f;

        }
        else if (destinationCell.GridPosition.y == Cell.GridPosition.y)
        {
            if (destinationCell.GridPosition.x > Cell.GridPosition.x)
                finalDestinationX += 0.5f;
            else
                finalDestinationX -= 0.5f;
        }

        Vector3 finalDestination = new Vector3(
                                            finalDestinationX, 
                                            transform.position.y,
                                            finalDestinationZ);
        Vector3 startPosition = transform.position;

        float timeElapsed = 0f;
        while (timeElapsed < _cellTransitionDuration)
        {
            transform.position = Vector3.Lerp(startPosition, finalDestination, timeElapsed / _cellTransitionDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = finalDestination;
    }

    #endregion
}
