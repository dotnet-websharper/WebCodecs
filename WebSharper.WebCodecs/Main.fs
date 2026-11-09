namespace WebSharper.WebCodecs

open WebSharper
open WebSharper.JavaScript
open WebSharper.JavaScript.Dom
open WebSharper.InterfaceGenerator

#nowarn "3886" // single-element Required/Optional lists are intentional

module Definition =

    let TypedArray = T<Int8Array> + T<Uint8Array> + T<Uint8ClampedArray> + T<Int16Array> + T<Uint16Array>
                        + T<Int32Array> + T<Uint32Array> + T<Float64Array> + T<Float32Array>

    let BufferData = T<ArrayBuffer> + TypedArray + T<DataView>

    let AudioDataInit =
        Pattern.Config "AudioDataInit" {
            Required = [
                "format", T<string> // Possible values: "u8", "s16", "s32", "f32", etc.
                "sampleRate", T<float> // Sample rate in Hz
                "numberOfFrames", T<int> // Number of frames in the sample
                "numberOfChannels", T<int> // Number of channels
                "timestamp", T<int> // Time in microseconds
                "data", TypedArray // Audio sample data
            ]
            Optional = [
                "transfer", !| T<ArrayBuffer> // Array of ArrayBuffers to transfer ownership
            ]
        }

    let AllocationSizeOptions =
        Pattern.Config "AllocationSizeOptions" {
            Required = [
                "planeIndex", T<int> // Index of the plane to return the size of
            ]
            Optional = [
                "frameOffset", T<int> // Offset into the plane data (default: 0)
                "frameCount", T<int> // Number of frames to return the size of (default: all)
            ]
        }

    let CopyToOptions =
        Pattern.Config "CopyToOptions" {
            Required = [
                "planeIndex", T<int> // Index of the plane to copy from
            ]
            Optional = [
                "frameOffset", T<int> // Offset into the plane data (default: 0)
                "frameCount", T<int> // Number of frames to copy (default: all)
            ]
        }

    let AudioData =
        Class "AudioData"
        |+> Static [
            Constructor AudioDataInit?init
        ]
        |+> Instance [
            "format" =? T<string> // Read-only, sample format of the audio
            "sampleRate" =? T<float> // Read-only, sample rate in Hz
            "numberOfFrames" =? T<int> // Read-only, number of frames
            "numberOfChannels" =? T<int> // Read-only, number of channels
            "duration" =? T<int> // Read-only, duration in microseconds
            "timestamp" =? T<int> // Read-only, timestamp in microseconds

            "allocationSize" => AllocationSizeOptions?options ^-> T<int> // Returns required memory size
            "copyTo" => BufferData?destination * CopyToOptions?options ^-> T<unit> // Copies sample data
            "clone" => T<unit> ^-> TSelf // Creates a new instance with the same media resource
            "close" => T<unit> ^-> T<unit> // Releases media resource reference
        ]

    let AudioDecoderConfig =
        Pattern.Config "AudioDecoderConfig" {
            Required = [
                "codec", T<string> // Codec identifier
            ]
            Optional = [
                "sampleRate", T<int> // Sample rate in Hz
                "numberOfChannels", T<int> // Number of audio channels
                "description", BufferData
            ]
        }

    let AudioDecoderInit =
        Pattern.Config "AudioDecoderInit" {
            Required = [
                "output", AudioData?callback ^-> T<unit> // Function to handle output frames (callback)
                "error", T<obj>?erro ^-> T<unit> // Function to handle errors (callback)
            ]
            Optional = []
        }

    let ConfigSupportedResult =
        Pattern.Config "ConfigSupportedResult" {
            Required = [
                "supported", T<bool> // Function to handle output frames (callback)
                "config", T<obj> // Function to handle errors (callback)
            ]
            Optional = []
        }

    let EncodedAudioChunkInit =
        Pattern.Config "EncodedAudioChunkInit" {
            Required = [
                "type", T<string> // Indicates if this chunk is a key chunk
                "timestamp", T<int> // Timestamp in microseconds
                "data", TypedArray // Encoded audio data
            ]
            Optional = [
                "duration", T<int> // Duration in microseconds
                "transfer", !|T<ArrayBuffer> // Array of ArrayBuffers to transfer ownership
            ]
        }

    let EncodedAudioChunk =
        Class "EncodedAudioChunk"
        |+> Static [
            Constructor EncodedAudioChunkInit?options // Constructor with required parameters
        ]
        |+> Instance [
            "type" =? T<string> // Read-only: Key chunk type
            "timestamp" =? T<int> // Read-only: Timestamp in microseconds
            "duration" =? T<int> // Read-only: Duration in microseconds
            "byteLength" =? T<int> // Read-only: Length in bytes

            "copyTo" => BufferData?destination ^-> T<unit> // Copies encoded audio data
        ]

    let EncodedVideoChunkInit =
        Pattern.Config "EncodedVideoChunkInit" {
            Required = [
                "type", T<string> // Indicates if this chunk is a key chunk
                "timestamp", T<int> // Timestamp in microseconds
                "data", TypedArray // Encoded video data
            ]
            Optional = [
                "duration", T<int> // Duration in microseconds
                "transfer", !|T<ArrayBuffer> // Array of ArrayBuffers to transfer ownership
            ]
        }

    let EncodedVideoChunk =
        Class "EncodedVideoChunk"
        |+> Static [
            Constructor EncodedVideoChunkInit?options // Constructor with required parameters
        ]
        |+> Instance [
            "type" =? T<string> // Read-only: Key chunk type
            "timestamp" =? T<int> // Read-only: Timestamp in microseconds
            "duration" =? T<int> // Read-only: Duration in microseconds
            "byteLength" =? T<int> // Read-only: Length in bytes

            "copyTo" => BufferData?destination ^-> T<unit>
        ]

    let AudioDecoder =
        Class "AudioDecoder"
        |=> Inherits T<EventTarget> // Inherits from EventTarget
        |+> Static [
            Constructor AudioDecoderInit?init // Constructor with init options
            "isConfigSupported" => AudioDecoderConfig?config ^-> T<Promise<_>>[ConfigSupportedResult] // Checks if config is supported
        ]
        |+> Instance [
            "decodeQueueSize" =? T<int> // Read-only property for queue size
            "state" =? T<string> // Read-only state of the decoder

            "configure" => AudioDecoderConfig?config ^-> T<unit> // Configures the decoder
            "decode" => EncodedAudioChunk?chunk ^-> T<unit> // Enqueues a decode request
            "flush" => T<unit> ^-> T<Promise<unit>> // Flushes the queue
            "reset" => T<unit> ^-> T<unit> // Resets the decoder
            "close" => T<unit> ^-> T<unit> // Closes the decoder and releases resources

            "ondequeue" =@ T<unit> ^-> T<unit>
            |> ObsoleteWithMessage "Use OnDequeue instead"
            "ondequeue" =@ T<Event> ^-> T<unit>
            |> WithSourceName "OnDequeue"
        ]

    let OpusEncoderConfig =
        Pattern.Config "OpusEncoderConfig" {
            Required = []
            Optional = [
                "application", T<string> // "audio" (default), "lowdelay", "voip"
                "complexity", T<int> // 0 to 10, higher = more computationally expensive
                "format", T<string> // "opus" (default) or "ogg"
                "frameDuration", T<int> // Frame duration in microseconds (default 20000)
                "packetlossperc", T<int> // Expected packet loss percentage (0-100, default 0)
                "signal", T<string> // "auto" (default), "music", "voice"
                "usedtx", T<bool> // Enable Discontinuous Transmission (DTX) (default false)
                "useinbandfec", T<bool> // Enable in-band Forward Error Correction (FEC) (default false)
            ]
        }

    let AudioEncoderConfig =
        Pattern.Config "AudioEncoderConfig" {
            Required = [
                "codec", T<string> // Codec identifier
                "sampleRate", T<int> // Sample rate in Hz
                "numberOfChannels", T<int> // Number of audio channels
            ]
            Optional = [
                "bitrate", T<int> // Encoding bitrate
                "bitrateMode", T<string> // "constant" or "variable" (default)
                "opus", OpusEncoderConfig.Type // Optional Opus-specific encoder configuration
            ]
        }

    let EncodedAudioChunkMetadata =
        Pattern.Config "EncodedAudioChunkMetadata" {
            Required = []
            Optional = [
                "decoderConfig", AudioDecoderConfig.Type // Decoder configuration
            ]
        }

    let AudioEncoderInit =
        Pattern.Config "AudioEncoderInit" {
            Required = [
                "output", EncodedAudioChunk?firstArg * !?EncodedAudioChunkMetadata?secondArg ^-> T<unit>  // Function to handle output chunks (callback)
                "error", T<obj>?error ^-> T<unit> // Function to handle errors (callback)
            ]
            Optional = []
        }

    let AudioEncoder =
        Class "AudioEncoder"
        |=> Inherits T<EventTarget> // Inherits from EventTarget
        |+> Static [
            Constructor AudioEncoderInit?init // Constructor with init options
            "isConfigSupported" => AudioEncoderConfig?config ^-> T<Promise<_>>[ConfigSupportedResult] // Checks if config is supported
        ]
        |+> Instance [
            "encodeQueueSize" =? T<int> // Read-only property for queue size
            "state" =? T<string> // Read-only state of the encoder

            "configure" => AudioEncoderConfig?config ^-> T<unit> // Configures the encoder
            "encode" => AudioData?data ^-> T<unit> // Enqueues an AudioData encoding request
            "flush" => T<unit> ^-> T<Promise<unit>> // Flushes the queue
            "reset" => T<unit> ^-> T<unit> // Resets the encoder
            "close" => T<unit> ^-> T<unit> // Closes the encoder and releases resources

            "ondequeue" =@ T<unit> ^-> T<unit>
            |> ObsoleteWithMessage "Use OnDequeue instead"
            "ondequeue" =@ T<Event> ^-> T<unit>
            |> WithSourceName "OnDequeue"
        ]

    let ImageDecoderInit =
        Pattern.Config "ImageDecoderInit" {
            Required = [
                "type", T<string> // MIME type of the image
                "data", BufferData + T<ReadableStream> // Encoded image data (ReadableStream as obj)
            ]
            Optional = [
                "premultiplyAlpha", T<string> // "none", "premultiply", "default"
                "colorSpaceConversion", T<string> // "none", "default"
                "desiredWidth", T<int> // Optional desired output width
                "desiredHeight", T<int> // Optional desired output height
                "preferAnimation", T<bool> // Whether to prefer an animated track
                "transfer", !| T<ArrayBuffer> // Array of ArrayBuffers to transfer ownership
            ]
        }

    let DecodeOptions =
        Pattern.Config "DecodeOptions" {
            Required = []
            Optional = [
                "frameIndex", T<int> // Frame index to decode (default: 0)
                "completeFramesOnly", T<bool> // If false, allows progressive decoding (default: true)
            ]
        }

    let ImageTrack =
        Class "ImageTrack"
        |+> Instance [
            "animated" =? T<bool> // Read-only: Indicates if the track has multiple frames (is animated)
            "frameCount" =? T<int> // Read-only: Number of frames in the track
            "repetitionCount" =? T<int> // Read-only: Number of times the animation repeats
            "selected" =? T<bool> // Read-only: Indicates if this track is selected for decoding
        ]

    let ImageTrackList =
        Class "ImageTrackList"
        |+> Instance [
            "ready" =? T<Promise<unit>> // Read-only: Resolves once tracks are populated
            "length" =? T<int> // Read-only: Number of tracks in the list
            "selectedIndex" =? T<int> // Read-only: Index of the selected track
            "selectedTrack" =? ImageTrack // Read-only: The currently selected ImageTrack (would need a separate binding)
        ]

    let VisibleRect =
        Pattern.Config "VisibleRect" {
            Required = [
                "x", T<int>
                "y", T<int>
                "width", T<int>
                "height", T<int>
            ]
            Optional = []
        }

    let LayoutEntry =
        Pattern.Config "LayoutEntry" {
            Required = [
                "offset", T<int> // Offset in bytes where the plane begins
                "stride", T<int> // Number of bytes per row including padding
            ]
            Optional = []
        }

    let VideoCopyToOptions =
        Pattern.Config "VideoCopyToOptions" {
            Required = []
            Optional = [
                "rect", VisibleRect.Type // DOMRectReadOnly (would need separate binding)
                "layout", !| LayoutEntry // Plane layout description (requires separate binding)
                "format", T<string>
                "colorSpace", T<string>
            ]
        }

    let VideoAllocationSizeOptions =
        Class "VideoAllocationSizeOptions"
        |=> Inherits VideoCopyToOptions

    let ColorSpace =
        Pattern.Config "ColorSpace" {
            Required = []
            Optional = [
                "primaries", T<string> // Video color primaries
                "transfer", T<string> // Video color transfer function
                "matrix", T<string> // Video color matrix
                "fullRange", T<bool> // Whether full-range color values are used
            ]
        }

    let VideoFrameInit =
        Pattern.Config "VideoFrameInit" {
            Required = []
            Optional = [
                "duration", T<int> // Duration of the frame in microseconds
                "timestamp", T<float> // Timestamp in microseconds
                "alpha", T<string> // "keep" (default) or "discard"
                "visibleRect", VisibleRect.Type // The visible rectangle of the frame
                "displayWidth", T<int> // Display width after aspect ratio adjustments
                "displayHeight", T<int> // Display height after aspect ratio adjustments
            ]
        }

    let VideoFrameDataInit =
        Pattern.Config "VideoFrameDataInit" {
            Required = [
                "format", T<string> // Pixel format: "I420", "RGBA", "BGRA", etc.
                "codedWidth", T<int> // Width including padding
                "codedHeight", T<int> // Height including padding
                "timestamp", T<int> // Timestamp in microseconds
            ]
            Optional = [
                "duration", T<int> // Duration of the frame in microseconds
                "layout", !| LayoutEntry // Plane layout description
                "visibleRect", VisibleRect.Type // The visible rectangle of the frame
                "displayWidth", T<int> // Display width after aspect ratio adjustments
                "displayHeight", T<int> // Display height after aspect ratio adjustments
                "colorSpace", ColorSpace.Type // Color space settings
                "transfer", !| T<ArrayBuffer> // Array of ArrayBuffers to transfer ownership
            ]
        }

    let VideoColorSpaceInit =
        Pattern.Config "VideoColorSpaceInit" {
            Required = []
            Optional = [
                "primaries", T<string> // Color gamut description
                "transfer", T<string> // Transfer characteristics of video samples
                "matrix", T<string> // Matrix coefficients for color transformation
                "fullRange", T<bool> // If true, full-range color values are used
            ]
        }

    let VideoColorSpace =
        Class "VideoColorSpace"
        |+> Static [
            Constructor !?VideoColorSpaceInit?options // Constructor with optional parameters
        ]
        |+> Instance [
            "primaries" =? T<string> // Read-only: Color gamut
            "transfer" =? T<string> // Read-only: Transfer characteristics
            "matrix" =? T<string> // Read-only: Matrix coefficients
            "fullRange" =? T<bool> // Read-only: Indicates if full-range color values are used
        ]
        |+> Instance [
            "toJSON" => T<unit> ^-> T<obj> // Returns JSON representation of VideoColorSpace
        ]

    let VideoFrame =
        Class "VideoFrame"
        |+> Static [
            Constructor (T<obj>?image * !?VideoFrameInit?options) // Accepts a canvas source (image, video, etc.)
            Constructor (BufferData?data * !?VideoFrameDataInit?options) // Accepts binary pixel data
        ]
        |+> Instance [
            "format" =? T<string> // Read-only: Pixel format
            "codedWidth" =? T<int> // Read-only: Width including padding
            "codedHeight" =? T<int> // Read-only: Height including padding
            "codedRect" =? T<DOMRectReadOnly> // Read-only: DOMRectReadOnly (would need separate binding)
            "visibleRect" =? T<DOMRectReadOnly> // Read-only: DOMRectReadOnly for visible area
            "displayWidth" =? T<int> // Read-only: Adjusted display width
            "displayHeight" =? T<int> // Read-only: Adjusted display height
            "duration" =? T<int> // Read-only: Duration in microseconds
            "timestamp" =? T<int> // Read-only: Timestamp in microseconds
            "colorSpace" =? VideoColorSpace // Read-only: VideoColorSpace (would need separate binding)

            "allocationSize" => !?VideoAllocationSizeOptions?options ^-> T<int> // Returns required memory size
            "copyTo" => BufferData?destination * !?VideoCopyToOptions?options ^-> T<unit> // Copies frame data
            "clone" => T<unit> ^-> TSelf // Creates a new instance with the same media resource
            "close" => T<unit> ^-> T<unit> // Releases media resource reference
        ]

    let DecodeResult =
        Pattern.Config "DecodeResult" {
            Required = [
                "image", VideoFrame.Type // VideoFrame (would need a separate binding)
                "complete", T<bool> // Indicates if this is the final full-detail output
            ]
            Optional = []
        }

    let ImageDecoder =
        Class "ImageDecoder"
        |+> Static [
            Constructor ImageDecoderInit?init // Constructor with required parameters
            "isTypeSupported" => T<string>?``type`` ^-> T<Promise<bool>> // Checks if a MIME type is supported
        ]
        |+> Instance [
            "complete" =? T<bool> // Read-only: Indicates if encoded data is completely buffered
            "completed" =? T<Promise<unit>> // Read-only: Resolves once `complete` is true
            "tracks" =? ImageTrackList // Read-only: ImageTrackList (would need a separate binding)
            "type" =? T<string> // Read-only: MIME type of the image

            "decode" => !?DecodeOptions?options ^-> T<Promise<_>>[DecodeResult] // Decodes a frame from an image
            "close" => T<unit> ^-> T<unit> // Closes the decoder and releases resources
            "reset" => T<unit> ^-> T<unit> // Aborts all pending decode operations
        ]

    let VideoDecoderConfig =
        Pattern.Config "VideoDecoderConfig" {
            Required = [
                "codec", T<string> // Codec identifier
            ]
            Optional = [
                "description", BufferData
                "codedWidth", T<int> // Width of the video frame
                "codedHeight", T<int> // Height of the video frame
                "displayAspectWidth", T<int> // Width after aspect ratio adjustments
                "displayAspectHeight", T<int> // Height after aspect ratio adjustments
                "hardwareAcceleration", T<string> // "no-preference", "prefer-hardware", "prefer-software"
                "optimizeForLatency", T<bool> // Whether to optimize for low latency
                "colorSpace", ColorSpace.Type // VideoColorSpace configuration (would need separate binding)
            ]
        }

    let VideoDecoderInit =
        Pattern.Config "VideoDecoderInit" {
            Required = [
                "output", VideoFrame?arg ^-> T<unit> // Function to handle output frames (callback)
                "error", T<obj>?arg ^-> T<unit> // Function to handle errors (callback)
            ]
            Optional = []
        }

    let VideoDecoder =
        Class "VideoDecoder"
        |=> Inherits T<EventTarget> // Inherits from EventTarget
        |+> Static [
            Constructor VideoDecoderInit?init // Constructor with initialization options
            "isConfigSupported" => VideoDecoderConfig?config ^-> T<Promise<_>>[ConfigSupportedResult] // Checks if the configuration is supported
        ]
        |+> Instance [
            "decodeQueueSize" =? T<int> // Read-only: Number of queued decode requests
            "state" =? T<string> // Read-only: Decoder state ("unconfigured", "configured", "closed")

            "configure" => VideoDecoderConfig?config ^-> T<unit> // Configures the decoder
            "decode" => EncodedVideoChunk?chunk ^-> T<unit> // Enqueues a video decoding request
            "flush" => T<unit> ^-> T<Promise<unit>> // Flushes the queue
            "reset" => T<unit> ^-> T<unit> // Resets the decoder
            "close" => T<unit> ^-> T<unit> // Closes the decoder and releases resources

            "ondequeue" =@ T<unit> ^-> T<unit>
            |> ObsoleteWithMessage "Use OnDequeue instead"
            "ondequeue" =@ T<Event> ^-> T<unit>
            |> WithSourceName "OnDequeue"
        ]

    let EncodedVideoChunkMetadata =
        Pattern.Config "EncodedVideoChunkMetadata" {
            Required = []
            Optional = [
                "decoderConfig", VideoDecoderConfig.Type // Decoder configuration (needs a separate binding)
                "alphaSideData", BufferData // Extra alpha channel data
                "svc", T<obj> // Temporal layer ID for SVC
            ]
        }

    let VideoEncoderInit =
        Pattern.Config "VideoEncoderInit" {
            Required = [
                "output", (EncodedVideoChunk?chunk * !?EncodedVideoChunkMetadata?metadata) ^-> T<unit> // Output callback
                "error", T<obj>?error ^-> T<unit> // Error callback
            ]
            Optional = []
        }

    let VideoEncoderConfig =
        Pattern.Config "VideoEncoderConfig" {
            Required = [
                "codec", T<string> // Codec identifier
            ]
            Optional = [
                "width", T<int> // Encoded frame width
                "height", T<int>
                "displayWidth", T<int> // Encoded frame width
                "displayHeight", T<int> // Encoded frame height
                "bitrate", T<int> // Bitrate of the encoded video
                "framerate", T<float> // Framerate of the encoded video
                "hardwareAcceleration", T<string> // "no-preference", "prefer-hardware", "prefer-software"
                "alpha", T<string> // Alpha channel mode: "keep", "discard"
                "latencyMode", T<string> // Encoding latency mode: "quality", "realtime"
                "scalabilityMode", T<string>
                "bitrateMode", T<string> // Video scalability mode (optional)
            ]
        }

    let CodecQuantizer =
        Pattern.Config "CodecQuantizer" {
            Required = []
            Optional = [
                "quantizer", T<int> // Frame quantizer value
            ]
        }

    let VideoEncoderEncodeOptions =
        Pattern.Config "VideoEncoderEncodeOptions" {
            Required = []
            Optional = [
                "keyFrame", T<bool> // Force encoding as a key frame (default: false)
                "vp9", CodecQuantizer.Type // VP9-specific encoding options
                "av1", CodecQuantizer.Type // AV1-specific encoding options
                "avc", CodecQuantizer.Type // AVC (H.264) encoding options
                "hevc", CodecQuantizer.Type // HEVC (H.265) encoding options
            ]
        }

    let VideoEncoder =
        Class "VideoEncoder"
        |=> Inherits T<EventTarget> // Inherits from EventTarget
        |+> Static [
            Constructor VideoEncoderInit?init // Constructor with init options
            "isConfigSupported" => VideoEncoderConfig?config ^-> T<Promise<_>>[ConfigSupportedResult] // Checks if config is supported
        ]
        |+> Instance [
            "encodeQueueSize" =? T<int> // Read-only: Number of queued encode requests
            "state" =? T<string> // Read-only: Encoder state

            "configure" => VideoEncoderConfig?config ^-> T<unit> // Configures the encoder
            "encode" => VideoFrame?frame * !?VideoEncoderEncodeOptions?options ^-> T<unit> // Enqueues a frame for encoding
            "flush" => T<unit> ^-> T<Promise<unit>> // Flushes all pending encodes
            "reset" => T<unit> ^-> T<unit> // Resets the encoder, clearing all pending operations
            "close" => T<unit> ^-> T<unit> // Closes the encoder and releases resources

            "ondequeue" =@ T<unit> ^-> T<unit>
            |> ObsoleteWithMessage "Use OnDequeue instead"
            "ondequeue" =@ T<Event> ^-> T<unit>
            |> WithSourceName "OnDequeue"
        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.WebCodecs" [
                VideoColorSpace
                VideoColorSpaceInit
                VideoFrameDataInit
                VideoFrameInit
                ColorSpace
                VideoAllocationSizeOptions
                VideoCopyToOptions
                LayoutEntry
                VisibleRect
                ImageTrackList
                ImageTrack
                DecodeOptions
                ImageDecoderInit
                AudioEncoder
                AudioEncoderInit
                AudioEncoderConfig
                AudioDecoder
                EncodedVideoChunk
                EncodedVideoChunkInit
                EncodedAudioChunk
                EncodedAudioChunkInit
                ConfigSupportedResult
                AudioDecoderInit
                AudioDecoderConfig
                VideoEncoder
                VideoEncoderEncodeOptions
                CodecQuantizer
                VideoEncoderConfig
                VideoEncoderInit
                EncodedVideoChunkMetadata
                VideoDecoder
                VideoDecoderInit
                VideoDecoderConfig
                ImageDecoder
                DecodeResult
                VideoFrame
                AudioDataInit
                AllocationSizeOptions
                CopyToOptions
                AudioData
                OpusEncoderConfig
                EncodedAudioChunkMetadata
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
