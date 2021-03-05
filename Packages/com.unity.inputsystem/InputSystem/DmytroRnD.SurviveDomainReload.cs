using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
namespace UnityEngine.InputSystem.DmytroRnD
{
    // small helper to replay NativeDeviceDiscovered after domain reload
    // really should be input module api instead
    internal class SurviveDomainReload : ScriptableObject, ISerializationCallbackReceiver
    {
        private Dictionary<int, string> _activeDevices = new Dictionary<int, string>();
        [SerializeField] private int[] _keys = { };
        [SerializeField] private string[] _values = { };

        private static SurviveDomainReload _singleton;

        private static void Bootstrap()
        {
            if (_singleton != null) return;
            CreateInstance<SurviveDomainReload>();
        }

        public void OnBeforeSerialize()
        {
            _keys = _activeDevices.Keys.ToArray();
            _values = _activeDevices.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            _activeDevices = _keys
                .Zip(_values, (deviceId, deviceDescriptorJson) => new {deviceId, deviceDescriptorJson})
                .ToDictionary(x => x.deviceId, x => x.deviceDescriptorJson);
        }

        private void OnDisable()
        {
            Core.NativeClear();
            Core.NativeSetup();
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            _singleton = this;
            Core.NativeClear();
            Core.NativeSetup();
            var toAdd = _singleton._activeDevices.ToDictionary(x => x.Key,
                x => (string) x.Value.Clone());
            foreach (var kv in toAdd)
            {
                Debug.Log($"restoring {kv.Key}");
                Core.NativeDeviceDiscovered(kv.Key, kv.Value);
            }
        }

        public static void Preserve(int deviceId, string deviceDescriptorJson)
        {
            Bootstrap();
            if (_singleton._activeDevices.ContainsKey(deviceId))
                _singleton._activeDevices[deviceId] = deviceDescriptorJson;
            else
                _singleton._activeDevices.Add(deviceId, deviceDescriptorJson);
        }

        public static void Remove(int deviceId)
        {
            Bootstrap();
            _singleton._activeDevices.Remove(deviceId);
        }
    }
}
#endif