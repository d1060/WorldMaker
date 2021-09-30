using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pathfinding
{
    public class AStar
    {
        public static List<Node> FindPath(Node start, Node target)
        {

            try
            {
                //Typical A* algorythm
                List<Node> foundPath = new List<Node>();

                //We need two lists, one for the nodes we need to check and one for the nodes we've already checked
                List<Node> openSet = new List<Node>();
                HashSet<Node> closedSet = new HashSet<Node>();

                //We start adding to the open set
                openSet.Add(start);

                while (openSet.Count > 0)
                {
                    Node currentNode = openSet[0];

                    for (int i = 0; i < openSet.Count; i++)
                    {
                        //We check the costs for the current node
                        //You can have more opt. here but that's not important now
                        if (openSet[i].nodeCost < currentNode.nodeCost)
                        {
                            //and then we assign a new current node
                            if (!currentNode.Equals(openSet[i]))
                            {
                                currentNode = openSet[i];
                            }
                        }
                    }

                    //we remove the current node from the open set and add to the closed set
                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode);

                    //if the current node is the target node
                    if (currentNode.Equals(target))
                    {
                        //that means we reached our destination, so we are ready to retrace our path
                        foundPath = RetracePath(start, currentNode);
                        break;
                    }

                    //if we haven't reached our target, then we need to start looking the neighbours
                    foreach (Node neighbour in currentNode.GetNeighbours())
                    {
                        if (closedSet.Contains(neighbour))
                            continue;

                        if (!neighbour.IsPathfindingElligible())
                            continue;

                        //Debug.Log("1.3");
                        if (neighbour.IsPathfindingSuddenEnd())
                        {
                            neighbour.PathFindingParent = currentNode;
                            closedSet.Add(neighbour);
                            return RetracePath(start, neighbour); ;
                        }

                        //we create a new movement cost for our neighbours
                        float newMovementCostToNeighbour = currentNode.nodeCost.gCost + currentNode.DistanceTo(neighbour);

                        //and if it's lower than the neighbour's cost
                        if (newMovementCostToNeighbour < neighbour.nodeCost.gCost || !openSet.Contains(neighbour))
                        {
                            //we calculate the new costs
                            neighbour.nodeCost.gCost = newMovementCostToNeighbour;
                            neighbour.nodeCost.hCost = neighbour.DistanceTo(target);
                            //Assign the parent node
                            neighbour.PathFindingParent = currentNode;
                            //And add the neighbour node to the open set
                            if (!openSet.Contains(neighbour))
                            {
                                openSet.Add(neighbour);
                            }
                        }
                    }
                }

                //we return the path at the end
                return foundPath;
            }
            catch (System.Exception ex)
            {
                string msg = "A* Pathfinding. Error creating path from " + start + " to " + target + ": " + ex.Message;
                if (ex.InnerException != null)
                    msg += ", " + ex.InnerException.Message;
                //Debug.Log(msg);
            }

            return null;
        }

        private static List<Node> RetracePath(Node startNode, Node endNode)
        {
            //Retrace the path, is basically going from the endNode to the startNode
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                //by taking the parentNodes we assigned
                currentNode = currentNode.PathFindingParent;
            }

            //then we simply reverse the list
            path.Reverse();

            return path;
        }
    }
}
