#region usings

using System;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Composable.System.Reflection;
using System.Web.Script.Serialization;

#endregion

namespace Composable.DDD
{
    //Review:mlidbo: Consider whether comparing using public properties only would make more sense. Maybe separate class?
    ///<summary>
    /// Base class for value objects that implements value equality based on instance fields.
    /// Properties are ignored when comparing. Only fields are used.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class ValueObject<T> : IEquatable<T> where T : ValueObject<T>
    {
        /// <see cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj, null))
                return false;

            var other = obj as T;

            return Equals(other);
        }

        /// <see cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            var fields = MemberAccessorHelper<T>.GetFieldGetters(GetType());

            const int startValue = 17;
            const int multiplier = 59;

            var hashCode = startValue;

            for(var i = 0; i < fields.Length; i++)
            {
                var value = fields[i](this);

                if (value is IEnumerable && !(value is string))
                {
                    var value1Array = ((IEnumerable)value).Cast<object>().Where(me => !ReferenceEquals(me, null)).ToArray();
                    foreach(var something in value1Array)
                    {
                        hashCode = hashCode * multiplier + something.GetHashCode();
                    }
                }
                else if(!ReferenceEquals(value, null))
                    hashCode = hashCode * multiplier + value.GetHashCode();
            }

            return hashCode;
        }

        /// <see cref="object.Equals(object)"/>
        public virtual bool Equals(T other)
        {
            if(ReferenceEquals(other, null))
                return false;

            var myType = GetType();
            var otherType = other.GetType();

            if(myType != otherType)
                return false;

            var fields = MemberAccessorHelper<T>.GetFieldGetters(GetType());

            for(var i = 0; i < fields.Length; i++)
            {
                var value1 = fields[i](other);
                var value2 = fields[i]((T)this);

                if(ReferenceEquals(value1, null))
                {
                    if(!ReferenceEquals(value2 , null))
                        return false;
                }
                else if (value1 is IEnumerable && !(value1 is string))
                {
                    if (ReferenceEquals(value2, null))
                    {
                        return false;
                    }
                    var value1Array = ((IEnumerable)value1).Cast<object>().ToArray();
                    var value2Array = ((IEnumerable)value2).Cast<object>().ToArray();
                    if (value1Array.Length != value2Array.Length)
                    {
                        return false;
                    }
                    for (int j = 0; j < value1Array.Length ; ++j)
                    {
                        if (!Equals(value1Array[j], value2Array[j]))
                        {
                            return false;
                        }
                    }
                }
                else if(!value1.Equals(value2))
                    return false;
            }

            return true;
        }


        ///<summary>Compares the objects for equality using value semantics</summary>
        public static bool operator ==(ValueObject<T> lhs, ValueObject<T> rhs)
        {
            if(ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !ReferenceEquals(lhs,null) && lhs.Equals(rhs);
        }

        ///<summary>Compares the objects for inequality using value semantics</summary>
        public static bool operator !=(ValueObject<T> lhs, ValueObject<T> rhs)
        {
            return !(lhs == rhs);
        }


        ///<returns>A JSON serialized version of the instance.</returns>
        public override string ToString()
        {
            try
            {
                return GetType().FullName + ":" + new JavaScriptSerializer().Serialize(this);
            }
            catch (Exception)
            {
                return GetType().FullName;
            }
        }
    }
}