namespace FluentRibbon.Uno.Showcase;

using System;
using System.IO;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

/// <summary>
/// Regression coverage for the Quick Access Toolbar clone of <see cref="InRibbonGallery"/>. The
/// showcase pins <c>GalInRibbon</c> into the QAT; its collapsed clone must render as a clean,
/// compact icon+chevron gallery button (matching the other QAT items), not the thin clipped
/// vertical sliver produced when the clone was clamped to 22x22 while its vertical collapsed
/// content wanted ~28px. Set <c>SHOWCASE_QAT_SHOT=path</c> to also save a PNG crop of the QAT.
/// </summary>
public sealed partial class MainPage
{
    private async Task VerifyQatGalleryCloneAsync()
    {
        AutoLog("QATGAL BEGIN");
        try
        {
            await SettleAsync(4, 120);

            QuickAccessToolBar? qat = null;
            foreach (var found in EnumerateDescendants<QuickAccessToolBar>(this))
            {
                qat = found;
                break;
            }

            if (qat is null)
            {
                AutoLog("  FAIL QATGAL QuickAccessToolBar not found");
                AutoLog("QATGAL END");
                return;
            }

            InRibbonGallery? clone = null;
            foreach (var gallery in EnumerateDescendants<InRibbonGallery>(qat))
            {
                clone = gallery;
                break;
            }

            if (clone is null)
            {
                AutoLog("  FAIL QATGAL gallery clone not found in QAT");
                AutoLog("QATGAL END");
                return;
            }

            var width = clone.ActualWidth;
            var height = clone.ActualHeight;
            var pos = clone.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));

            var button = FindByName(clone, "CollapsedContent");
            var buttonWidth = button?.ActualWidth ?? 0;
            var buttonHeight = button?.ActualHeight ?? 0;

            AutoLog(
                $"  clone={width:F1}x{height:F1} at ({pos.X:F1},{pos.Y:F1}); collapsedButton={buttonWidth:F1}x{buttonHeight:F1}; isCollapsed={clone.IsCollapsed}");

            // A healthy compact gallery button is roughly as wide as the other QAT items (~28px)
            // and ~24px tall so the icon and chevron are legible. The old bug clamped it to 22x22
            // with ~28px of vertical content clipped into it (a thin sliver: height >> width-usable).
            if (height is >= 20 and <= 34 && width is >= 24 and <= 72)
            {
                AutoLog($"  QATGAL OK (clone {width:F1}x{height:F1} within compact-button bounds)");
            }
            else
            {
                AutoLog(
                    $"  FAIL QATGAL clone {width:F1}x{height:F1} outside compact-button bounds (w:24..72, h:20..34)");
            }

            var shotPath = Environment.GetEnvironmentVariable("SHOWCASE_QAT_SHOT");
            if (!string.IsNullOrEmpty(shotPath))
            {
                try
                {
                    await CaptureElementPngAsync(qat, shotPath);
                    AutoLog($"  QATGAL screenshot saved: {shotPath}");
                }
                catch (Exception shotEx)
                {
                    AutoLog($"  QATGAL screenshot failed: {shotEx.GetType().Name}: {shotEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            AutoLog($"  QATGAL THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("QATGAL END");
    }

    private static async Task CaptureElementPngAsync(FrameworkElement element, string path)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(element);
        var pixels = await bitmap.GetPixelsAsync();

        var pixelBytes = new byte[pixels.Length];
        using (var pixelReader = DataReader.FromBuffer(pixels))
        {
            pixelReader.ReadBytes(pixelBytes);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            96,
            96,
            pixelBytes);
        await encoder.FlushAsync();

        stream.Seek(0);
        var bytes = new byte[stream.Size];
        using (var reader = new DataReader(stream))
        {
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
        }

        File.WriteAllBytes(path, bytes);
    }
}
