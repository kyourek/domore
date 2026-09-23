using Domore.Buffers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text;

[TestFixture]
internal sealed class TextDecoderTest {
    [Test]
    public async Task Decode_CompletesAsCanceledWhenTheDecoderTokenIsCanceled() {
        var sequence = new SequenceUpdater<byte>();
        var pool = new BufferOptions().CreatePool<char>();
        using var cancellation = new CancellationTokenSource();
        var decoder = new TextDecoder("utf-8",
                                      replacementFallback: null,
                                      byteSequence: sequence,
                                      bufferPool: pool,
                                      cancellationToken: cancellation.Token);
        try {
            var decode = decoder.Decode(CancellationToken.None);
            await Task.Yield();
            cancellation.Cancel();
            var segment = new SequenceSegment<byte>(new byte[] { 0x61 });
            sequence.Update(segment, segment);

            var completed = await Task.WhenAny(decode, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.That(completed, Is.SameAs(decode), "The decoder did not complete after cancellation.");

            var decoded = await decode;
            Assert.That(decoded.Canceled, Is.True);
            Assert.That(decoded.Complete, Is.True);
            Assert.That(decoded.Success, Is.False);
        }
        finally {
            pool.Free();
        }
    }

    [Test]
    public async Task Decode_DecodesEveryByteWhenUpdatesArriveFasterThanTheyAreProcessed() {
        var bytes = System.Text.Encoding.UTF8.GetBytes(string.Concat(
            System.Linq.Enumerable.Repeat("h\u00e9llo \u4e2d\u6587\r\n", 20)));
        var expected = System.Text.Encoding.UTF8.GetString(bytes);
        for (var i = 0; i < 100; i++) {
            var sequence = new SequenceUpdater<byte>();
            var pool = new BufferOptions().CreatePool<char>();
            var decoder = new TextDecoder("utf-8",
                                          replacementFallback: null,
                                          byteSequence: sequence,
                                          bufferPool: pool,
                                          cancellationToken: CancellationToken.None);
            try {
                var first = new SequenceSegment<byte>(bytes.AsMemory(0, 1));
                var last = first;
                sequence.Update(first, last);
                for (var j = 1; j < bytes.Length; j++) {
                    last = last.Append(bytes.AsMemory(j, 1));
                    sequence.Update(first, last);
                }
                sequence.Complete();

                var decoded = default(DecodedText);
                for (; ; ) {
                    var decode = decoder.Decode(CancellationToken.None);
                    var completed = await Task.WhenAny(decode, Task.Delay(TimeSpan.FromSeconds(5)));
                    Assert.That(completed, Is.SameAs(decode), $"Iteration {i} did not complete.");
                    decoded = await decode;
                    if (decoded.Complete) {
                        break;
                    }
                }
                Assert.That(decoded.Success, Is.True, $"Iteration {i} did not succeed.");
                Assert.That(decoded.Text(), Is.EqualTo(expected), $"Iteration {i} decoded the wrong text.");
            }
            finally {
                pool.Free();
            }
        }
    }
}
