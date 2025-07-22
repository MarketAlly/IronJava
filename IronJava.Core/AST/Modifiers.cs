using System;

namespace IronJava.Core.AST
{
    /// <summary>
    /// Java access modifiers and other modifiers as flags.
    /// </summary>
    [Flags]
    public enum Modifiers
    {
        None = 0,
        Public = 1 << 0,
        Protected = 1 << 1,
        Private = 1 << 2,
        Static = 1 << 3,
        Final = 1 << 4,
        Abstract = 1 << 5,
        Native = 1 << 6,
        Synchronized = 1 << 7,
        Transient = 1 << 8,
        Volatile = 1 << 9,
        Strictfp = 1 << 10,
        Default = 1 << 11,
        Sealed = 1 << 12,
        NonSealed = 1 << 13
    }

    public static class ModifiersExtensions
    {
        public static bool IsPublic(this Modifiers modifiers) => (modifiers & Modifiers.Public) != 0;
        public static bool IsProtected(this Modifiers modifiers) => (modifiers & Modifiers.Protected) != 0;
        public static bool IsPrivate(this Modifiers modifiers) => (modifiers & Modifiers.Private) != 0;
        public static bool IsStatic(this Modifiers modifiers) => (modifiers & Modifiers.Static) != 0;
        public static bool IsFinal(this Modifiers modifiers) => (modifiers & Modifiers.Final) != 0;
        public static bool IsAbstract(this Modifiers modifiers) => (modifiers & Modifiers.Abstract) != 0;
    }
}