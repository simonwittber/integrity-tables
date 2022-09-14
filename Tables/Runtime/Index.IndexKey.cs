namespace Tables;

public partial class Index<T>
{
    private struct IndexKey : IEquatable<IndexKey>
    {
        public bool Equals(IndexKey other)
        {
            return Equals(a, other.a) && Equals(b, other.b) && Equals(c, other.c) && Equals(d, other.d);
        }

        public override bool Equals(object obj)
        {
            return obj is IndexKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d);
        }

        public static bool operator ==(IndexKey left, IndexKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexKey left, IndexKey right)
        {
            return !left.Equals(right);
        }

        object a, b, c, d;

        public void SetValue(int size, object v)
        {
            switch (size)
            {
                case 0:
                    a = v;
                    return;
                case 1:
                    b = v;
                    return;
                case 2:
                    c = v;
                    return;
                case 3:
                    d = v;
                    return;
            }

            throw new ArgumentException("Size is limited to 4.");
        }
    }
}