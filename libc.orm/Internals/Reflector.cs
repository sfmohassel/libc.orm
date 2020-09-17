using System;
using System.Collections.Generic;
using System.Linq;
using FastMember;
namespace libc.orm.Internals {
    internal sealed class Reflector {
        private static readonly Type s = typeof(string);
        private readonly MemberSet members;
        private readonly ObjectAccessor o;
        private readonly TypeAccessor t;
        public Reflector() {
        }
        public Reflector(Type type) {
            t = TypeAccessor.Create(type);
            members = t.GetMembers();
        }
        public Reflector(object obj, bool allowNonPublic = false) {
            o = ObjectAccessor.Create(obj, allowNonPublic);
        }
        public Reflector(Type type, object obj, bool allowNonPublic = false) : this(type) {
            o = ObjectAccessor.Create(obj, allowNonPublic);
        }
        public MemberSet GetMembers() {
            return members;
        }
        public ObjectAccessor GetObjectAccessor() {
            return o;
        }
        public TypeAccessor GetTypeAccessor() {
            return t;
        }
        public IEnumerable<string> GetMemberNames() {
            return members.Select(a => a.Name);
        }
        public object Get(string propertyName) {
            return o[propertyName];
        }
        public void Set(string propertyName, object value, bool setNullOrDefaultValue) {
            var member = members.FirstOrDefault(a => a.Name == propertyName);
            if (member == null) return;
            if (value != null) {
                o[propertyName] = value;
                return;
            }
            if (!setNullOrDefaultValue) return;
            var isNullable = Nullable.GetUnderlyingType(member.Type) != null || member.Type == s;
            if (isNullable)
                o[propertyName] = null;
            else
                o[propertyName] = Activator.CreateInstance(member.Type);
        }
    }
}