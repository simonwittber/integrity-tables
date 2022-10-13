namespace IntegrityTables;

internal partial class UniqueIndex<T>
{
    /// <summary>
    /// This type is used to create a Dictionary key using up to 4 arbitrary objects.
    /// </summary>
    private struct MultiFieldKey : IEquatable<MultiFieldKey>
    {
        object a, b, c, d;
        
        public bool Equals(MultiFieldKey other)
        {
            return Equals(a, other.a) && Equals(b, other.b) && Equals(c, other.c) && Equals(d, other.d);
        }

        public override bool Equals(object obj)
        {
            return obj is MultiFieldKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d);
        }

        public static bool operator ==(MultiFieldKey left, MultiFieldKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MultiFieldKey left, MultiFieldKey right)
        {
            return !left.Equals(right);
        }
        
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