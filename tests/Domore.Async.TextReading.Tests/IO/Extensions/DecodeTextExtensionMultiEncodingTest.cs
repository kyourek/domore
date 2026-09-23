using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domore.Text;

namespace Domore.IO.Extensions;

[TestFixture]
public class DecodeTextExtensionMultiEncodingTest {
    private static readonly string Text = string.Concat(
        System.Linq.Enumerable.Repeat("h\u00e9llo \u4e2d\u6587 line\r\n", 300));

    private static readonly IReadOnlyList<string> Candidates = new[] {
        "utf-8",
        "utf-16BE",
        "utf-16",
        "iso-8859-1",
        "utf-32"
    };

    private string File;

    private async Task<DecodedText> DecodeText(int streamBufferSize, int textBufferSize) {
        var options = new DecodedTextOptions {
            Encoding = new List<string>(Candidates)
        };
        options.StreamBuffer.Size = streamBufferSize;
        options.TextBuffer.Size = textBufferSize;
        using (options.Disposable()) {
            var task = new FileInfo(File).DecodeText((DecodedTextDelegate)null, options, CancellationToken.None);
            var done = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.That(done, Is.SameAs(task), "Decoding did not complete. Decoder results were lost.");
            return await task;
        }
    }

    [SetUp]
    public void SetUp() {
        File = Path.GetTempFileName();
        System.IO.File.WriteAllText(File, Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    [TearDown]
    public void TearDown() {
        System.IO.File.Delete(File);
    }

    [Test]
    public async Task DecodeText_PrefersTheFirstCandidateEncodingThatSucceeds([Values(1, 3, 512)] int streamBufferSize) {
        var decoded = await DecodeText(streamBufferSize, textBufferSize: 512);
        Assert.That(decoded, Is.Not.Null);
        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.UTF8.EncodingName));
        Assert.That(decoded.Text(), Is.EqualTo(Text));
    }

    [Test]
    public async Task DecodeText_CompletesEveryTimeWithManyCandidateEncodings() {
        for (var i = 0; i < 200; i++) {
            var decoded = await DecodeText(
                streamBufferSize: 1 + (i % 7),
                textBufferSize: 1 + (i % 5));
            Assert.That(decoded, Is.Not.Null, $"Iteration {i} produced no result.");
            Assert.That(decoded.Success, Is.True, $"Iteration {i} did not succeed.");
            Assert.That(decoded.EncodingName, Is.EqualTo(Encoding.UTF8.EncodingName), $"Iteration {i} used the wrong encoding.");
            Assert.That(decoded.Text(), Is.EqualTo(Text), $"Iteration {i} decoded the wrong text.");
        }
    }

    [Test]
    public async Task DecodeText_PreservesOnlyTheSelectedCandidate() {
        var options = new DecodedTextOptions {
            Encoding = ["utf-8", "iso-8859-1"]
        };
        options.TextBuffer.Clear = true;
        options.TextBuffer.Shared = false;
        var completed = new Dictionary<object, DecodedText>();
        DecodedText selected;
        using (options.Disposable()) {
            selected = await new FileInfo(File).DecodeText(
                (decoded, _) => {
                    if (decoded.Success) {
                        completed[decoded.State] = decoded;
                    }
                    return Task.CompletedTask;
                },
                options,
                CancellationToken.None);
            Assert.That(completed.Count, Is.EqualTo(2));
            Assert.That(completed[selected.State].Decoder.TextWinner, Is.EqualTo(Text));
            foreach (var candidate in completed.Values) {
                if (candidate.State != selected.State) {
                    Assert.That(candidate.Decoder.TextWinner, Is.Null);
                }
            }
        }
        Assert.That(selected.Text(), Is.EqualTo(Text));
    }
}
