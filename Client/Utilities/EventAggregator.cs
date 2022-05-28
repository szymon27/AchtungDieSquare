using System;

namespace Client.Utilities
{
    public class EventAggregator
    {
        public event Action<string> ChangeView;
        public void OnChangeView(string view)
            => ChangeView?.Invoke(view);
    }
}
