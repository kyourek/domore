using System;
using System.IO;
using System.Linq;

namespace Domore.ReleaseActions {
    internal class Clean : ReleaseAction {
        private void Recurse(DirectoryInfo directory, Action<FileSystemInfo> action) {
            if (null == directory) throw new ArgumentNullException(nameof(directory));
            if (null == action) throw new ArgumentNullException(nameof(action));

            void act(FileSystemInfo info) {
                try {
                    action(info);
                }
                catch (Exception ex) {
                    Log.Debug(ex);
                }
            }

            if (directory.Exists) {
                directory.GetDirectories().ToList().ForEach(d => Recurse(d, action));
                directory.GetFiles().ToList().ForEach(f => {
                    if (f.Exists) {
                        act(f);
                    }
                });
                act(directory);
            }
        }

        public override void Work() {
            var directory = new DirectoryInfo(CodeBase.Path);
            Recurse(directory, info => info.Attributes = FileAttributes.Normal);
            Recurse(directory, info => info.Delete());
        }
    }
}
