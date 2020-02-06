using System;
using libc.models;
namespace libc.orm.Models.Interfaces {
    public interface IDataGuid : IHasId<Guid>, IData {
    }
}