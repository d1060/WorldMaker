using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pathfinding
{
    public abstract class Node
    {
        public Node PathFindingParent { get; set; }
        public NodeCost nodeCost;

        public Node()
        {
            nodeCost = new NodeCost();
        }

        public abstract List<Node> GetNeighbours();
        public abstract bool IsPathfindingElligible(); // OCEAN and NONE Tiles.
        public abstract bool IsPathfindingSuddenEnd(); // Tiles already with Roads.
        public abstract float DistanceTo(Node target);
    }
}
