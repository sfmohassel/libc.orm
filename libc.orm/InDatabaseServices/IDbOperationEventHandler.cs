namespace libc.orm.InDatabaseServices {
    public interface IDbOperationEventHandler {
    }
    public interface IDbOperationEventHandler<in TEvent> : IDbOperationEventHandler where TEvent : IDbOperationEvent {
        void Handle(TEvent ev);
    }
}