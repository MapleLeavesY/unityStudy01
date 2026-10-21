using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core {
    public class ClassEntry
    {
        public Type classType;
        public string path;
        public string name;
    }

    public static class Register
    {
        public static readonly ClassEntry HelloClass = new ClassEntry() { classType = typeof(HelloWorld), path = "elele", name = "eeee"};

        public static IReadOnlyList<ClassEntry> All { get; } = new[]
        {
            HelloClass,
        };
    }
    
}