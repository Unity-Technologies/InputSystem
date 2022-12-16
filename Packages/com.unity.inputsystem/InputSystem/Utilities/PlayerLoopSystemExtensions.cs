using System.Linq;
using UnityEngine.LowLevel;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class PlayerLoopSystemExtensions
    {
        // Recursively searches for TExistingSystem in the children of playerLoop.
        // Inserts the new playerloop system at the end of the subsystem collection unless 'insertFirst'
        // is true.
        public static bool InsertSystemAsSubSystemOf<TNewSystem, TExistingSystem>(this ref PlayerLoopSystem playerLoop,
            PlayerLoopSystem.UpdateFunction updateDelegate, bool insertFirst = false)
        {
            if (playerLoop.type == typeof(TExistingSystem))
            {
                // it's ok if the subSystemList is null because it will be created below
                if (playerLoop.subSystemList != null && playerLoop.subSystemList.Any(s => s.type == typeof(TNewSystem)))
                    return false;

                ArrayHelpers.InsertAt(ref playerLoop.subSystemList,
                    insertFirst ? 0 : playerLoop.subSystemList?.Length ?? 0,
                    new PlayerLoopSystem
                    {
                        type = typeof(TNewSystem),
                        updateDelegate = updateDelegate
                    });

                return true;
            }

            if (playerLoop.subSystemList == null)
                return false;

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                var system = playerLoop.subSystemList[i];
                if (system.InsertSystemAsSubSystemOf<TNewSystem, TExistingSystem>(updateDelegate, insertFirst))
                {
                    playerLoop.subSystemList[i].subSystemList = system.subSystemList;
                    return true;
                }
            }

            return false;
        }
    }
}
