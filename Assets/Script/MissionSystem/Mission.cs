using System;

namespace MissionSystem
{
    [Serializable]
    public class Mission
    {
        public event Action<Mission> OnComplete;

        public void Complete()
        {
            OnComplete?.Invoke(this);

            Cleanup();

            // clear subscribers to avoid memory leaks
            OnComplete = null;
        }

        // self cleanup method, can be overridden by derived classes to perform additional cleanup tasks
        protected virtual void Cleanup() { }
    }
}
