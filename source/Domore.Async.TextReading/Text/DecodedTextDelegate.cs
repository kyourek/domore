using System.Threading;
using System.Threading.Tasks;

namespace Domore.Text;

/// <summary>
/// Handles the progress of a decode.
/// </summary>
/// <param name="decode">
/// The text decoded so far by one candidate encoding. Updates from different candidates may
/// be interleaved; <see cref="DecodedText.State"/> tells them apart.
/// </param>
/// <param name="cancellationToken">The token passed to the decode.</param>
public delegate Task DecodedTextDelegate(DecodedText decode, CancellationToken cancellationToken);
