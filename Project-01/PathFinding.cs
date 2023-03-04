using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding : MonoBehaviour
{
    //Each ghost in the game has a different personlaity
    public enum Ghosts
    {
        BLINKY, //Blinky is the most aggressive ghost who always chases Pac-Man
        CLYDE, //Clyde is scared of pacman and if it comes close to pacman, it will move to its scatter area
        INKY, //Inky will not pursue Pac-Man unless other ghosts are near.
        PINKY //Is the most clever one it tries to move ahead of pacman trying to ambush it
    }

    public Ghosts ghosts;

    public Transform blinky;

    private MeshRenderer thisMaterial;

    //PATHFINDING
    List<Node> path = new List<Node>();
    private int D = 10; //Heuristic distance cost per step
    private Node lastVisitedNode;
    public Gridd _gridd;

    //Targets
    public Transform pacmanTarget;
    private Transform currentTarget;
    public List<Transform> homeTarget = new List<Transform>();
    public List<Transform> scatterTarget = new List<Transform>();

    //Movement
    private float speed = 3f;

    private Vector3 nextPos, destination;

    //Direction
    public Vector3 up = Vector3.zero,
        right = new Vector3(0, 90, 0),
        down = new Vector3(0, 180, 0),
        left = new Vector3(0, 270, 0),
        currentDirection = Vector3.zero;

    //STATEMACHINE
    public enum GhostStates
    {
        HOME,
        LEAVING_HOME,
        CHASE,
        SCATTER,
        FRIGHTENED,
        GOT_EATEN,
    }

    public GhostStates state;

    //Apperance
    //private int activeAppearance; //0 normal, 1 frightened, 2 dead
    public Material[] appearance;

    //HomeTimer
    private float timer = 3f;
    private float curTime = 0f;


    //Release Info
    public int pointsToCollect;
    public bool released = false;

    //Reset state
    private Vector3 initPosition;
    private GhostStates initState;

    private void Start()
    {
        initPosition = transform.position;
        initState = state;
        destination = transform.position;
        currentDirection = up;
        thisMaterial = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        CheckState();
    }

    /// <summary>
    /// A*
    /// </summary>
    void FindThePath()
    {
        Node startNode = _gridd.NodeRequest(transform.position); //current pos in grid
        Node goalNode = _gridd.NodeRequest(currentTarget.position); //pacman position in grid

        AStarAlgorithm(startNode, goalNode); //method to write
        PathTracer(startNode, goalNode);
    }

    void AStarAlgorithm(Node startNode, Node goalNode)
    {
        /**
        Arda Atasoy
        20180808045
        */
        /**
          The openset set holds nodes that ghosts will examine for or not.
          The closeset  holds nodes that ghosts have already examined and dropped from their agenda.
        */
        List<Node> OpenSet = new List<Node>();
        List<Node> ClosedSet = new List<Node>();

        /**
          characters will start scanning naturally from their starting point. 
        */
        OpenSet.Add(startNode);


        /**
        If there are no nodes left for the characters to examine, the game is ended.
        */
        while (OpenSet.Count() > 0)
        {

            /**
            This section finds the most reasonable node in the open set.
            */
            int bestAttempt = 0;
            for (int i = 0; i < OpenSet.Count(); i++)
            {
                if (OpenSet[i].FCost < OpenSet[bestAttempt].FCost)
                {
                    bestAttempt = i;
                }
            }
            Node currentNode = OpenSet[bestAttempt];


            /**
            Checks whether the current chase has finished.
            */
            if (currentNode.Equals(goalNode))
            {
                PathTracer(startNode, goalNode);
                break;
            }

            /**
            The examined node is removed from the openset and transferred to the closeset.
            */
            OpenSet.Remove(currentNode);
            ClosedSet.Add(currentNode);

            /**
            obtain neighbors of currentNode
            */
            List<Node> neighbors = _gridd.GetNeighborNodes(currentNode);

            /**
            Seach neighbors
            */
            foreach (Node neighbor in neighbors)
            {

            /**
            Is the node that you want to examine a place that can be examined?
            */
                if (!ClosedSet.Contains(neighbor) && neighbor.walkable)
                {

                    /**
                     It measures the total going distance from the current neighbor to the examined neighbor.
                    */
                    int temp = currentNode.gCost + GetDistance(neighbor, currentNode);

                    /**
                    It's about whether the path will updated or not.
                    */
                    bool updatePath = false;

                    /**
                    Is the neighbor added to the opensset beforehand?
                    */
                    if (OpenSet.Contains(neighbor))
                    {
                        /**
                        If it is a neighbor whose travel distance was previously measured on
                         a different path and that path is better than the current one.
                        */
                        if (temp < neighbor.gCost)
                        {
                            updatePath = true;
                            neighbor.gCost = temp;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    /**
                    If node is not an openset, it gives the distance value to that node and adds it to the openset.
                    */
                    else
                    {
                        neighbor.gCost = temp;
                        updatePath = true;
                        OpenSet.Add(neighbor);
                    }
                    
                    /**
                    It works if the path will be updated.
                    */
                    if (updatePath)
                    {
                        neighbor.hCost = GetDistance(neighbor, goalNode);
                        neighbor.parentNode = currentNode;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tracing the path for each ghost
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="goalNode"></param>
    void PathTracer(Node startNode, Node goalNode)
    {
        lastVisitedNode = startNode;
        path.Clear();
        Node currentNode = goalNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parentNode;
        }

        //reverse path to get it sorted right
        path.Reverse();

        if (ghosts == Ghosts.BLINKY)
        {
            _gridd.blinkyPath = path;
        }

        if (ghosts == Ghosts.INKY)
        {
            _gridd.inkyPath = path;
        }

        if (ghosts == Ghosts.PINKY)
        {
            _gridd.pinkyPath = path;
        }

        if (ghosts == Ghosts.CLYDE)
        {
            _gridd.clydePath = path;
        }
    }

    /// <summary>
    /// Calculation of the distance between two nodes (Heuristic)
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    int GetDistance(Node a, Node b)
    {
        int distX = Mathf.Abs(a.posX - b.posX);
        int distZ = Mathf.Abs(a.posZ - b.posZ);

        return D * (distX + distZ);
    }

    /// <summary>
    /// Physical movement of the ghosts to the target
    /// </summary>
    void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, destination,
            speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) < 0.0001f)
        {
            //Find the path
            FindThePath();

            if (path.Count > 0)
            {
                //Destination
                nextPos = _gridd.NextPathPoint(path[0]);
                destination = nextPos;

                //Rotation
                SetDirection();
                transform.localEulerAngles = currentDirection;
            }
        }
    }

    /// <summary>
    /// Handling of the direction which the ghost moves towards
    /// </summary>
    void SetDirection()
    {
        int dirX = (int)(nextPos.x - transform.position.x);
        int dirZ = (int)(nextPos.z - transform.position.z);

        if (dirX == 0 && dirZ > 0)
        {
            currentDirection = up;
        }
        else if (dirX > 0 && dirZ == 0)
        {
            currentDirection = right;
        }
        else if (dirX < 0 && dirZ == 0)
        {
            currentDirection = left;
        }
        else if (dirX == 0 && dirZ < 0)
        {
            currentDirection = down;
        }
    }

    /// <summary>
    /// Behaviours of the each different states
    /// </summary>
    void CheckState()
    {
        switch (state)
        {
            case GhostStates.HOME:
                thisMaterial.material = appearance[0];
                speed = 1.5f;
                if (!homeTarget.Contains(currentTarget))
                {
                    currentTarget = homeTarget[0];
                }

                for (int i = 0; i < homeTarget.Count; i++)
                {
                    if (Vector3.Distance(transform.position, homeTarget[i].position) < 0.0001f &&
                        currentTarget == homeTarget[i])
                    {
                        i++;
                        if (i >= homeTarget.Count)
                        {
                            i = 0;
                        }

                        currentTarget = homeTarget[i];
                        Debug.Log("Target changed to" + i);
                        continue;
                    }
                }

                if (released)
                {
                    curTime = curTime + Time.deltaTime;
                    if (curTime >= timer)
                    {
                        curTime = 0;
                        state = GhostStates.CHASE;
                    }
                }

                Move();
                break;

            case GhostStates.LEAVING_HOME:
                thisMaterial.material = appearance[0];
                break;
            //Chase pacman
            case GhostStates.CHASE:
                thisMaterial.material = appearance[0];
                speed = 3f;
                if (ghosts == Ghosts.CLYDE) //Clyde personality coding
                {
                    if (Vector3.Distance(transform.position, pacmanTarget.position) <= 8)
                    {
                        if (!scatterTarget.Contains(currentTarget))
                        {
                            currentTarget = scatterTarget[0];
                        }

                        for (int i = 0; i < scatterTarget.Count; i++)
                        {
                            if (Vector3.Distance(transform.position, scatterTarget[i].position) < 0.0001f &&
                                currentTarget == scatterTarget[i])
                            {
                                i++;
                                if (i >= scatterTarget.Count)
                                {
                                    i = 0;
                                }

                                currentTarget = scatterTarget[i];
                                Debug.Log("Target changed to" + i);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        currentTarget = pacmanTarget;
                    }
                }

                if (ghosts != Ghosts.CLYDE)
                {
                    currentTarget = pacmanTarget;
                }

                if (ghosts == Ghosts.PINKY)
                {
                    PinkyBehaviour();
                }

                if (ghosts == Ghosts.BLINKY)
                {
                    currentTarget = pacmanTarget;
                }

                if (ghosts == Ghosts.INKY)
                {
                    InkyBehaviour();
                }

                Move();
                break;

            case GhostStates.SCATTER:
                thisMaterial.material = appearance[0];
                speed = 3f;
                if (!scatterTarget.Contains(currentTarget))
                {
                    currentTarget = scatterTarget[0];
                }

                for (int i = 0; i < scatterTarget.Count; i++)
                {
                    if (Vector3.Distance(transform.position, scatterTarget[i].position) < 0.0001f &&
                        currentTarget == scatterTarget[i])
                    {
                        i++;
                        if (i >= scatterTarget.Count)
                        {
                            i = 0;
                        }

                        currentTarget = scatterTarget[i];
                        Debug.Log("Target changed to" + i);
                        continue;
                    }
                }

                Move();
                break;

            case GhostStates.FRIGHTENED:
                thisMaterial.material = appearance[1];
                speed = 1.5f;
                if (!scatterTarget.Contains(currentTarget))
                {
                    currentTarget = scatterTarget[0];
                }

                for (int i = 0; i < scatterTarget.Count; i++)
                {
                    if (Vector3.Distance(transform.position, scatterTarget[i].position) < 0.0001f &&
                        currentTarget == scatterTarget[i])
                    {
                        i++;
                        if (i >= scatterTarget.Count)
                        {
                            i = 0;
                        }

                        currentTarget = scatterTarget[i];
                        Debug.Log("Target changed to" + i);
                        continue;
                    }
                }

                Move();
                break;

            case GhostStates.GOT_EATEN:
                thisMaterial.material = appearance[2];
                speed = 11f;
                currentTarget = homeTarget[0];

                if (Vector3.Distance(transform.position, homeTarget[0].position) < 0.0001f)
                {
                    state = GhostStates.HOME;
                }

                Move();
                break;
        }
    }

    private void InkyBehaviour()
    {
        Transform blinkyToPacman = new GameObject().transform;
        Transform target = new GameObject().transform;
        Transform goal = new GameObject().transform;

        blinkyToPacman.position = new Vector3(pacmanTarget.position.x - blinky.position.x, 0,
            pacmanTarget.position.z - blinky.position.z);
        target.position = new Vector3(pacmanTarget.position.x + blinkyToPacman.position.x, 0,
            pacmanTarget.position.z + blinkyToPacman.position.z);

        goal.position = _gridd.GetNearestNonWallNode(target.position);
        currentTarget = goal;
        Debug.DrawLine(transform.position, currentTarget.position);

        Destroy(target.gameObject);
        Destroy(blinkyToPacman.gameObject);
        Destroy(goal.gameObject);
    }

    /// <summary>
    /// Pinky's personality (try to move ahead of pacman)
    /// </summary>
    private void PinkyBehaviour()
    {
        Transform aheadTarget = new GameObject().transform;
        int lookAhead = 4;
        //Set a target
        for (int i = lookAhead; i > 0; i--)
        {
            if (!_gridd.ChechInsideGrid(aheadTarget.position))
            {
                lookAhead--;
                aheadTarget.position = pacmanTarget.position + pacmanTarget.transform.forward * lookAhead;
            }
            else
            {
                break;
            }
        }

        aheadTarget.position = pacmanTarget.position + pacmanTarget.transform.forward * lookAhead;
        Debug.DrawLine(transform.position, aheadTarget.position);
        currentTarget = aheadTarget;
        Destroy(aheadTarget.gameObject);
    }

    public void Reset()
    {
        transform.position = initPosition;
        state = initState;
        destination = transform.position;
        currentDirection = up;
    }


}