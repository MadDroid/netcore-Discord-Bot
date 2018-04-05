using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Bot.Helpers
{
    public static class ListHelpers
    {
        /// <summary>
        /// Gets the item from the list.
        /// </summary>
        /// <typeparam name="T">The Type of the item to be retrieved.</typeparam>
        /// <param name="list">The list were the item to be retrieved is.</param>
        /// <param name="func">A <see cref="Func{T, TResult}"/> to evaluate if the item must be retrieved.</param>
        /// <param name="result">The retrieved item.</param>
        /// <returns>True if the item was retrieved.</returns>
        public static bool TryGetItem<T>(this List<T> list, Func<T, bool> func, out T result)
        {
            foreach (var item in list)
            {
                if(func.Invoke(item))
                {
                    result = item;
                    return true;
                }
            }

            result = default(T);
            return false;
        }
    }
}
