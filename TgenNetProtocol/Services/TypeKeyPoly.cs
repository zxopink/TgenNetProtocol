using System;
using System.Collections.Generic;
using System.Text;

namespace TgenNetProtocol.Services
{
    /// <summary>
    /// Like Type but checks for polymorphism.
    /// So if you have a class that inherits from another class, it will return true if you check for the base class
    /// </summary>
    internal struct TypeKeyPoly
    {
        public Type Type { get; }

        public TypeKeyPoly(Type type)
        {
            Type = type;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeKeyPoly key)
            {
                if(key.Type == null)
                    return Type == null;

                if (Type == key.Type)
                    return true;

                return Type.IsSubclassOf(key.Type);
            }
            return false;
        }

        public static implicit operator TypeKeyPoly(Type t) => 
            new TypeKeyPoly(t);

        public static bool operator ==(TypeKeyPoly key1, TypeKeyPoly key2)
        {
            if (key1.Type == null)
            {
                return key2.Type == null;
            }
            return key1.Equals(key2);
        }

        public static bool operator !=(TypeKeyPoly key1, TypeKeyPoly key2) =>
            !(key1 == key2);


    }
}
