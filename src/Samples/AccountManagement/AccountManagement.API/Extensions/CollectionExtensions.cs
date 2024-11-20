﻿using System.Collections.Generic;

namespace AccountManagement.Extensions;

static class CollectionExtensions
{
    /// <summary>
    /// Adds the second parameter to the collection if the first parameter is true.
    /// </summary>
    public static void AddIf<TValue>(this ICollection<TValue> me, bool condition, TValue toAdd)
    {
        if(condition)
        {
            me.Add(toAdd);
        }
    }
}