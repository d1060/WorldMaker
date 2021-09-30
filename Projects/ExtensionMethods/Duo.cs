using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Duo<T, Y>
{
    T one = default(T);
    Y two = default(Y);

    public Duo(T t, Y y)
    {
        one = t;
        two = y;
    }

    public T First { get { return one; } set { one = value; } }
    public Y Second { get { return two; } set { two = value; } }

    public override string ToString()
    {
        return one.ToString() + "|" + two.ToString();
    }

    public override int GetHashCode()
    {
        int myHash = unchecked(one.GetHashCode() * 523 + two.GetHashCode() * 541);
        return myHash;
    }

    public override bool Equals(object obj)
    {
        // False if the object is null
        if (obj == null)
            return false;

        // Try casting to a DistanceCell. If it fails, return false;
        Duo<T, Y> pDuo = obj as Duo<T, Y>;
        if (pDuo == null)
            return false;

        return (this.one.Equals(pDuo.one) && this.two.Equals(pDuo.two));
    }

    public static bool operator ==(Duo<T, Y> a, Duo<T, Y> b)
    {
        // If both are null, or both are same instance, return true.
        if (System.Object.ReferenceEquals(a, b))
        {
            return true;
        }

        // If either is null, return false.
        // Cast a and b to objects to check for null to avoid calling this operator method
        // and causing an infinite loop.
        if ((object)a == null || (object)b == null)
        {
            return false;
        }

        return (a.one.Equals(b.one) && a.two.Equals(b.two));
    }

    public static bool operator !=(Duo<T, Y> a, Duo<T, Y> b)
    {
        return !(a == b);
    }

    public class DuoComparer : IEqualityComparer<Duo<T, Y>>
    {
        public bool Equals(Duo<T, Y> x, Duo<T, Y> y)
        {
            return x.First.Equals(y.First) && x.Second.Equals(y.Second);
        }

        public int GetHashCode(Duo<T, Y> t)
        {
            return t.GetHashCode();
        }
    }
}
