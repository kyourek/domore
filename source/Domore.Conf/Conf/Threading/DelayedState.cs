using System;
using System.Threading;

namespace Domore.Conf.Threading;

internal sealed class DelayedState {
    private object State;

    public int Delay {
        get => _Delay;
        set {
            if (value < Timeout.Infinite) {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _Delay = value;
        }
    }
    private int _Delay;

    public Action Attempt(Action work) {
        if (work is null) {
            throw new ArgumentNullException(nameof(work));
        }
        var state = State = new object();
        var delay = Delay;
        var alive = true;
        Timer timer = null;
        timer = new Timer(state: state, dueTime: Timeout.Infinite, period: Timeout.Infinite,
            callback: callbackState => {
                lock (timer) {
                    if (alive) {
                        alive = false;
                        using (timer) {
                            if (callbackState == State) {
                                work();
                            }
                        }
                    }
                }
            });
        timer.Change(delay, Timeout.Infinite);
        return () => {
            lock (timer) {
                if (alive) {
                    alive = false;
                    using (timer) {
                        timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
        };
    }
}
