using UnityEngine;

namespace xasset
{
    [DisallowMultipleComponent]
    public sealed class Updater : MonoBehaviour
    {
        private static float _realtimeSinceUpdateStartup;
        [SerializeField] private float _maxUpdateTimeSlice = 0.01f;
        public static float maxUpdateTimeSlice { get; set; }
        public static bool busy => Time.realtimeSinceStartup - _realtimeSinceUpdateStartup >= maxUpdateTimeSlice;

        private void Awake()
        {
            maxUpdateTimeSlice = _maxUpdateTimeSlice;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _realtimeSinceUpdateStartup = Time.realtimeSinceStartup;
            Loadable.UpdateAll();
            Operation.UpdateAll();
            AsyncUpdate.UpdateAll();
        }

        private void OnDestroy()
        {
            Loadable.ClearAll();
            Operation.ClearAll();
            AsyncUpdate.ClearAll();
        }


        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            var updater = FindObjectOfType<Updater>();
            if (updater != null)
            {
                return;
            }

            new GameObject("Updater").AddComponent<Updater>();
        }
    }
}