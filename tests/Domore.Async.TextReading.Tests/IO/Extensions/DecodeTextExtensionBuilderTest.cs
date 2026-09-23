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
public class DecodeTextExtensionBuilderTest {
    private class Builder : DecodedTextBuilder {
        private readonly StringBuilder StringBuilder = new();

        protected override Task Add(ReadOnlyMemory<char> memory, CancellationToken cancellationToken) {
            StringBuilder.Append(new string(memory.Span));
            return Task.CompletedTask;
        }

        protected override Task Clear(CancellationToken cancellationToken) {
            StringBuilder.Clear();
            return Task.CompletedTask;
        }

        public string Text =>
            StringBuilder.ToString();
    }

    private static readonly string Text = string.Concat(
        System.Linq.Enumerable.Repeat("h\u00e9llo \u4e2d\u6587 line\r\n", 300));

    private string File;

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
    public async Task DecodeText_BuildsTheWholeTextWhileDecodingIsInProgress() {
        for (var i = 0; i < 1000; i++) {
            var options = new DecodedTextOptions {
                Encoding = new List<string> { "utf-8" }
            };
            options.StreamBuffer.Size = 1 + (i % 7);
            options.TextBuffer.Size = 1 + (i % 5);
            using (options.Disposable()) {
                var builder = new Builder();
                var decoded = await new FileInfo(File).DecodeText(builder, options, CancellationToken.None);
                Assert.That(decoded, Is.Not.Null, $"Iteration {i} produced no result.");
                Assert.That(decoded.Success, Is.True, $"Iteration {i} did not succeed.");
                Assert.That(decoded.TextLength, Is.EqualTo(Text.Length), $"Iteration {i} decoded the wrong length.");
                Assert.That(decoded.Text(), Is.EqualTo(Text), $"Iteration {i} decoded the wrong text.");
                Assert.That(builder.Text, Is.EqualTo(Text), $"Iteration {i} built the wrong text.");
            }
        }
    }
}

