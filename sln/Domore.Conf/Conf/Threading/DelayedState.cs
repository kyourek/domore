using System;
using System.Threading;

namespace Domore.Conf.Threading {
    internal sealed class DelayedState {
        private object State;

        public int Delay { get; set; }

        public Action Attempt(Action work) {
            State = new object();
            var alive = true;
            var timer = default(Timer);
            timer = new Timer(state: State, dueTime: Delay, period: Timeout.Infinite, callback: state => {
                lock (timer) {
                    if (alive) {
                        alive = false;
                        using (timer) {
                            if (state == State) {
                                work();
                            }
                        }
                    }
                }
            });
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
}
