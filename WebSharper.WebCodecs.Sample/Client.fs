namespace WebSharper.WebCodecs.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Notation
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.WebCodecs

[<JavaScript>]
module Client =
    // The template is loaded from the DOM, so editing index.html and refreshing
    // the browser is enough; a rebuild is only needed when holes change.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let status = Var.Create "Ready."
    let result = Var.Create ""
    let support = Var.Create ""

    let codec = "vp8"
    let width = 640
    let height = 360

    // Probe the global constructor without pulling in an external dependency.
    let videoEncoderConstructor: obj = JS.Global?VideoEncoder
    let webCodecsAvailable = not (videoEncoderConstructor ===. JS.Undefined)

    let videoEncoderConfig () =
        VideoEncoderConfig(
            codec = codec,
            Width = width,
            Height = height,
            Bitrate = 500000
        )

    // Running log of the EncodedVideoChunks produced by the encoder's output callback.
    let mutable chunkLog = ""

    let videoEncoderInit () =
        VideoEncoderInit(
            output =
                (fun chunk _metadata ->
                    // Copy the encoded chunk into a typed array to inspect its shape.
                    let bytes = Uint8Array(chunk.ByteLength)
                    chunk.CopyTo(bytes)
                    let line =
                        sprintf "Encoded a %s chunk of %d bytes (timestamp %d µs); the copy is %d bytes long."
                            chunk.Type chunk.ByteLength chunk.Timestamp bytes.Length
                    chunkLog <- (if chunkLog = "" then line else chunkLog + "\n" + line)),
            error = fun err -> status := "Encoder error: " + string (err?message)
        )

    // Draws a simple two-tone test frame on the canvas.
    let drawFrame (ctx: CanvasRenderingContext2D) =
        ctx.FillStyle <- "#0f1420"
        ctx.FillRect(0., 0., float width, float height)
        ctx.FillStyle <- "#7c74f5"
        ctx.FillRect(40., 60., 240., 120.)
        ctx.FillStyle <- "#fbbf24"
        ctx.FillRect(320., 140., 240., 120.)

    let refreshSupport () =
        if not webCodecsAvailable then
            support := "WebCodecs is not available in this browser."
        else
            promise {
                let! configSupport = VideoEncoder.IsConfigSupported(videoEncoderConfig ())
                if configSupport.Supported then
                    support := sprintf "VideoEncoder supports the %s configuration." codec
                else
                    support := sprintf "VideoEncoder is present, but the %s configuration is not supported." codec
            }
            |> ignore

    let encodeFrame () =
        if not webCodecsAvailable then
            status := "WebCodecs (VideoEncoder) is not available in this browser."
        else
            promise {
                status := "Encoding..."
                result := ""
                chunkLog <- ""

                let config = videoEncoderConfig ()
                let! configSupport = VideoEncoder.IsConfigSupported(config)

                if not configSupport.Supported then
                    status := sprintf "The %s encoder configuration is not supported." codec
                else
                    // The canvas is only touched inside the click handler.
                    let canvas = JS.Document.GetElementById("canvas") |> As<HTMLCanvasElement>
                    let ctx = canvas.GetContext("2d") |> As<CanvasRenderingContext2D>
                    drawFrame ctx

                    let encoder = new VideoEncoder(videoEncoderInit ())
                    encoder.Configure(config)

                    // Timestamps are expressed in microseconds.
                    let timestamp = float (Date.Now() * 1000)
                    let frame = new VideoFrame(canvas, VideoFrameInit(Timestamp = timestamp))

                    // Clone the frame, encode the clone, then the original.
                    let clone = frame.Clone()
                    encoder.Encode(clone)
                    clone.Close()
                    encoder.Encode(frame)
                    frame.Close()

                    do! encoder.Flush()
                    encoder.Close()

                    result := chunkLog
                    status := "Frame encoded."
            }
            |> ignore

    [<SPAEntryPoint>]
    let Main () =
        refreshSupport ()

        IndexTemplate.Main()
            .encodeFrame(fun _ -> encodeFrame ())
            .status(status.V)
            .result(result.V)
            .support(support.V)
            .Doc()
        |> Doc.RunById "main"
