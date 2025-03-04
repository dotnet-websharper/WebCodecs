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

            encodeResult.Value <- $"Frame encoded! Please check on the console."
            Console.Log("Frame encoded!")
        }

    [<SPAEntryPoint>]
    let Main () =
        IndexTemplate.Main()
            .EncodeResult(encodeResult.V)
            .EncodeFrame(fun _ -> 
                async {
                    do! encodeFrame() |> Promise.AsAsync
                }
                |> Async.StartImmediate
            )
            .Doc()
        |> Doc.RunById "main"
