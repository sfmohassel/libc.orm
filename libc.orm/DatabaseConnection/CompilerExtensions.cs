using SqlKata.Compilers;

namespace libc.orm.DatabaseConnection
{
    public static class CompilerExtensions
    {
        public static CompilerHelper GetHelper(this Compiler compiler)
        {
            return new CompilerHelper(compiler);
        }
    }
}