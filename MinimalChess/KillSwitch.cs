using System;

namespace MinimalChess
{
    struct KillSwitch
    {
        Func<bool> _killSwitch;
        bool _aborted;

        public KillSwitch(Func<bool> killSwitch = null)
        {
            _killSwitch = killSwitch;
            _aborted = _killSwitch != null && _killSwitch();
        }

        public bool Get(bool update)
        {
            if (!_aborted && update && _killSwitch != null)
                _aborted = _killSwitch();
            return _aborted;
        }
    }
}
