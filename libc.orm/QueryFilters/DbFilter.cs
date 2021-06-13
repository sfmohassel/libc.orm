namespace libc.orm.QueryFilters
{
    public abstract class DbFilter
    {
        public string Error { get; protected set; }
        public bool Ok => string.IsNullOrWhiteSpace(Error);
    }
}