# WebSharper WebCodecs API Binding

This repository provides an F# [WebSharper](https://websharper.com/) binding for the [WebCodecs API](https://developer.mozilla.org/en-US/docs/Web/API/WebCodecs_API), allowing efficient encoding and decoding of media streams in WebSharper applications.

## Repository Structure

The repository consists of two main projects:

1. **Binding Project**:

   - Contains the F# WebSharper binding for the WebCodecs API.

2. **Sample Project**:
   - Demonstrates how to use the WebCodecs API with WebSharper syntax.
   - Includes a GitHub Pages demo: [View Demo](https://dotnet-websharper.github.io/WebCodecs/).

## Installation

To use this package in your WebSharper project, add the NuGet package:

```bash
   dotnet add package WebSharper.WebCodecs
```

## Building

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Steps

1. Clone the repository:

   ```bash
   git clone https://github.com/dotnet-websharper/WebCodecs.git
   cd WebCodecs
   ```

2. Build the Binding Project:

   ```bash
   dotnet build WebSharper.WebCodecs/WebSharper.WebCodecs.fsproj
   ```

3. Build and Run the Sample Project:

   ```bash
   cd WebSharper.WebCodecs.Sample
   dotnet build
   dotnet run
   ```

4. Open the hosted demo to see the Sample project in action:
   [https://dotnet-websharper.github.io/WebCodecs/](https://dotnet-websharper.github.io/WebCodecs/)

## Example Usage

Below is an example of how to use the WebCodecs API in a WebSharper project:

```fsharp
namespace WebSharper.WebCodecs.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.WebCodecs
open WebSharper.PerformanceAPI

[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let encodeResult = Var.Create ""

    let canvas = JS.Document.GetElementById("canvas") |> As<HTMLCanvasElement>
    let ctx = canvas.GetContext("2d") |> As<CanvasRenderingContext2D>

    // Draw a red rectangle as a test frame
    ctx.FillStyle <- "red"
    ctx.FillRect(100., 100., 200., 100.)

    // Create a VideoEncoder
    let videoEncoder =
        new VideoEncoder(
            VideoEncoderInit(
                output = (fun chunk metadata ->
                    Console.Log("Encoded Chunk:", chunk)
                    Console.Log("Metadata:", metadata)),
                error = fun (err) -> Console.Error("Encoding error:", err?message)
            )
        )

    // Configure the encoder
    videoEncoder.Configure(
        VideoEncoderConfig(
            codec = "vp8",  // Options: "vp9", "av1", "h264"
            Width = 640,
            Height = 360,
            Bitrate = 500000  // Adjust bitrate for quality
        )
    )

    let encodeFrame() =
        promise {
            let performance = obj
            let frame = new VideoFrame(canvas, VideoFrameInit(Timestamp = JS.Window.Performance.Now()))

            // Encode the frame
            videoEncoder.Encode(frame)
            frame.Close() // Free memory

            encodeResult.Value <- "Frame encoded! Please check on the console."
            Console.Log("Frame encoded!")
        }

    [<SPAEntryPoint>]
    let Main () =
        IndexTemplate.Main()
            // Display encoding result message in the UI
            .EncodeResult(encodeResult.V)
            // Handle encoding frame button click
            .EncodeFrame(fun _ ->
                async {
                    do! encodeFrame() |> Promise.AsAsync
                }
                |> Async.StartImmediate
            )
            .Doc()
        |> Doc.RunById "main"
```

This example demonstrates how to encode video frames using the WebCodecs API in WebSharper.

## Important Considerations

- **Browser Compatibility**: Ensure your target browsers support the WebCodecs API; check [MDN WebCodecs API](https://developer.mozilla.org/en-US/docs/Web/API/WebCodecs_API) for details.
- **Performance**: Encoding can be CPU-intensive; consider optimizing frame rates and bitrates.
- **Use Cases**: The WebCodecs API is designed for real-time video processing, media streaming, and custom video pipelines.
