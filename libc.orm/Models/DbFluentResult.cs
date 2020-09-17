using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace libc.orm.Models {
    public class DbFluentResult {
        [JsonIgnore]
        public bool Success => Errors.Count == 0;
        [JsonIgnore]
        public bool Fail => Errors.Count > 0;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Messages { get; set; } = new List<string>();
        public DbFluentResult AddError(params string[] errors) {
            if (errors != null) Errors.AddRange(errors);
            return this;
        }
        public DbFluentResult AddMessage(params string[] messages) {
            if (messages != null) Messages.AddRange(messages);
            return this;
        }
        /// <summary>
        /// </summary>
        /// <param name="delimiter">null means <see cref="Environment.NewLine" /></param>
        /// <returns></returns>
        public string ConcatErrors(string delimiter = null) {
            var d = delimiter ?? Environment.NewLine;
            return Errors == null ? string.Empty : string.Join(d, Errors);
        }
        /// <summary>
        /// </summary>
        /// <param name="delimiter">null means <see cref="Environment.NewLine" /></param>
        /// <returns></returns>
        public string ConcatMessages(string delimiter = null) {
            var d = delimiter ?? Environment.NewLine;
            return Messages == null ? string.Empty : string.Join(d, Messages);
        }
    }
    public class DbFluentResult<T> : DbFluentResult {
        public T Value { get; set; }
        public DbFluentResult<T> SetValue(T value) {
            Value = value;
            return this;
        }
    }
    public static class DbFluentResultExtensions {
        public static bool IsOk(this DbFluentResult res) {
            return res?.Success ?? false;
        }
        public static bool IsOk<T>(this DbFluentResult<T> res) {
            return res?.Success ?? false;
        }
    }
}