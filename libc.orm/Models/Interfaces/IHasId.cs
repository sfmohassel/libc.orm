namespace libc.orm.Models.Interfaces
{
    public interface IHasId<T>
    {
        T Id { get; set; }
    }
}