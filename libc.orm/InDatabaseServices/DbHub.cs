using System;
using System.Collections;
using System.Collections.Generic;
using Autofac;
namespace libc.orm.InDatabaseServices {
    public class DbHub : IDbHub {
        private readonly ILifetimeScope container;
        public DbHub(ILifetimeScope container) {
            this.container = container;
        }
        public void Publish<TEvent>(TEvent ev) where TEvent : IDbOperationEvent {
            var evType = ev.GetType();
            //handlers
            var handlers = getHandlers(evType);
            foreach (dynamic handler in handlers) handler.Handle((dynamic) ev);
        }
        private IEnumerable getHandlers(Type evType) {
            return (IEnumerable) container.Resolve(
                typeof(IEnumerable<>).MakeGenericType(
                    typeof(IDbOperationEventHandler<>).MakeGenericType(evType)
                )
            );
        }
    }
}