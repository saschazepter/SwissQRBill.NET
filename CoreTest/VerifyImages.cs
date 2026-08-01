//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2021 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.SwissQRBill.Generator;
using Codecrete.SwissQRBill.Testing;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using ImageMagick;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VerifyTests;
using VerifyXunit;

namespace Codecrete.SwissQRBill.CoreTest
{
    public class VerifyImages
    {
        protected static readonly VerifySettings Settings = new VerifySettings();

        static VerifyImages()
        {
            VerifierSettings.RegisterStreamConverter("pdf", ConvertPdfToPng);
            ImageComparer.RegisterComparers(threshold: 0.05, ErrorMetric.PerceptualHash);

            Settings.UseDirectory("ReferenceFiles");
        }

        protected VerifyImages() { }

        private static ConversionResult ConvertPdfToPng(string name, Stream stream, IReadOnlyDictionary<string, object> context)
        {
            var pngStreams = new List<Stream>();

            using (var docReader = DocLib.Instance.GetDocReader(stream.ToArray(), new PageDimensions(scalingFactor: 2)))
            {
                for (var pageIndex = 0; pageIndex < docReader.GetPageCount(); pageIndex++)
                {
                    SKBitmap bitmap;
                    using (var pageReader = docReader.GetPageReader(pageIndex))
                    {
                        var width = pageReader.GetPageWidth();
                        var height = pageReader.GetPageHeight();
                        // The page is rendered onto a white background. Without it, the page content would
                        // end up in the alpha channel only (black pixels throughout, varying transparency).
                        // ImageMagick's error metrics ignore the alpha channel, and the image comparison
                        // would then consider two entirely different QR bills as equal.
                        var pixelData = pageReader.GetImage(new NaiveTransparencyRemover());

                        bitmap = new SKBitmap();
                        var gcHandle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                        var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Opaque);
                        bitmap.InstallPixels(info, gcHandle.AddrOfPinnedObject(), width * 4, delegate { gcHandle.Free(); });
                    }

                    using (var skiaImage = SKImage.FromBitmap(bitmap))
                    using (var data = skiaImage.Encode(SKEncodedImageFormat.Png, 90))
                    {
                        pngStreams.Add(new MemoryStream(data.ToArray()));
                    }
                }
            }

            return new ConversionResult(null, pngStreams.Select(e => new Target("png", e, null, false)));
        }

        public static SettingsTask Verify(byte[] imageData, GraphicsFormat format, [CallerFilePath] string sourceFile = "")
        {
            var extension = format.ToString().ToLowerInvariant();
            return Verifier.Verify(imageData, settings: Settings, extension: extension, sourceFile: sourceFile);
        }

        public static SettingsTask VerifySvg(byte[] svg, [CallerFilePath] string sourceFile = "")
        {
            return Verifier.Verify(svg, settings: Settings, extension: "svg", sourceFile: sourceFile);
        }

        public static SettingsTask VerifyPdf(byte[] pdf, [CallerFilePath] string sourceFile = "")
        {
            return Verifier.Verify(pdf, settings: Settings, extension: "pdf", sourceFile: sourceFile);
        }
    }
}