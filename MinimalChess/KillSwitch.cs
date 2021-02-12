using System;

namespace MinimalChess
{
    public struct KillSwitch
    {
        Func<bool> _killSwitch;
        bool _aborted;

        public KillSwitch(Func<bool> killSwitch = null)
        {
            _killSwitch = killSwitch;
            _aborted = _killSwitch == null ? false : _killSwitch();
        }

        public bool Triggered
        {
            get
            {
                if (!_aborted && _killSwitch != null)
                    _aborted = _killSwitch();
                return _aborted;
            }
        }
    }
}
