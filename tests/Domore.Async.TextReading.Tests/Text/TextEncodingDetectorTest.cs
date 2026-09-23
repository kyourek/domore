using NUnit.Framework;
using System.Buffers;
using System.Text;

namespace Domore.Text;

[TestFixture]
public class TextEncodingDetectorTest {
    private TextEncodingDetector Subject;

    private bool TryDetect(byte[] bytes, bool complete, out Encoding encoding, out int preambleLength) {
        return Subject.TryDetect(new ReadOnlySequence<byte>(bytes), complete, out encoding, out preambleLength);
    }

    [SetUp]
    public void SetUp() {
        Subject = new TextEncodingDetector();
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 0xFF })]
    [TestCase(new byte[] { 0xFF, 0xFE })]
    [TestCase(new byte[] { 0xFF, 0xFE, 0x00 })]
    [TestCase(new byte[] { 0xFE })]
    [TestCase(new byte[] { 0xEF })]
    [TestCase(new byte[] { 0xEF, 0xBB })]
    [TestCase(new byte[] { 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00 })]
    [TestCase(new byte[] { 0x00, 0x00, 0xFE })]
    public void TryDetect_ReturnsFalseWhenMoreBytesAreNeeded(byte[] bytes) {
        var detected = TryDetect(bytes, complete: false, out _, out _);
        Assert.That(detected, Is.False);
    }

    [TestCase(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, 12000, 4)]
    [TestCase(new byte[] { 0x00, 0x00, 0xFE, 0xFF }, 12001, 4)]
    [TestCase(new byte[] { 0xEF, 0xBB, 0xBF }, 65001, 3)]
    [TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x61 }, 65001, 3)]
    [TestCase(new byte[] { 0xFE, 0xFF }, 1201, 2)]
    [TestCase(new byte[] { 0xFE, 0xFF, 0x00 }, 1201, 2)]
    [TestCase(new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, 1200, 2)]
    public void TryDetect_DetectsThePreamble(byte[] bytes, int codePage, int preambleLength) {
        var detected = TryDetect(bytes, complete: false, out var encoding, out var length);
        Assert.That(detected, Is.True);
        Assert.That(encoding, Is.Not.Null);
        Assert.That(encoding.CodePage, Is.EqualTo(codePage));
        Assert.That(length, Is.EqualTo(preambleLength));
    }

    [TestCase(new byte[] { 0x61 })]
    [TestCase(new byte[] { 0xFF, 0x61 })]
    [TestCase(new byte[] { 0xFE, 0x61 })]
    [TestCase(new byte[] { 0xEF, 0xBB, 0x61 })]
    [TestCase(new byte[] { 0x00, 0x00, 0xFE, 0x61 })]
    public void TryDetect_DetectsNoPreamble(byte[] bytes) {
        var detected = TryDetect(bytes, complete: false, out var encoding, out var preambleLength);
        Assert.That(detected, Is.True);
        Assert.That(encoding, Is.Null);
        Assert.That(preambleLength, Is.Zero);
    }

    [Test]
    public void TryDetect_DoesNotMistakeAPartialUTF32PreambleForUTF16() {
        var detected = TryDetect([0xFF, 0xFE], complete: false, out _, out _);
        Assert.That(detected, Is.False, "UTF-32 LE starts with the UTF-16 LE preamble, so two bytes are not enough.");
    }

    [TestCase(new byte[] { 0xFF, 0xFE }, 1200, 2)]
    [TestCase(new byte[] { 0xFF, 0xFE, 0x00 }, 1200, 2)]
    public void TryDetect_DetectsUTF16WhenTheStreamEndsBeforeAUTF32PreambleCouldComplete(byte[] bytes, int codePage, int preambleLength) {
        var detected = TryDetect(bytes, complete: true, out var encoding, out var length);
        Assert.That(detected, Is.True);
        Assert.That(encoding, Is.Not.Null);
        Assert.That(encoding.CodePage, Is.EqualTo(codePage));
        Assert.That(length, Is.EqualTo(preambleLength));
    }

    [TestCase(new byte[] { })]
    [TestCase(new byte[] { 0xFF })]
    [TestCase(new byte[] { 0xFE })]
    [TestCase(new byte[] { 0xEF, 0xBB })]
    [TestCase(new byte[] { 0x00, 0x00, 0xFE })]
    public void TryDetect_DetectsNoPreambleWhenTheStreamEndsFirst(byte[] bytes) {
        var detected = TryDetect(bytes, complete: true, out var encoding, out var preambleLength);
        Assert.That(detected, Is.True);
        Assert.That(encoding, Is.Null);
        Assert.That(preambleLength, Is.Zero);
    }
}
