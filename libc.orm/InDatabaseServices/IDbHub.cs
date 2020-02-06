namespace libc.orm.InDatabaseServices {
    public interface IDbHub {
        void Publish<TEvent>(TEvent ev) where TEvent : IDbOperationEvent;
    }
}