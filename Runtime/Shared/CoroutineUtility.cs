using System.Collections;
using System.Collections.Generic;

namespace DungeonGen
{
    public static class CoroutineUtility
    {
        /// <summary>
        /// Runs a coroutine to completion synchronously, following nested routines and ignoring yields.
        /// Used for editor preview and tests, so one code path serves both sync and async generation.
        /// </summary>
        public static void RunImmediately(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);

            while (stack.Count > 0)
            {
                var top = stack.Peek();

                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                    stack.Push(nested);
            }
        }
    }
}
