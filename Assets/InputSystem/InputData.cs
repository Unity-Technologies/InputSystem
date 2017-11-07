using UnityEngine.Collections;

////WIP: This is non-functional ATM; transitioning to this system will likely be gradual
////     The goal here is to keep the class-based API intact but make it a pure frontend for the data kept in here
////     The data in here should also make domain reload survivability much simpler

////TODO: provide a way by which the system can be stripped down to just this (i.e. have the higher-level part never initialize)

namespace ISX
{
    // Struct-only representation of input system data.
    // This is how the input system works underneath. Exposed classes are wrappers around functionality
    // in here.
    public struct InputData
    {
        ////REVIEW: how should modifiers and processors be handled in this system?

        public struct ArrayRangeData
        {
            public int firstIndex;
            public int count;
        }

        public struct ControlHierarchyData
        {
            public int parentIndex;
            public ArrayRangeData children;
        }

        ////TODO: this should tie into a mechanism where *any* job can process input the same way
        // A C# job that updates state from events and processes change monitors watching
        // for state changes.
        public struct ProcessInputJob
        {
            public InputEventBuffer events;
            public NativeArray<InputStateBlock> stateBlocks;
        }

        ////REVIEW: how should strings be handled in this system?
        private string[] m_Controls;
        private string[] m_Usages;
        private string[] m_Aliases;
        private string[] m_Templates;

        // Array of parent/child relation ships. One entry for each control in the system.
        private NativeArray<ControlHierarchyData> m_ControlHierarchy;
        // Array of usages for each control. One entry for each control in the system. Range maps to range
        // in m_Usages.
        private NativeArray<ArrayRangeData> m_ControlUsages;
        // Array of aliases for each control. One entry for each control in the system. Range maps to range
        // in m_Aliases.
        private NativeArray<ArrayRangeData> m_ControlAliases;
        // Templates that controls are instantiated from. One entry for each control in the system.
        private NativeArray<int> m_ControlTemplates;

        private NativeArray<InputStateBlock> m_StateBlocks;
        private InputStateBuffers m_StateBuffers;
    }
}
