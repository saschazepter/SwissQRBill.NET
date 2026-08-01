//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2021 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using ImageMagick;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VerifyTests;

namespace Codecrete.SwissQRBill.Testing
{
    /// <summary>
    /// Image comparer for Verify, comparing images with ImageMagick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a replacement for <c>VerifyImageMagick.RegisterComparers(...)</c>. It behaves the same
    /// but adds a fast path: if the received data is byte-for-byte identical to the verified data,
    /// the images are equal and the expensive ImageMagick comparison is skipped.
    /// </para>
    /// <para>
    /// The byte comparison succeeds for almost all tests as the generated output is deterministic.
    /// The ImageMagick comparison — in particular with the <see cref="ErrorMetric.PerceptualHash"/>
    /// metric, which takes seconds per A4 sized image — is then only needed for the few images
    /// that genuinely differ, e.g. due to a different font version or platform.
    /// </para>
    /// </remarks>
    public static class ImageComparer
    {
        private static readonly string[] Extensions = { "png", "jpg", "bmp", "tiff", "svg", "pdf" };

        /// <summary>
        /// Registers the image comparer for the common image file extensions.
        /// </summary>
        /// <param name="threshold">Maximum difference (as reported by the error metric) for images to be considered equal.</param>
        /// <param name="metric">Error metric used to quantify the difference between two images.</param>
        public static void RegisterComparers(double threshold, ErrorMetric metric)
        {
            foreach (var extension in Extensions)
            {
                VerifierSettings.RegisterStreamComparer(
                    extension,
                    (received, verified, context) => Compare(received, verified, threshold, metric));
            }
        }

        private static Task<CompareResult> Compare(Stream received, Stream verified, double threshold, ErrorMetric metric)
        {
            if (HaveEqualContent(received, verified))
            {
                return Task.FromResult(CompareResult.Equal);
            }

            received.Position = 0;
            verified.Position = 0;

            double? difference;
            using (var receivedImage = new MagickImage(received))
            using (var verifiedImage = new MagickImage(verified))
            {
                difference = receivedImage.Compare(verifiedImage, metric);
            }

            if (difference <= threshold)
            {
                return Task.FromResult(CompareResult.Equal);
            }

            return Task.FromResult(CompareResult.NotEqual($"diff({difference}) > threshold({threshold})"));
        }

        private static bool HaveEqualContent(Stream stream1, Stream stream2)
        {
            stream1.Position = 0;
            stream2.Position = 0;

            if (stream1.CanSeek && stream2.CanSeek && stream1.Length != stream2.Length)
            {
                return false;
            }

            var buffer1 = new byte[16 * 1024];
            var buffer2 = new byte[16 * 1024];

            while (true)
            {
                var length1 = ReadFully(stream1, buffer1);
                var length2 = ReadFully(stream2, buffer2);

                if (length1 != length2)
                {
                    return false;
                }

                for (var i = 0; i < length1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        return false;
                    }
                }

                if (length1 < buffer1.Length)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Reads from the stream until the buffer is full or the end of the stream is reached.
        /// </summary>
        private static int ReadFully(Stream stream, byte[] buffer)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var length = stream.Read(buffer, offset, buffer.Length - offset);
                if (length == 0)
                {
                    break;
                }

                offset += length;
            }

            return offset;
        }
    }
}
