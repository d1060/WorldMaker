using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pathfinding
{
    public class NodeCost : IComparer<NodeCost>, IEqualityComparer<NodeCost>
    {
        public float hCost; // How far away we are from the end.
        public float gCost; // How far away we are from start.

        // Use this for initialization
        public NodeCost()
        {

        }

        public float fCost
        {
            get
            {
                return hCost + gCost;
            }
        }

        public int Compare(NodeCost x, NodeCost y)
        {
            // False if the object is null
            if (x == null || y == null)
                return 0;

            if (x as NodeCost == null || y as NodeCost == null)
                return 0;

            if (((NodeCost)x).LessThan((NodeCost)y))
                return -1;

            if (((NodeCost)x).GreaterThan((NodeCost)y))
                return 1;

            return 0;
        }

        public int Compare(object x, object y)
        {
            // False if the object is null
            if (x == null || y == null)
                return 0;

            if (x as NodeCost == null || y as NodeCost == null)
                return 0;

            if (((NodeCost)x).LessThan((NodeCost)y))
                return -1;

            if (((NodeCost)x).GreaterThan((NodeCost)y))
                return 1;

            return 0;
        }

        public bool Equals(NodeCost x, NodeCost y)
        {
            // False if the object is null
            if (x == null || y == null)
                return false;

            if (x as NodeCost == null || y as NodeCost == null)
                return false;

            if (((NodeCost)x).LessThan((NodeCost)y))
                return false;

            if (((NodeCost)x).GreaterThan((NodeCost)y))
                return false;

            return true;
        }

        public new bool Equals(object x, object y)
        {
            // False if the object is null
            if (x == null || y == null)
                return false;

            if (x as NodeCost == null || y as NodeCost == null)
                return false;

            if (((NodeCost)x).LessThan((NodeCost)y))
                return false;

            if (((NodeCost)x).GreaterThan((NodeCost)y))
                return false;

            return true;
        }

        public int GetHashCode(NodeCost obj)
        {
            int hash = hCost.GetHashCode() ^ gCost.GetHashCode();
            return hash;
        }

        public int GetHashCode(object obj)
        {
            int hash = hCost.GetHashCode() ^ gCost.GetHashCode();
            return hash;
        }

        public static bool operator <(NodeCost a, NodeCost b)
        {
            if (a.fCost < b.fCost ||
                (a.fCost == b.fCost &&
                a.hCost < b.hCost))
                return true;
            else
                return false;
        }

        public static bool operator >(NodeCost a, NodeCost b)
        {
            if (a.fCost > b.fCost ||
                (a.fCost == b.fCost &&
                a.hCost > b.hCost))
                return true;
            else
                return false;
        }

        public bool LessThan(NodeCost b)
        {
            if (fCost < b.fCost ||
                (fCost == b.fCost &&
                hCost < b.hCost))
                return true;
            else
                return false;
        }

        public bool GreaterThan(NodeCost b)
        {
            if (fCost > b.fCost ||
                (fCost == b.fCost &&
                hCost > b.hCost))
                return true;
            else
                return false;
        }

        public static int Sort(NodeCost a, NodeCost b)
        {
            return a.Compare(a, b);
        }

        public override string ToString()
        {
            return "h=" + hCost.ToString() + ";g=" + gCost.ToString();
        }
    }
}
