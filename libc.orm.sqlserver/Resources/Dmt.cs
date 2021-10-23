using System.Reflection;
using libc.translation;

namespace libc.orm.sqlserver.Resources
{

    internal static class Dmt
    {
        public static readonly ILocalizer Instance = new Localizer(
            new JsonLocalizationSource(
                Assembly.GetExecutingAssembly(),
                $"{typeof(Dmt).Namespace}.dmt.i18n.json", PropertyCaseSensitivity.CaseInsensitive
            )
        );

        public static string ColumnNameCannotBeNullOrEmpty => Instance.Get("ColumnNameCannotBeNullOrEmpty");
    }

}